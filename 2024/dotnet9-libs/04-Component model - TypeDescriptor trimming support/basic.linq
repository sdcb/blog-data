<Query Kind="Program">
  <Namespace>System.ComponentModel</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// dotnet publish -c Release -r win-x64 --self-contained true /p:PublishTrimmed=true

public class Program
{
	public static void Main()
	{
		RunIt();
	}

	public static void RunIt()
	{
		// 在这里，我们定义一个ExampleClass类型。
		// 这个类型将用作TypeDescriptor的示例。

		// （最后说）为了解决上述问题，并确保反射能够访问属性，
		// 我们需要注册这个类型。
		TypeDescriptor.RegisterType<ExampleClass>();

		// 使用typeof获取ExampleClass的Type对象
		// 并传递给另一个方法进行处理。
		Test(typeof(ExampleClass));
	}

	private static void Test(Type type)
	{
		// 在自包含和修剪方案中，
		// 直接使用TypeDescriptor.GetProperties会产生警告。
		// 例如，IL2026和IL2067警告。
		PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type);

		// 在修剪的情况下，这里属性计数将是0，而不是实际的属性数量2。
		Console.WriteLine($"Property count before registration: {properties.Count}");

		// 通过注册后的类型获取属性，
		// 避免警告并确保正确的属性计数。
		properties = TypeDescriptor.GetPropertiesFromRegisteredType(type);

		// 打印注册后的属性数量。
		Console.WriteLine($"Property count after registration: {properties.Count}");
	}
}

public class ExampleClass
{
	public string? Property1 { get; set; }
	public int Property2 { get; set; }
}