// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace CP.Collections.Tests;

/// <summary>Exercises manual observable contracts.</summary>
/// <typeparam name="T">The emitted value type.</typeparam>
internal sealed class ManualObservable<T> : IObservable<T>
{
    /// <summary>Stores the _observers fixture value.</summary>
    private readonly List<IObserver<T>> _observers = [];

    /// <summary>Provides subscribe behavior.</summary>
    /// <param name = "observer">The observer input.</param>
    /// <returns>The requested test value.</returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        _observers.Add(observer);
        return new Subscription(_observers, observer);
    }

    /// <summary>Provides push behavior.</summary>
    /// <param name = "value">The value input.</param>
    internal void Push(T value)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(value);
        }
    }

    /// <summary>Provides push error behavior.</summary>
    /// <param name = "error">The error input.</param>
    internal void PushError(Exception error)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnError(error);
        }
    }

    /// <summary>Provides complete behavior.</summary>
    internal void Complete()
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnCompleted();
        }
    }

    /// <summary>Exercises subscription contracts.</summary>
    /// <param name="observers">The source's active observers.</param>
    /// <param name="observer">The observer removed on disposal.</param>
    private sealed class Subscription(List<IObserver<T>> observers, IObserver<T> observer) : IDisposable
    {
        /// <summary>Provides dispose behavior.</summary>
        public void Dispose() => observers.Remove(observer);
    }
}
