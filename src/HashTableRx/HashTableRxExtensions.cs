// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Provides extensions for reflected types and reactive hash tables.</summary>
public static class HashTableRxExtensions
{
    /// <summary>Provides observable and value helpers for reactive hash tables.</summary>
    /// <param name="table">The reactive hash table.</param>
    extension(IHashTableRx? table)
    {
        /// <summary>Observes updates for a single dotted variable path.</summary>
        /// <typeparam name="T">The observed value type.</typeparam>
        /// <param name="variable">The dotted variable path.</param>
        /// <returns>An observable sequence of matching values.</returns>
        public IObservable<T?> Observe<T>(string variable)
        {
            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var observedVariable = table.UseUpperCase ? variable.ToUpperInvariant() : variable;
            return table
                .ObserveAll
                .Where(change => (table.UseUpperCase ? change.key.ToUpperInvariant() : change.key) == observedVariable)
                .Select(change => (T?)change.value)
                .DistinctUntilChanged();
        }

        /// <summary>Gets the value of a dotted variable path.</summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="variable">The dotted variable path.</param>
        /// <returns>The current value, or the default value when the path is missing or incompatible.</returns>
        public T? Value<T>(string? variable)
        {
            if (table is null || variable is null)
            {
                return default;
            }

            var observedVariable = table.UseUpperCase ? variable.ToUpperInvariant() : variable;
            var raw = table[observedVariable];
            if (raw is null)
            {
                return default;
            }

            try
            {
                return raw is T value ? value : (T?)raw;
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        /// <summary>Sets the value of a dotted variable path.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="variable">The dotted variable path.</param>
        /// <param name="value">The value to set.</param>
        /// <returns><c>true</c> when the value was set; otherwise, <c>false</c>.</returns>
        public bool Value<T>(string? variable, T? value)
        {
            if (table is null)
            {
                return false;
            }

            if (variable is null)
            {
                throw new InvalidVariableException(variable);
            }

            var observedVariable = table.UseUpperCase ? variable.ToUpperInvariant() : variable;
            object? raw;
            try
            {
                raw = table[observedVariable];
            }
            catch (InvalidOperationException)
            {
                raw = null;
            }

            if (raw is null)
            {
                throw new InvalidVariableException(observedVariable);
            }

            if (raw.GetType() != value?.GetType())
            {
                throw new InvalidCastException($"Failed To Set Value, unable to cast from {typeof(T)}");
            }

            table[observedVariable] = value;
            return true;
        }
    }

    /// <summary>Provides helpers for reflected member types.</summary>
    /// <param name="type">The reflected member type.</param>
    extension(Type? type)
    {
        /// <summary>Determines whether the type can be treated as a primitive leaf value.</summary>
        /// <returns><c>true</c> when the type is a primitive leaf value; otherwise, <c>false</c>.</returns>
        [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Only type metadata checks; no reflection on members.")]
        public bool IsPrimitiveArray()
        {
            if (type is null)
            {
                return false;
            }

            if (type.IsPrimitive || type == typeof(string) || type.IsTwinCATStringArray())
            {
                return true;
            }

            if (!type.IsArray)
            {
                return false;
            }

            var elementType = type.GetElementType();
            return elementType?.IsPrimitive == true || elementType == typeof(string);
        }

        /// <summary>Determines whether the type can be treated as a primitive leaf value.</summary>
        /// <returns><c>true</c> when the type is a primitive leaf value; otherwise, <c>false</c>.</returns>
        [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Compatibility alias for IsPrimitiveArray.")]
        public bool IsPrimativeArray() => type.IsPrimitiveArray();

        /// <summary>Determines whether the type is a TwinCAT string wrapper array.</summary>
        /// <returns><c>true</c> when the type is a TwinCAT string wrapper array; otherwise, <c>false</c>.</returns>
        public bool IsTwinCATStringArray() =>
            type?.IsArray == true &&
            type.Name.Contains("STRING_", StringComparison.Ordinal) &&
            type.Name.Contains("_WRAPPER", StringComparison.Ordinal);
    }
}
