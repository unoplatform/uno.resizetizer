using System;
using System.IO;
using System.Text;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Resizetizer.Generators;

[Generator(LanguageNames.CSharp)]
internal sealed class WindowTitleGenerator : IIncrementalGenerator
{
    private const string UnoResizetizerIcon = nameof(UnoResizetizerIcon);
    private const string IsUnoHead = nameof(IsUnoHead);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get the AnalyzerConfigOptionsProvider
        var optionsProvider = context.AnalyzerConfigOptionsProvider;
        var compilationProvider = context.CompilationProvider;

        // Combine optionsProvider and compilationProvider
        var combinedProvider = optionsProvider.Combine(compilationProvider);

        // Define the source generator logic
        var sourceCodeProvider = combinedProvider.Select((combined, cancellationToken) =>
        {
            var options = combined.Left.GlobalOptions;
            if (!GetProperty(options, IsUnoHead) || !HasUnoIcon(options, out var unoIcon))
            {

                return (ClassBuilder[])[
                    CodeBuilder.Create("__Empty__")
                        .AddClass("GeneratorResult")
                        .WithSummary($"IsUnoHead: {GetPropertyValue(options, IsUnoHead)}, UnoResizetizerIcon: {GetPropertyValue(options, UnoResizetizerIcon)}")
                ];
            }

            var compilation = combined.Right;
            return
            [
                GenerateLegacyNamespaceCompat(),
                GenerateWindowTitleExtension(options, compilation.AssemblyName, unoIcon)
            ];
        });

        // Register the source generator logic to add the generated source code
        context.RegisterSourceOutput(sourceCodeProvider, (context, classBuilders) =>
        {
            foreach (var classBuilder in classBuilders)
            {
                AddSource(context, classBuilder);
            }
        });
    }

    private static ClassBuilder GenerateWindowTitleExtension(AnalyzerConfigOptions options, string assemblyName, string unoIcon)
    {
        var rootNamespace = GetPropertyValue(options, "RootNamespace");
        var iconName = Path.GetFileNameWithoutExtension(unoIcon);
        var windowTitle = GetPropertyValue(options, "ApplicationTitle");
        if (string.IsNullOrEmpty(windowTitle))
        {
            windowTitle = assemblyName!;
        }

        var builder = CodeBuilder.Create(rootNamespace)
            .AddClass("WindowExtensions")
            .MakeStaticClass()
            .WithSummary(@"Extension methods for the <see cref=""global::Microsoft.UI.Xaml.Window"" /> class.");

        builder.AddMethod("SetWindowIcon")
            .AddParameter("this global::Microsoft.UI.Xaml.Window", "window")
            .MakeStaticMethod()
            .MakePublicMethod()
            .WithSummary(@"This will set the Window Icon for the given <see cref=""global::Microsoft.UI.Xaml.Window"" /> using the provided UnoIcon.")
            .WithBody(w =>
            {
                w.AppendUnindentedLine("#if WINDOWS && !HAS_UNO");
                w.AppendLine("var hWnd = global::WinRT.Interop.WindowNative.GetWindowHandle(window);");
                w.NewLine();
                w.AppendLine("// Retrieve the WindowId that corresponds to hWnd.");
                w.AppendLine("global::Microsoft.UI.WindowId windowId = global::Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);");
                w.NewLine();
                w.AppendLine("// Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.");
                w.AppendLine("global::Microsoft.UI.Windowing.AppWindow appWindow = global::Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);");
                w.AppendLine($@"appWindow.SetIcon(""{iconName}.ico"");");
                w.NewLine();
                w.AppendLine("// Set the Window Title Only if it has the Default WinUI Desktop value and we are running Unpackaged");
                // We're no longer checking for IsPackaged as this seems to be needed when Packaged as well.
                w.If(@"appWindow.Title == ""WinUI Desktop""")
                    .WithBody(b =>
                    {
                        b.AppendLine($@"appWindow.Title = ""{windowTitle}"";");
                    })
                    .EndIf();
                w.AppendUnindentedLine("#endif");
            });

        return builder;

        //builder.AddMethod("IsPackaged")
        //    .WithReturnType("bool")
        //    .MakePrivateMethod()
        //    .MakeStaticMethod()
        //    .WithBody(w =>
        //    {
        //        using (w.Block("try"))
        //        {
        //            w.AppendLine("return global::Windows.ApplicationModel.Package.Current != null;");
        //        }
        //        using (w.Block("catch"))
        //        {
        //            w.AppendLine("return false;");
        //        }
        //    });
    }

    private static string GetPropertyValue(AnalyzerConfigOptions options, string key) =>
        options.TryGetValue($"build_property.{key}", out var value) ? value : string.Empty;

    private static bool GetProperty(AnalyzerConfigOptions options, string key) =>
        bool.TryParse(GetPropertyValue(options, key), out var result) && result;

    private static bool HasUnoIcon(AnalyzerConfigOptions options, out string unoIcon)
    {
        unoIcon = GetPropertyValue(options, UnoResizetizerIcon);
        return !string.IsNullOrEmpty(unoIcon) && !unoIcon.Contains(",");
    }

    private static ClassBuilder GenerateLegacyNamespaceCompat()
    {
        return CodeBuilder.Create("Uno.Resizetizer")
            .AddClass("__LegacyResizetizerSupport__")
            .WithSummary("This is added to ensure the Uno.Resizetizer namespace is present to avoid breaking any applications.")
            .MakeStaticClass();
    }

    private static void AddSource(SourceProductionContext context, ClassBuilder builder) =>
        context.AddSource($"{builder.FullyQualifiedName}.g.cs", SourceText.From(builder.Build(), Encoding.UTF8));
}
