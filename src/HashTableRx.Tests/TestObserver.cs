// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace CP.Collections.Tests;

/// <summary>Exercises test observer contracts.</summary>
/// <typeparam name="T">The observed value type.</typeparam>
/// <param name="onNext">Receives each emitted value.</param>
/// <param name="onError">Receives source errors when supplied.</param>
/// <param name="onCompleted">Receives source completion when supplied.</param>
internal sealed class TestObserver<T>(Action<T> onNext, Action<Exception>? onError = null, Action? onCompleted = null) : IObserver<T>
{
    /// <summary>Provides on completed behavior.</summary>
    public void OnCompleted() => onCompleted?.Invoke();

    /// <summary>Provides on error behavior.</summary>
    /// <param name = "error">The error input.</param>
    public void OnError(Exception error) => onError?.Invoke(error);

    /// <summary>Provides on next behavior.</summary>
    /// <param name = "value">The value input.</param>
    public void OnNext(T value) => onNext(value);
}
