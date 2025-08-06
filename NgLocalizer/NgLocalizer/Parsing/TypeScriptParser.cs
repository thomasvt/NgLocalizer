using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NgLocalizer.ViewModels;

namespace NgLocalizer.Parsing
{
    public class TypeScriptParser : IFileParser
    {
        private readonly Regex _translationRegex = new ("translateService\\.(instant|get)\\('(?<key>.*?)'.*?\\)");

        public string FileMask => "*.ts";

        public IEnumerable<TokenUsage> ParseFile(string file)
        {
                // note that we use the indexes of matches later on in the tool, so the indexes need to match with the original file.
                foreach (Match match in _translationRegex.Matches(File.ReadAllText(file).Replace("\r", " ")))
                {
                    yield return new TokenUsage
                    {
                        Token = match.Groups["key"].Value,
                        Begin = match.Groups["key"].Index,
                        Length = match.Groups["key"].Length,
                        FullFilename = file
                    };
                }
        }
    }
}
