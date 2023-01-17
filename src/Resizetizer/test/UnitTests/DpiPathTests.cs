using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class DpiPathTests
	{
		public class GetOriginal
		{
			[Theory]
			[InlineData("")]
			public void ReturnsCorrectFolder(string folder)
			{
				var paths = DpiPath.GetOriginal();

				Assert.Equal(folder, paths.Path);
			}

			[Theory]
			[InlineData(1)]
			public void ReturnsCorrectScale(decimal scale)
			{
				var paths = DpiPath.GetOriginal();

				Assert.Equal(scale, paths.Scale);
			}
		}
	}
}
