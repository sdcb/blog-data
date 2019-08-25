<Query Kind="Program">
  <Namespace>System.Text.Json</Namespace>
</Query>

void Main()
{
    LoadUsers(@"C:\Users\flysha.zhou\Desktop\test-data.json").Dump();
}

List<User> LoadUsers(string jsonfile)
{
    string path = jsonfile;
    byte[] bytes = File.ReadAllBytes(path);
    return JsonSerializer.Deserialize<List<User>>(bytes);
}

IEnumerable<object> Measure(Action action, int times = 5)
{
    return Enumerable.Range(1, times).Select(i =>
    {
        var sw = Stopwatch.StartNew();

        long memory1 = GC.GetTotalMemory(true);
        long allocate1 = GC.GetTotalAllocatedBytes(true);
        {
            action();
        }
        long allocate2 = GC.GetTotalAllocatedBytes(true);
        long memory2 = GC.GetTotalMemory(true);

        sw.Stop();
        return new
        {
            次数 = i, 
            分配内存 = (allocate2 - allocate1).ToString("N0"),
            内存提高 = (memory2 - memory1).ToString("N0"), 
            耗时 = sw.ElapsedMilliseconds,
        };
    });
}

class User
{
    public int Id { get; set; }
    public int Gender { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime BirthDate { get; set; }
    public string Company { get; set; }
    public string Phone { get; set; }
    public string Website { get; set; }
    public string SSN { get; set; }
}