// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reactive.Subjects
{
    /// <summary>
    /// Rx Mixins.
    /// </summary>
    public static class RxMixins
    {
        /// <summary>
        /// Determines whether [is primative array].
        /// </summary>
        /// <param name="this">The this.</param>
        /// <returns><c>true</c> if [is primative array] [the specified this]; otherwise, <c>false</c>.</returns>
        public static bool IsPrimativeArray(this Type @this)
        {
            if (@this?.IsPrimitive == true || @this == typeof(string))
            {
                return true;
            }

            if (@this?.IsArray == true)
            {
                var typeString = @this?.FullName?.Replace("[]", string.Empty);
                var type = Type.GetType(typeString!);
                return type?.IsPrimitive == true || type == typeof(string);
            }

            return false;
        }

        /// <summary>
        /// Called when Subject has observers.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The @this.</param>
        /// <param name="value">The value.</param>
        public static void OnNextHasObservers<T>(this AsyncSubject<T> @this, T value)
        {
            if (@this?.HasObservers == true)
            {
                @this.OnNext(value);
            }
        }

        /// <summary>
        /// Called when Subject has observers.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The @this.</param>
        /// <param name="value">The value.</param>
        public static void OnNextHasObservers<T>(this BehaviorSubject<T> @this, T value)
        {
            if (@this?.HasObservers == true)
            {
                @this.OnNext(value);
            }
        }

        /// <summary>
        /// Called when Subject has observers.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The @this.</param>
        /// <param name="value">The value.</param>
        public static void OnNextHasObservers<T>(this ReplaySubject<T> @this, T value)
        {
            if (@this?.HasObservers == true)
            {
                @this.OnNext(value);
            }
        }

        /// <summary>
        /// Called when Subject has observers.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The @this.</param>
        /// <param name="value">The value.</param>
        public static void OnNextHasObservers<T>(this Subject<T> @this, T value)
        {
            if (@this?.HasObservers == true)
            {
                @this.OnNext(value);
            }
        }
    }
}
