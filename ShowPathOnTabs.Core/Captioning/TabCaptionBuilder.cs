using System;
using System.Collections.Generic;
using System.Text;

namespace ShowPathOnTabs.Core.Captioning
{
    public static class TabCaptionBuilder
    {
        public static string Build(
            string tabPath,
            IReadOnlyList<string> openTabPaths)
        {
            return tabPath;
        }

        private static string AddPathToCaption(
            string tabPath,
            IReadOnlyList<string> otherOpenTabPaths)
        {
            return tabPath;
        }
    }
}
