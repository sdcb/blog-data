<Query Kind="Statements">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 定义一个只读字符跨度，它是可以在不分配额外内存的情况下处理字符串的结构
ReadOnlySpan<char> input = "Hello, world! How are you?";

// 使用新的 EnumerateSplits 方法拆分输入字符串
// 使用正则表达式模式 "[aeiou]" 代表所有的英文元音字母
// 和传统的 String.Split 或 Regex.Split 不同，它不需要创建字符串数组

foreach (Range r in Regex.EnumerateSplits(input, @"\b"))
{
	// 输出拆分后的每段字符串
	// input[r] 利用 Range 对象获取结果的每个部分
	Console.WriteLine($"Split: \"{input[r]}\"");
}

// 解释输出结果：
// Split: ""
// Split: "Hello"
// Split: ", "
// Split: "world"
// Split: "! "
// Split: "How"
// Split: " "
// Split: "are"
// Split: " "
// Split: "you"
// Split: "?"

// EnumerateSplits 减少了内存分配，这在处理大字符串时极有帮助
// 可以更高效地进行字符串拆分和处理