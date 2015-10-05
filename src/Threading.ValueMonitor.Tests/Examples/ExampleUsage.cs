using System;

namespace Threading.Examples
{
	public class ExampleUsage
	{
		private static readonly ValueMonitor<Guid> _entityIdLock = new ValueMonitor<Guid>();

		public void DoSomething(Guid entityId)
		{
			_entityIdLock.Enter(entityId);
			try
			{
				// Do work that requires synchronization here.
			}
			finally
			{
				_entityIdLock.Exit(entityId);
			}
		}
	}
}