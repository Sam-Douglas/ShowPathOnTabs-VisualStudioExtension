using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShowPathOnTabs.Core.Captioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShowPathOnTabs.Services;

/// <summary>
/// Updates tab captions to show the path where there is ambiguity
/// </summary>
/// <param name="package"></param>
internal sealed class TabCaptionService(AsyncPackage package) : IVsRunningDocTableEvents, IDisposable
{
    private readonly AsyncPackage _package = package ?? throw new ArgumentNullException(nameof(package));
    private IVsRunningDocumentTable? _runningDocTable;
    private IVsUIShell? _uiShell;
    private uint _rdtCookie;
    private bool _disposed;

    public async Task InitializeAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        _runningDocTable = await _package.GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        _uiShell = await _package.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;

        if (_runningDocTable == null) return;

        _runningDocTable.AdviseRunningDocTableEvents(this, out _rdtCookie);

        UpdateTabCaptions();
    }

    public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

    public int OnAfterSave(uint docCookie) => VSConstants.S_OK;

    public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

    /// <summary>
    /// Tab is about to be fully closed
    /// </summary>
    public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        UpdateTabCaptions();
        return VSConstants.S_OK;
    }

    /// <summary>
    /// A tab is about to be in focus.
    /// FirstShow is non-zero the first time a tab is opened.
    /// </summary>
    public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (fFirstShow != 0)
            UpdateTabCaptions();
        return VSConstants.S_OK;
    }

    /// <summary>
    /// Tab is closed or hidden
    /// </summary>
    public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        UpdateTabCaptions();
        return VSConstants.S_OK;
    }

    private void UpdateTabCaptions()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_runningDocTable == null) return;

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
            Debug.WriteLine($"Error in RefreshAllCaptions: {ex.Message}");
        }
    }

    private List<(string FilePath, IVsWindowFrame Frame)> GetOpenDocuments()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var result = new List<(string FilePath, IVsWindowFrame Frame)>();

        if (_runningDocTable == null || _uiShell == null) return result;

        try
        {
            // Enumerator for every tab
            _uiShell.GetDocumentWindowEnum(out IEnumWindowFrames frameEnumerator);
            if (frameEnumerator == null) return result;

            IVsWindowFrame[] frameBuffer = new IVsWindowFrame[1];
            while (frameEnumerator.Next(1, frameBuffer, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                var frame = frameBuffer[0];
                if (frame == null) continue;

                // Get tab path
                frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object monikerValue);

                // Only keep file based tabs
                if (monikerValue is string filePath && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    result.Add((filePath, frame));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetOpenDocuments: {ex.Message}");
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ThreadHelper.ThrowIfNotOnUIThread();
        if (_runningDocTable != null && _rdtCookie != 0)
        {
            _runningDocTable.UnadviseRunningDocTableEvents(_rdtCookie);
            _rdtCookie = 0;
        }
    }
}