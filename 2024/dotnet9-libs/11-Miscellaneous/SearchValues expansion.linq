<Query Kind="Program">
  <Namespace>System.Buffers</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	// 声明一个静态的 SearchValues 对象 s_animals，用于在字符串中查找指定的动物名称。
	// 使用 StringComparison.OrdinalIgnoreCase 来忽略大小写。
	private static readonly SearchValues<string> s_animals =
		SearchValues.Create(new[] { "cat", "mouse", "dog", "dolphin" }, StringComparison.OrdinalIgnoreCase);

	static void Main()
	{
		// 定义一个字符串，其中包含多个单词，包括我们要查找的动物名称。
		string text = "There is a cat and a dog in the backyard.";

		// 调用 IndexOfAnimal 方法，找出第一个出现的动物名称的索引。
		int index = IndexOfAnimal(text);

		// 输出结果的索引，如果找不到则返回 -1。
		Console.WriteLine(index >= 0 ? $"Animal found at index {index}." : "No animal found.");
	}

	// 定义一个方法，通过利用 SearchValues 来查找第一个出现的动物的索引。
	public static int IndexOfAnimal(string text) =>
		// 使用 AsSpan 将字符串转换为 Span，以便进行高效的索引操作。
		text.AsSpan().IndexOfAny(s_animals);
}