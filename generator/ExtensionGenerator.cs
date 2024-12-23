using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace generator;

#pragma warning disable RS1041
// just a demo generator which uses description attribute to create an extension method used in app
[Generator(LanguageNames.CSharp)]
public class ExtensionGenerator : IIncrementalGenerator
{
    private static readonly string Marker = typeof(DescriptionAttribute).FullName!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            Marker,
            predicate: (node, _) => node is MethodDeclarationSyntax,
            transform: (ctx, _) => ctx.TargetSymbol as IMethodSymbol
        );

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            (sourceProductionContext, symbolAndMethods) =>
                DoGenerate(symbolAndMethods.Right, sourceProductionContext)
        );
    }

    private void DoGenerate(
        ImmutableArray<IMethodSymbol?> methods,
        SourceProductionContext sourceProductionContext
    )
    {
        var types = new List<(string Method, string Description)>();
        foreach (var methodSymbol in methods)
        {
            var commandAttributes = methodSymbol?.GetAttributes()
                .FirstOrDefault(attr =>
                    attr.AttributeClass?.ToDisplayString() == Marker
                );

            if (commandAttributes is not null && methodSymbol is not null)
            {
                types.Add((
                    methodSymbol.Name,
                    commandAttributes
                        .ConstructorArguments[0]
                        .Value!.ToString()!
                        .Replace("\"", "\\\"")
                ));
            }
        }

        if (types.Any())
        {
            RegisterAppExtension(sourceProductionContext, types);
        }
    }

    private void RegisterAppExtension(
        SourceProductionContext context,
        List<(string Method, string Description)> types
    )
    {
        context.AddSource(
            "AppExtension.generated.cs",
            $@"
using app;
using System.Collections;

namespace demo.generated;

public static class AppExtension
{{
    public static IDictionary<string, string> ListMethodDescriptions(this App app)
    {{
        return new Dictionary<string, string>
        {{
            {string.Join(", ", types.Select(it => $"{{\"{it.Method}\", \"{it.Description}\"}}"))}
        }};
    }}
}}
");
    }
}

