<Query Kind="Program">
  <NuGetReference>Bogus</NuGetReference>
  <Namespace>Bogus.Extensions.UnitedStates</Namespace>
  <Namespace>System.Text.Json</Namespace>
</Query>

void Main()
{
	string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\test-data.json";
	using var file = File.Create(path);
	using var writer = new Utf8JsonWriter(file, new JsonWriterOptions { Indented = true });
	var data = new Bogus.Faker<Data>()
		.RuleFor(x => x.Id, x => x.IndexFaker + 1)
		.RuleFor(x => x.Gender, x => x.Person.Gender)
		.RuleFor(x => x.FirstName, (x, u) => x.Name.FirstName(u.Gender))
		.RuleFor(x => x.LastName, (x, u) => x.Name.LastName(u.Gender))
		.RuleFor(x => x.Email, (x, u) => x.Internet.Email(u.FirstName, u.LastName))
		.RuleFor(x => x.BirthDate, x => x.Person.DateOfBirth)
		.RuleFor(x => x.Company, x => x.Person.Company.Name)
		.RuleFor(x => x.Phone, x => x.Person.Phone)
		.RuleFor(x => x.Website, x => x.Person.Website)
		.RuleFor(x => x.SSN, x => x.Person.Ssn())
		.GenerateForever().Take(6_0000);
	JsonSerializer.Serialize(writer, data);
	Process.Start("explorer", @$"/select, ""{path}""".Dump());
}

class Data
{
	public int Id { get; set; }
	public Bogus.DataSets.Name.Gender Gender { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string Email { get; set; }
	public DateTime BirthDate { get; set; }
	public string Company { get; set; }
	public string Phone { get; set; }
	public string Website { get; set; }
	public string SSN { get; set; }
}