using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using Threading.Helpers;

namespace Threading
{
	[TestClass]
    public class ValueMonitorTestConcurrentWorkByMultipleValuesIsSynchronized : BaseValueMonitorTest
    {
		// *** NOTE: This test class is designed to only have one test method.
		// This test executes concurrent work across multiple values.
		// Concurrent work across threads for the same value will be synchronized,
		// but concurrent work for different values can be executed simultaneously
		// or can be overlapped.

	    [TestMethod]
	    public void Concurrent_Work_By_Multiple_Values_Is_Synchronized()
	    {
			// Arrange
			const int iterationsPerTestItem = 50;

			OperationCount = 0;
			GuidValueMonitor = new ValueMonitor<Guid>();

		    var testItems =
			    new List<TestItem>
			    {
				    new TestItem {Id = Guid.NewGuid(), MyValue = 0},
				    new TestItem {Id = Guid.NewGuid(), MyValue = 0},
					new TestItem {Id = Guid.NewGuid(), MyValue = 0},
				    new TestItem {Id = Guid.NewGuid(), MyValue = 0}
			    };

		    foreach (var testItem in testItems)
				WorkDataDictionary.Add(testItem, new List<WorkDataItem>(iterationsPerTestItem));

			var expectedOperationCount = iterationsPerTestItem * testItems.Count;

			var tasks = new List<Task>(expectedOperationCount);

			// Act
			for (var i = 0; i < iterationsPerTestItem; ++i)
			{
				for (var n = 0; n < testItems.Count; ++n)
				{
					var testItemIndex = n; // Avoid access to a modified closure.
					var task = Task.Run(() => WorkerMethod(testItems[testItemIndex]));
					tasks.Add(task);
				}
			}

		    foreach (var task in tasks)
			    task.Wait();

			Console.WriteLine("Total Operations .........: {0}", OperationCount);
			Console.WriteLine("ValueMonitor.LockCount ...: {0}", GuidValueMonitor.LockCount);
			Console.WriteLine();
		    WriteWorkDataToConsole();

			// Assert
			OperationCount.ShouldEqual(expectedOperationCount);
			GuidValueMonitor.LockCount.ShouldEqual(0);

		    foreach (var key in WorkDataDictionary.Keys)
		    {
			    var workDataItems = WorkDataDictionary[key];
				workDataItems.Count.ShouldEqual(iterationsPerTestItem);
		    }

			AssertWorkDataTimesAreCorrect();
	    }
    }
}