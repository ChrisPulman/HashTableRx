// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace CP.Collections.Tests;

/// <summary>Exercises observable test extensions contracts.</summary>
internal static class ObservableTestExtensions
{
    /// <summary>Extension members for <c>IObservable&lt;T&gt;</c>.</summary>
    /// <typeparam name="T">The emitted value type.</typeparam>
    /// <param name="source">The observable supplying the first value.</param>
    extension<T>(IObservable<T> source)
    {
        /// <summary>Provides first async behavior.</summary>
        /// <returns>The requested test value.</returns>
        internal Task<T> FirstAsync()
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            IDisposable? subscription = null;
            subscription = source.Subscribe(new TestObserver<T>(
                value =>
                {
                    if (!completion.TrySetResult(value))
                    {
                        return;
                    }

                    subscription?.Dispose();
                },
                error => completion.TrySetException(error),
                () => completion.TrySetException(new InvalidOperationException("The observable completed without a value."))));
            return completion.Task;
        }
    }
}
