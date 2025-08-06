using System.Collections.Generic;
using NgLocalizer.ViewModels;

namespace NgLocalizer.Parsing
{
    public interface IFileParser
    {
        string FileMask { get; }
        IEnumerable<TokenUsage> ParseFile(string file);
    }
}
