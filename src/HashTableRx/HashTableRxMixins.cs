// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (@this == null || @this.Count == 0 || variable == null)
        {
            return default;
        }

        if (@this.UseUpperCase)
        {
            variable = variable?.ToUpperInvariant();
        }

        return (T?)@this[variable!];
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
        if (@this == null || @this.Count == 0)
        {
            return false;
        }

        if (@this?.UseUpperCase == true)
        {
            variable = variable?.ToUpperInvariant();
        }

        if (@this!.Value<T>(variable) == null)
        {
            throw new InvalidVariableException(variable);
        }

        if (@this!.Value<T>(variable)?.GetType() != value?.GetType())
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
    public static void SetStructure(this IHashTableRx @this, object value)
    {
        if (@this == null || value == null)
        {
            return;
        }

        @this[true] = value;
    }
}
