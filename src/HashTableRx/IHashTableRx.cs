// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Defines a reactive hash table that can project structured objects into named values.</summary>
/// <seealso cref="INotifyPropertyChanged"/>
/// <seealso cref="INotifyPropertyChanging"/>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "By design.")]
public interface IHashTableRx : IEnumerable, IDisposable, ICollection, INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>Gets or sets a value indicating whether all named paths are normalized to upper case.</summary>
    bool UseUpperCase { get; set; }

    /// <summary>Gets an observable sequence of all changed values.</summary>
    IObservable<(string key, object? value)> ObserveAll { get; }

    /// <summary>Gets the caller-owned metadata table associated with this instance.</summary>
    HashTable Tag { get; }

    /// <summary>Gets the backing structured object after applying current table values.</summary>
    object? Structure
    {
        [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
        get;
    }

    /// <summary>Gets or sets the value at the specified dotted path.</summary>
    /// <param name="fullName">The dotted value path.</param>
    /// <returns>The current value, or null when the path is not present.</returns>
    object? this[string fullName] { get; set; }

    /// <summary>Adds the specified key and value.</summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to store.</param>
    void Add(object key, object? value);

    /// <summary>Adds the specified nested hash table.</summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The nested table to store.</param>
    void Add(object key, HashTableRx value);

    /// <summary>Determines whether this table contains the key, optionally searching nested tables.</summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="searchAll">A value indicating whether nested tables should be searched.</param>
    /// <returns><c>true</c> when the key exists; otherwise, <c>false</c>.</returns>
    bool ContainsKey(object key, bool searchAll);

    /// <summary>Loads a structured object into the reactive table.</summary>
    /// <param name="value">The structured object to load.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    void SetStructure(object? value);
}
