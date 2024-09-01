using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using System.Text.Json;
//using System.Text.Json.Serialization;

using TestAssembly;
using Maid.Docs.Shared;

namespace Maid.Docs.Scraper;


public class CodeScraper
{

	static List<ISymbol> _memberDeclarations = [];
	static Dictionary<string, List<object>> _membersByNamespace = [];

	public static async Task ScrapeCodeAsync(DocConfig config)
	{
		var docs = new Shared.Docs();
		docs.DocId = "Maid.Docs";
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
			DocsMember docsMember = new DocsMember();
			//if (typeName.EndsWith("NamespaceSymbol"))
			//{
			//	Console.WriteLine($"Namespace: {member.ContainingNamespace}.{member.Name}");
			//	continue;
			//}

			if (member is INamedTypeSymbol typeSymbol)
			{
				docsMember = new TypeDocs(docs.DocId, GetDocsId(typeSymbol))
					.Configure(type =>
					{
						type.TypeName = typeSymbol.Name;
						type.Namespace = typeSymbol.ContainingNamespace is not null? DocsMemberRef.Create(docs.DocId, typeSymbol.ContainingNamespace.ToString()!) : string.Empty;
						type.AssemblyName = typeSymbol.ContainingAssembly.Name;
						type.AccessModifier = Helpers.AccessModifierFrom(typeSymbol.DeclaredAccessibility);
						type.InheritedType = typeSymbol.BaseType is not null? DocsMemberRef.Create(docs.DocId, GetDocsId(typeSymbol.BaseType)) : string.Empty;
					});
			}
			else if (member is IMethodSymbol methodSymbol)
			{
				docsMember = new MethodDocs(docs.DocId, GetDocsId(methodSymbol))
					.Configure(method =>
					{
						method.MethodName = methodSymbol.Name;
						method.ReturnType = DocsMemberRef.Create(docs.DocId, GetDocsId(methodSymbol.ReturnType));
						method.AccessModifier = Helpers.AccessModifierFrom(methodSymbol.DeclaredAccessibility);
						method.Attributes = methodSymbol.GetAttributes().Select(attr => new Shared.Attribute(attr.AttributeClass.Name, attr.ConstructorArguments.Select(arg => (arg.Type.Name, arg.Value.ToString())).ToArray())).ToList();
					});

			}
			//else if (member is IPropertySymbol propertySymbol)
			//{
			//	serializedSymbol = new
			//	{
			//		propertySymbol.Name,
			//		propertySymbol.ContainingNamespace,
			//		xmlid = propertySymbol.GetDocumentationCommentId(),
			//		xml = propertySymbol.GetDocumentationCommentXml(),
			//		attrs = propertySymbol.GetAttributes(),
			//		propertySymbol.Parameters,
			//		propertySymbol.DeclaredAccessibility,
			//		propertySymbol.IsAbstract,
			//		propertySymbol.IsIndexer,
			//		propertySymbol.IsReadOnly,
			//		propertySymbol.IsSealed,
			//		propertySymbol.IsStatic,
			//		propertySymbol.IsVirtual,
			//		propertySymbol.MetadataName,
			//		propertySymbol.OriginalDefinition,
			//	};
			//}
			//else
			//{
			//	serializedSymbol = new
			//	{
			//		member.Name,
			//		member.ContainingNamespace,
			//		xmlid = member.GetDocumentationCommentId(),
			//		xml = member.GetDocumentationCommentXml(),
			//		attrs = member.GetAttributes(),
			//		member.DeclaredAccessibility,
			//		member.MetadataName,
			//		member.OriginalDefinition,
			//	};
			//}
			
			docs.DocsMembers.Add(docsMember);

		}

		File.WriteAllText(Path.Combine(config.OutputDir, docs.DocId) + ".json", JsonConvert.SerializeObject(docs));
	}
	public static string GetDocsId(ISymbol symbol)
	{
		return symbol.GetDocumentationCommentId() ?? $"{symbol.ContainingNamespace}.{symbol.Name}";
	}

}

public static class Helpers
{
	public static AccessModifier AccessModifierFrom(Accessibility accessibility) => accessibility switch
	{
		Accessibility.Public => AccessModifier.Public,
		Accessibility.Protected => AccessModifier.Protected,
		Accessibility.Private => AccessModifier.Private,
		Accessibility.Internal => AccessModifier.Internal,
		Accessibility.ProtectedOrInternal => AccessModifier.ProtectedOrInternal,
		Accessibility.ProtectedAndInternal => AccessModifier.ProtectedAndInternal,
		_ => AccessModifier.Private
	};
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