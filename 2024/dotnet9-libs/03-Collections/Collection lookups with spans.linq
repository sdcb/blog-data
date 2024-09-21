<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 用于演示的输入文本
        ReadOnlySpan<char> input = "Hello world! Hello .NET 9";

        // 计算单词出现次数
        Dictionary<string, int> wordCounts = CountWords(input);

        // 输出单词计数
        foreach (var wordCount in wordCounts)
        {
            Console.WriteLine($"{wordCount.Key}: {wordCount.Value}");
        }
    }

    // 使用 Dictionary<TKey, TValue>.GetAlternateLookup() 来实现对 Span 的查找
    private static Dictionary<string, int> CountWords(ReadOnlySpan<char> input)
    {
        // 创建一个不区分大小写的字典用于存储单词计数
        Dictionary<string, int> wordCounts = new(StringComparer.OrdinalIgnoreCase);

        // 获取专用于 Span 的查找功能
        Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> spanLookup = wordCounts.GetAlternateLookup<ReadOnlySpan<char>>();

        // 正则表达式用于拆分单词
        foreach (ValueMatch wordRange in Regex.EnumerateMatches(input, @"\b\w+\b"))
        {
            // 获取当前单词
            ReadOnlySpan<char> word = input[wordRange.Index..(wordRange.Index + wordRange.Length)];

			// 使用 Span 查找，更新计数
			spanLookup[word] = spanLookup.TryGetValue(word, out int count) ? count + 1 : 1;
		}

		// 返回结果字典
		return wordCounts;
	}
}