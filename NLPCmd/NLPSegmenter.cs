using Google.Apis.Auth.OAuth2;
using Google.Cloud.Language.V1;
using Google.Protobuf.Collections;
using Grpc.Auth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NLPCmd
{
    public static class NLPSegmentManager
    {
        public static string DEFAULT_CLASS_NAME = "ww";

        public static INLPSegmenter CreateSegmenter(string authenJsonFile)
        {
            return new GoolgeNLPSegmenter(authenJsonFile);
        }

        private static Dictionary<string, string> ParseAttributes(Dictionary<string, string> attributes, string className)
        {
            if (attributes == null)
                attributes = new Dictionary<string, string>();

            attributes.Add("class", DEFAULT_CLASS_NAME);

            if (!string.IsNullOrEmpty(className))
                attributes["class"] = className;

            return attributes;
        }

        public static Dictionary<string, object> Parse(this INLPSegmenter nLPSegmenter
            , string text
            , string language
            , bool useEntity = false
            , string className = null
            , int maxLength = int.MaxValue
            , Dictionary<string, string> attributes = null)
        {
            // TODO: Implement Cache + DB here
            attributes = ParseAttributes(attributes, className);
            var htmlCode = nLPSegmenter.Segment(text, language, useEntity).HtmlSerialize(attributes, maxLength);
            return new Dictionary<string, object>
            {
                { "Chunks", nLPSegmenter.Chunks },
                { "HtmlCode", htmlCode }
            };
        }

        public static string HtmlSerialize(this INLPSegmenter nLPSegmenter, Dictionary<string, string> attributes, int maxLength)
        {
            return nLPSegmenter.Chunks.HtmlSerialize(attributes, maxLength);
        }
    }

    public interface INLPSegmenter
    {
        ChunkList Chunks { get; set; }
        INLPSegmenter Segment(string text, string language, bool useEntity);
    }

    public class GoolgeNLPSegmenter : INLPSegmenter
    {
        private List<string> SupportedLanguages = new List<string>
        {
            "ja", "ko", "zh", "zh-TW", "zh-CN", "zh-HK", "zh-Hant"
        };

        private List<string> DependentLabel = new List<string>
        {
            "P", "SNUM", "PRT", "AUX", "SUFF", "AUXPASS", "RDROP", "NUMBER", "NUM", "PREF"
        };

        public virtual ChunkList Chunks { get; set; }
        public string CredentialPath { get; private set; }
        public LanguageServiceClient LanguageService { get; private set; }

        public GoolgeNLPSegmenter(string credentialPath)
        {
            CredentialPath = credentialPath;
            Authenticate();
        }

        private void Authenticate()
        {
            var credential = GoogleCredential.FromFile(CredentialPath)
                                .CreateScoped(LanguageServiceClient.DefaultScopes);
            var channel = new Grpc.Core.Channel(
                LanguageServiceClient.DefaultEndpoint.ToString()
                , credential.ToChannelCredentials());
            LanguageService = LanguageServiceClient.Create(channel);
        }

        private ChunkList GetSourceChunks(string text, string language)
        {
            var chunks = new ChunkList();
            var seek = 0;
            var tokens = GetAnnotations(text, language);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var word = token.Text.Content;
                var beginOffset = token.Text.BeginOffset;
                var label = token.DependencyEdge.Label;
                var pos = token.PartOfSpeech.Tag;
                if (beginOffset > seek)
                {
                    chunks.Add(Chunk.SpaceChunk);
                    seek = beginOffset;
                }

                var chunk = new Chunk(word, pos.ToString(), label.ToString());
                if (DependentLabel.Any(x => x == chunk.Label.ToUpper()))
                    chunk.Dependency = i < token.DependencyEdge.HeadTokenIndex;
                if (chunk.IsPunct())
                    chunk.Dependency = chunk.IsOpenPunct();
                chunks.Add(chunk);
                seek += word.Length;
            }

            return chunks;
        }

        private RepeatedField<Token> GetAnnotations(string text, string language)
        {
            var document = new Document
            {
                Content = text,
                Type = Document.Types.Type.PlainText
            };

            if (!string.IsNullOrEmpty(language))
                document.Language = language;

            var response = LanguageService.AnnotateText(document, new AnnotateTextRequest.Types.Features()
            {
                ExtractSyntax = true
            }, EncodingType.Utf32);

            return response.Tokens;
        }

        private List<object> GetEntities(string text, string language)
        {
            var document = new Document
            {
                Content = text,
                Type = Document.Types.Type.PlainText
            };

            if (!string.IsNullOrEmpty(language))
                document.Language = language;

            var response = LanguageService.AnalyzeEntities(document, EncodingType.Utf32);
            var result = new List<dynamic>();

            foreach (var entity in response.Entities)
            {
                var mentions = entity.Mentions;
                if (mentions == null) continue;

                var entityText = mentions[0].Text;
                var offset = entityText.BeginOffset;

                foreach (var word in entityText.Content.Split(' '))
                {
                    result.Add(new
                    {
                        Content = word,
                        BeginOffset = offset
                    });
                    offset += word.Length;
                }
            }

            return result;
        }

        private ChunkList GroupChunksByEntities(ChunkList chunks, List<dynamic> entities)
        {
            foreach (var entity in entities)
            {
                ChunkList chunks2concat = chunks.GetOverlaps(entity.BeginOffset, entity.Content.Length);
                if (chunks2concat == null || chunks2concat.Count <= 0) continue;
                var newChunkWord = string.Join(@"", chunks2concat.Select(x => x.Word));
                var newChunk = new Chunk(newChunkWord);
                chunks.Swap(chunks2concat, newChunk);
            }

            return chunks;
        }

        public INLPSegmenter Segment(string text, string language, bool useEntities = false)
        {
            if (!SupportedLanguages.Any(x => x == language))
                throw new NotSupportedException($"{language} is not supported!");

            var sourceChunks = GetSourceChunks(text, language);
            var chunks = sourceChunks;
            if (useEntities)
            {
                var entities = GetEntities(text, language);
                chunks = GroupChunksByEntities(sourceChunks, entities);
            }
            chunks.ResolveDependencies();

            this.Chunks = chunks;
            return this;
        }
    }
}
