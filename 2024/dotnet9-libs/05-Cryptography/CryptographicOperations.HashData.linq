<Query Kind="Program">
  <Namespace>System.Security.Cryptography</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main(string[] args)
    {
        while (true)
		{
			// 提示用户输入哈希算法名称（如 MD5, SHA1, SHA256, SHA384, SHA512），输入 exit 结束
			Console.Write("请输入哈希算法名称（exit 退出）：");
			string? algorithmName = Console.ReadLine();

			// 判断用户是否输入了退出命令
			if (algorithmName == "exit")
			{
				break;
			}

			// 提示用户输入字符串明文
			Console.Write("请输入字符串明文：");
			string? input = Console.ReadLine();

			// 调用哈希处理函数，并将结果以十六进制形式输出到控制台
			HashAndPrintData(new HashAlgorithmName(algorithmName), Encoding.UTF8.GetBytes(input!));
		}
	}

	static void HashAndPrintData(HashAlgorithmName hashAlgorithmName, byte[] data)
	{
		try
		{
			// 使用指定的哈希算法对数据进行哈希处理
			byte[] hash = CryptographicOperations.HashData(hashAlgorithmName, data);

			// 将哈希结果转换为十六进制字符串
			string hashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

			// 输出哈希结果
			Console.WriteLine($"哈希结果（十六进制）: {hashHex}");
		}
		catch (CryptographicException)
		{
			Console.WriteLine("无效的哈希算法名称。请重新输入。");
		}
	}
}