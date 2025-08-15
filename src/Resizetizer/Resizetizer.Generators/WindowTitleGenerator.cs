using System;
using System.IO;
using System.Linq;
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
        var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.Assembly.Name);
        var additionalTextsProvider = context.AdditionalTextsProvider;

        // To avoid breaking existing applications, we add a legacy namespace compat class.
        // We make sure to add it for all compilation (including for HR compilations!) without any filter (other than assembly name to reduce load).
        context.RegisterSourceOutput(assemblyNameProvider, (srcCtx, _) => AddSource(srcCtx, GenerateLegacyNamespaceCompat()));

        var extensionPropertiesProvider = optionsProvider.Combine(assemblyNameProvider).Select((x, cancellationToken) =>
        {
            var (options, assemblyName) = x;

            var rootNamespace = GetPropertyValue(options.GlobalOptions, "RootNamespace");
            var windowTitle = GetPropertyValue(options.GlobalOptions, "ApplicationTitle");

            if (string.IsNullOrEmpty(windowTitle))
            {
                windowTitle = assemblyName;
            }

            return string.IsNullOrEmpty(rootNamespace) || string.IsNullOrEmpty(windowTitle) ? null : new ExtensionPropertiesContext(rootNamespace, windowTitle);
        });

        // Combine optionsProvider and compilationProvider
        var iconNameProvider = additionalTextsProvider
            .Where(x => Path.GetFileName(x.Path).Equals("UnoImage.inputs", StringComparison.InvariantCultureIgnoreCase))
            .Select((additionalText, cancellationToken) =>
            {
                var sourceText = additionalText.GetText(cancellationToken);
                return FindAppIconFile(sourceText.ToString());
            })
            .Where(x => !string.IsNullOrEmpty(x))
            .Select((x, _) => Path.GetFileNameWithoutExtension(x));

        // Define the source generator logic
        var sourceCodeProvider = iconNameProvider
            .Combine(extensionPropertiesProvider)
            .Select((x, _) =>
            {
                var (iconName, coreContext) = x;
                if (string.IsNullOrEmpty(iconName) || string.IsNullOrEmpty(coreContext?.RootNamespace) || 
                string.IsNullOrEmpty(coreContext?.WindowTitle))
                {
                    return null;
                }

                return new ExtensionGenerationContext(coreContext.RootNamespace, iconName, coreContext.WindowTitle);

            }).Where(result => result != null);

        // Register the source generator logic to add the generated source code
        context.RegisterSourceOutput(sourceCodeProvider, (sourceContext, extensionContext) =>
        {
            if (!string.IsNullOrEmpty(extensionContext.WindowTitle))
            {
                AddSource(sourceContext, GenerateWindowTitleExtension(extensionContext.RootNamespace, extensionContext.IconName, extensionContext.WindowTitle));
            }
        });
    }

    internal record ExtensionPropertiesContext(string RootNamespace, string WindowTitle);

    internal record ExtensionGenerationContext(string RootNamespace, string IconName, string WindowTitle);

    private static string FindAppIconFile(string content)
    {
        // Split the content into lines
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Split the line into key-value pairs
            var properties = line.Split(';')
                .Select(property => property.Split('='))
                .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : null);

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
            .AddNamespaceImport("System.ComponentModel")
            .AddClass("WindowExtensions")
            .AddAttribute("[EditorBrowsable(EditorBrowsableState.Never)]")
            .MakeStaticClass()
            .MakePublicClass()
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
            .AddNamespaceImport("System.ComponentModel")
            .AddClass("__LegacyResizetizerSupport__")
            .WithSummary("This is added to ensure the Uno.Resizetizer namespace is present to avoid breaking any applications.")
            .AddAttribute("[EditorBrowsable(EditorBrowsableState.Never)]")
            .MakePublicClass()
            .MakeStaticClass();
    }

    private static void AddSource(SourceProductionContext context, ClassBuilder builder) =>
        context.AddSource($"{builder.FullyQualifiedName}.g.cs", SourceText.From(builder.Build(), Encoding.UTF8));
}
