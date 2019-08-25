<Query Kind="Statements">
  <NuGetReference>Aspose.Cells</NuGetReference>
  <Namespace>Aspose.Cells</Namespace>
</Query>

#load "load-all"

Util.NewProcess = true;
List<User> users = LoadUsers(@"C:\Users\sdfly\Desktop\test-data\test-data.json");

Measure(() =>
{
    Export(users, null);
}).Dump("Baseline");

string Export<T>(List<T> data, string path)
{
    PropertyInfo[] props = typeof(User).GetProperties();
	string noCache = null;
    for (var i = 0; i < props.Length; ++i)
    {
        noCache = props[i].Name;
    }
    for (var i = 0; i < data.Count; ++i)
    {
        for (var j = 0; j < props.Length; ++j)
        {
            noCache = props[j].GetValue(data[i]).ToString();
        }
    }
	return noCache;
}