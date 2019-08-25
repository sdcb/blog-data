<Query Kind="Statements">
  <NuGetReference>DocumentFormat.OpenXml</NuGetReference>
  <Namespace>DocumentFormat.OpenXml.Packaging</Namespace>
  <Namespace>DocumentFormat.OpenXml.Spreadsheet</Namespace>
  <Namespace>DocumentFormat.OpenXml</Namespace>
</Query>

#load "load-all"

Util.NewProcess = true;
List<User> users = LoadUsers(@"C:\Users\sdfly\Desktop\test-data\test-data.json");

Measure(() =>
{
    Export(users, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\export.openXml.xlsx");
}).Dump("OpenXML");

void Export<T>(List<T> data, string path)
{
    using SpreadsheetDocument excel = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);

    WorkbookPart workbookPart = excel.AddWorkbookPart();
    workbookPart.Workbook = new Workbook();

    WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
    worksheetPart.Worksheet = new Worksheet(new SheetData());

    Sheets sheets = excel.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
    Sheet sheet = new Sheet
    {
        Id = excel.WorkbookPart.GetIdOfPart(worksheetPart),
        SheetId = 1,
        Name = "Sheet1"
    };
    sheets.Append(sheet);
    
    SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

    PropertyInfo[] props = typeof(User).GetProperties();
    {    // header
        var row = new Row() { RowIndex = 1 };
        sheetData.Append(row);
        row.Append(props.Select((prop, i) => new Cell
        {
            CellReference = ('A' + i - 1) + row.RowIndex.Value.ToString(),
            CellValue = new CellValue(props[i].Name),
            DataType = new EnumValue<CellValues>(CellValues.String),
        }));
    }
    sheetData.Append(data.Select((item, i) => 
    {
        var row = new Row { RowIndex = (uint)(i + 2) };
        row.Append(props.Select((prop, j) => new Cell
        {
            CellReference = ('A' + j - 1) + row.RowIndex.Value.ToString(),
            CellValue = new CellValue(props[j].GetValue(data[i]).ToString()),
            DataType = new EnumValue<CellValues>(CellValues.String),
        }));
        return row;
    }));
    excel.Save();
}