using System;
using System.Collections.Generic;
using System.Threading;

namespace Threading
{
	/// <summary>
	/// Provides a mechanism that synchronizes thread access based on value granularity.
	/// </summary>
	/// <typeparam name="T">The type of the value on which the ValueMonitor is based.</typeparam>
	/// <remarks>
	/// The <see cref="ValueMonitor{T}"/> class is similar to the <see cref="Monitor" /> class but is scoped for a specific value of the specified type, <typeparamref name="T"/>.
	/// <para>
	/// The <see cref="System.Threading.Monitor"/> class is used as the underlying thread synchronization primitive that provides the actual thread synchronization mechanism.
	/// Unlike the Monitor class, which is static, the <see cref="ValueMonitor{T}"/> class is an instance class. This allows multiple, concurrent instances of the same type
	/// to be created for usages across different applications scopes as required.
	/// </para>
	/// <para>
	/// The <see cref="ValueMonitor{T}"/> class manages a dictionary of lock object instances, one lock instance for each unique value of type <typeparamref name="T"/>.
	/// This allows a block of code to by synchronized by value, such as an entity identifier, such that multiple threads may concurrently process items with differing values
	/// but will block when more than one of the same value is being processed concurrently. The ability to synchronize by value provides more granular locking and results
	/// in less thread blocking than using a typical single lock object approach, especially when the typical use case is that items with mostly differing values are being
	/// processed but it is still possible for items with the same value to be processed concurrently.
	/// </para>
	/// <para>
	/// The lock object instances that are maintained in the dictionary are not retained indefinitely as that would lead to a memory leak. Instead, each lock object instance
	/// uses a reference count to track the number of concurrent threads that are locking against each lock object instance. When a lock object instance's reference count is
	/// zero, it is removed from the dictionary when that final lock is released during the <see cref="Exit"/> method execution.
	/// </para>
	/// <para>
	/// The <see cref="ValueMonitor{T}"/> class implements <see cref="IDisposable"/> and when an instance is disposed, the <see cref="Enter"/> method cannot be called successfully
	/// and will result in a <see cref="ObjectDisposedException"/> being thrown while the <see cref="Exit"/> method is still safe to call, it does not perform any work because
	/// all lock object references have already been released. This allows existing locks to finish up cleanly without having an exception thrown when they call <see cref="Exit"/>
	/// after disposal.
	/// </para>
	/// </remarks>
	/// <example>
	/// public class ExampleUsage
	/// {
	/// 	private static readonly ValueMonitor&lt;Guid&gt; _entityIdLock = new ValueMonitor&lt;Guid&gt;();
	///
	/// 	public void DoSomething(Guid entityId)
	/// 	{
	/// 		_entityIdLock.Enter(entityId);
	/// 		try
	/// 		{
	/// 			// Do work that requires synchronization here.
	/// 		}
	/// 		finally
	/// 		{
	/// 			_entityIdLock.Exit(entityId);
	/// 		}
	/// 	}
	/// }
	/// </example>
	public class ValueMonitor<T> : IDisposable
	{
		private readonly Dictionary<T, Lock> _dictionary;
		private readonly object _dictionaryLock;
		private volatile bool _isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="ValueMonitor{T}"/> class.
		/// </summary>
		public ValueMonitor()
		{
			_dictionary = new Dictionary<T, Lock>();
			_dictionaryLock = new object();
			_isDisposed = false;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			lock (_dictionaryLock)
			{
				_dictionary.Clear();
			}

		}

		/// <summary>
		/// Acquires an exclusive lock on the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public void Enter(T value)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);

			Lock @lock;

			lock (_dictionaryLock)
			{
				if (false == _dictionary.TryGetValue(value, out @lock))
				{
					@lock = new Lock(value);
					_dictionary.Add(value, @lock);
				}
				Interlocked.Increment(ref @lock.ReferenceCount);

				//System.Diagnostics.Debug.WriteLine("***DEBUG: ValueMonitor<T> Increment Lock Reference Count {0} = {1}", value, @lock.ReferenceCount);
			}

			// Enter the lock.
			Monitor.Enter(@lock);
		}

		/// <summary>
		/// Releases an exclusive lock on the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public void Exit(T value)
		{
			if (_isDisposed)
				return;

			Lock @lock;

			lock (_dictionaryLock)
			{
				if (false == _dictionary.TryGetValue(value, out @lock))
				{
					//System.Diagnostics.Debug.WriteLine("***DEBUG: ValueMonitor<T>.Exit method could not find the lock in the dictionary. This should NEVER occur.");

					// There is nothing to do.
					// Throwing an exception here just proves to be annoying.
					return;
				}

				Interlocked.Decrement(ref @lock.ReferenceCount);

				//System.Diagnostics.Debug.WriteLine("***DEBUG: ValueMonitor<T> Decrement Lock Reference Count {0} = {1}", value, @lock.ReferenceCount);

				// If there are no references to the lock, remove it from the dictionary.
				// Not removing the lock from the dictionary can result in a memory leak
				// for long running applications and/or those with manu unique values.
				if (@lock.ReferenceCount < 1)
					_dictionary.Remove(value);
			}

			// Exit the lock.
			Monitor.Exit(@lock);
		}

		/// <summary>
		/// Gets the internal lock count.
		/// </summary>
		/// <value>
		/// The internal lock count.
		/// </value>
		/// <remarks>
		/// Gets the number of locks that are currently allocated.
		/// Each concurrent unique value is represented by one lock.
		/// </remarks>
		public int LockCount
		{
			get
			{
				if (_isDisposed)
					return 0;

				lock (_dictionaryLock)
				{
					return _dictionary.Count;
				}
			}
		}

		#region " Lock Class "

		/// <summary>
		/// The object on which an actual lock is taken by the <see cref="ValueMonitor{T}"/> class' underlying thread synchronization primitive.
		/// </summary>
		/// <seealso cref="ValueMonitor{T}"/>
		public class Lock
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="Lock"/> class.
			/// </summary>
			/// <param name="value">The value on which the lock is synchronizing.</param>
			internal Lock(T value)
			{
				ReferenceCount = 0;
				Value = value;
			}

			/// <summary>
			/// Tracks the number of lock references taken on the lock instance.
			/// </summary>
			internal int ReferenceCount;

			/// <summary>
			/// The value on which the lock is synchronizing.
			/// </summary>
			internal T Value;
		}

		#endregion
	}
}
