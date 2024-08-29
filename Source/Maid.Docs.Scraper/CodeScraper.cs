using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TestAssembly;

namespace Maid.Docs.Scraper;

public class CodeScraper
{
	public static async Task ScrapeCodeAsync(DocConfig config)
	{
		var workspace = MSBuildWorkspace.Create();
		foreach (var projectPath in config.Projects)
		{
			var project = await workspace.OpenProjectAsync(projectPath);
			var compilation = await project.GetCompilationAsync();
			var syntaxTrees = compilation.SyntaxTrees;


			var symbols = await ProcessSyntaxTrees(syntaxTrees, compilation);

			using var sw = new StreamWriter(Path.Combine(config.OutputDir,project.AssemblyName) + ".json");
			sw.WriteLine(JsonConvert.SerializeObject(new { AssemblyName = project.AssemblyName, Types = symbols }));
		}
		
	}
	public static async Task<IEnumerable<TypeDoc>> ProcessSyntaxTrees(IEnumerable<SyntaxTree> syntaxTrees, Compilation compilation)
	{
		var nodes = new List<ISymbol>();
		foreach (var syntaxTree in syntaxTrees)
		{
			//get class symbol
			var semanticModel = compilation.GetSemanticModel(syntaxTree);

			//get xml documentation from symbol
			var root = await syntaxTree.GetRootAsync();
			nodes.AddRange([.. root.DescendantNodes().Where(dn => dn is ClassDeclarationSyntax or RecordDeclarationSyntax).Select(c => semanticModel.GetDeclaredSymbol(c))]);
		}
		return nodes.Select(s => TypeDoc.FromSymbol(s));
	}
}

public class DocConfig
{
	public List<string> Projects { get; set; }
	public string OutputDir { get; set; }
}

public class TypeDoc
{
	public string TypeName { get; set; }
	public string BaseTypeName { get; set; }
	public List<TypeDoc> InheritedTypes { get; set; } = new();
	public string XmlDocId { get; set; }
	public string XmlDocText { get; set; }
	public string JsonDocText { get; set; }

	public static TypeDoc FromSymbol(ISymbol symbol)
	{
		XElement xmlel = XElement.Parse(symbol.GetDocumentationCommentXml()!);

		JObject jsonObj = new JObject(new JProperty(xmlel.Name.LocalName, xmlel.Value));

		Console.WriteLine(jsonObj.ToString());
		return new TypeDoc
			{
				TypeName = symbol.Name,
				XmlDocId = symbol.GetDocumentationCommentId()!,
				//XmlDocText = symbol.GetDocumentationCommentXml(),
				JsonDocText = xmlel.ToString(),
			//InheritedTypes = FromSymbol((symbol as INamedTypeSymbol)?.Interfaces),
			};
	}

	public static List<TypeDoc> FromSymbol(IEnumerable<ISymbol>? symbols)
	{
		if (symbols is null) return new();
		return symbols.Select(FromSymbol).ToList();
	}
}