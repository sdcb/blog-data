<Query Kind="Program">
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 创建一个新的 CngKeyCreationParameters 对象来配置密钥的创建。
        CngKeyCreationParameters cngCreationParams = new CngKeyCreationParameters
        {
            // 指定密钥的提供程序为 Microsoft 软件密钥存储提供程序。
            Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
            // 设置密钥创建选项，包括使用虚拟化安全来保护密钥。
            // RequireVbs: 要求虚拟化层面安全
            // OverwriteExistingKey: 如果密钥已存在，则覆盖它
            KeyCreationOptions = CngKeyCreationOptions.RequireVbs | CngKeyCreationOptions.OverwriteExistingKey,
        };

        // 使用指定的算法和名称创建密钥，并应用上面的创建参数。
        using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, "myKey", cngCreationParams))
        // 创建 ECDsaCng 对象来进行基于椭圆曲线的数字签名算法操作。
        using (ECDsaCng ecdsa = new ECDsaCng(key))
		{
			// 使用密钥进行操作，例如签名和验证。
			// 这里可以添加具体操作的逻辑。
			// 使用这种方式创建的密钥由 VBS 保护，
			// 提供了额外的安全性以防止管理员级别的密钥盗窃攻击。
		}
	}
}