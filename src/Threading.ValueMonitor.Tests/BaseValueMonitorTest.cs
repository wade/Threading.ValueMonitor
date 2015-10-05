using System;
using System.Collections.Generic;
using System.Threading;
using Should;
using Threading.Helpers;

namespace Threading
{
	public abstract class BaseValueMonitorTest
	{
		private readonly bool _expectObjectDisposedException;
		private int _operationCount;

		protected BaseValueMonitorTest() : this(false)
		{
		}

		protected BaseValueMonitorTest(bool expectObjectDisposedException)
		{
			_expectObjectDisposedException = expectObjectDisposedException;
			Random = new Random();
			WorkDataDictionary = new Dictionary<TestItem, List<WorkDataItem>>();
		}

		protected ValueMonitor<Guid> GuidValueMonitor { get; set; }

		protected bool ObjectDisposedExceptionCaught { get; private set; }

		protected int OperationCount
		{
			get { return _operationCount; }
			set { _operationCount = value; }
		}

		protected Random Random { get; private set; }

		protected Dictionary<TestItem, List<WorkDataItem>> WorkDataDictionary { get; set; }

		protected void AssertWorkDataTimesAreCorrect()
		{
			// Assert - Compare critical section times. There must not be any overlap.
			foreach (var key in WorkDataDictionary.Keys)
			{
				var workDataItems = WorkDataDictionary[key];

				for (var i = 1; i < workDataItems.Count; ++i) // Note: Starting with the second item (index 1) intentionally.
				{
					var workDataItem1 = workDataItems[i - 1];
					var workDataItem2 = workDataItems[i];

					// Sanity check consistency in each work data item.
					workDataItem1.Time2.ShouldBeGreaterThanOrEqualTo(workDataItem1.Time1);
					workDataItem1.Time3.ShouldBeGreaterThanOrEqualTo(workDataItem1.Time2);

					workDataItem2.Time2.ShouldBeGreaterThanOrEqualTo(workDataItem2.Time1);
					workDataItem2.Time3.ShouldBeGreaterThanOrEqualTo(workDataItem2.Time2);

					// Ensure that the critical section time block (Time2 to Time3) of work data item 2
					// do not overlap with the critical section time block of work data item 1.
					workDataItem2.Time2.ShouldBeGreaterThanOrEqualTo(workDataItem1.Time3);
				}
			}
		}

		protected void WorkerMethod(TestItem testItem)
		{
			var dt1 = DateTime.Now;
			var dt2 = DateTime.Now;
			var threadId = Thread.CurrentThread.ManagedThreadId;
			var workDataItems = WorkDataDictionary[testItem];

			try
			{
				// Enter critical section
				GuidValueMonitor.Enter(testItem.Id);
				try
				{
					// Get the date-time once inside the critical section.
					dt2 = DateTime.Now;

					// Pause the worker thread for a random amount of time.
					var milliseconds = Random.Next(0, 100); // Zero to 100 milliseconds.
					if (milliseconds > 1)
						Thread.Sleep(milliseconds);

					// Do some work. Ensure the work is not atomic.
					testItem.MyValue++;
					testItem.MyValueText = testItem.MyValue.ToString().ToLowerInvariant().Trim();

					// Increment the operations count.
					Interlocked.Increment(ref _operationCount);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Unexpected Exception occurred:");
					Console.WriteLine(ex);
				}
				finally
				{
					var dt3 = DateTime.Now;

					workDataItems.Add(
						new WorkDataItem
						{
							ItemId = testItem.Id,
							ItemMyValue = testItem.MyValue,
							ItemMyValueText = testItem.MyValueText,
							ThreadId = threadId,
							Time1 = dt1,
							Time2 = dt2,
							Time3 = dt3
						});

					// Exit critical section
					GuidValueMonitor.Exit(testItem.Id);
				}
			}
			catch (ObjectDisposedException)
			{
				if (_expectObjectDisposedException)
				{
					ObjectDisposedExceptionCaught = true;
					return;
				}

				throw;
			}
		}

		protected void WriteWorkDataToConsole()
		{
			foreach (var key in WorkDataDictionary.Keys)
			{
				var workDataItems = WorkDataDictionary[key];
				foreach (var workDataItem in workDataItems)
				{
					Console.WriteLine(
						"Thread {0}; {1} - {2} - {3}; TestItem.Id = {4}; TestItem.MyValue = {5}; TestItem.MyValueText = {6}",
						workDataItem.ThreadId.ToString().PadLeft(2, ' '),
						workDataItem.Time1.ToString("ss.ffffff"),
						workDataItem.Time2.ToString("ss.ffffff"),
						workDataItem.Time3.ToString("ss.ffffff"),
						workDataItem.ItemId.ToString("n"),
						workDataItem.ItemMyValue.ToString().PadLeft(4, ' '),
						workDataItem.ItemMyValueText.PadLeft(4, ' ')
						);
				}
			}
		}
	}
}