// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

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
        [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "No reflection is used; this only checks signal observers and forwards OnNext.")]
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
