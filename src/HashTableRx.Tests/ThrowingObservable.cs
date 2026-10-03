// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace CP.Collections.Tests;

/// <summary>Exercises throwing observable contracts.</summary>
/// <typeparam name="T">The observable value type.</typeparam>
internal sealed class ThrowingObservable<T> : IObservable<T>
{
    /// <summary>Provides subscribe behavior.</summary>
    /// <param name = "observer">The observer input.</param>
    /// <returns>The requested test value.</returns>
    public IDisposable Subscribe(IObserver<T> observer) => throw new InvalidOperationException("Subscription failed.");
}
