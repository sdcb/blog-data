<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		string sourceText = """
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            Sed non risus. Suspendisse lectus tortor, dignissim sit amet, 
            adipiscing nec, ultricies sed, dolor. Cras elementum ultrices amet diam.
        """;

		// 我们要找出文本中出现频率最高的单词。

		KeyValuePair<string, int> mostFrequentWord = sourceText
			// 先根据空格、句号和逗号拆分字符串，移除空字符串。
			.Split(new char[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)
			// 转换所有单词为小写，以避免大小写影响。
			.Select(word => word.ToLowerInvariant())
			// 使用 CountBy 来统计每个单词出现的次数。
			.CountBy(word => word)
			// 找出出现次数最多的单词。
			.MaxBy(pair => pair.Value);

		// 输出频率最高的单词。
		Console.WriteLine(mostFrequentWord.Key); // amet
	}
}