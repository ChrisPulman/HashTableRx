// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Provides extensions for reactive signal primitives.</summary>
public static class RxExtensions
{
    /// <summary>Provides publishing helpers for replay signals.</summary>
    /// <typeparam name="T">The signal value type.</typeparam>
    /// <param name="signal">The replay signal.</param>
    extension<T>(ReplaySignal<T> signal)
    {
        /// <summary>Publishes a value only when the signal currently has observers.</summary>
        /// <param name="value">The value to publish.</param>
        public void OnNextHasObservers(T value)
        {
            if (!signal.HasObservers)
            {
                return;
            }

            signal.OnNext(value);
        }
    }
}
