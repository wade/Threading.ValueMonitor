# Threading.ValueMonitor #

Threading.ValueMonitor is a .NET library that contains the `Threading.ValueMonitor<T>` generic class which provides a mechanism that synchronizes thread access to a block of code based on value granularity. 


## NuGet Package ##

This library is available from the NuGet Gallery as the **Threading.ValueMonitor** package.

To install **Threading.ValueMonitor**, run the following command in the Package Manager Console

    Install-Package Threading.ValueMonitor 

The package currently provides a version built against the Microsoft .NET Framework 4.5.


## Details ##

The `ValueMonitor<T>` class is similar to the .NET Framework's `System.Threading.Monitor` class but is scoped for a specific value of the specified type, `T`. The `System.Threading.Monitor` class is used as the underlying thread synchronization primitive that provides the actual thread synchronization mechanism. Unlike the `System.Threading.Monitor` class, which is static, the `ValueMonitor<T>` class is an instance class. This allows multiple, concurrent instances of the same type to be created for usages across different applications scopes as required.

The `ValueMonitor<T>` class manages a dictionary of lock object instances, one lock instance for each unique value of type `T`. This allows a block of code to by synchronized by value, such as an entity identifier, such that multiple threads may concurrently process items with differing values but will block when more than one of the same value is being processed concurrently. The ability to synchronize by value provides more granular locking and results in less thread blocking than using a typical single lock object approach, especially when the typical use case is that items with mostly differing values are being processed but it is still possible for items with the same value to be processed concurrently.

The lock object instances that are maintained in the dictionary are not retained indefinitely as that would lead to a memory leak. Instead, each lock object instance uses a reference count to track the number of concurrent threads that are locking against each lock object instance. When a lock object instance's reference count is zero, it is removed from the dictionary when that final lock is released during the `Exit` method execution.

The `ValueMonitor<T>` class implements `System.IDisposable` and when an instance is disposed, the `Enter` method cannot be called successfully and will result in a `System.ObjectDisposedException` being thrown while the `Exit` method is still safe to call, it does not perform any work because all lock object references have already been released. This allows existing locks to finish up cleanly without having an exception thrown when they call `Exit` after disposal.


## Example Usage, C# ##

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

