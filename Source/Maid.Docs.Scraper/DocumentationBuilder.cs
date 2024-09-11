using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Maid.Docs.Shared;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

using Newtonsoft.Json;

namespace Maid.Docs.Scraper.CSharp;
public static class Helper
{
	public static InternalLink GetInternalLinkForSymbol<T>(T symbol) where T : ISymbol
	{
		var identifier = GetIdentifier(symbol);
		return new()
		{
			Identifier = $"{identifier.GetName()}",
			Name = symbol.Name
		};
	}

	public static IdentifierBase GetIdentifier<T>(T symbol) where T : ISymbol
	{
		if (symbol is INamedTypeSymbol nts) return GetClassIdentifier(nts);
		if (symbol is IMethodSymbol ms) return GetMethodIdentifier(ms);
		if (symbol is IFieldSymbol fs) return GetFieldIdentifier(fs);
		if (symbol is IPropertySymbol ps) return GetPropertyIdentifier(ps);

		throw new NotImplementedException();
	}

	public static ClassIdentifier GetClassIdentifier(INamedTypeSymbol symbol)
	{
		return new()
		{
			AssemblyName = symbol.ContainingAssembly.Name,
			Namespace = symbol.ContainingNamespace.Name,
			Name = symbol.Name
		};
	}
	public static InterfaceIdentifier GetInterfaceIdentifier(INamedTypeSymbol symbol)
	{
		return new()
		{
			AssemblyName = symbol.ContainingAssembly.Name,
			Namespace = symbol.ContainingNamespace.Name,
			Name = symbol.Name
		};
	}

	public static MethodIdentifier GetMethodIdentifier(IMethodSymbol symbol)
	{
		var classIdentifier = GetClassIdentifier(symbol.ContainingType);
		return new()
		{
			AssemblyName = classIdentifier.AssemblyName,
			Namespace = classIdentifier.Namespace,
			Name = symbol.Name,
			Parent = GetClassIdentifier(symbol.ContainingType)
		};
	}

	public static ConstructorIdentifier GetConstructorIdentifier(IMethodSymbol symbol)
	{
		var classIdentifier = GetClassIdentifier(symbol.ContainingType);
		return new()
		{
			AssemblyName = classIdentifier.AssemblyName,
			Namespace = classIdentifier.Namespace,
			Name = symbol.Name,
			Parent = GetClassIdentifier(symbol.ContainingType)
		};
	}

	public static FieldIdentifier GetFieldIdentifier(IFieldSymbol symbol)
	{
		var classIdentifier = GetClassIdentifier(symbol.ContainingType);
		return new()
		{
			AssemblyName = classIdentifier.AssemblyName,
			Namespace = classIdentifier.Namespace,
			Name = symbol.Name,
			Parent = GetClassIdentifier(symbol.ContainingType)
		};
	}

	public static PropertyIdentifier GetPropertyIdentifier(IPropertySymbol symbol)
	{
		var classIdentifier = GetClassIdentifier(symbol.ContainingType);
		return new()
		{
			AssemblyName = classIdentifier.AssemblyName,
			Namespace = classIdentifier.Namespace,
			Name = symbol.Name,
			Parent = GetClassIdentifier(symbol.ContainingType)
		};
	}

	public static InternalDocumentationMemberId GetInternalDocumentationMemberId(ISymbol symbol)
	{
		return new()
		{
			FriendlyName = symbol.Name,
			Link = GetInternalLinkForSymbol(symbol)
		};
	}

}
public class DocumentationBuilder
{
	private Documentation Documentation { get; set; } = new();
	private static string GetDocumentationJson(string xml)
	{
		List<string> docMembers = [];
		if (string.IsNullOrEmpty(xml) is false)
		{

			var xmlelement = XElement.Parse(xml);
			docMembers.AddRange(xmlelement.Nodes().Select(node => node.ToString())/*.Select(node => JsonConvert.SerializeXNode(node))*/);
		}
		return JsonConvert.SerializeObject(new { members = docMembers });

	}

	public async Task<Documentation> BuildDocumentation(DocsConfig config)
	{

		foreach (var projectPath in config.Projects)
		{
			var workspace = MSBuildWorkspace.Create();
			var project = await workspace.OpenProjectAsync(projectPath);

			foreach (var document in project.Documents)
			{
				var syntaxTree = await document.GetSyntaxTreeAsync();
				if (syntaxTree is null) continue;
				var root = await syntaxTree.GetRootAsync();
				if (root is null) continue;

				var compilation = await document.Project.GetCompilationAsync();

				if (compilation is null) continue;

				var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
				foreach (var cds in classes)
				{
					var classSymbol = compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(cds);
					Console.WriteLine($"Found class {cds.TryGetInferredMemberName()}");
					if (classSymbol is null) continue;

					//create variable that contains only content of XML node 



					var classDocumentation = new ClassDocumentation()
					{
						Identifier = Helper.GetClassIdentifier(classSymbol),
						DocumentationMemberIdBase = Helper.GetInternalDocumentationMemberId(classSymbol),
						DocumentationXML = classSymbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
						DocumentationJson = //get leading trivia xml 
					};

					var members = cds.Members;

					foreach (var member in members)
					{

						var symbol = compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(member);
						if (symbol is null)
						{
							//Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), member.Kind())}' with name '{member.TryGetInferredMemberName()}' with syntaxtree '{member.SyntaxTree}' could not be parsed");
							continue;
						}
						var docXml = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###";
						if (member is FieldDeclarationSyntax fds)
						{
							var fieldSymbol = symbol as IFieldSymbol;
							if (fieldSymbol is null)
							{
								Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), fds.Kind())}' with name ''{fds.TryGetInferredMemberName()} with syntaxtree '{fds.SyntaxTree}' could not be casted to {nameof(IFieldSymbol)}");
								continue;
							}

							var fieldDocumentation = new FieldDocumentation()
							{
								Identifier = Helper.GetFieldIdentifier(fieldSymbol),
								Accessibility = Enum.GetName(typeof(Accessibility), fieldSymbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								FieldType = Helper.GetIdentifier(fieldSymbol.Type),
								IsStatic = fieldSymbol.IsStatic,
								IsAbstract = fieldSymbol.IsAbstract,
								IsSealed = fieldSymbol.IsSealed,
								IsVirtual = fieldSymbol.IsVirtual,
								IsPartial = false,
								DocumentationMemberIdBase = Helper.GetInternalDocumentationMemberId(fieldSymbol),
								DocumentationXML = docXml,
								DocumentationJson = GetDocumentationJson(docXml)
							};
							//	Helper.GetFieldIdentifier(fieldSymbol),
							//	Helper.GetInternalDocumentationMemberId(symbol))
							//{
							//	Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
							//	IsStatic = symbol.IsStatic,
							//	IsAbstract = symbol.IsAbstract,
							//	IsSealed = symbol.IsSealed,
							//	IsVirtual = symbol.IsVirtual,
							//	IsPartial = false,
							//	FieldType = Helper.GetIdentifier(fieldSymbol.Type),
							//	DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",

							//};

							classDocumentation.TypeMembers.Add(fieldDocumentation);
							continue;
						}
						else if (member is PropertyDeclarationSyntax property)
						{
							var propertySymbol = symbol as IPropertySymbol;
							if (propertySymbol is null)
							{
								Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), property.Kind())}' with syntaxtree '{property.SyntaxTree}' could not be casted to {nameof(IPropertySymbol)}");
								continue;
							}

							var propertyDocumentation = new PropertyDocumentation()
							{
								Identifier = Helper.GetPropertyIdentifier(propertySymbol),
								Accessibility = Enum.GetName(typeof(Accessibility), propertySymbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = propertySymbol.IsStatic,
								IsAbstract = propertySymbol.IsAbstract,
								IsSealed = propertySymbol.IsSealed,
								IsVirtual = propertySymbol.IsVirtual,
								IsPartial = false,
								DocumentationMemberIdBase = Helper.GetInternalDocumentationMemberId(propertySymbol),
								DocumentationXML = docXml,
								PropertyType = Helper.GetIdentifier(propertySymbol.Type),
								DocumentationJson = GetDocumentationJson(docXml),

							};
							//	Helper.GetPropertyIdentifier(propertySymbol),
							//	Helper.GetInternalDocumentationMemberId(symbol))
							//{
							//	Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
							//	IsStatic = symbol.IsStatic,
							//	IsAbstract = symbol.IsAbstract,
							//	IsSealed = symbol.IsSealed,
							//	IsVirtual = symbol.IsVirtual,
							//	IsPartial = false,
							//	PropertyType = Helper.GetIdentifier(propertySymbol.Type),
							//	DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
							//};

							classDocumentation.AddTypeMember(propertyDocumentation);
							continue;
						}
						else if (member is MethodDeclarationSyntax method)
						{
							var methodSymbol = symbol as IMethodSymbol;
							if (methodSymbol is null)
							{
								Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), method.Kind())}' with syntaxtree '{method.SyntaxTree}' could not be casted to {nameof(IMethodSymbol)}");
								continue;
							}

							var methodDocumentation = new MethodDocumentation()
							{
								Identifier = Helper.GetMethodIdentifier(methodSymbol),
								Accessibility = Enum.GetName(typeof(Accessibility), methodSymbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = methodSymbol.IsStatic,
								IsAbstract = methodSymbol.IsAbstract,
								IsSealed = methodSymbol.IsSealed,
								IsVirtual = methodSymbol.IsVirtual,
								IsPartial = false,
								Arguments = methodSymbol.Parameters.Select(p => (Helper.GetIdentifier(p.Type), p.Name)).ToList(),
								DocumentationMemberIdBase = Helper.GetInternalDocumentationMemberId(symbol),
								DocumentationXML = docXml,
								DocumentationJson = GetDocumentationJson(docXml),

							};
							//	Helper.GetMethodIdentifier(methodSymbol),
							//	Helper.GetInternalDocumentationMemberId(symbol))
							//{
							//	Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
							//	IsStatic = symbol.IsStatic,
							//	IsAbstract = symbol.IsAbstract,
							//	IsSealed = symbol.IsSealed,
							//	IsVirtual = symbol.IsVirtual,
							//	IsPartial = false,
							//	Arguments = methodSymbol.Parameters.Select(p => (Helper.GetIdentifier(p.Type), p.Name)).ToList(),
							//	DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
							//};

							classDocumentation.AddTypeMember(methodDocumentation);
							continue;
						}
						else if (member is ConstructorDeclarationSyntax constructor)
						{
							var constructorSymbol = symbol as IMethodSymbol;
							if (constructorSymbol is null)
							{
								Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), constructor.Kind())}' with syntaxtree '{constructor.SyntaxTree}' could not be casted to {nameof(IMethodSymbol)}");
								continue;
							}

							var constructorDocumentation = new ConstructorDocumentation()
							{
								Identifier = Helper.GetConstructorIdentifier(constructorSymbol),
								Accessibility = Enum.GetName(typeof(Accessibility), constructorSymbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = constructorSymbol.IsStatic,
								IsAbstract = constructorSymbol.IsAbstract,
								IsSealed = constructorSymbol.IsSealed,
								IsVirtual = constructorSymbol.IsVirtual,
								IsPartial = false,
								Arguments = constructorSymbol.Parameters.Select(p => (Helper.GetIdentifier(p.Type), p.Name)).ToList(),
								DocumentationMemberIdBase = Helper.GetInternalDocumentationMemberId(symbol),
								DocumentationXML = docXml,
								DocumentationJson = GetDocumentationJson(docXml),

							};
							//	new(Helper.GetClassIdentifier(constructorSymbol.ContainingType)),
							//	Helper.GetInternalDocumentationMemberId(symbol))
							//{
							//	Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
							//	IsStatic = symbol.IsStatic,
							//	IsAbstract = symbol.IsAbstract,
							//	IsSealed = symbol.IsSealed,
							//	IsVirtual = symbol.IsVirtual,
							//	IsPartial = false,
							//	Arguments = constructorSymbol.Parameters.Select(p => (Helper.GetIdentifier(p.Type), p.Name)).ToList(),
							//	DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
							//};

							classDocumentation.AddTypeMember(constructorDocumentation);
							continue;
						}
						//else if (member is InterfaceDeclarationSyntax)
						//{
						//	var interfaceSymbol = symbol as INamedTypeSymbol;
						//	if (interfaceSymbol is null)
						//	{
						//		Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), member.Kind())}' with syntaxtree '{member.SyntaxTree}' could not be casted to {nameof(INamedTypeSymbol)}");
						//		continue;
						//	}

						//	var interfaceDocumentation = new InterfaceDocumentation(
						//		Helper.GetInterfaceIdentifier(interfaceSymbol),
						//		Helper.GetInternalDocumentationMemberId(symbol))
						//	{
						//		DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",

						//	};

						//	classDocumentation.AddTypeMember(interfaceDocumentation);
						//	continue;
						//}
						if (member is EnumDeclarationSyntax)
						{
							Console.WriteLine($"Found enum definition - skipping");
							continue;
						}
						if (member is StructDeclarationSyntax)
						{
							Console.WriteLine($"Found struct definition - skipping");
							continue;
						}
						if (member is DelegateDeclarationSyntax)
						{
							Console.WriteLine($"Found delegate definition - skipping");
							continue;
						}
						if (member is EventDeclarationSyntax)
						{
							Console.WriteLine($"Found event definition - skipping");
							continue;
						}
						if (member is EnumMemberDeclarationSyntax)
						{
							Console.WriteLine($"Found enum member definition - skipping");
							continue;
						}
						Console.WriteLine($"Found {member.GetType()} - skipping");

					}
					Documentation.AddDocumentationMember(classDocumentation);
				}
			}
		}
		return Documentation;
	}
}
