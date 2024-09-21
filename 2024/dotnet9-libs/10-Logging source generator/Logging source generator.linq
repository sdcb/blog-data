<Query Kind="Program">
  <IncludeAspNet>true</IncludeAspNet>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 需要在.NET SDK或Visual Studio中运行
// .NET 9 引入了一些新特性，其中一个重要更新是支持使用主构造函数的类进行日志记录。
// 这里我们定义一个有主构造函数的部分类 ClassWithPrimaryConstructor。

using Microsoft.Extensions.Logging;

// 我们在类声明行直接定义了主构造函数。
// ILogger 是用于日志记录的接口，我们通过主构造函数注入它。
public partial class ClassWithPrimaryConstructor(ILogger logger)
{
    // 这里我们使用了 [LoggerMessage] 属性，这是日志源生成器提供的特性。
    // 第一个参数是事件ID，这里设置为 0，代表这是一个特定的日志事件。
    // LogLevel.Debug 指定了日志的级别，表示我们记录的是调试信息。
    // 最后一个参数 "Test." 是日志消息，它将在我们调用 Test 方法时输出。
    [LoggerMessage(0, LogLevel.Debug, "Test.")]
    public partial void Test();
}

// 使用示例
class Program
{
    static void Main(string[] args)
    {
        // 创建一个 LoggerFactory 实例，用于生成 ILogger。
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug).AddConsole(); // 将日志输出到控制台
        });

        // 通过 LoggerFactory 创建 ILogger 的实例。
        var logger = loggerFactory.CreateLogger<ClassWithPrimaryConstructor>();

        // 使用主构造函数创建 ClassWithPrimaryConstructor 的实例。
        var myClass = new ClassWithPrimaryConstructor(logger);

		// 调用 Test 方法，此时会使用传入的 ILogger 生成和输出日志。
		myClass.Test();
	}
}

// 通过这个示例，可以看到如何使用 C# 12 中引入的主构造函数结合日志源生成器进行日志记录。
// 主构造函数和部分方法使代码更简洁和易于维护。