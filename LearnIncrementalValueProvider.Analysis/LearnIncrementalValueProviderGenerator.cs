using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;

namespace LearnIncrementalValueProvider.Analysis
{
    [Generator]
    public class LearnIncrementalValueProviderGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            Debugger.Launch();
            var additionalTextsProvider = context.AdditionalTextsProvider;

            //context.RegisterSourceOutput(additionalTextsProvider, (ctx, additionalTexts) =>
            //{
            //    var path = additionalTexts.Path;
            //    var text = additionalTexts.GetText(ctx.CancellationToken);
            //});

            var additionalTextsJson = additionalTextsProvider.Where(static (text) => text.Path.EndsWith(".json"));

            //context.RegisterSourceOutput(additionalTextsJson, (ctx, additionalTexts) =>
            //{
            //    var path = additionalTexts.Path;
            //    var text = additionalTexts.GetText(ctx.CancellationToken);
            //    Debugger.Log(1, path, path);
            //});

            var additionalTextsFileName = additionalTextsProvider.Select(static (text, _) => Path.GetFileName(text.Path));
            //context.RegisterSourceOutput(additionalTextsFileName, (ctx, additionalTexts) =>
            //{
            //    Debugger.Log(1, additionalTexts, additionalTexts);
            //});

            var additionalTextsCollect = additionalTextsProvider.Collect();
            //context.RegisterSourceOutput(additionalTextsCollect, (ctx, additionalTexts) =>
            //{
            //    foreach (var text in additionalTexts)
            //    {
            //        Debugger.Log(1, text.Path, text.Path);
            //    }
            //});

            var compilation = context.CompilationProvider;

            var compilationAndAdditionalTexts = additionalTextsProvider.Combine(compilation);
            context.RegisterSourceOutput(compilationAndAdditionalTexts, (ctx, paris) =>
            {
                var c = paris.Right;
                var t = paris.Left;
            });
        }
    }
}
