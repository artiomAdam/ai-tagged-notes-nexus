

using System.Text.RegularExpressions;
namespace Nexus.Core.Services
{
    


        public class E5Tokenizer
        {
            private readonly Dictionary<string, int> _vocab;
            private readonly Regex _basicTokenizer = new(@"[\w']+|[^\w\s]", RegexOptions.Compiled);
            private readonly int _unkId;
            private readonly int _clsId;
            private readonly int _sepId;
            private readonly int _padId;

            public E5Tokenizer(string vocabPath)
            {
                var lines = File.ReadAllLines(vocabPath);
                _vocab = lines.Select((w, i) => new { w, i }).ToDictionary(x => x.w, x => x.i);

                _unkId = _vocab["[UNK]"];
                _clsId = _vocab["[CLS]"];
                _sepId = _vocab["[SEP]"];
                _padId = _vocab["[PAD]"];
            }

            public int[] Tokenize(string text, int maxLen = 512)
            {
                text = text.ToLower().Trim();

                var tokens = new List<int> { _clsId };

                foreach (Match match in _basicTokenizer.Matches(text))
                {
                    string word = match.Value;
                    tokens.AddRange(WordPieceTokenize(word));
                }

                tokens.Add(_sepId);

                if (tokens.Count > maxLen)
                    tokens = tokens.Take(maxLen).ToList();

                while (tokens.Count < maxLen)
                    tokens.Add(_padId);

                return tokens.ToArray();
            }

            private IEnumerable<int> WordPieceTokenize(string word)
            {
                if (_vocab.ContainsKey(word))
                    return new[] { _vocab[word] };

                var tokens = new List<int>();
                int start = 0;
                while (start < word.Length)
                {
                    int end = word.Length;
                    string curSubstr = "";
                    while (start < end)
                    {
                        string substr = word.Substring(start, end - start);
                        if (start > 0)
                            substr = "##" + substr;

                        if (_vocab.ContainsKey(substr))
                        {
                            curSubstr = substr;
                            break;
                        }
                        end--;
                    }

                    if (curSubstr == "")
                        return new[] { _unkId };

                    tokens.Add(_vocab[curSubstr]);
                    start = end;
                }
                return tokens;
            }
        }
    
}
