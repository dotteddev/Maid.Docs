using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using TestAssembly;

namespace Maid.Docs.Scraper;

public class CodeScraper
{

	static List<ISymbol> _memberDeclarations = [];
	static Dictionary<string, List<object>> _membersByNamespace = [];

	public static async Task ScrapeCodeAsync(DocConfig config)
	{
		var workspace = MSBuildWorkspace.Create();
		foreach (var projectPath in config.Projects)
		{
			var project = await workspace.OpenProjectAsync(projectPath);
			var compilation = await project.GetCompilationAsync();
			if (compilation is null) continue;
			var syntaxTrees = compilation.SyntaxTrees;

			foreach (var syntaxtree in syntaxTrees)
			{
				var semanticmodel = compilation.GetSemanticModel(syntaxtree);
				var root = await syntaxtree.GetRootAsync();
				var members = root.DescendantNodes().OfType<MemberDeclarationSyntax>();

				_memberDeclarations.AddRange(members.Select(m => semanticmodel.GetDeclaredSymbol(m)!));
			}
		}

		foreach (var member in _memberDeclarations.Where(m => m is not null))
		{
			object serializedSymbol;
			//if (typeName.EndsWith("NamespaceSymbol"))
			//{
			//	Console.WriteLine($"Namespace: {member.ContainingNamespace}.{member.Name}");
			//	continue;
			//}

			if (member is INamedTypeSymbol typeSymbol)
			{
				//serializedSymbol = new
				//{
				//	typeSymbol.Name,
				//	typeSymbol.ContainingNamespace,
				//	typeSymbol.Interfaces,
				//	xmlid = typeSymbol.GetDocumentationCommentId(),
				//	xml = typeSymbol.GetDocumentationCommentXml(),
				//	//attrs = typeSymbol.GetAttributes(),
				//	members = typeSymbol.GetMembers().Select(n=>n.Name),
				//	baseTypeName = typeSymbol.BaseType?.Name,
				//	typekind = typeSymbol.TypeKind,
				//	typeParams = typeSymbol.TypeParameters.Select(tp=>tp.Name),
				//	typeSymbol.DeclaredAccessibility,
				//	allinterfaces = typeSymbol.AllInterfaces.Select(tp => tp.Name),
				//	typeSymbol.Arity, 
				//	ctors = typeSymbol.Constructors.Select(tp => tp.Name),
				//	containingModule = typeSymbol.ContainingModule.Name,
				//	containingSymbol = typeSymbol.ContainingSymbol.Name,
				//	typeSymbol.InstanceConstructors,
				//	typeSymbol.IsAbstract,
				//	typeSymbol.IsAnonymousType,
				//	typeSymbol.IsSealed,
				//	typeSymbol.IsStatic,
				//	typeSymbol.IsValueType,
				//	typeSymbol.MetadataName,
				//	typeSymbol.OriginalDefinition,
				//	typeSymbol.TypeArguments
				//};
				serializedSymbol = JsonSerializer.Serialize(typeSymbol);
			}
			else if (member is IMethodSymbol methodSymbol)
			{
				//serializedSymbol = new
				//{
				//	methodSymbol.Name,
				//	methodSymbol.ContainingNamespace,
				//	xmlid = methodSymbol.GetDocumentationCommentId(),
				//	xml = methodSymbol.GetDocumentationCommentXml(),
				//	//attrs = methodSymbol.GetAttributes(),
				//	methodSymbol.Parameters,
				//	methodSymbol.ReturnType,
				//	methodSymbol.TypeParameters,
				//	methodSymbol.DeclaredAccessibility,
				//	methodSymbol.IsAbstract,
				//	methodSymbol.IsAsync,
				//	methodSymbol.IsExtensionMethod,
				//	methodSymbol.IsGenericMethod,
				//	methodSymbol.IsImplicitlyDeclared,
				//	methodSymbol.IsOverride,
				//	methodSymbol.IsSealed,
				//	methodSymbol.IsStatic,
				//	methodSymbol.IsVirtual,
				//	methodSymbol.MetadataName,
				//	methodSymbol.OriginalDefinition,
				//	methodSymbol.OverriddenMethod,
				//	methodSymbol.TypeArguments
				//};
				serializedSymbol = JsonSerializer.Serialize(methodSymbol);
			}
			else if (member is IPropertySymbol propertySymbol)
			{
				serializedSymbol = new
				{
					propertySymbol.Name,
					propertySymbol.ContainingNamespace,
					xmlid = propertySymbol.GetDocumentationCommentId(),
					xml = propertySymbol.GetDocumentationCommentXml(),
					attrs = propertySymbol.GetAttributes(),
					propertySymbol.Parameters,
					propertySymbol.DeclaredAccessibility,
					propertySymbol.IsAbstract,
					propertySymbol.IsIndexer,
					propertySymbol.IsReadOnly,
					propertySymbol.IsSealed,
					propertySymbol.IsStatic,
					propertySymbol.IsVirtual,
					propertySymbol.MetadataName,
					propertySymbol.OriginalDefinition,
				};
			}
			else
			{
				serializedSymbol = new
				{
					member.Name,
					member.ContainingNamespace,
					xmlid = member.GetDocumentationCommentId(),
					xml = member.GetDocumentationCommentXml(),
					attrs = member.GetAttributes(),
					member.DeclaredAccessibility,
					member.MetadataName,
					member.OriginalDefinition,
				};
			}
			var namespc = member.ContainingNamespace.ToString();
			if (!_membersByNamespace.TryGetValue(namespc, out List<object>? value))
			{
				value = new List<object>();
				_membersByNamespace[namespc] = value;
			}
			value.Add(serializedSymbol);

		}
		foreach(var (ns, members) in _membersByNamespace)
		{
			File.WriteAllText(Path.Combine(config.OutputDir, $"{ns.Replace("<global namespace>", "global").Replace(" ", "-")}.json"), Newtonsoft.Json.JsonConvert.SerializeObject(members, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
		}
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

		//JObject jsonObj = new JObject(new JProperty(xmlel.Name.LocalName, xmlel.Value));

		//Console.WriteLine(jsonObj.ToString());
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