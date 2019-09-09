<Query Kind="Statements">
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

int GetForturn(string name, DateTime birthDay, int faithCount)
{
	using (var h = new Rfc2898DeriveBytes(name + birthDay + faithCount,
		salt: new byte[8] { 44, 2, 3, 4, 5, 6, 7, 8 },
		iterations: 10086))
	{
		return (int)(BitConverter.ToUInt64(h.GetBytes(8), 0) % 100) + 1;
	};
}

GetForturn("狗二", new DateTime(1994, 5, 17), 0).Dump();
GetForturn("狗三", new DateTime(1996, 11, 3), 1).Dump();