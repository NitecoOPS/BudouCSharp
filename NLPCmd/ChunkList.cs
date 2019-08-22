using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace NLPCmd
{
    public class ChunkList : List<Chunk>
    {
        public ChunkList GetOverlaps(int offset, int length)
        {
            if (string.Join(@"", this.Select(x => x.Word))[offset] == ' ')
                offset += 1;// this.Where(x => x.Word.Length > offset).Count(x => string.Concat("", x.Word)[offset] == ' ');

            var index = 0;
            var result = new ChunkList();
            foreach (var chunk in this)
            {
                if ((offset < index + chunk.Word.Length) && index < offset + length)
                    result.Add(chunk);
                index += chunk.Word.Length;
            }

            return result;
        }

        public void Swap(ChunkList oldChunks, Chunk newChunk)
        {
            var indexes = oldChunks.Select(x => IndexOf(x)).ToArray();
            foreach (var i in indexes)
            {
                RemoveAt(i);
            }
            RemoveAt(indexes.Last() + 1);

            Insert(indexes[0], newChunk);
        }

        public void ResolveDependencies()
        {
            ConcatenateInner(true);
            ConcatenateInner(false);
            InsertBreakLines();
        }

        private void InsertBreakLines()
        {
            var targetChunks = new ChunkList();
            foreach (var chunk in this)
            {
                if (chunk.Word.Last() == ' ' && chunk.HasCJK())
                {
                    chunk.Word = new string(chunk.Word.Reverse().ToArray());
                    targetChunks.Add(chunk);
                    targetChunks.Add(Chunk.BreakLineChunk);
                    continue;
                }

                targetChunks.Add(chunk);
            }

            Clear();
            AddRange(targetChunks);
        }

        private void ConcatenateInner(bool direction)
        {
            var tmpBuckets = new ChunkList();
            var sourceChunks = direction ? this : this.Concat(new ChunkList()).Reverse();
            var targetChunks = new ChunkList();
            foreach (var chunk in sourceChunks)
            {
                if (chunk.Dependency == direction
                    || (direction == false && chunk.IsSpace()))
                {
                    tmpBuckets.Add(chunk);
                    continue;
                }

                tmpBuckets.Add(chunk);

                if (!direction)
                    tmpBuckets.Reverse();

                var newWord = string.Join("", tmpBuckets.Select(x => x.Word));
                var newChunk = new Chunk(newWord, chunk.Pos, chunk.Label, chunk.Dependency);

                targetChunks.Add(newChunk);
                tmpBuckets = new ChunkList();
            }

            if (tmpBuckets != null && tmpBuckets.Count > 0)
                targetChunks.AddRange(tmpBuckets);

            if (!direction)
                targetChunks.Reverse();

            Clear();
            AddRange(targetChunks);
        }

        public string HtmlSerialize(Dictionary<string, string> attributes, int maxLength)
        {
            var doc = HtmlNode.CreateNode("<span></span>");
            foreach (var chunk in this)
            {
                if (chunk.HasCJK()
                    && !(maxLength > 0 && chunk.Word.Length > maxLength))
                {
                    var ele = HtmlNode.CreateNode("<span></span>");
                    ele.InnerHtml = chunk.Word;
                    if (attributes != null)
                        foreach (var attr in attributes)
                        {
                            ele.SetAttributeValue(attr.Key, attr.Value);
                        }

                    doc.AppendChild(ele);
                    continue;
                }

                doc.AppendChild(CreateTextNode(chunk.Word));
            }

            return doc.OuterHtml;
        }

        /// <summary>
        /// CreateTextNode by html-agility-pack
        /// </summary>
        /// <param name="word"></param>
        /// <see cref="https://html-agility-pack.net/knowledge-base/20882626/htmlagilitypack--create-html-text-node"/>
        private HtmlNode CreateTextNode(string word)
        {
            var doc = new HtmlDocument();
            var textNode = doc.CreateElement("title");
            textNode.InnerHtml = HtmlDocument.HtmlEncode(word);
            return textNode;
        }
    }
}