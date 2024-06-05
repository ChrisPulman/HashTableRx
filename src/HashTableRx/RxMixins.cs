// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CP.Collections;

namespace System.Reactive.Subjects
{
    /// <summary>
    /// Rx Mixins.
    /// </summary>
    public static class RxMixins
    {
        /// <summary>
        /// Called when Subject has observers.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The @this.</param>
        /// <param name="value">The value.</param>
        public static void OnNextHasObservers<T>(this SubjectBase<T> @this, T value)
        {
            if (@this?.HasObservers == true)
            {
                @this.OnNext(value);
            }
        }
    }
}
