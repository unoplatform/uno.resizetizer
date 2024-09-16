using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Uno.Resizetizer;

internal abstract partial class SkiaSharpTools
{
    private delegate void SetDllImportResolverDelegate(Assembly assembly, Func<string, Assembly, DllImportSearchPath?, IntPtr> resolver);
    private delegate bool TryLoadDelegate(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, out IntPtr libHandle);

    private static bool _initialized;
    private static MethodInfo _setDllImportResolver;
    private static TryLoadDelegate _tryLoad;

    static SkiaSharpTools()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;

            InitializeWindowsSearchPaths();

            var isNetCore = Type.GetType("System.Runtime.Loader.AssemblyLoadContext") != null;

            if (isNetCore)
            {
                SetupResolver();
            }
            else
            {
                SetupWindows();
            }
        }
    }

    /// <remarks>
    /// Load libraries explicitly on Windows, as search paths may not be available when
    /// running inside VS msbuild nodes.
    /// </remarks>
    private static void SetupWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var runtimePath in GetRuntimesFolder())
            {
                if (Directory.Exists(runtimePath))
                {
                    foreach (var file in Directory.GetFiles(runtimePath, "*.dll"))
                    {
                        var r = LoadLibrary(file);
                    }
                }
            }
        }
    }

    /// <remarks>
    /// Initializes SkiaSharp in a netcore environment, where the assemblies are loaded 
    /// through the System.Runtime.InteropServices.NativeLibrary, but uses the system dll search paths.
    /// </remarks>
    private static void InitializeWindowsSearchPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var runtimePath in GetRuntimesFolder())
            {
                _ = AddDllDirectory(runtimePath);
            }
        }
    }

    private static void SetupResolver()
    {
        var dllImportResolverDelegateType = Type.GetType("System.Runtime.InteropServices.DllImportResolver")!;

        // We're building with netstandard 2.0 which does not provide those APIs, but we
        // know we're running on netcore. Let's use them through reflection.
        _setDllImportResolver = Type
            .GetType("System.Runtime.InteropServices.NativeLibrary")
            ?.GetMethod("SetDllImportResolver", [typeof(Assembly), dllImportResolverDelegateType]);

        _tryLoad = (TryLoadDelegate)Type
            .GetType("System.Runtime.InteropServices.NativeLibrary")
            ?.GetMethod("TryLoad", [typeof(string), typeof(Assembly), typeof(DllImportSearchPath?), typeof(IntPtr).MakeByRefType()])
            ?.CreateDelegate(typeof(TryLoadDelegate));

        if (_setDllImportResolver is not null && _tryLoad is not null)
        {
            var importResolverMethod = typeof(SkiaSharpTools).GetMethod(nameof(ImportResolver), BindingFlags.Static | BindingFlags.NonPublic);
            var importResolverDelegate = Delegate.CreateDelegate(dllImportResolverDelegateType, null, importResolverMethod!);

            _setDllImportResolver.Invoke(null, [typeof(SkiaSharp.SKAlphaType).Assembly, importResolverDelegate]);
        }
        else
        {
            throw new InvalidOperationException($"Unable to find System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver or TryLoad");
        }
    }

    private static string[] GetRuntimesFolder()
    {
        if (typeof(SkiaSharpTools).Assembly.Location is { } location
            && Path.GetDirectoryName(location) is { } directory)
        {
            var bitness = Environment.Is64BitProcess ? "x64" : "x86";
            var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : bitness;
            var ridOS = GetRidOS();

            return 
            [
                Path.Combine(directory, "runtimes", ridOS + "-" + arch, "native"),
                Path.Combine(directory, "runtimes", ridOS, "native")
            ];
        }
        else
        {
            throw new InvalidOperationException("Unable to get the tools assembly location");
        }
    }

    private static string GetRidOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }

        throw new NotSupportedException("This operating system is not supported");
    }

    static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr libHandle = IntPtr.Zero;
        var searchFlags = DllImportSearchPath.SafeDirectories
            | DllImportSearchPath.UserDirectories;

        if (libraryName.Equals("libHarfBuzzSharp", StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("libSkiaSharp", StringComparison.OrdinalIgnoreCase))
        {
            if (!NixTryLoad(libraryName, out libHandle) 
                && !_tryLoad(libraryName + ".dll", typeof(SkiaSharpTools).Assembly, searchFlags, out libHandle))
            {
                throw new InvalidOperationException($"Failed to load {libraryName}");
            }
        }

        bool NixTryLoad(string library, out IntPtr handle)
        {
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "dylib" : "so";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                foreach (var runtimePath in GetRuntimesFolder())
                {
                    IntPtr localDlOpen(string fileName)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            return Linux.dlopen(fileName, false);
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            return dlopen_macos(fileName, 0);
                        }
                        else
                        {
                            throw new NotSupportedException("This operating system is not supported");
                        }
                    }

                    handle = localDlOpen(Path.Combine(runtimePath, library + "." + extension));

                    if (handle != IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }

            handle = IntPtr.Zero;
            return false;
        }

        return libHandle;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int AddDllDirectory(string NewDirectory);

    [DllImport("libSystem.dylib", EntryPoint = "dlopen")]
    public static extern IntPtr dlopen_macos(string fileName, int flags);

    // Declare dllimport for loadlibrary
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    // Imported from https://github.com/mono/SkiaSharp/blob/482e6ee2913a08a7cad76520ccf5fbce97c7c23b/binding/Binding.Shared/LibraryLoader.cs
    private static class Linux
    {
        private const string SystemLibrary = "libdl.so";
        private const string SystemLibrary2 = "libdl.so.2"; // newer Linux distros use this

        private const int RTLD_LAZY = 1;
        private const int RTLD_NOW = 2;
        private const int RTLD_DEEPBIND = 8;

        public static IntPtr dlopen(string path, bool lazy = true)
        {
            try
            {
                return dlopen2(path, (lazy ? RTLD_LAZY : RTLD_NOW) | RTLD_DEEPBIND);
            }
            catch (DllNotFoundException)
            {
                return dlopen1(path, (lazy ? RTLD_LAZY : RTLD_NOW) | RTLD_DEEPBIND);
            }
        }

        [DllImport(SystemLibrary, EntryPoint = "dlopen")]
        private static extern IntPtr dlopen1(string path, int mode);
        [DllImport(SystemLibrary2, EntryPoint = "dlopen")]
        private static extern IntPtr dlopen2(string path, int mode);
    }

}
