using System;

namespace NLPCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            // The text to analyze.
            string text =
                @"六本木ヒルズでご飯を食べます。";
            //@"渋谷のカレーを食べに行く。";
            var jsonFilePath = @"E:\Users\ha.bui\source\repos\ConsoleApp1\ConsoleApp1\authen.json";
            var result = NLPSegmentManager.CreateSegmenter(jsonFilePath)
                .Parse(text, "ja");
            var resultWithEntity = NLPSegmentManager.CreateSegmenter(jsonFilePath)
                .Parse(text, "ja", true);

            Console.WriteLine($"Original: {text}");
            Console.WriteLine($"Output without entity: {result["HtmlCode"]}");
            Console.WriteLine($"Output with entity: {resultWithEntity["HtmlCode"]}");

            Console.ReadLine();
        }
    }
}
