<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 假设output.txt中有多行文本，我们将读取这些行
        IEnumerable<string> lines = File.ReadAllLines(Util.CurrentQueryPath);

        // 使用新的Index<TSource>方法为集合中的每个元素自动生成索引
        // Index()方法返回一个包含索引和值的元组
        foreach ((int index, string line) in lines.Index())
        {
            // 输出每行的行号和内容，行号加1是因为索引从0开始
            Console.WriteLine($"Line number: {index + 1}, Line: {line}");
        }
	}
}