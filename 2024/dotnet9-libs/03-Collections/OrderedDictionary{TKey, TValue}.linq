<Query Kind="Program">
  <Namespace>System.Collections.Specialized</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 创建一个新的有序字典，键为string，值为int
        // 这个字典会按照插入顺序维护键值对，并提供快速的键查找
        OrderedDictionary<string, int> d = new()
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        // 添加一个新的键值对 ("d", 4)
        d.Add("d", 4);

        // 移除第一个元素，此时移除的是 ("a", 1)
        d.RemoveAt(0);

        // 移除第三个元素，此时移除的是 ("d", 4)
        d.RemoveAt(2);

        // 在第一个位置插入一个新的键值对 ("e", 5)
        d.Insert(0, "e", 5);

        // 输出剩余的键值对
        foreach (KeyValuePair<string, int> entry in d)
        {
            // 依次输出 [e, 5], [b, 2], [c, 3]
            Console.WriteLine(entry);
        }

		// Output:
		// [e, 5]
		// [b, 2]
		// [c, 3]

		// 非泛型版本的OrderedDictionary示例（.NET Framework 2.0发布）
		OrderedDictionary oldOrderedDict = new OrderedDictionary();

		// 添加一些初始化键值对
		oldOrderedDict["a"] = 1;
		oldOrderedDict["b"] = 2;
		oldOrderedDict["c"] = 3;

		// 添加新的键值对
		oldOrderedDict.Add("d", 4);

		// 移除第一个元素 ("a", 1)
		oldOrderedDict.RemoveAt(0);

		// 在第一个位置插入一个新的键值对
		oldOrderedDict.Insert(0, "e", 5);

		// 非泛型版本的遍历输出
		foreach (DictionaryEntry entry in oldOrderedDict)
		{
			// 输出包含键值对的格式
			Console.WriteLine($"[{entry.Key}, {entry.Value}]");
		}

		// Output:
		// [e, 5]
		// [b, 2]
		// [c, 3]
		// [d, 4]
	}
}