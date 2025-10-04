// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CP.Collections;

/// <summary>
/// Represents a collection of key/value pairs that are organized based on the hash code of the key.
/// </summary>
[Serializable]
public class HashTable : IObservable<(string key, object? value)>, ICancelable, ICollection, IEnumerable
{
    // Fields
    private readonly Dictionary<string, object?> _dict = [];
    private readonly SingleAssignmentDisposable _subscription = new();
    private readonly IScheduler _scheduler;
    private bool _disposed;

    // Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class.
    /// </summary>
    public HashTable()
        : this(TaskPoolScheduler.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class with a scheduler.
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
    /// Initializes a new instance of the <see cref="HashTable"/> class with a source observable.
    /// </summary>
    /// <param name="source">The source.</param>
    public HashTable(IObservable<(string key, object? value)> source)
        : this(TaskPoolScheduler.Default, source)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class with a scheduler and source observable.
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

    // Properties
    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    public int Count => _dict.Count;

    /// <summary>
    /// Gets the keys.
    /// </summary>
    /// <value>
    /// The keys.
    /// </value>
    public string[] Keys => [.. _dict.Keys];

    /// <summary>
    /// Gets a value indicating whether access to the collection is synchronized (thread safe).
    /// </summary>
    public bool IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection.
    /// </summary>
    public object SyncRoot => ((ICollection)_dict).SyncRoot;

    /// <summary>
    /// Gets a value indicating whether the object is disposed.
    /// </summary>
    public bool IsDisposed => _subscription.IsDisposed;

    /// <summary>
    /// Gets the source observable.
    /// </summary>
    public IObservable<(string key, object? value)>? Source { get; private set; }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    protected ReplaySubject<(string key, object? value)> Subject { get; } = new(1);

    // Indexer
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value associated with the specified key, or null if not found.</returns>
    public object? this[object key]
    {
        get => _dict.TryGetValue(key?.ToString()!, out var value) ? value : null;
        set => _dict[key?.ToString()!] = value;
    }

    // Public Methods
    /// <summary>
    /// Adds an element with the specified key and value into the table.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Add(object key, object? value) => AddToBase((key?.ToString()!, value));

    /// <summary>
    /// Removes all elements from the table.
    /// </summary>
    public void Clear() => _scheduler?.Schedule(() => _dict.Clear());

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets an observable for the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>An observable sequence containing the key and value.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "No reflection used; simple observable wrapper around indexer.")]
    public IObservable<(string key, object value)> Get(object key)
    {
#if NETSTANDARD2_0
        return Observable.Start(() => this[key], _scheduler).Select(x => (key.ToString(), x!));
#else
        return Observable.Start(() => this[key], _scheduler).Select(x => (key.ToString()!, x!));
#endif
    }

    /// <summary>
    /// Removes the element with the specified key from the table.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    public void Remove(object key) => _scheduler.Schedule(() => _dict.Remove(key?.ToString()!));

    /// <summary>
    /// Subscribes the specified observer.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <returns>A disposable subscription.</returns>
    public IDisposable Subscribe(IObserver<(string key, object? value)> observer) => Source!.Subscribe(observer);

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection.</param>
    /// <param name="index">The zero-based index in array at which copying begins.</param>
    public void CopyTo(System.Array array, int index) => ((ICollection)_dict).CopyTo(array, index);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator GetEnumerator() => _dict.GetEnumerator();

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(object key) => _dict.ContainsKey(key?.ToString()!);

    // Protected Methods
    /// <summary>
    /// Releases the unmanaged resources used by the HashTable and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _subscription?.Dispose();
                Subject.Dispose();
            }

            _disposed = true;
        }
    }

    // Private Methods
    /// <summary>
    /// Adds a key/value pair to the dictionary and notifies observers.
    /// </summary>
    /// <param name="keyValuePair">The key/value pair.</param>
    private void AddToBase((string key, object? value) keyValuePair)
    {
        try
        {
            Subject.OnNext(keyValuePair);
            _dict.Add(keyValuePair.key, keyValuePair.value);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }

    /// <summary>
    /// Sets up the source observable.
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
