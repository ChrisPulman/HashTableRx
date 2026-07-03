// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.Collections.Tests;

internal sealed class TestObserver<T>(
    Action<T> onNext,
    Action<Exception>? onError = null,
    Action? onCompleted = null) : IObserver<T>
{
    public void OnCompleted() => onCompleted?.Invoke();

    public void OnError(Exception error) => onError?.Invoke(error);

    public void OnNext(T value) => onNext(value);
}

internal sealed class ManualObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = [];

    public IDisposable Subscribe(IObserver<T> observer)
    {
        _observers.Add(observer);
        return new Subscription(_observers, observer);
    }

    public void Push(T value)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(value);
        }
    }

    public void PushError(Exception error)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnError(error);
        }
    }

    public void Complete()
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.OnCompleted();
        }
    }

    private sealed class Subscription(List<IObserver<T>> observers, IObserver<T> observer) : IDisposable
    {
        public void Dispose() => observers.Remove(observer);
    }
}

internal sealed class ThrowingObservable<T> : IObservable<T>
{
    public IDisposable Subscribe(IObserver<T> observer) => throw new InvalidOperationException("Subscription failed.");
}

internal static class ObservableTestExtensions
{
    public static Task<T> FirstAsync<T>(this IObservable<T> source)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        IDisposable? subscription = null;
        subscription = source.Subscribe(new TestObserver<T>(
            value =>
            {
                if (completion.TrySetResult(value))
                {
                    subscription?.Dispose();
                }
            },
            error => completion.TrySetException(error),
            () => completion.TrySetException(new InvalidOperationException("The observable completed without a value."))));

        return completion.Task;
    }
}
