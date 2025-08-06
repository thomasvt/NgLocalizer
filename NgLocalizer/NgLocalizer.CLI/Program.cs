using CommandLine;

namespace NgLocalizer.CLI
{
    public class Options
    {
        [Option('s', "src", Required = true, HelpText = "Path to your Angular 'src' folder.")]
        public string Src { get; set; }

        [Option('o', "output", Required = true, HelpText = "JSON output file.")]
        public string Output { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    var extractor = new TokenUsageExtractor(opts.Src);
                    var tokens = extractor.Extract().Select(t => t.Token).Distinct().ToList();
                    tokens.Sort();
                    File.WriteAllLines(opts.Output, tokens);
                })
                .WithNotParsed(errors =>
                {
                });
        }
    }
}
