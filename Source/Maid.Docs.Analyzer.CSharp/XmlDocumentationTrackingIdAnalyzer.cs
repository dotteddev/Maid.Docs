using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Maid.Docs.Analyzer.CSharp
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class XmlDocumentationTrackingIdAnalyzer : DiagnosticAnalyzer
	{
		private const string MISSING_TRACKING_ID_DIAGNOSTIC_ID = "MD001";
		public static readonly DiagnosticDescriptor MissingTrackingId = new DiagnosticDescriptor(MISSING_TRACKING_ID_DIAGNOSTIC_ID, "Missing trackingId", "Missing documentation <trackingId></trackingId>", "Docs", DiagnosticSeverity.Error, true, customTags: new[] { WellKnownDiagnosticTags.Build });
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [MissingTrackingId]; } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
		}

		private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			var comments = context.Tree.GetRoot().DescendantTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
			foreach (var comment in comments)
			{
				if(context.IsGeneratedCode) continue;
				if (comment.Token.LeadingTrivia.ToString().Contains("<trackingId>")) 
					continue;
				context.ReportDiagnostic(Diagnostic.Create(MissingTrackingId, comment.GetLocation()));				
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocumentationTrackingIdCodeFix)),Shared]
	public class XmlDocumentationTrackingIdCodeFix : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ["MD001"];
		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var diagnostic = context.Diagnostics.First();
			context.RegisterCodeFix(CodeAction.Create("Add <trackingId>{{GUID}}</trackingId>", (c, ct) =>
			{
				return Fix(context.Document, diagnostic.Location, ct);
			},"add-tracking-id-tag-to-doc-xml"), diagnostic);
			return Task.CompletedTask;
		}
		private async Task<Document> Fix(Document document, Location location, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var node = root.FindNode(location.SourceSpan);
			var comment = node.GetLeadingTrivia().First(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

			

			var newComment = comment.Token.LeadingTrivia.Add(SyntaxFactory.DocumentationCommentExterior($"/// <trackingId>{Guid.NewGuid()}</trackingId>\r\n"));
			var newRoot = root.ReplaceToken(comment.Token, comment.Token.WithLeadingTrivia(newComment));
			return document.WithSyntaxRoot(newRoot);
		}
		public override FixAllProvider GetFixAllProvider()
		{
			//FixAllProvider.Create(async (context, document, diagnostic) =>
			//{
			//	var cts = new CancellationTokenSource();
			//	foreach (var diag in diagnostic)
			//	{
			//		if (FixableDiagnosticIds.Contains(diag.Id))
			//			document = await AddTrackingIdAsync(document, diag.Location, cts.Token);
			//	}
			//	return document;
			//});

			//return base.GetFixAllProvider();
			return WellKnownFixAllProviders.BatchFixer;
		}
	}
}
