<Query Kind="Statements">
  <NuGetReference>NPOI</NuGetReference>
  <Namespace>NPOI.XSSF.UserModel</Namespace>
  <Namespace>NPOI.SS.UserModel</Namespace>
</Query>

#load "load-all"

Util.NewProcess = true;
List<User> users = LoadUsers(@"C:\Users\sdfly\Desktop\test-data\test-data.json");

Measure(() =>
{
    Export(users, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\export.npoi.xlsx");
}).Dump("NPOI");

void Export<T>(List<T> data, string path)
{
    IWorkbook workbook = new XSSFWorkbook();
    ISheet sheet = workbook.CreateSheet("Sheet1");

    var headRow = sheet.CreateRow(0);
    PropertyInfo[] props = typeof(User).GetProperties();
    for (var i = 0; i < props.Length; ++i)
    {
        headRow.CreateCell(i).SetCellValue(props[i].Name);
    }
    for (var i = 0; i < data.Count; ++i)
    {
        var row = sheet.CreateRow(i + 1);
        for (var j = 0; j < props.Length; ++j)
        {
            row.CreateCell(j).SetCellValue(props[j].GetValue(data[i]).ToString());
        }
    }

    using var file = File.Create(path);
    workbook.Write(file);
}