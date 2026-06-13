using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShowPathOnTabs.Core.Captioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShowPathOnTabs.Services;

/// <summary>
/// Re-writes tab captions when any tabs are opened or closed
/// </summary>
internal sealed class TabCaptionService(AsyncPackage package) : IVsRunningDocTableEvents, IDisposable
{
    private readonly AsyncPackage _package = package ?? throw new ArgumentNullException(nameof(package));
    private IVsRunningDocumentTable? _rdt;
    private IVsUIShell? _uiShell;
    private uint _rdtCookie;
    private bool _disposed;

    // Called once from the package on startup. Grabs VS services,
    // subscribes to document events, and captions any tabs already open.
    public async Task InitializeAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        _rdt = await _package.GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        _uiShell = await _package.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;

        if (_rdt == null) return;

        _rdt.AdviseRunningDocTableEvents(this, out _rdtCookie);

        RefreshAllCaptions();
    }

    // --- RDT event hooks: only these three actually trigger a refresh ---
    // The rest are required by the interface but unused.

    public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
    public int OnAfterSave(uint docCookie) => VSConstants.S_OK;
    public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

    public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        RefreshAllCaptions(); // a tab is closing — recheck everything
        return VSConstants.S_OK;
    }

    public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (fFirstShow != 0) RefreshAllCaptions(); // a new tab just opened
        return VSConstants.S_OK;
    }

    public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        RefreshAllCaptions(); // a tab was hidden/closed
        return VSConstants.S_OK;
    }

    // Recomputes and applies a caption for every currently open tab.
    private void RefreshAllCaptions()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_rdt == null) return;

        try
        {
            var openDocs = GetOpenDocuments();
            if (openDocs.Count == 0) return;

            var allOpenPaths = openDocs.Select(d => d.FilePath).ToList();

            foreach (var (filePath, frame) in openDocs)
            {
                string caption = TabCaptionBuilder.Build(filePath, allOpenPaths);
                frame.SetProperty((int)__VSFPROPID.VSFPROPID_OwnerCaption, caption);
            }
        }
        catch (Exception ex)
        {
            // Swallow errors here — an exception escaping an RDT callback
            // can disrupt the IDE's event dispatch.
            System.Diagnostics.Debug.WriteLine($"Error in RefreshAllCaptions: {ex.Message}");
        }
    }

    // Walks every open document window frame and returns its file path + frame.
    private List<(string FilePath, IVsWindowFrame Frame)> GetOpenDocuments()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var result = new List<(string FilePath, IVsWindowFrame Frame)>();

        if (_rdt == null || _uiShell == null) return result;

        try
        {
            _uiShell.GetDocumentWindowEnum(out IEnumWindowFrames pEnum);
            if (pEnum == null) return result;

            IVsWindowFrame[] frames = new IVsWindowFrame[1];
            while (pEnum.Next(1, frames, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                var frame = frames[0];
                if (frame == null) continue;

                frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object pathObj);

                if (pathObj is string filePath && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    result.Add((filePath, frame));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetOpenDocuments: {ex.Message}");
        }

        return result;
    }

    // Unsubscribes from RDT events so VS doesn't call back into a dead object.
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ThreadHelper.ThrowIfNotOnUIThread();
        if (_rdt != null && _rdtCookie != 0)
        {
            _rdt.UnadviseRunningDocTableEvents(_rdtCookie);
            _rdtCookie = 0;
        }
    }
}