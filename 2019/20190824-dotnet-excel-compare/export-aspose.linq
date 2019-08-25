<Query Kind="Statements">
  <NuGetReference>Aspose.Cells</NuGetReference>
  <Namespace>Aspose.Cells</Namespace>
</Query>

#load "load-all"

Util.NewProcess = true;
List<User> users = LoadUsers(@"C:\Users\sdfly\Desktop\test-data\test-data.json");

Measure(() =>
{
    Export(users, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\export.aspose.xlsx");
}).Dump("Aspose");

void Export<T>(List<T> data, string path)
{
    using var excel = new Workbook();
    Worksheet sheet = excel.Worksheets["Sheet1"];
    PropertyInfo[] props = typeof(User).GetProperties();
    for (var i = 0; i < props.Length; ++i)
    {
        sheet.Cells[0, i].Value = props[i].Name;
    }
    for (var i = 0; i < data.Count; ++i)
    {
        for (var j = 0; j < props.Length; ++j)
        {
            sheet.Cells[i + 1, j].Value = props[j].GetValue(data[i]);
        }
    }
    excel.Save(path);
}