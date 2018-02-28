using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ThomasLevesque.Analyzers.CSharp.Internal;

namespace ThomasLevesque.Analyzers.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UsingStatementMissingAwaitAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TLV0001";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UsingStatementMissingAwaitAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UsingStatementMissingAwaitAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UsingStatementMissingAwaitAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
        }

        private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax) context.Node;

            var typeSyntax = usingStatement?.Declaration?.Type;

            if (typeSyntax != null && context.SemanticModel.GetTypeInfo(typeSyntax).Type is INamedTypeSymbol type)
            {
                if (IsTaskWithResult(type, out var resultType) && IsDisposable(resultType))
                {
                    var span = TextSpan.FromBounds(usingStatement.UsingKeyword.Span.Start, usingStatement.CloseParenToken.Span.End);
                    var location = Location.Create(context.Node.SyntaxTree, span);
                    location.SourceTree.TryGetText(out var sourceText);
                    string diagnosticText = sourceText.ToString(span);
                    var diagnostic = Diagnostic.Create(Rule, location, diagnosticText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsTaskWithResult(INamedTypeSymbol type, out ITypeSymbol resultType)
        {
            resultType = null;

            if (!type.IsGenericType)
            {
                return false;
            }

            if (type.GetFullName() != "System.Threading.Tasks.Task`1")
            {
                return false;
            }

            resultType = type.TypeArguments.First();
            return true;
        }

        private bool IsDisposable(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType && namedType.GetFullName() == "System.IDisposable")
            {
                return true;
            }

            if (type.AllInterfaces.Any(IsDisposable))
            {
                return true;
            }

            return false;
        }
    }
}
