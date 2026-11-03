using Xunit;

namespace Uno.Resizetizer.Tests
{
	// ResizetizeImages_v0.TargetPlatform is static, so the tests that set or read it must not run in parallel
	[CollectionDefinition(Name)]
	public class TargetPlatformCollection
	{
		public const string Name = "TargetPlatform";
	}
}
