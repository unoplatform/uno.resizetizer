using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Build.Framework;
using Xunit.Abstractions;

namespace Uno.Resizetizer.Tests
{
	public abstract class MSBuildTaskTestFixture<TTask> : BaseTest, IBuildEngine
		where TTask : Microsoft.Build.Framework.ITask, new()
	{
		private readonly ITestOutputHelper? _testOutputHelper;

		protected MSBuildTaskTestFixture() { }

		protected MSBuildTaskTestFixture(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}

		protected readonly TestLogger Logger;

		protected List<BuildErrorEventArgs> LogErrorEvents = new List<BuildErrorEventArgs>();
		protected List<BuildMessageEventArgs> LogMessageEvents = new List<BuildMessageEventArgs>();
		protected List<CustomBuildEventArgs> LogCustomEvents = new List<CustomBuildEventArgs>();
		protected List<BuildWarningEventArgs> LogWarningEvents = new List<BuildWarningEventArgs>();

		// IBuildEngine

		bool IBuildEngine.ContinueOnError => false;

		int IBuildEngine.LineNumberOfTaskNode => 0;

		int IBuildEngine.ColumnNumberOfTaskNode => 0;

		string IBuildEngine.ProjectFileOfTaskNode => $"Fake{GetType().Name}Project.proj";

		bool IBuildEngine.BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new NotImplementedException();

		void IBuildEngine.LogCustomEvent(CustomBuildEventArgs e)
		{
			_testOutputHelper?.WriteLine("CUSTOM - ({0}) {1}: {2}", e.SenderName, e.Timestamp, e.Message);
			LogCustomEvents.Add(e);
		}

		void IBuildEngine.LogErrorEvent(BuildErrorEventArgs e)
		{
			_testOutputHelper?.WriteLine("ERROR - ({0}) {1}: {2}", e.SenderName, e.Timestamp, e.Message);
			LogErrorEvents.Add(e);
		}

		void IBuildEngine.LogMessageEvent(BuildMessageEventArgs e)
		{
			_testOutputHelper?.WriteLine("LOG - ({0}) {1}: {2}", e.SenderName, e.Timestamp, e.Message);
			LogMessageEvents.Add(e);
		}

		void IBuildEngine.LogWarningEvent(BuildWarningEventArgs e)
		{
			_testOutputHelper?.WriteLine("WARNING - ({0}) {1}: {2}", e.SenderName, e.Timestamp, e.Message);
			LogWarningEvents.Add(e);
		}

		protected TTask CreateTask() =>
			new()
			{
				BuildEngine = this,
			};
	}
}