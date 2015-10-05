using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using Threading.Helpers;

namespace Threading
{
	[TestClass]
    public class ValueMonitorDisposalTest : BaseValueMonitorTest
    {
		// *** NOTE: This test class is designed to only have one test method.

		private const bool ExpectObjectDisposedException = true;

		public ValueMonitorDisposalTest() : base(ExpectObjectDisposedException)
		{
		}

	    [TestMethod]
	    public void Concurrent_Work_By_A_Single_Value_Is_Synchronized_And_Handles_Disposal_Correctly()
	    {
			// Arrange
			const int iterations = 100;
		    const int disposeAfterCount = 50;

			OperationCount = 0;
			GuidValueMonitor = new ValueMonitor<Guid>();

		    var testItem = new TestItem {Id = Guid.NewGuid(), MyValue = 0};

			WorkDataDictionary.Add(testItem, new List<WorkDataItem>(iterations));

		    var tasks = new Task[iterations];

			// Act
			for (var i = 0; i < iterations; ++i)
			{
				tasks[i] = Task.Run(() => WorkerMethod(testItem));

				if (i == (disposeAfterCount - 1))
				{
					for (var n = 0; n < disposeAfterCount; ++n)
						tasks[n].Wait();
					GuidValueMonitor.Dispose();
				}
			}

		    foreach (var task in tasks)
			    task.Wait();

			Console.WriteLine("Total Operations .........: {0}", OperationCount);
			Console.WriteLine("ValueMonitor.LockCount ...: {0}", GuidValueMonitor.LockCount);
			Console.WriteLine();
			WriteWorkDataToConsole();

			// Assert
			ObjectDisposedExceptionCaught.ShouldBeTrue();
			OperationCount.ShouldEqual(disposeAfterCount);
			GuidValueMonitor.LockCount.ShouldEqual(0);
			WorkDataDictionary[testItem].Count.ShouldEqual(OperationCount);
			AssertWorkDataTimesAreCorrect();
	    }
    }
}