<Query Kind="Program">
  <Namespace>System.Reflection.Metadata</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

// TypeName 类：提供与 System.Type 相似的功能，但与运行时环境解耦。特别适合用于需要解析和处理类型名称的场景，如序列化和编译器。
// 解析选项：通过 TypeNameParseOptions 自定义解析行为，以适应不同的需求。
// 属性集成：类似于 System.Type 中的多个属性，如 FullName、IsArray、GetArrayRank 等，增强了类型解析的信息获取。
// 程序集信息暴露：通过 AssemblyNameInfo 类，确保解析过程中的程序集信息是不可变的，提升了安全性。
// 不支持等式比较：因为没有实现 IEquatable，因此比较对象时需要手动进行各种属性的比较，而不是直接使用等价性检查。

internal class RestrictedSerializationBinder
{
    Dictionary<string, Type> AllowList { get; set; }

    RestrictedSerializationBinder(Type[] allowedTypes)
        => AllowList = allowedTypes.ToDictionary(type => type.FullName!);

    Type? GetType(ReadOnlySpan<char> untrustedInput)
    {
        if (!TypeName.TryParse(untrustedInput, out TypeName? parsed))
        {
            throw new InvalidOperationException($"Invalid type name: '{untrustedInput.ToString()}'");
        }

        if (AllowList.TryGetValue(parsed.FullName, out Type? type))
        {
            return type;
        }
        else if (parsed.IsSimple
            && parsed.AssemblyName is not null
            && parsed.AssemblyName.Name == "MyTrustedAssembly")
        {
            return Type.GetType(parsed.AssemblyQualifiedName, throwOnError: true);
        }

        throw new InvalidOperationException($"Not allowed: '{untrustedInput.ToString()}'");
    }

    public static void Main(string[] args)
    {
        // 定义允许的类型列表，只允许安全的类型
        var allowedTypes = new[] { typeof(string), typeof(int) };

		// 实例化 RestrictedSerializationBinder
		var binder = new RestrictedSerializationBinder(allowedTypes);

		// 测试输入类型名称
		var typeName = "System.String, mscorlib"; // 可以根据需要修改此测试字符串

		try
		{
			// 使用新的类型解析功能
			var type = binder.GetType(typeName);
			Console.WriteLine($"Type found: {type}");
		}
		catch (InvalidOperationException ex)
		{
			// 捕获并输出异常信息
			Console.WriteLine(ex.Message);
		}
	}
}