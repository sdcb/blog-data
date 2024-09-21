<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 使用 .NET 9 中的 Int32.BigMul 方法
        // 计算两个 Int32 类型的数的乘积，返回 long 类型
        int num1 = 123456;
        int num2 = 654321;
        long resultInt32 = Int32.BigMul(num1, num2);
        Console.WriteLine($"Int32.BigMul: {resultInt32}");

        // 使用 .NET 9 中的 Int64.BigMul 方法
        // 计算两个 Int64 类型的数的乘积，返回 Int128 类型
        long num3 = 9223372036854775807;
        long num4 = 2;
        Int128 resultInt64 = Int64.BigMul(num3, num4); // .NET 9 支持返回 Int128
        Console.WriteLine($"Int64.BigMul: {resultInt64}");

        // 使用 .NET 9 中的 UInt32.BigMul 方法
        // 计算两个 UInt32 类型的数的乘积，返回 ulong 类型
        uint num5 = 4000000000;
        uint num6 = 2;
        ulong resultUInt32 = UInt32.BigMul(num5, num6);
        Console.WriteLine($"UInt32.BigMul: {resultUInt32}");

        // 使用 .NET 9 中的 UInt64.BigMul 方法
        // 计算两个 UInt64 类型的数的乘积，返回 UInt128 类型
        ulong num7 = 18446744073709551615UL;
        ulong num8 = 2;
        UInt128 resultUInt64 = UInt64.BigMul(num7, num8); // .NET 9 支持返回 UInt128
        Console.WriteLine($"UInt64.BigMul: {resultUInt64}");
	}
}