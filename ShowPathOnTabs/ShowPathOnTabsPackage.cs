using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop; // Required for UIContextGuids80
using ShowPathOnTabs.Services;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ShowPathOnTabs
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // Tells VS to auto-load this package in the background as soon as a solution opens.
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class ShowPathOnTabsPackage : AsyncPackage
    {
        public const string PackageGuidString = "0075df24-a377-4274-a304-a8501ad061e8";

        // Hold a reference so the service isn't garbage collected.
        // Nullable because it's only assigned once InitializeAsync runs.
        private TabCaptionService? _tabCaptionService;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _tabCaptionService = new TabCaptionService(this);
            await _tabCaptionService.InitializeAsync();
        }

        // Unhook RDT events when the package unloads
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tabCaptionService?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}