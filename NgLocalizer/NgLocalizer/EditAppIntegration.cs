using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using NgLocalizer.ViewModels;

namespace NgLocalizer
{
    internal static class EditAppIntegration
    {
        /// <summary>
        /// Opens the sourcefile in vscode (reuses open instance), and, if available, puts the cursor at the first <see cref="TokenUsage"/>.
        /// </summary>
        public static void RevealInVSCode(TreeItem treeItem)
        {
            if (!GetSourceFile(treeItem, out var path))
                return;

            GetFirstUsageLineInfo(treeItem, out var line, out var @char);

            ExecCommandLine($"code -g \"{path}\":{line}:{@char}"); // https://code.visualstudio.com/docs/editor/command-line
        }

        /// <summary>
        /// Opens the sourcefile in vscode (reuses open instance), and, if available, puts the cursor at the first <see cref="TokenUsage"/>.
        /// </summary>
        public static void RevealSelectedTokenUsageInVSCode(TreeItem treeItem)
        {
            if (!GetSourceFile(treeItem, out var path))
                return;

            GetLineInfo(treeItem.SelectedTokenUsage, out var line, out var @char);

            ExecCommandLine($"code -g \"{path}\":{line}:{@char}"); // https://code.visualstudio.com/docs/editor/command-line
        }

        public static void OpenSourceFileInDefaultEditor(TreeItem treeItem)
        {
            if (!GetSourceFile(treeItem, out var path))
                return;

            Process.Start($"{path}");
        }

        private static bool GetSourceFile(TreeItem treeItem, out string path)
        {
            path = null;

            if (treeItem == null)
            {
                return false;
            }

            if (treeItem.FullFilename == null)
            {
                MessageBox.Show("That item has no Source File associated with it.", "Unknown source file", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            path = treeItem.FullFilename;
            return true;
        }

        private static void GetFirstUsageLineInfo(TreeItem treeItem, out int line, out int @char)
        {
            var firstUsage = treeItem.TokenUsages?.FirstOrDefault();
            GetLineInfo(firstUsage, out line, out @char);
        }

        private static void GetLineInfo(TokenUsage usage, out int line, out int @char)
        {
            if (usage == null)
            {
                line = 1;
                @char = 1;
                return;
            }

            using var fr = File.OpenRead(usage.FullFilename);
            var buffer = new byte[usage.Begin];
            fr.Read(buffer, 0, usage.Begin);
            line = 1; // lines are 1-based.
            @char = 1; // char positions are 1-based.
            for (var i = 0; i < usage.Begin; i++)
            {
                if (buffer[i] == 13) // return, we ignore the linefeed (10)
                {
                    line++;
                    @char = -1; // 2 less than 1, for skipping the Return and Newline chars.
                }

                @char++;
            }
        }

        private static void ExecCommandLine(string commandLine)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C " + commandLine
            };
            process.StartInfo = startInfo;
            process.Start();
        }

        
    }
}
