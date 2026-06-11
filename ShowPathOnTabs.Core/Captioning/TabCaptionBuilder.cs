using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShowPathOnTabs.Core.Captioning
{
    public static class TabCaptionBuilder
    {
        /// <summary>
        /// Builds a new caption for a given tab that includes the minimum required prefix for it to be unique.
        /// </summary>
        /// <param name="tabPath"></param>
        /// <param name="openTabPaths"></param>
        /// <returns></returns>
        public static string Build(
            string tabPath,
            IReadOnlyList<string> openTabPaths)
        {
            string fileName = Path.GetFileName(tabPath);

            // Find paths to other files with the same file name as tabPath
            var siblingTabPaths = openTabPaths
                .Where(path => !path.Equals(tabPath, StringComparison.OrdinalIgnoreCase)
                         && Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (siblingTabPaths.Count == 0)
                return fileName;

            return MakeUnique(tabPath, siblingTabPaths);
        }

        private static string MakeUnique(
            string tabPath,
            IReadOnlyList<string> siblngPaths)
        {
            // Separate paths into lists of folders
            var directorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var separatedTabPath = tabPath.Split(directorySeparators);
            var separatedSiblingPaths = siblngPaths.Select(path => path.Split(directorySeparators)).ToList();

            string joinSeparator = Path.DirectorySeparatorChar.ToString(); // Has to be a string in .net Framework

            // Adds one parent folder at a time until the string caption is unique
            for (int depth = 0; depth <= separatedTabPath.Length; depth++)
            {
                // Build a candidate string for the current depth
                int skipCount = separatedTabPath.Length - (depth + 1);
                string candidate = string.Join(joinSeparator, separatedTabPath.Skip(skipCount));

                // Check if the candidate string is unique amongst all siblings
                bool isUnique = separatedSiblingPaths.All(siblingPath =>
                {
                    if (siblingPath.Length < depth + 1) return true;

                    int siblingSkipCount = siblingPath.Length - (depth + 1);
                    string siblingCandidate = string.Join(joinSeparator, siblingPath.Skip(siblingSkipCount));

                    return !candidate.Equals(siblingCandidate, StringComparison.OrdinalIgnoreCase);
                });

                if (isUnique)
                    return candidate;
            }

            return tabPath;
        }
    }
}
