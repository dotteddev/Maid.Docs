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
public class XmlDocMember(string Content);
/// <summary>
/// Represents <summary></summary> tag in xml documentation
/// </summary>
/// <param name="Content">Content of the tag</param>
public class XmlSummaryDocMember(string Content) : XmlDocMember(Content);
/// <summary>
/// Represents <param></param> tag in xml documentation
/// </summary>
/// <param name="Name">Name of the parameter</param>
/// <param name="Content">Content of the tag</param>
public class XmlParamDocMember(string Name, string Content) : XmlDocMember(Content);
/// <summary>
/// Represents <returns></returns> tag in xml documentation
/// </summary>
/// <param name="Content">Content of the tag</param>
public class XmlReturnsDocMember(string Content) : XmlDocMember(Content);
/// <summary>
/// Represents <example></example> tag in xml documentation
/// </summary>
/// <param name="Content">Content of the tag</param>
public class XmlExampleDocMember(string Content) : XmlDocMember(Content);
/// <summary>
/// Represents <remarks></remarks> tag in xml documentation
/// </summary>
/// <param name="Content">Content of the tag</param>
public class XmlRemarksDocMember(string Content) : XmlDocMember(Content);
/// <summary>
/// Represents <exception></exception> tag in xml documentation
/// </summary>
/// <param name="Name">Name of the exception</param>
/// <param name="Content">Content of the tag</param>
public class XmlExceptionDocMember(string Name, string Content) : XmlDocMember(Content);

public class XmlSeeAlsoDocMember(string Content) : XmlDocMember(Content);
public class XmlIncludeDocMember(string Content) : XmlDocMember(Content);
public enum EMemberType
{
	CTOR,
	METHOD,
	PROPERTY,
	EVENT,
	FIELD,
	DELEGATE

}
public class TypeMemberDoc(EMemberType Type)
{
	public string Id { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string XMLDoc { get; set; } = string.Empty;

	public static TypeMemberDoc From(string id)
	{
		if(id.StartsWith("M:"))
			return new MethodDoc();
		if (id.StartsWith("P:"))
			return new PropertyDoc();
		if (id.StartsWith("E:"))
			return new EventDoc();
		if (id.StartsWith("F:"))
			return new FieldDoc();
		throw new Exception("Invalid member id");

	}
}
public class ConstructorDoc() : TypeMemberDoc(EMemberType.CTOR);
public class MethodDoc() : TypeMemberDoc(EMemberType.METHOD);
public class PropertyDoc() : TypeMemberDoc(EMemberType.PROPERTY);
public class EventDoc() : TypeMemberDoc(EMemberType.EVENT);
public class FieldDoc() : TypeMemberDoc(EMemberType.FIELD);
public class DelegateDoc() : TypeMemberDoc(EMemberType.DELEGATE);

public class TypeDoc
{
	public string TypeName { get; set; } = string.Empty;
	public string Namespace { get; set; } = string.Empty;
	public string AssemblyName { get; set; } = string.Empty;
	public string PackageSlug { get; set; } = string.Empty;
	public List<TypeDoc> InheritedTypes { get; set; } = new();

	public List<TypeMemberDoc> Members { get; set; }



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