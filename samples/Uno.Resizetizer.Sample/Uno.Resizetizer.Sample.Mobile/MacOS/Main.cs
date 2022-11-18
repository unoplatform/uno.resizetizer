using AppKit;

namespace Uno.Resizetizer.Sample.macOS;
internal static class MainClass
{
	static void Main(string[] args)
	{
		NSApplication.Init();
		NSApplication.SharedApplication.Delegate = new App();
		NSApplication.Main(args);
	}
}

