<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 这里我们定义一个元组数组，包含一些ID和分数。
        (string id, int score)[] data =
        {
            ("0", 42),
            ("1", 5),
            ("2", 4),
            ("1", 10),
            ("0", 25),
        };

        // 使用AggregateBy方法对数据进行聚合。
        // keySelector选择用于分组的键，这里我们通过id进行分组。
        // seed初始化累积值为0。
        // (totalScore, curr) => totalScore + curr.score 是累加函数。
        var aggregatedData =
            data.AggregateBy(
                keySelector: entry => entry.id,
                seed: 0,
                (totalScore, curr) => totalScore + curr.score
            );

        // 输出聚合后的数据。
        // 结果为每个id的总分数。
        foreach (var item in aggregatedData)
		{
			Console.WriteLine(item);
		}
		// 结果将是：
		// (0, 67)
		// (1, 15)
		// (2, 4)
	}
}