using System.IO;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Resizetizer.Generators;

[Generator(LanguageNames.CSharp)]
internal sealed class WindowTitleGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var options = context.AnalyzerConfigOptions.GlobalOptions;
        if (!GetProperty(options, "IsUnoHead") || !HasUnoIcon(options, out var unoIcon))
        {
            return;
        }

        GenerateLegacyNamespaceCompat(context);

        GenerateWindowTitleExtension(context, unoIcon);
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    private static void GenerateWindowTitleExtension(GeneratorExecutionContext context, string unoIcon)
    {
        var options = context.AnalyzerConfigOptions.GlobalOptions;
        var rootNamespace = GetPropertyValue(options, "RootNamespace");
        var iconName = Path.GetFileNameWithoutExtension(unoIcon);
        var windowTitle = GetPropertyValue(options, "ApplicationTitle");
        if (string.IsNullOrEmpty(windowTitle))
        {
            windowTitle = context.Compilation.AssemblyName!;
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

        AddSource(context, builder);
    }

    private static string GetPropertyValue(AnalyzerConfigOptions options, string key) =>
        options.TryGetValue($"build_property.{key}", out var value) ? value : string.Empty;

    private static bool GetProperty(AnalyzerConfigOptions options, string key) =>
        bool.TryParse(GetPropertyValue(options, key), out var result) && result;

    private static bool HasUnoIcon(AnalyzerConfigOptions options, out string unoIcon)
    {
        unoIcon = GetPropertyValue(options, "UnoResizetizerIcon");
        return !string.IsNullOrEmpty(unoIcon) && !unoIcon.Contains(",");
    }

    private static void GenerateLegacyNamespaceCompat(GeneratorExecutionContext context)
    {
        var builder = CodeBuilder.Create("Uno.Resizetizer")
            .AddClass("__LegacyResizetizerSupport__")
            .WithSummary("This is added to ensure the Uno.Resizetizer namespace is present to avoid breaking any applications.")
            .MakeStaticClass();

        AddSource(context, builder);
    }

    private static void AddSource(GeneratorExecutionContext context, ClassBuilder builder) =>
        context.AddSource($"{builder.FullyQualifiedName}.g.cs", builder.Build());
}
