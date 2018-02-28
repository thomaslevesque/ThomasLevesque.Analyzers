using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace ThomasLevesque.Analyzers.CSharp.Test
{
    [TestClass]
    public class UsingStatementMissingAwaitAnalyzerTests : CodeFixVerifier
    {

        [TestMethod]
        public void Diagnostic_should_not_be_triggered_if_await_is_present_in_using_statement()
        {
            var test = @"
    using System;
    using System.IO;
    using System.Threading.Tasks;

    namespace TheNamespace
    {
        class TheClass
        {
            public async Task Foo()
            {
                await Task.Delay(100);
                using (var stream = await GetStreamAsync())
                {
                }
            }

            public Task<Stream> GetStreamAsync() => throw new NotImplementedException();
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_should_not_be_triggered_if_await_is_not_necessary_in_using_statement()
        {
            var test = @"
    using System;
    using System.IO;
    using System.Threading.Tasks;

    namespace TheNamespace
    {
        class TheClass
        {
            public async Task Foo()
            {
                await Task.Delay(100);
                using (var stream = GetStream())
                {
                }
            }

            public Stream GetStream() => throw new NotImplementedException();
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_should_be_triggered_if_await_is_missing_in_using_statement()
        {
            var test = @"
    using System;
    using System.IO;
    using System.Threading.Tasks;

    namespace TheNamespace
    {
        class TheClass
        {
            public async Task Foo()
            {
                await Task.Delay(100);
                using (var stream = GetStreamAsync())
                {
                }
            }

            public Task<Stream> GetStreamAsync() => throw new NotImplementedException();
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "TLV0001",
                Message = "Await keyword is missing in using statement \'using (var stream = GetStreamAsync())\'. This will dispose the task, not the task\'s result.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 17) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void CodeFix_should_add_missing_await_in_using_statement()
        {
            var test = @"
    using System;
    using System.IO;
    using System.Threading.Tasks;

    namespace TheNamespace
    {
        class TheClass
        {
            public async Task Foo()
            {
                await Task.Delay(100);
                using (var stream = GetStreamAsync())
                {
                }
            }

            public Task<Stream> GetStreamAsync() => throw new NotImplementedException();
        }
    }";

            var fixtest = @"
    using System;
    using System.IO;
    using System.Threading.Tasks;

    namespace TheNamespace
    {
        class TheClass
        {
            public async Task Foo()
            {
                await Task.Delay(100);
                using (var stream = await GetStreamAsync())
                {
                }
            }

            public Task<Stream> GetStreamAsync() => throw new NotImplementedException();
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UsingStatementMissingAwaitCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UsingStatementMissingAwaitAnalyzer();
        }
    }
}
