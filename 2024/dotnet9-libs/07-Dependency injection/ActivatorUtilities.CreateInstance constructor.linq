<Query Kind="Statements">
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 创建一个新的服务集合，用于注册依赖项
ServiceCollection serviceCollection = new();

// 注册 Bar 类型为瞬态服务
serviceCollection.AddTransient<Bar>();

// 注册 Baz 类型为瞬态服务
serviceCollection.AddTransient<Baz>();

// 构建服务提供者，供依赖注入系统使用
ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

// 使用 ActivatorUtilities.CreateInstance 方法创建 Foo 的实例
ActivatorUtilities.CreateInstance<Foo>(serviceProvider).Dump();

public class Foo
{
	// 在 .NET 9 中，标记了 ActivatorUtilitiesConstructorAttribute 的构造函数
	// 将始终被优先调用，无论其他构造函数的数量或排序如何
	[ActivatorUtilitiesConstructor]
	public Foo(Bar bar)
	{
		this.Bar = bar; // 将传入的 Bar 对象赋给属性
	}

	// 另一个没有标记的构造函数，接受 Baz
	public Foo(Baz baz)
	{
		this.Baz = baz; // 将传入的 Baz 对象赋给属性
	}

	public Bar? Bar { get; }
	public Baz? Baz { get; }
}

public class Bar { } // 一个简单的 Bar 类
public class Baz { } // 一个简单的 Baz 类

// .NET 9 更新中，ActivatorUtilities.CreateInstance 的变化确保了
// 标记了 ActivatorUtilitiesConstructor 的构造函数会被优先选中
// 这提升了构造函数选择的确定性和可预测性