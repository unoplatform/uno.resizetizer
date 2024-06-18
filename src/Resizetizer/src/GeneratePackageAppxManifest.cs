#nullable enable
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Uno.Resizetizer
{
	public class GeneratePackageAppxManifest_v0 : Task
	{
		const string DefaultPlaceholder = "$placeholder$";
		const string PngPlaceholder = "$placeholder$.png";
		const string PackageVersionPlaceholder = "0.0.0.0";
		const string ImageExtension = ".png";

		const string UapNamespace = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

		const string ErrorVersionNumberCombination = "ApplicationDisplayVersion '{0}' was not a valid 3 part semver version number and/or ApplicationVersion '{1}' was not a valid integer.";

		[Required]
		public string IntermediateOutputPath { get; set; } = null!;

		[Required]
		public ITaskItem[] AppxManifest { get; set; } = Array.Empty<ITaskItem>();

		public string? TargetFramework { get; set; }

		public string? GeneratedFilename { get; set; }

		public string? ApplicationId { get; set; }

		public string? ApplicationDisplayVersion { get; set; }

		public string? ApplicationVersion { get; set; }

		public string? ApplicationTitle { get; set; }

		public string? AssemblyName { get; set; }

		public string? Authors { get; set; }

		public string? ApplicationPublisher { get; set; }

		public string? Description { get; set; }

		public string? TargetPlatformVersion { get; set; }

		public string? TargetPlatformMinVersion { get; set; }

		public ITaskItem[]? AppIcon { get; set; }

		public ITaskItem[]? SplashScreen { get; set; }

		[Output]
		public ITaskItem GeneratedAppxManifest { get; set; } = null!;

		[Output]
		public string DisplayName { get; private set; } = null!;

		public override bool Execute()
		{
#if DEBUG_RESIZETIZER
			//System.Diagnostics.Debugger.Launch();
#endif
			try
			{
				ApplicationTitle ??= AssemblyName;
				ApplicationId ??= AssemblyName;
				Description ??= ApplicationTitle;
				Authors ??= ApplicationTitle;
				ApplicationPublisher ??= Authors;

				ResizetizeImages_v0._TargetFramework = TargetFramework;
				Directory.CreateDirectory(IntermediateOutputPath);

				var filename = Path.Combine(IntermediateOutputPath, GeneratedFilename ?? "Package.appxmanifest");

				if (AppxManifest.Length > 1)
				{
					Log.LogWarning("Multiple AppxManifest files were provided. Only the first one will be used.");
				}

				FileHelper.WriteFileIfChanged(
					filename,
					Log,
					writer =>
					{
						var appx = XDocument.Load(AppxManifest[0].ItemSpec);

						UpdateManifest(appx);

						// Define the settings for the XmlWriter
						var settings = new XmlWriterSettings
						{
							Indent = true,
							Encoding = writer.Encoding,
							OmitXmlDeclaration = false // Ensure the XML declaration is included
						};

						using var xmlWriter = XmlWriter.Create(writer, settings);
						appx.Save(xmlWriter);
					});

				GeneratedAppxManifest = new TaskItem(filename);
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}

			return !Log.HasLoggedErrors;
		}

		void UpdateManifest(XDocument appx)
		{
			var appIconInfo = AppIcon?.Length > 0 ? ResizeImageInfo.Parse(AppIcon[0]) : null;
			var splashInfo = SplashScreen?.Length > 0 ? ResizeImageInfo.Parse(SplashScreen[0]) : null;

			var uapXmlns = appx.Root.Attributes()
				.Where(a => a.IsNamespaceDeclaration && a.Value == UapNamespace)
				.Select(a => XNamespace.Get(a.Value))
				.FirstOrDefault();
			var xmlns = appx.Root!.GetDefaultNamespace();

			// <Identity Name="" Version="" />
			// <Identity>
			var xidentity = xmlns + "Identity";
			var identity = appx.Root.Element(xidentity);
			if (identity == null)
			{
				identity = new XElement(xidentity);
				appx.Root.Add(identity);
			}

			// Name=""
			UpdateIdentityName(identity);

			// Publisher=""
			UpdateIdentityPublisher(identity);

			// Version=""
			UpdateIdentityVersion(identity);

			if (!string.IsNullOrEmpty(TargetPlatformVersion) && !string.IsNullOrEmpty(TargetPlatformMinVersion))
			{
				SetDependencyTargetDeviceFamily(appx, TargetPlatformVersion!, TargetPlatformMinVersion!);
			}

			// <Properties>
			//   <DisplayName />
			//   <Logo />
			// </Properties>
			var xproperties = xmlns + "Properties";
			var properties = appx.Root.Element(xproperties);
			if (properties == null)
			{
				properties = new XElement(xproperties);
				appx.Root.Add(properties);
			}

			UpdatePropertiesDisplayName(properties, xmlns);
			UpdatePropertiesPublisherDisplayName(properties, xmlns);
			UpdatePropertiesLogo(properties, xmlns, appIconInfo);

			// <Applications>
			//   <Application>
			//     <uap:VisualElements DisplayName="" Description="" BackgroundColor="" Square150x150Logo="" Square44x44Logo="">
			//       <uap:DefaultTile Wide310x150Logo="" Square71x71Logo="" Square310x310Logo="" ShortName="">
			//         <uap:ShowNameOnTiles>
			//           <uap:ShowOn />
			//         </uap:ShowNameOnTiles>
			//       </uap:DefaultTile>
			//       <uap:SplashScreen Image="" />
			//     </uap:VisualElements>
			//   </Application>
			// </Applications>
			var xapplications = xmlns + "Applications";
			var applications = appx.Root.Element(xapplications);
			if (applications == null)
			{
				applications = new XElement(xapplications);
				appx.Root.Add(applications);
			}

			// <Application>
			var xapplication = xmlns + "Application";
			var application = applications.Element(xapplication);
			if (application == null)
			{
				application = new XElement(xapplication);
				applications.Add(application);
			}

			// <uap:VisualElements>
			var xvisual = uapXmlns + "VisualElements";
			var visual = application.Element(xvisual);
			if (visual == null)
			{
				visual = new XElement(xvisual);
				application.Add(visual);
			}

			// <uap:DefaultTile>
			var xtile = uapXmlns + "DefaultTile";
			var tile = visual.Element(xtile);
			if (tile == null)
			{
				tile = new XElement(xtile);
				visual.Add(tile);
			}

			// <uap:ShowNameOnTiles>
			var xshowname = uapXmlns + "ShowNameOnTiles";
			var showname = tile.Element(xshowname);
			if (showname == null)
			{
				showname = new XElement(xshowname);
				tile.Add(showname);
			}

			if (!string.IsNullOrEmpty(ApplicationTitle))
			{
				UpdateVisualElementsDisplayName(visual);
				UpdateVisualElementsDescription(visual);

				UpdateDefaultTileShortName(tile);
			}

			if (appIconInfo != null)
			{
				UpdateVisualElementsBackground(visual, appIconInfo);
				UpdateVisualElementsSquare150Logo(visual, appIconInfo);
				UpdateVisualElementsSquare44Logo(visual, appIconInfo);

				UpdateDefaultTileWide310Logo(tile, appIconInfo);
				UpdateDefaultTileSquare71Logo(tile, appIconInfo);
				UpdateDefaultTileSquare310Logo(tile, appIconInfo);

				// <ShowOn>
				var xshowon = xmlns + "ShowOn";
				var showons = showname.Elements(xshowon).ToArray();
				if (showons.All(x => x.Attribute("Tile")?.Value != "square150x150Logo"))
				{
					showname.Add(new XElement(xshowon, new XAttribute("Tile", "square150x150Logo")));
				}

				if (showons.All(x => x.Attribute("Tile")?.Value != "wide310x150Logo"))
				{
					showname.Add(new XElement(xshowon, new XAttribute("Tile", "wide310x150Logo")));
				}
			}

			if (splashInfo != null)
			{
				// <uap:SplashScreen>
				var xsplash = uapXmlns + "SplashScreen";
				var splash = visual.Element(xsplash);
				if (splash == null)
				{
					splash = new XElement(xsplash);
					visual.Add(splash);
				}

				UpdateSplashScreenImage(splash, splashInfo);
				UpdateSplashScreenBackgroundColor(splash, splashInfo);
			}
		}

		private void UpdateIdentityName(XElement identity)
		{
			if (!string.IsNullOrEmpty(ApplicationId))
			{
				var xname = "Name";
				var attr = identity.Attribute(xname);
				if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == DefaultPlaceholder)
				{
					identity.SetAttributeValue(xname, ApplicationId);
				}
			}
		}

		private void UpdateIdentityPublisher(XElement identity)
		{
			if (!string.IsNullOrEmpty(ApplicationPublisher))
			{
				var xpublisher = "Publisher";
				var attr = identity.Attribute(xpublisher);
				if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == DefaultPlaceholder)
				{
					identity.SetAttributeValue(xpublisher, $"O={ApplicationPublisher}");
				}
			}
		}

		private void UpdateIdentityVersion(XElement identity)
		{
			ApplicationDisplayVersion ??= "1.0.0";
			ApplicationVersion ??= "1";
			var xname = "Version";
			var attr = identity.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PackageVersionPlaceholder)
			{
				if (!TryMergeVersionNumbers(ApplicationDisplayVersion, ApplicationVersion, out var finalVersion))
				{
					Log.LogError(ErrorVersionNumberCombination, ApplicationDisplayVersion, ApplicationVersion);
					return;
				}

				identity.SetAttributeValue(xname, finalVersion);
			}
		}

		private void UpdatePropertiesDisplayName(XElement properties, XNamespace xmlns)
		{
			// <DisplayName>
			var xdisplayname = xmlns + "DisplayName";
			if (!string.IsNullOrEmpty(ApplicationTitle))
			{
				var xelem = properties.Element(xdisplayname);
				if (xelem == null || string.IsNullOrEmpty(xelem.Value) || xelem.Value == DefaultPlaceholder)
				{
					properties.SetElementValue(xdisplayname, ApplicationTitle);
				}
			}

			DisplayName = properties.Element(xdisplayname).Value;
		}

		private void UpdatePropertiesPublisherDisplayName(XElement properties, XNamespace xmlns)
		{
			// <PublisherDisplayName>
			var xpublisherDisplayName = xmlns + "PublisherDisplayName";
			if (!string.IsNullOrEmpty(ApplicationPublisher))
			{
				var xelem = properties.Element(xpublisherDisplayName);
				if (xelem == null || string.IsNullOrEmpty(xelem.Value) || xelem.Value == DefaultPlaceholder)
				{
					properties.SetElementValue(xpublisherDisplayName, ApplicationPublisher);
				}
			}
		}

		private void UpdatePropertiesLogo(XElement properties, XNamespace xmlns, ResizeImageInfo? appIconInfo)
		{
			// <Logo>
			if (appIconInfo != null)
			{
				var xname = xmlns + "Logo";
				var xelem = properties.Element(xname);
				if (xelem == null || string.IsNullOrEmpty(xelem.Value) || xelem.Value == PngPlaceholder)
				{
					var dpi = DpiPath.Windows.StoreLogo[0];
					var path = Path.Combine(dpi.Path + appIconInfo.OutputPath, appIconInfo.OutputName + dpi.NameSuffix + ImageExtension);
					properties.SetElementValue(xname, path);
				}
			}
		}

		private void UpdateVisualElementsDisplayName(XElement visual)
		{
			// DisplayName=""
			var xname = "DisplayName";
			var attr = visual.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == DefaultPlaceholder)
			{
				visual.SetAttributeValue(xname, ApplicationTitle);
			}
		}

		private void UpdateVisualElementsDescription(XElement visual)
		{
			// Description=""
			var xname = "Description";
			var attr = visual.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == DefaultPlaceholder)
			{
				visual.SetAttributeValue(xname, Description);
			}
		}

		private static void UpdateVisualElementsBackground(XElement visual, ResizeImageInfo appIconInfo)
		{
			// BackgroundColor=""
			var xname = "BackgroundColor";
			var attr = visual.Attribute(xname);

			if (attr is null || string.IsNullOrEmpty(attr.Value))
			{
				if (appIconInfo?.Color is not null)
				{
					visual.SetAttributeValue(xname, Utils.SkiaColorWithoutAlpha(appIconInfo.Color));
				}
			}
		}

		private static void UpdateVisualElementsSquare150Logo(XElement visual, ResizeImageInfo appIconInfo)
		{
			// Square150x150Logo=""
			var xname = "Square150x150Logo";
			var attr = visual.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PngPlaceholder)
			{
				var dpi = DpiPath.Windows.MediumTile[0];
				var path = Path.Combine(dpi.Path + appIconInfo.OutputPath, appIconInfo.OutputName + dpi.NameSuffix + ImageExtension);
				visual.SetAttributeValue(xname, path);
			}
		}

		private static void UpdateVisualElementsSquare44Logo(XElement visual, ResizeImageInfo appIconInfo)
		{
			// Square44x44Logo=""
			var xname = "Square44x44Logo";
			var attr = visual.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PngPlaceholder)
			{
				var dpi = DpiPath.Windows.Logo[0];
				var path = Path.Combine(dpi.Path + appIconInfo.OutputPath, appIconInfo.OutputName + dpi.NameSuffix + ImageExtension);
				visual.SetAttributeValue(xname, path);
			}
		}

		private static void UpdateDefaultTileWide310Logo(XElement tile, ResizeImageInfo appIconInfo)
		{
			// Wide310x150Logo=""
			var xname = "Wide310x150Logo";
			var attr = tile.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PngPlaceholder)
			{
				var dpi = DpiPath.Windows.WideTile[0];
				var path = Path.Combine(dpi.Path + appIconInfo.OutputPath, appIconInfo.OutputName + dpi.NameSuffix + ImageExtension);
				tile.SetAttributeValue(xname, path);
			}
		}

		private static void UpdateDefaultTileSquare71Logo(XElement tile, ResizeImageInfo appIconInfo)
		{
			// Square71x71Logo=""
			var xname = "Square71x71Logo";
			var attr = tile.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PngPlaceholder)
			{
				var dpi = DpiPath.Windows.SmallTile[0];
				var path = Path.Combine(dpi.Path + appIconInfo.OutputPath, appIconInfo.OutputName + dpi.NameSuffix + ImageExtension);
				tile.SetAttributeValue(xname, path);
			}
		}

		private static void UpdateDefaultTileSquare310Logo(XElement tile, ResizeImageInfo appIconInfo)
		{
			// Square310x310Logo=""
			var xname = "Square310x310Logo";
			var attr = tile.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PngPlaceholder)
			{
				var dpi = DpiPath.Windows.LargeTile[0];
				var path = Path.Combine(dpi.Path + appIconInfo.OutputPath, appIconInfo.OutputName + dpi.NameSuffix + ImageExtension);
				tile.SetAttributeValue(xname, path);
			}
		}

		private void UpdateDefaultTileShortName(XElement tile)
		{
			// ShortName=""
			var xname = "ShortName";
			var attr = tile.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value))
			{
				tile.SetAttributeValue(xname, ApplicationTitle);
			}
		}

		private static void UpdateSplashScreenImage(XElement splash, ResizeImageInfo splashInfo)
		{
			// Image=""
			var xname = "Image";
			var attr = splash.Attribute(xname);
			if (attr == null || string.IsNullOrEmpty(attr.Value) || attr.Value == PngPlaceholder)
			{
				var dpi = DpiPath.Windows.SplashScreen[0];
				var path = Path.Combine(dpi.Path, splashInfo.OutputName + dpi.NameSuffix + ImageExtension);
				splash.SetAttributeValue(xname, path);
			}
		}

		private static void UpdateSplashScreenBackgroundColor(XElement splash, ResizeImageInfo splashInfo)
		{
			// BackgroundColor=""
			var xname = "BackgroundColor";
			var attr = splash.Element(xname);

			if (splashInfo?.Color is not null && (attr is null || string.IsNullOrEmpty(attr.Value)))
			{
				splash.SetAttributeValue(xname, Utils.SkiaColorWithoutAlpha(splashInfo.Color));
			}
		}

		private static void SetDependencyTargetDeviceFamily(XDocument appx, string targetPlatformVersion, string targetPlatformMinVersion)
		{
			var xmlns = appx.Root!.GetDefaultNamespace();
			var xdependencies = xmlns + "Dependencies";
			var dependencies = appx.Root.Element(xdependencies);
			if (dependencies is null)
			{
				dependencies = new XElement(xdependencies);
				appx.Root.Add(dependencies);
			}

			var targetDeviceFamilyElements = dependencies.Elements().Where(x => x.Name == xmlns + "TargetDeviceFamily");
			if (targetDeviceFamilyElements is null || !targetDeviceFamilyElements.Any())
			{
				var universal = new XElement(xmlns + "TargetDeviceFamily");
				universal.SetAttributeValue(xmlns + "Name", "Windows.Universal");

				var desktop = new XElement(xmlns + "TargetDeviceFamily");
				desktop.SetAttributeValue(xmlns + "Name", "Windows.Desktop");

				dependencies.Add(universal, desktop);
				targetDeviceFamilyElements = [universal, desktop];
			}

			foreach (var target in targetDeviceFamilyElements)
			{
				SetVersion(target, xmlns + "MinVersion", targetPlatformMinVersion);
				SetVersion(target, xmlns + "MaxVersionTested", targetPlatformVersion);
			}
		}

		private static void SetVersion(XElement target, XName attributeName, string version)
		{
			var attr = target.Attributes().FirstOrDefault(x => IsVersionAttribute(x, attributeName));
			if (attr is null || string.IsNullOrEmpty(attr.Value) || attr.Value == PackageVersionPlaceholder)
			{
				target.SetAttributeValue(attributeName, version);
			}
		}

		static bool IsVersionAttribute(XAttribute attribute, XName attributeName)
		{
			var currentAttributeName = attribute.Name.LocalName;
			var expectedAttributeName = attributeName.LocalName;

			var currentAttributeNamespace = attribute.Name.Namespace.NamespaceName;
			var expectedAttributeNamespace = attributeName.NamespaceName;

			// The Version may not have a current Namespace and should use the default namespace
			if (string.IsNullOrEmpty(currentAttributeNamespace))
				return currentAttributeName == expectedAttributeName;

			return currentAttributeName == expectedAttributeName && currentAttributeNamespace == expectedAttributeNamespace;
		}

		public static bool TryMergeVersionNumbers(string? displayVersion, string? version, out string? finalVersion)
		{
			displayVersion = displayVersion?.Trim();
			version = version?.Trim();
			finalVersion = null;

			// either a 4 part display version and no version or a 3 part display and an int version
			var parts = displayVersion?.Split('.') ?? Array.Empty<string>();
			if (parts.Length > 3 && !string.IsNullOrEmpty(version))
			{
				return false;
			}
			else if (parts.Length > 4)
			{
				return false;
			}

			var v = new int[4];
			for (var i = 0; i < 4 && i < parts.Length; i++)
			{
				if (!int.TryParse(parts[i], out var parsed))
				{
					return false;
				}

				v[i] = parsed;
			}

			if (!string.IsNullOrEmpty(version))
			{
				if (!int.TryParse(version, out var parsed))
				{
					return false;
				}

				v[3] = parsed;
			}

			finalVersion = $"{v[0]:0}.{v[1]:0}.{v[2]:0}.{v[3]:0}";
			return true;
		}
	}
}