<Query Kind="Statements">
  <NuGetReference>EPPlus</NuGetReference>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>OfficeOpenXml</Namespace>
</Query>

#load "load-all"

Util.NewProcess = true;
List<User> users = LoadUsers(@"C:\Users\sdfly\Desktop\test-data\test-data.json");

Measure(() =>
{
    Export(users, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\export.epplus.xlsx");
}).Dump("EPPlus");

void Export<T>(List<T> data, string path)
{
    using var stream = File.Create(path);
    using var excel = new ExcelPackage(stream);
    ExcelWorksheet sheet = excel.Workbook.Worksheets.Add("Sheet1");
    PropertyInfo[] props = typeof(User).GetProperties();
    for (var i = 0; i < props.Length; ++i)
    {
        sheet.Cells[1, i + 1].Value = props[i].Name;
    }
    for (var i = 0; i < data.Count; ++i)
    {
        for (var j = 0; j < props.Length; ++j)
        {
            sheet.Cells[i + 2, j + 1].Value = props[j].GetValue(data[i]);
        }
    }
    excel.Save();
}