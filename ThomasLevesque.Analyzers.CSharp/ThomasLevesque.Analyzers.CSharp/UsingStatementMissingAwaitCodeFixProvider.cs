using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThomasLevesque.Analyzers.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingStatementMissingAwaitCodeFixProvider)), Shared]
    public class UsingStatementMissingAwaitCodeFixProvider : CodeFixProvider
    {
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UsingStatementMissingAwaitCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UsingStatementMissingAwaitAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var usingStatement = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<UsingStatementSyntax>().FirstOrDefault();
            if (usingStatement == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title.ToString(),
                    createChangedDocument: ct => AddAwaitAsync(context.Document, usingStatement, ct),
                    equivalenceKey: UsingStatementMissingAwaitAnalyzer.DiagnosticId + "-CodeFix"),
                diagnostic);
        }

        private async Task<Document> AddAwaitAsync(Document document, UsingStatementSyntax usingStatement, CancellationToken cancellationToken)
        {
            var newDeclarators = new List<VariableDeclaratorSyntax>();
            foreach (var declarator in usingStatement.Declaration.Variables)
            {
                var newInitializer = declarator.Initializer.WithValue(SyntaxFactory.AwaitExpression(declarator.Initializer.Value));
                var newDeclarator = declarator.WithInitializer(newInitializer);
                newDeclarators.Add(newDeclarator);
            }

            var newDeclaration = usingStatement.Declaration.WithVariables(SyntaxFactory.SeparatedList(newDeclarators));
            var newUsingStatement = usingStatement.WithDeclaration(newDeclaration);
            
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(usingStatement, newUsingStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
