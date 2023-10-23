using System;
using GLib;
using Uno.UI.Runtime.Skia;

namespace Resizetizer.Extensions.Sample.Skia.Gtk;

public class Program
{
	public static void Main(string[] args)
	{
		ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
		{
			Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
			expArgs.ExitApplication = true;
		};

		var host = new Uno.UI.Runtime.Skia.Gtk.GtkHost(() => new AppHead());

		host.Run();
	}
}
