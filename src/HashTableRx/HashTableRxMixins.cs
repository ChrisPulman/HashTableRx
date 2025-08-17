// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace CP.Collections;

/// <summary>
/// HashTableRxMixins.
/// </summary>
public static class HashTableRxMixins
{
    /// <summary>
    /// Determines whether [is primative array].
    /// </summary>
    /// <param name="this">The this.</param>
    /// <returns><c>true</c> if [is primative array] [the specified this]; otherwise, <c>false</c>.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Only type metadata checks; no reflection on members.")]
    public static bool IsPrimativeArray(this Type @this)
    {
        if (@this?.IsPrimitive == true || @this == typeof(string))
        {
            return true;
        }

        if (@this?.IsArray == true)
        {
            var elementType = @this.GetElementType();
            return elementType?.IsPrimitive == true || elementType == typeof(string);
        }

        return false;
    }

    /// <summary>
    /// Get value of fullName called on scheduler, IObservable length is one.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="this">The this.</param>
    /// <param name="variable">The index.</param>
    /// <returns>
    /// A Observable.
    /// </returns>
    public static IObservable<T?> Observe<T>(this IHashTableRx @this, string variable)
    {
        if (@this == null)
        {
            throw new ArgumentNullException(nameof(@this));
        }

        if (@this.UseUpperCase)
        {
            variable = variable?.ToUpper()!;
        }

        return @this.ObserveAll.Where(x => (@this.UseUpperCase ? x.key.ToUpper() : x.key) == variable).Select(x => (T?)x.value).DistinctUntilChanged().Publish().RefCount();
    }

    /// <summary>
    /// Values the specified variable.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="this">The this.</param>
    /// <param name="variable">The variable.</param>
    /// <returns>The value of the Tag.</returns>
    public static T? Value<T>(this IHashTableRx @this, string? variable)
    {
        if (@this == null || variable == null)
        {
            return default;
        }

        if (@this.UseUpperCase)
        {
            variable = variable?.ToUpperInvariant();
        }

        try
        {
            var raw = @this[variable!];
            if (raw is T t)
            {
                return t;
            }

            return (T?)raw;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Values the specified variable.
    /// </summary>
    /// <typeparam name="T">The Type.</typeparam>
    /// <param name="this">The this.</param>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <returns>True if value was set.</returns>
    public static bool Value<T>(this IHashTableRx @this, string? variable, T? value)
    {
        if (@this == null)
        {
            return false;
        }

        if (@this?.UseUpperCase == true)
        {
            variable = variable?.ToUpperInvariant();
        }

        object? raw;
        try
        {
            raw = @this![variable!];
        }
        catch
        {
            raw = null;
        }

        if (raw is null)
        {
            throw new InvalidVariableException(variable);
        }

        if (raw.GetType() != value?.GetType())
        {
            throw new InvalidCastException($"Failed To Set Value, unable to cast from {typeof(T)}");
        }

        @this![variable!] = value;
        return true;
    }

    /// <summary>
    /// Gets the structure.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <returns>
    /// An object of the current values.
    /// </returns>
    [RequiresUnreferencedCode("May use reflection if structure contains fields/properties.")]
    public static object? GetStructure(this IHashTableRx @this)
    {
        if (@this == null)
        {
            return default;
        }

        return @this[true];
    }

    /// <summary>
    /// Sets the structure.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <param name="value">The value.</param>
    [RequiresUnreferencedCode("Uses reflection to traverse structure.")]
    public static void SetStructure(this IHashTableRx @this, object value)
    {
        if (@this == null || value == null)
        {
            return;
        }

        @this[true] = value;
    }
}
