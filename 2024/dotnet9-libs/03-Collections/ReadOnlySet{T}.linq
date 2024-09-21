<Query Kind="Program">
  <Namespace>System.Collections.ObjectModel</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

public class Example
{
	// 1. 定义一个私有的可变HashSet，_set，其内容是可变的。
	private readonly HashSet<int> _set = new() { 1, 2, 3, 4, 5 };

	// 2. 定义了一个ReadOnlySet包装器，_setWrapper，用来将可变的HashSet转换为只读视图。
	private ReadOnlySet<int>? _setWrapper;

	// 3. Set属性返回ReadOnlySet，如果_setWrapper为空，则通过??=初始化它。
	public ReadOnlySet<int> Set => _setWrapper ??= new(_set);

	public void DisplaySet()
	{
		Console.WriteLine("ReadOnlySet 包含以下元素：");

		// 4. DisplaySet方法用于展示ReadOnlySet的内容，利用集合的遍历特性。
		foreach (var item in Set)
		{
			Console.WriteLine(item);
		}
	}

	public static void Main()
	{
		Example example = new();

		// 5. Main方法实例化Example类并调用方法展示集合元素。
		example.DisplaySet();

		// 6. 最后，我们看到了一行被注释掉的代码，强调ReadOnlySet是不可修改的。
		// example.Set.Add(6); // 编译错误：无法对只读集合进行修改
	}
}