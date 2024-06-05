// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CP.Collections;

/// <summary>
/// Represents a collection of key/value pairs that are organized based on the hash code of the key.
/// </summary>
[Serializable]
public class HashTable : Hashtable, IObservable<(string key, object? value)>, ICancelable
{
    private readonly SingleAssignmentDisposable _subscription = new();
    private readonly IScheduler _scheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class.
    /// </summary>
    public HashTable()
        : this(TaskPoolScheduler.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    public HashTable(IScheduler scheduler)
    {
        _scheduler = scheduler;
        SetupSource();
        try
        {
            _subscription.Disposable = Source?.ObserveOn(scheduler).Subscribe(AddToBase);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class.
    /// </summary>
    /// <param name="source">The source.</param>
    public HashTable(IObservable<(string key, object? value)> source)
        : this(TaskPoolScheduler.Default, source)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="source">The source.</param>
    public HashTable(IScheduler scheduler, IObservable<(string key, object? value)> source)
    {
        _scheduler = scheduler;
        Source = source;
        try
        {
            _subscription.Disposable = Source.ObserveOn(scheduler).Subscribe(AddToBase);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="HashTable"/> class.
    /// </summary>
    ~HashTable()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets the source.
    /// </summary>
    /// <value>The source.</value>
    public IObservable<(string key, object? value)>? Source { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the object is disposed.
    /// </summary>
    public bool IsDisposed => _subscription.IsDisposed;

    /// <summary>
    /// Gets the subject.
    /// </summary>
    protected ReplaySubject<(string key, object? value)> Subject { get; } = new(1);

    /// <summary>
    /// Adds an element with the specified key and value into the <see cref="Hashtable"/>.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add. The value can be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the <see cref="Hashtable"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The <see cref="Hashtable"/> is read-only.-or- The <see
    /// cref="Hashtable"/> has a fixed size.
    /// </exception>
    public override void Add(object key, object? value) =>
        AddToBase((key?.ToString()!, value));

    /// <summary>
    /// Removes all elements from the <see cref="Hashtable"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// The <see cref="Hashtable"/> is read-only.
    /// </exception>
    public override void Clear() => _scheduler?.Schedule(base.Clear);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get(indexer get) called on scheduler, IObservable length is one.
    /// </summary>
    /// <param name="key">The index.</param>
    /// <returns>A Observable.</returns>
    public IObservable<(string key, object value)> Get(object key) =>
#if NETSTANDARD2_0
        Observable.Start(() => this[key], _scheduler).Select(x => (key.ToString(), x!));
#else
        Observable.Start(() => this[key], _scheduler).Select(x => (key.ToString()!, x!));
#endif

    /// <summary>
    /// Removes the element with the specified key from the <see cref="Hashtable"/>.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    /// The <see cref="Hashtable"/> is read-only.-or- The <see
    /// cref="Hashtable"/> has a fixed size.
    /// </exception>
    public override void Remove(object key) =>
        _scheduler.Schedule(() => base.Remove(key));

    /// <summary>
    /// Subscribes the specified observer.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <returns>A Disposable.</returns>
    public IDisposable Subscribe(IObserver<(string key, object? value)> observer) =>
        Source!.Subscribe(observer);

    /// <summary>
    /// Disposes the specified disposing.
    /// </summary>
    /// <param name="disposing">if set to <c>true</c> [disposing].</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscription?.Dispose();
            Subject.Dispose();
        }
    }

    /// <summary>
    /// Adds to base.
    /// </summary>
    /// <param name="keyValuePair">The key value pair.</param>
    private void AddToBase((string key, object? value) keyValuePair)
    {
        try
        {
            Subject.OnNext(keyValuePair);
            base.Add(keyValuePair.key, keyValuePair.value);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }

    /// <summary>
    /// Setup the source.
    /// </summary>
    private void SetupSource()
    {
        try
        {
            if (Source != null)
            {
                var merge = Source.Merge(Subject);
                Source = merge.Publish();
            }
            else
            {
                var merge = Subject;
                Source = merge.Publish();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }
}
