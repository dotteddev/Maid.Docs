using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Maid.Docs.Shared;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Maid.Docs.Scraper.CSharp;
public static class Helper
{
	public static InternalLink GetInternalLinkForSymbol<T>(T symbol) where T : ISymbol
	{

		return new(symbol.Name, GetIdentifier<T>(symbol));
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
		return new(symbol.ContainingNamespace.Name, symbol.Name, symbol.ContainingAssembly.Identity.GetDisplayName());
	}
	public static InterfaceIdentifier GetInterfaceIdentifier(INamedTypeSymbol symbol)
	{
		return new(symbol.ContainingNamespace.Name, symbol.Name, symbol.ContainingAssembly.Identity.GetDisplayName());
	}

	public static MethodIdentifier GetMethodIdentifier(IMethodSymbol symbol)
	{
		return new(GetClassIdentifier(symbol.ContainingType), symbol.Name);
	}

	public static FieldIdentifier GetFieldIdentifier(IFieldSymbol symbol)
	{
		return new(GetClassIdentifier(symbol.ContainingType), symbol.Name);
	}

	public static PropertyIdentifier GetPropertyIdentifier(IPropertySymbol symbol)
	{
		return new(GetClassIdentifier(symbol.ContainingType), symbol.Name);
	}

	public static InternalDocumentationMemberId GetInternalDocumentationMemberId(ISymbol symbol)
	{
		return new(symbol.Name, GetInternalLinkForSymbol(symbol));
	}

}
public class DocumentationBuilder
{
	private Documentation Documentation { get; set; }

	public async Task<Documentation> BuildDocumentation(DocsConfig config)
	{

		Documentation = new Documentation();

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

					var classDocumentation = new ClassDocumentation(
						Helper.GetClassIdentifier(classSymbol),
						Helper.GetInternalDocumentationMemberId(classSymbol));

					var members = cds.Members;

					foreach (var member in members)
					{
						var symbol = compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(member);
						if (symbol is null)
						{
							//Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), member.Kind())}' with name '{member.TryGetInferredMemberName()}' with syntaxtree '{member.SyntaxTree}' could not be parsed");
							continue;
						}
						if (member is FieldDeclarationSyntax fds)
						{
							var fieldSymbol = symbol as IFieldSymbol;
							if (fieldSymbol is null)
							{
								Console.WriteLine($"Symbol of kind '{Enum.GetName(typeof(SyntaxKind), fds.Kind())}' with name ''{fds.TryGetInferredMemberName()} with syntaxtree '{fds.SyntaxTree}' could not be casted to {nameof(IFieldSymbol)}");
								continue;
							}

							var fieldDocumentation = new FieldDocumentation(
								Helper.GetFieldIdentifier(fieldSymbol),
								Helper.GetInternalDocumentationMemberId(symbol))
							{
								Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = symbol.IsStatic,
								IsAbstract = symbol.IsAbstract,
								IsSealed = symbol.IsSealed,
								IsVirtual = symbol.IsVirtual,
								IsPartial = false,
								FieldType = Helper.GetIdentifier(fieldSymbol.Type),
								DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",

							};

							classDocumentation.AddTypeMember(fieldDocumentation);
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

							var propertyDocumentation = new PropertyDocumentation(
								Helper.GetPropertyIdentifier(propertySymbol),
								Helper.GetInternalDocumentationMemberId(symbol))
							{
								Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = symbol.IsStatic,
								IsAbstract = symbol.IsAbstract,
								IsSealed = symbol.IsSealed,
								IsVirtual = symbol.IsVirtual,
								IsPartial = false,
								PropertyType = Helper.GetIdentifier(propertySymbol.Type),
								DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
							};

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

							var methodDocumentation = new MethodDocumentation(
								Helper.GetMethodIdentifier(methodSymbol),
								Helper.GetInternalDocumentationMemberId(symbol))
							{
								Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = symbol.IsStatic,
								IsAbstract = symbol.IsAbstract,
								IsSealed = symbol.IsSealed,
								IsVirtual = symbol.IsVirtual,
								IsPartial = false,
								Arguments = methodSymbol.Parameters.Select(p => (Helper.GetIdentifier(p.Type), p.Name)).ToList(),
								DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
							};

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

							var constructorDocumentation = new ConstructorDocumentation(
								new(Helper.GetClassIdentifier(constructorSymbol.ContainingType)),
								Helper.GetInternalDocumentationMemberId(symbol))
							{
								Accessibility = Enum.GetName(typeof(Accessibility), symbol.DeclaredAccessibility) ?? "###--INVALID_ACCESIBILITY--###",
								IsStatic = symbol.IsStatic,
								IsAbstract = symbol.IsAbstract,
								IsSealed = symbol.IsSealed,
								IsVirtual = symbol.IsVirtual,
								IsPartial = false,
								Arguments = constructorSymbol.Parameters.Select(p => (Helper.GetIdentifier(p.Type), p.Name)).ToList(),
								DocumentationXML = symbol.GetDocumentationCommentXml() ?? "###--NO_DOCUMENTATION--###",
							};

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
