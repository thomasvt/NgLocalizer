using NgLocalizer.Parsing;
using NgLocalizer.ViewModels;

namespace NgLocalizer.CLI
{
    public class TokenUsageExtractor(string sourceFolder)
    {
        private readonly IFileParser[] _fileParsers = { new HtmlParser(), new TypeScriptParser() };

        public List<TokenUsage> Extract()
        {
            return ProcessFolder(sourceFolder);
        }

        private List<TokenUsage> ProcessFolder(string folder)
        {
            var tokenUsages = ParseAllFilesInFolder(folder);

            foreach (var directory in Directory.GetDirectories(folder))
                ProcessSubFolder(directory, tokenUsages);

            return tokenUsages;
        }

        private List<TokenUsage> ParseAllFilesInFolder(string folder)
        {
            var list = new List<TokenUsage>();

            foreach (var parser in _fileParsers)
            {
                foreach (var file in Directory.GetFiles(folder, parser.FileMask))
                {
                    list.AddRange(GetTokenUsages(file, parser));
                }
            }

            return list;
        }

        private static IEnumerable<TokenUsage> GetTokenUsages(string file, IFileParser parser)
        {
            return parser.ParseFile(file);
        }

        private void ProcessSubFolder(string directory, List<TokenUsage> list)
        {
            list.AddRange(ProcessFolder(directory));
        }
    }
}
