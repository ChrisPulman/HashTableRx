// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETFRAMEWORK
#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Provides string API compatibility for older target frameworks.</summary>
internal static class StringCompatibilityExtensions
{
    /// <summary>Provides string API compatibility members.</summary>
    /// <param name="source">The source string.</param>
    extension(string source)
    {
        /// <summary>Determines whether a string contains another string by using the specified comparison.</summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparisonType">The string comparison to use.</param>
        /// <returns><c>true</c> when the value is present; otherwise, <c>false</c>.</returns>
        public bool Contains(string value, StringComparison comparisonType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.IndexOf(value, comparisonType) >= 0;
        }
    }
}
#endif
