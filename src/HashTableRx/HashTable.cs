// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Represents a reactive collection of key/value pairs keyed by their string representation.</summary>
[Serializable]
public class HashTable : IObservable<(string key, object? value)>, IDisposable, ICollection, IEnumerable
{
    /// <summary>Stores current table values by key.</summary>
    private readonly Dictionary<string, object?> _dictionary = [];

    /// <summary>Holds the current source subscription.</summary>
    private readonly AssignmentSlot _subscription = new();

    /// <summary>Schedules source and public clear operations.</summary>
    private readonly ISequencer _scheduler;

    /// <summary>Tracks whether managed resources have been released.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="HashTable"/> class.</summary>
    public HashTable()
#if REACTIVE_SHIM
        : this(Scheduler.Default)
#else
        : this(Sequencer.Default)
#endif
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HashTable"/> class with a scheduler.</summary>
    /// <param name="scheduler">The scheduler.</param>
    public HashTable(ISequencer scheduler)
    {
        _scheduler = scheduler;
        Source = Subject;
    }

    /// <summary>Initializes a new instance of the <see cref="HashTable"/> class with a source observable.</summary>
    /// <param name="source">The source.</param>
    public HashTable(IObservable<(string key, object? value)> source)
#if REACTIVE_SHIM
        : this(Scheduler.Default, source)
#else
        : this(Sequencer.Default, source)
#endif
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HashTable"/> class with a scheduler and source observable.</summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="source">The source.</param>
    public HashTable(ISequencer scheduler, IObservable<(string key, object? value)> source)
    {
        _scheduler = scheduler;
        Source = Subject;
        try
        {
            _subscription.Create(
                source.Subscribe(
                    new Observer<(string key, object? value)>(
                        value => _ = _scheduler.Schedule(() => AddToBase(value)),
                        ex => Trace.TraceWarning(ex.ToString()),
                        () => { })));
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }

    /// <summary>Gets the number of elements contained in the collection.</summary>
    public int Count => _dictionary.Count;

    /// <summary>Gets a value indicating whether access to the collection is synchronized.</summary>
    public bool IsSynchronized => false;

    /// <summary>Gets a value indicating whether the object is disposed.</summary>
    public bool IsDisposed => _subscription.IsDisposed;

    /// <summary>Gets a snapshot of the keys.</summary>
    public string[] Keys => [.. _dictionary.Keys];

    /// <summary>Gets the source observable.</summary>
    public IObservable<(string key, object? value)> Source { get; }

    /// <summary>Gets an object that can be used to synchronize access to the collection.</summary>
    public object SyncRoot => ((ICollection)_dictionary).SyncRoot;

    /// <summary>Gets the signal used to publish table changes.</summary>
    protected ReplaySignal<(string key, object? value)> Subject { get; } = new(1);

    /// <summary>Gets or sets the value associated with the specified key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value associated with the specified key, or null if not found.</returns>
    public object? this[object key]
    {
        get => GetBaseValue(key);
        set => SetBaseValue(key, value);
    }

    /// <summary>Adds an element with the specified key and value into the table.</summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Add(object key, object? value)
    {
        if (key is null)
        {
            return;
        }

        AddToBase((key.ToString()!, value));
    }

    /// <summary>Removes all elements from the table.</summary>
    public void Clear() => _ = _scheduler.Schedule(_dictionary.Clear);

    /// <summary>Performs application-defined tasks associated with freeing managed resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets an observable for the value associated with the specified key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>An observable sequence containing the key and value.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "No reflection used; simple observable wrapper around indexer.")]
    public IObservable<(string key, object value)> Get(object key) =>
        Signal.Create<(string key, object value)>(observer => _scheduler.Schedule(() =>
        {
            observer.OnNext((key.ToString()!, this[key]!));
            observer.OnCompleted();
        }));

    /// <summary>Removes the element with the specified key from the table.</summary>
    /// <param name="key">The key of the element to remove.</param>
    public void Remove(object key)
    {
        if (key is null)
        {
            return;
        }

        _ = _scheduler.Schedule(() => _dictionary.Remove(key.ToString()!));
    }

    /// <summary>Subscribes the specified observer.</summary>
    /// <param name="observer">The observer.</param>
    /// <returns>A disposable subscription.</returns>
    public IDisposable Subscribe(IObserver<(string key, object? value)> observer) => Subject.Subscribe(observer);

    /// <summary>Copies the elements of the collection to an array, starting at a particular array index.</summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection.</param>
    /// <param name="index">The zero-based index in array at which copying begins.</param>
    public void CopyTo(Array array, int index) => ((ICollection)_dictionary).CopyTo(array, index);

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator GetEnumerator() => _dictionary.GetEnumerator();

    /// <summary>Determines whether the dictionary contains the specified key.</summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(object key) => key is not null && _dictionary.ContainsKey(key.ToString()!);

    /// <summary>Clears the backing dictionary synchronously for derived types that rebuild their value map.</summary>
    protected void ClearBaseValues() => _dictionary.Clear();

    /// <summary>Gets a value directly from the backing dictionary.</summary>
    /// <param name="key">The key to read.</param>
    /// <returns>The stored value, or null when the key is not present.</returns>
    protected object? GetBaseValue(object key) =>
        key is not null && _dictionary.TryGetValue(key.ToString()!, out var value) ? value : null;

    /// <summary>Removes a value directly from the backing dictionary.</summary>
    /// <param name="key">The key to remove.</param>
    protected void RemoveBaseValue(object key) => _ = _dictionary.Remove(key.ToString()!);

    /// <summary>Sets a value directly into the backing dictionary.</summary>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value to store.</param>
    protected void SetBaseValue(object key, object? value)
    {
        if (key is null)
        {
            return;
        }

        _dictionary[key.ToString()!] = value;
    }

    /// <summary>Releases the unmanaged resources used by the HashTable and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _subscription.Dispose();
            Subject.Dispose();
        }

        _disposed = true;
    }

    /// <summary>Adds a key/value pair to the dictionary and notifies observers.</summary>
    /// <param name="keyValuePair">The key/value pair.</param>
    private void AddToBase((string key, object? value) keyValuePair)
    {
        _dictionary[keyValuePair.key] = keyValuePair.value;
        try
        {
            Subject.OnNext(keyValuePair);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
    }

    /// <summary>Adapts callback delegates to an observable observer.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="onNext">The callback for next values.</param>
    /// <param name="onError">The callback for errors.</param>
    /// <param name="onCompleted">The callback for completion.</param>
    private sealed class Observer<T>(
        Action<T> onNext,
        Action<Exception> onError,
        Action onCompleted) : IObserver<T>
    {
        /// <inheritdoc />
        public void OnCompleted() => onCompleted();

        /// <inheritdoc />
        public void OnError(Exception error) => onError(error);

        /// <inheritdoc />
        public void OnNext(T value) => onNext(value);
    }
}
