using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NgLocalizer.ViewModels;

namespace NgLocalizer
{
    public static class SourcePreviewLoader
    {
        /// <summary>
        /// Loads the code preview around tokenusages of a treeItem into the CodeView properties of that treeItem.
        /// </summary>
        public static async Task LoadSourceCodePreviewAsync(TreeItem treeItem)
        {

            if (treeItem is not { IsTextItem: true })
                return;

            await using var stream = File.OpenRead(treeItem.FullFilename);

            foreach (var tokenUsage in treeItem.TokenUsages)
            {
                await LoadTokenUsageCodePreview(tokenUsage, stream);
            }

        }

        private static async Task LoadTokenUsageCodePreview(TokenUsage tokenUsage, FileStream stream)
        {
            try
            {
                var buffer = new byte[300];
                var keyPosition = tokenUsage.Begin;

                // get part before KEY

                var length = 300;
                if (keyPosition < length)
                    length = keyPosition;
                stream.Seek(keyPosition - length, SeekOrigin.Begin);
                await stream.ReadAsync(buffer, 0, length);
                tokenUsage.CodeViewBeforeKey = Encoding.Default.GetString(buffer, 0, length);
                tokenUsage.CodeViewBeforeKey = RemoveFirstIncompleteLine(tokenUsage.CodeViewBeforeKey);

                // get KEY
                await stream.ReadAsync(buffer, 0, tokenUsage.Length);
                tokenUsage.CodeViewKey = Encoding.Default.GetString(buffer, 0, tokenUsage.Length);

                // get part after KEY
                length = 300;
                if (stream.Position + length > stream.Length)
                    length = (int)stream.Length - (int)stream.Position;
                await stream.ReadAsync(buffer, 0, length);
                tokenUsage.CodeViewAfterKey = Encoding.Default.GetString(buffer, 0, length);
                tokenUsage.CodeViewAfterKey = RemoveLastIncompleteLine(tokenUsage.CodeViewAfterKey);
            }
            catch (Exception e)
            {
                tokenUsage.CodeViewBeforeKey = $"{e.GetType()}: {e.Message}";
                // lets not kill the app for not finding a source preview
            }
        }

        private static string RemoveLastIncompleteLine(string text)
        {
            var lastNewline = text.LastIndexOf('\r');
            if (lastNewline > 1)
                text = text[..lastNewline];
            return text;
        }

        private static string RemoveFirstIncompleteLine(string text)
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline < text.Length - 1)
                text = text[(firstNewline + 1)..];
            return text;
        }
    }
}
