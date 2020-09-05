using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer
{
    [Generator]
    public class BuildDateGenerator : ISourceGenerator
    {
        private const string attrSource = @"
using System;
namespace DarkestDungeonRandomizer {
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class IncludeBuildDatePropertyAttribute
    {
        public string PropName { get; }

        public IncludeBuildDatePropertyAttribute(string propName)
        {
            PropName = propName;
        }
    }
}
";

        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new BuildDateAttributeSyntaxReciever());
        }

        public void Execute(SourceGeneratorContext context)
        {
            context.AddSource("IncludeBuildDatePropertyAttribute", SourceText.From(attrSource, Encoding.UTF8));

            if (!(context.SyntaxReceiver is BuildDateAttributeSyntaxReciever receiver))
                return;

            CSharpParseOptions options = (CSharpParseOptions)((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attrSource, Encoding.UTF8), options));

            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("DarkestDungeonRandomizer.IncludeBuildDatePropertyAttribute")!;

            string buildDate = DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("en-US"));
            string augments = "using System;\n";

            foreach (var candidate in receiver.candidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(candidate.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(candidate)!;
                var attr = symbol.GetAttributes().First(x => x.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));
                var propName = attr.ConstructorArguments.First().Value as string;

                augments += $@"
namespace {symbol.ContainingNamespace.Name} {{
    public partial class {symbol.Name}
    {{
        public string {propName} {{ get; }} = {buildDate};
    }}
}}
";
            }
            context.AddSource($"Augments", SourceText.From(augments, Encoding.UTF8));
        }
    }

    class BuildDateAttributeSyntaxReciever : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> candidateClasses { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Business logic to decide what we're interested in goes here
            if (syntaxNode is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0)
            {
                candidateClasses.Add(classDecl);
            }
        }
    }
}
