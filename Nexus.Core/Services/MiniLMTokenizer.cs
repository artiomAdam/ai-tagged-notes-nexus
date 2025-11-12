using System.Text.RegularExpressions;

namespace Nexus.Core.Services
{
    public class MiniLMTokenizer
    {
        private readonly Dictionary<string, int> _vocab;
        private readonly Regex _splitter = new(@"\w+|[^\w\s]", RegexOptions.Compiled);

        public MiniLMTokenizer(string vocabPath)
        {
            _vocab = File.ReadAllLines(vocabPath)
                         .Select((w, i) => new { w, i })
                         .ToDictionary(x => x.w, x => x.i);
        }

        public int[] Tokenize(string text, int maxLen = 256)
        {
            var tokens = _splitter.Matches(text.ToLower())
                                  .Select(m => m.Value)
                                  .Select(t => _vocab.ContainsKey(t) ? _vocab[t] : _vocab["[UNK]"])
                                  .Take(maxLen)
                                  .ToList();

            tokens.Insert(0, _vocab["[CLS]"]);
            tokens.Add(_vocab["[SEP]"]);

            while (tokens.Count < maxLen) tokens.Add(0);
            return tokens.ToArray();
        }
    }
}