<Query Kind="Statements">
  <Namespace>System.Buffers.Text</Namespace>
  <Namespace>Microsoft.AspNetCore.WebUtilities</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// Base64Url 是一种在 URL 中安全使用的编码方案。
// 它避免了使用 '+' 和 '/' '=' 等字符，这些字符在 URL 中有特殊含义。
ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes("Hello, World!");

// 使用 .NET 9 的 Base64Url 类进行编码
byte[] encoded = Base64Url.EncodeToUtf8(bytes);
Console.WriteLine($"Base64Url 编码: {Encoding.UTF8.GetString(encoded)}");

// Base64编码的输出存在 '+' '/' '='3个特殊字符
Console.WriteLine($"Base64 编码：{Convert.ToBase64String(bytes)}");

// 在 .NET 9 之前，开发者通常使用 ASP.NET Core 中的 
// Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode 方法。
// 这个方法虽然有效，但需要专门引用 ASP.NET Core，不适用于所有项目，
// 并且强制返回字符串，可能导致一些性能损失。
string encodedWebEncoders = WebEncoders.Base64UrlEncode(bytes.ToArray());
Console.WriteLine($"WebEncoders Base64Url 编码: {encodedWebEncoders}");

// 解码回原始字节
byte[] decodedBytes = Base64Url.DecodeFromUtf8(encoded);
string decodedString = Encoding.UTF8.GetString(decodedBytes);
Console.WriteLine($"解码后字符串: {decodedString}");

// 这里还值得一提的是，.NET 9 中的 WebEncoders.Base64UrlEncode 方法
// 其内部实现也已经优化为使用 Base64Url 类。