﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace HelloWorld_IncrementalGenerator.Analysis
{
    [Generator]
    public class HelloSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilation = context.CompilationProvider;
            context.RegisterSourceOutput(compilation, (sourceProductionContext, c) =>
            {
                var mainMethod = c.GetEntryPoint(sourceProductionContext.CancellationToken);

                string source = $@"// <auto-generated/>
using System;

namespace {mainMethod.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {mainMethod.ContainingType.Name}
    {{
        static partial void Hello(string name) =>
            Console.WriteLine($""Hello: '{{name}}'"");
    }}
}}
";
                var typeName = mainMethod.ContainingType.Name;

                var sourceText = SourceText.From(source, Encoding.UTF8);
                sourceProductionContext.AddSource($"{typeName}.g.cs", sourceText);
            });
        }
    }
}
