using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class UnoResizetizerTargetsTests
	{
		[Fact]
		public void ForegroundFilesAreInputsToImageResizetization()
		{
			var repositoryRoot = FindRepositoryRoot();
			var targetsPath = Path.Combine(repositoryRoot, "src", ".nuspec", "Uno.Resizetizer.targets");
			var document = XDocument.Load(targetsPath);
			var target = document.Descendants(document.Root.Name.Namespace + "Target")
				.Single(element => (string)element.Attribute("Name") == "UnoResizetizeImages");
			var inputs = (string)target.Attribute("Inputs");

			Assert.Contains("@(UnoIcon->'%(ForegroundFile)')", inputs);
		}

		private static string FindRepositoryRoot()
		{
			for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
			{
				if (File.Exists(Path.Combine(directory.FullName, "src", ".nuspec", "Uno.Resizetizer.targets")))
				{
					return directory.FullName;
				}
			}

			throw new DirectoryNotFoundException("Could not locate the repository root.");
		}
	}
}
