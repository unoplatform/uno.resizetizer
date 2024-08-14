using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        var additionalTextsProvider = context.AdditionalTextsProvider;

        // Combine optionsProvider and compilationProvider
        var combinedProvider = additionalTextsProvider
            .Combine(compilationProvider)
            .Combine(optionsProvider);

        // Define the source generator logic
        var sourceCodeProvider = combinedProvider.Select(GenerateExtensionGenerationContext).Where(result => result != null);

        // Register the source generator logic to add the generated source code
        context.RegisterSourceOutput(sourceCodeProvider, (sourceContext, extensionContext) =>
        {
            if (!string.IsNullOrEmpty(extensionContext.WindowTitle))
            {
                GenerateLegacyNamespaceCompat();
                GenerateWindowTitleExtension(extensionContext.RootNamespace, extensionContext.UnoIcon, extensionContext.WindowTitle);
            }
        });
    }

    static ExtensionGenerationContext GenerateExtensionGenerationContext(((AdditionalText, Compilation), AnalyzerConfigOptionsProvider) combined, CancellationToken cancellationToken)
    {
        var ((additionalText, compilation), options) = combined;
        var additionalFile = additionalText;

        if (!GetProperty(options.GlobalOptions, IsUnoHead))
        {
            return null;
        }
        else if (Path.GetFileName(additionalFile.Path).Equals("UnoImage.inputs", StringComparison.InvariantCultureIgnoreCase))
        {
            var text = additionalFile.GetText(cancellationToken);
            var textContent = text?.ToString();
            var unoIcon = ParseFile(textContent);

            var rootNamespace = GetPropertyValue(options.GlobalOptions, "RootNamespace");
            var iconName = Path.GetFileNameWithoutExtension(unoIcon);
            var windowTitle = GetPropertyValue(options.GlobalOptions, "ApplicationTitle");
            if (string.IsNullOrEmpty(windowTitle))
            {
                windowTitle = compilation.AssemblyName!;
            }

            return new ExtensionGenerationContext(rootNamespace, iconName, windowTitle);
        }

        return null;
    }

    internal record ExtensionGenerationContext(string RootNamespace, string UnoIcon, string WindowTitle);

    private static string ParseFile(string content)
    {
        // Split the content into lines
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Split the line into key-value pairs
            var properties = line.Split(';').Select(property => property.Split('=')).ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : null);

            // Check if IsAppIcon is true
            if (properties.TryGetValue("IsAppIcon", out var isAppIcon) && bool.TryParse(isAppIcon, out var isAppIconValue) && isAppIconValue)
            {
                // Return the file path
                if (properties.TryGetValue("File", out var filePath))
                {
                    return filePath;
                }
            }
        }

        // Return null if no app icon is found
        return null;
    }

    private static ClassBuilder GenerateWindowTitleExtension(string rootNamespace, string iconName, string windowTitle)
    {
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

        // NOTE: This method has been removed as it seems WinUI isn't setting the title when Packaged. Keeping in case we need this in the future.
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

        return builder;
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
