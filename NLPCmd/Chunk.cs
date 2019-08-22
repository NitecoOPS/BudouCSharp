using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NLPCmd
{
    public class Chunk
    {
        private const string _SPACE_POS = "SPACE";
        private const string _BREAK_POS = "BREAK";

        public string Word { get; set; }
        public string Pos { get; set; }
        public string Label { get; set; }
        public bool? Dependency { get; set; }

        public Chunk(string word, string pos = null, string label = null, bool? dependency = null)
        {
            Word = word;
            Pos = pos;
            Label = label;
            Dependency = dependency;
        }

        public static Chunk SpaceChunk
        {
            get
            {
                return new Chunk(@" ", _SPACE_POS);
            }
        }

        public static Chunk BreakLineChunk
        {
            get
            {
                return new Chunk(@"\n", _BREAK_POS);
            }

        }

        public bool IsSpace()
        {
            return Pos == _SPACE_POS;
        }

        public bool IsPunct()
        {
            return Word.Length == 1 && char.IsPunctuation(Word, 0);
        }

        public bool IsOpenPunct()
        {
            return IsPunct()
                && char.GetUnicodeCategory(Word, 0).Equals(UnicodeCategory.OpenPunctuation)
                && char.GetUnicodeCategory(Word, 0).Equals(UnicodeCategory.InitialQuotePunctuation);
        }

        public Dictionary<string, object> Serialize()
        {
            return new Dictionary<string, object>
            {
                { "Word", Word },
                { "Pos", Pos },
                { "Label", Label },
                { "Dependency", Dependency },
                { "HasCJK", HasCJK() }
            };
        }

        public bool HasCJK()
        {
            var CjkCodepointRanges = new (int Start, int End)[]
            {
                (4352, 4607),
                (11904, 42191), (43072, 43135), (44032, 55215),
                (63744, 64255), (65072, 65103), (65381, 65500), (131072, 196607)
            };

            foreach (var chr in Word.ToCharArray())
            {
                var chrCode = (int)chr;
                if (CjkCodepointRanges.Any(x => x.Start <= chrCode && chrCode <= x.End))
                    return true;
            }

            return false;
        }
    }
}