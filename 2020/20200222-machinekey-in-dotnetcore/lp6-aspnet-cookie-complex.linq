<Query Kind="Program">
  <Namespace>Microsoft.AspNetCore.Authentication</Namespace>
  <Namespace>Microsoft.AspNetCore.WebUtilities</Namespace>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Security.Claims</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <AppConfig>
    <Content>
      <configuration>
        <runtime>
          <loadFromRemoteSources enabled="true" />
          <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
            <dependentAssembly>
              <assemblyIdentity name="System.Runtime.InteropServices.RuntimeInformation" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
              <bindingRedirect oldVersion="4.0.0.0-4.0.2.0" newVersion="4.0.2.0" />
            </dependentAssembly>
          </assemblyBinding>
        </runtime>
      </configuration>
    </Content>
  </AppConfig>
  <IncludeAspNet>true</IncludeAspNet>
</Query>

void Main()
{
    string cookie = "请改成从asp.net网站中获取";
    byte[] protectedData = WebEncoders.Base64UrlDecode(cookie);
    string validationKey = "请改成web.config的machineKey中复制";
    string decryptionKey = "请改成web.config的machineKey中复制";

    byte[] bytes = MachineKey.Unprotect(protectedData, validationKey, decryptionKey,
        decryptionAlgorithmName: "AES",
        validationAlgorithmName: "HMACSHA1",
        primaryPurpose: "User.MachineKey.Protect",
        "Microsoft.Owin.Security.Cookies.CookieAuthenticationMiddleware",
        "ApplicationCookie", "v1");
    AuthenticationTicketTool.Deserialize(bytes).Dump();
}

static class AuthenticationTicketTool
{
    public static AuthenticationTicket Deserialize(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        using GZipStream input = new GZipStream(stream, CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(input);
        return Read(reader);
    }

    static AuthenticationTicket Read(BinaryReader reader)
    {
        ClaimsIdentity id = ReadClaims(reader);
        AuthenticationProperties props = ReadProp(reader);
        return new AuthenticationTicket(new ClaimsPrincipal(new[] { id }), props, "AuthenticationSchema");
    }
    
    static ClaimsIdentity ReadClaims(BinaryReader reader)
    {
        if (reader == null) throw new ArgumentNullException("reader");
        if (reader.ReadInt32() != 3) return null;

        string authenticationType = reader.ReadString();
        string name = ReadWithDefault(reader, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        string roleType = ReadWithDefault(reader, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        int claimCount = reader.ReadInt32();
        Claim[] claims = new Claim[claimCount];
        for (int i = 0; i != claimCount; i++)
        {
            string type = ReadWithDefault(reader, name);
            string value = reader.ReadString();
            string valueType = ReadWithDefault(reader, "http://www.w3.org/2001/XMLSchema#string");
            string localAuthority = ReadWithDefault(reader, "LOCAL AUTHORITY");
            string originalIssuer = ReadWithDefault(reader, localAuthority);
            claims[i] = new Claim(type, value, valueType, localAuthority, originalIssuer);
        }
        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, authenticationType, name, roleType);
        if (reader.ReadInt32() > 0)
        {
            claimsIdentity.BootstrapContext = reader.ReadString();
        }
        return claimsIdentity;
    }

    static string ReadWithDefault(BinaryReader reader, string defaultValue)
    {
        string text = reader.ReadString();
        if (string.Equals(text, "\0", StringComparison.Ordinal))
        {
            return defaultValue;
        }
        return text;
    }

    static AuthenticationProperties ReadProp(BinaryReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (reader.ReadInt32() != 1) return null;
        
        int count = reader.ReadInt32();
        Dictionary<string, string> dict = new Dictionary<string, string>(count);
        for (int i = 0; i != count; i++)
        {
            string key = reader.ReadString();
            string value = reader.ReadString();
            dict[key] = value;
        }
        return new AuthenticationProperties(dict);
    }
}

static class MachineKey
{
    public static byte[] Unprotect(byte[] protectedData, string validationKey, string decryptionKey, string decryptionAlgorithmName, string validationAlgorithmName, string primaryPurpose, params string[] specificPurposes)
    {
        using (SymmetricAlgorithm symmetricAlgorithm = CryptoConfig.CreateFromName(decryptionAlgorithmName) as SymmetricAlgorithm)
        {
            symmetricAlgorithm.Key = SP800_108.DeriveKey(HexToBinary(decryptionKey), primaryPurpose, specificPurposes);
            using (KeyedHashAlgorithm keyedHashAlgorithm = CryptoConfig.CreateFromName(validationAlgorithmName) as KeyedHashAlgorithm)
            {
                keyedHashAlgorithm.Key = SP800_108.DeriveKey(HexToBinary(validationKey), primaryPurpose, specificPurposes);
                int blockCount = symmetricAlgorithm.BlockSize / 8;
                int hashCount = keyedHashAlgorithm.HashSize / 8;
                checked
                {
                    int dataCount = protectedData.Length - blockCount - hashCount;
                    if (dataCount <= 0)
                    {
                        return null;
                    }
                    byte[] hash = keyedHashAlgorithm.ComputeHash(protectedData, 0, blockCount + dataCount);
                    if (BuffersAreEqual(protectedData, blockCount + dataCount, hashCount, hash, 0, hash.Length))
                    {
                        byte[] iv = new byte[blockCount];
                        Buffer.BlockCopy(protectedData, 0, iv, 0, iv.Length);
                        symmetricAlgorithm.IV = iv;
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (ICryptoTransform transform = symmetricAlgorithm.CreateDecryptor())
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(protectedData, blockCount, dataCount);
                                    cryptoStream.FlushFinalBlock();
                                    return memoryStream.ToArray();
                                }
                            }
                        }
                    }
                    return null;
                }
            }
        }
    }

    static byte[] HexToBinary(string data)
    {
        if (data == null || data.Length % 2 != 0)
        {
            return null;
        }
        byte[] array = new byte[data.Length / 2];
        for (int i = 0; i < array.Length; i++)
        {
            int i1 = HexToInt(data[2 * i]);
            int i2 = HexToInt(data[2 * i + 1]);
            if (i1 == -1 || i2 == -1)
            {
                return null;
            }
            array[i] = (byte)((i1 << 4) | i2);
        }
        return array;
        int HexToInt(char h)
        {
            if (h < '0' || h > '9')
            {
                if (h < 'a' || h > 'f')
                {
                    if (h < 'A' || h > 'F')
                    {
                        return -1;
                    }
                    return h - 65 + 10;
                }
                return h - 97 + 10;
            }
            return h - 48;
        }
    }

    static bool BuffersAreEqual(byte[] buffer1, int buffer1Offset, int buffer1Count, byte[] buffer2, int buffer2Offset, int buffer2Count)
    {
        bool flag = buffer1Count == buffer2Count;
        for (int i = 0; i < buffer1Count; i++)
        {
            flag &= (buffer1[buffer1Offset + i] == buffer2[buffer2Offset + i % buffer2Count]);
        }
        return flag;
    }

    static class SP800_108
    {
        public static byte[] DeriveKey(byte[] keyDerivationKey, string primaryPurpose, params string[] specificPurposes)
        {
            using (HMACSHA512 hmac = new HMACSHA512(keyDerivationKey))
            {
                GetKeyDerivationParameters(out byte[] label, out byte[] context, primaryPurpose, specificPurposes);
                return DeriveKeyImpl(hmac, label, context, keyDerivationKey.Length * 8);
            }
        }

        private static byte[] DeriveKeyImpl(HMAC hmac, byte[] label, byte[] context, int keyLengthInBits)
        {
            int labelLength = (label != null) ? label.Length : 0;
            int contextLength = (context != null) ? context.Length : 0;
            checked
            {
                byte[] array = new byte[4 + labelLength + 1 + contextLength + 4];
                if (labelLength != 0)
                {
                    Buffer.BlockCopy(label, 0, array, 4, labelLength);
                }
                if (contextLength != 0)
                {
                    Buffer.BlockCopy(context, 0, array, 5 + labelLength, contextLength);
                }
                WriteUInt32ToByteArrayBigEndian((uint)keyLengthInBits, array, 5 + labelLength + contextLength);
                int i = 0;
                int blocks = unchecked(keyLengthInBits / 8);
                byte[] result = new byte[blocks];
                uint pos = 1u;
                while (blocks > 0)
                {
                    WriteUInt32ToByteArrayBigEndian(pos, array, 0);
                    byte[] hash = hmac.ComputeHash(array);
                    int minLen = Math.Min(blocks, hash.Length);
                    Buffer.BlockCopy(hash, 0, result, i, minLen);
                    i += minLen;
                    blocks -= minLen;
                    pos++;
                }
                return result;
            }
        }

        private static void WriteUInt32ToByteArrayBigEndian(uint value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }

    static readonly UTF8Encoding SecureUTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    static void GetKeyDerivationParameters(out byte[] label, out byte[] context, string primaryPurpose, params string[] specificPurposes)
    {
        label = SecureUTF8Encoding.GetBytes(primaryPurpose);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, SecureUTF8Encoding))
            {
                foreach (string value in specificPurposes)
                {
                    binaryWriter.Write(value);
                }
                context = memoryStream.ToArray();
            }
        }
    }
}