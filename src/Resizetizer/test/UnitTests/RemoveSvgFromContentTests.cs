using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class RemoveSvgFromContentTests : MSBuildTaskTestFixture<RemoveSvgFromContentTask_v0>
	{
		RemoveSvgFromContentTask_v0 GetNewTask(ITaskItem[] collectionToRemove, ITaskItem[] unoImages)
		{
			return new()
			{
				CollectionToRemove = collectionToRemove,
				UnoImages = unoImages
			};
		}


		[Fact]
		public void RemoveSvgContentIfItIsUnoImage()
		{
			var assetPath = "Assets/Icons/back.svg";
			var unoImage = new TaskItem(assetPath);
			var content = new TaskItem(assetPath);

			var task = GetNewTask(new[] { content }, new[] { unoImage });

			var success = task.Execute();


			Assert.True(success);
			Assert.True(task.RemovedItems.Length > 0);
		}

		[Fact]
		public void DoNotRemoveSvgIfHasSameNameButInDifferentLocation()
		{
			var unoImage = new TaskItem("Assets/Icons/back.svg");
			var content = new TaskItem("Assets/SVG/back.svg");

			var task = GetNewTask(new[] { content }, new[] { unoImage });

			var success = task.Execute();


			Assert.True(success);
			Assert.True(task.RemovedItems.Length is 0);
		}
	}
}
