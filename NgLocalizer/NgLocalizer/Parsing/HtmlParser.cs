using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NgLocalizer.ViewModels;

namespace NgLocalizer.Parsing
{
    internal class HtmlParser : IFileParser
    {
        private readonly Regex _translationRegex = new Regex("(?<code>[^{\"]*)\\|\\s*translate");
        private readonly Regex _keyInTranslationRegex = new Regex("'(?<key>.*?)'");

        public string FileMask => "*.html";

        public IEnumerable<TokenUsage> ParseFile(string file)
        {
            // note that we use the indexes of matches later on in the tool, so the indexes need to match with the original file.
            foreach (Match match in _translationRegex.Matches(File.ReadAllText(file).Replace("\r", " ")))
            {
                foreach (var keyMatch in _keyInTranslationRegex.Matches(match.Groups["code"].Value).Cast<Match>())
                {
                    yield return new TokenUsage
                    {
                        Token = keyMatch.Groups["key"].Value,
                        Begin = match.Groups["code"].Index + keyMatch.Groups["key"].Index,
                        Length = keyMatch.Groups["key"].Length,
                        FullFilename = file
                    };
                }
            }
        }
    }
}
