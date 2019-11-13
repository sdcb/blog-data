<Query Kind="Program">
  <NuGetReference>Sdcb.TypeScriptAST</NuGetReference>
  <Namespace>Sdcb.TypeScript</Namespace>
  <Namespace>Sdcb.TypeScript.TsTypes</Namespace>
  <Namespace>System.Text.Json</Namespace>
</Query>

void Main()
{
	ParseFiles(Directory.EnumerateFiles(
		path: @"C:\Users\sdfly\source\repos\ShootR\ShootR\ShootR\Client\Ships", "*.ts")).Dump();
}

// ShootR code is forked from: https://github.com/NTaylorMullen/ShootR
static Dictionary<string, ClassDef> ParseFiles(IEnumerable<string> files) => 
    files
    .Select(x => new TypeScriptAST(File.ReadAllText(x), x))
    .SelectMany(x => x.OfKind(SyntaxKind.ClassDeclaration))
    .Select(x => new ClassDef
    {
        Name = x.OfKind(SyntaxKind.Identifier).FirstOrDefault().GetText(),
        Properties = x.OfKind(SyntaxKind.PropertyDeclaration)
            .Select(x => new PropertyDef
            {
                Name = x.IdentifierStr,
                IsPublic = x.First.Kind != SyntaxKind.PrivateKeyword,
                IsStatic = x.OfKind(SyntaxKind.StaticKeyword).Any(),
                Type = GetType(x),
            }).ToList(),
        Methods = x.OfKind(SyntaxKind.Constructor).Concat(x.OfKind(SyntaxKind.MethodDeclaration))
            .Select(x => new MethodDef
            {
                Name = x is ConstructorDeclaration ctor ? ".ctor" : x.IdentifierStr,
                IsPublic = x.First.Kind != SyntaxKind.PrivateKeyword,
                IsStatic = x.OfKind(SyntaxKind.StaticKeyword).Any(),
                Parameters = ((ISignatureDeclaration)x).Parameters.Select(x => new ParameterDef
                {
                    Name = x.OfKind(SyntaxKind.Identifier).FirstOrDefault().GetText(),
                    Type = GetType(x),
                }).ToList(),
                ReturnType = GetReturnType(x),
            }).ToList(),
    }).ToDictionary(x => x.Name, v => v);

static string GetReturnType(Node node) => node.Children.OfType<TypeNode>().FirstOrDefault()?.GetText();

static string GetType(Node node) => node switch
{
    var x when x.OfKind(SyntaxKind.TypeReference).Any() => x.OfKind(SyntaxKind.TypeReference).First().GetText(),
    _ => node.Last switch
    {
        LiteralExpression literal => literal.Kind.ToString()[..^7].ToLower() switch
        {
            "numeric" => "number",
            var x => x,
        },
        var x => x.GetText(),
    }, 
};

class ClassDef
{
    public string Name { get; set; }

    public List<PropertyDef> Properties { get; set; }

    public List<MethodDef> Methods { get; set; }
}

class PropertyDef
{
    public string Name { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
    public string Type { get; set; }
    public override string ToString() => (IsPublic ? "+" : "-") + $" {Name}: " + (String.IsNullOrWhiteSpace(Type) ? "any" : Type);
}

class MethodDef
{
    public string Name { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
    public List<ParameterDef> Parameters { get; set; }
    public string ReturnType { get; set; }
    public override string ToString() => 
        (IsPublic ? "+" : "-")
        + $" {Name}({String.Join(", ", Parameters)})"
        + (Name == ".ctor" ? "" : $": {ReturnType}");
}

class ParameterDef
{
    public string Name { get; set; }
    public string Type { get; set; }
    public override string ToString() => $"{Name}: {Type}";
}