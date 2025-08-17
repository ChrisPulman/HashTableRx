// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;

namespace CP.Collections;

/// <summary>
/// interface for Rx Hash Table.
/// </summary>
/// <seealso cref="INotifyPropertyChanged"/>
/// <seealso cref="INotifyPropertyChanging"/>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "By design.")]
public interface IHashTableRx : IEnumerable, ICollection, INotifyPropertyChanged, INotifyPropertyChanging, ICancelable
{
    /// <summary>
    /// Gets or sets a value indicating whether this instance is TwinCat 3.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is t c3; otherwise, <c>false</c>.
    /// </value>
    bool UseUpperCase { get; set; }

    /// <summary>
    /// Gets the observe all.
    /// </summary>
    /// <value>The observe all.</value>
    IObservable<(string key, object? value)> ObserveAll { get; }

    /// <summary>
    /// Gets the tag.
    /// </summary>
    /// <value>The tag.</value>
    HashTable? Tag { get; }

    /// <summary>
    /// Gets or sets the <see cref="object"/> with the specified use reflection.
    /// </summary>
    /// <value>The <see cref="object"/>.</value>
    /// <param name="useReflection">if set to <c>true</c> [use reflection].</param>
    /// <returns>An object.</returns>
    object? this[bool useReflection]
    {
        [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
        get;
        [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
        set;
    }

    /// <summary>
    /// Gets or sets the <see cref="object"/> with the specified full name.
    /// </summary>
    /// <value>The <see cref="object"/>.</value>
    /// <param name="fullName">The full name.</param>
    /// <returns>An object.</returns>
    object? this[string fullName] { get; set; }

    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void Add(object key, object? value);

    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void Add(object key, HashTableRx value);

    /// <summary>
    /// Determines whether the specified key contains key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="searchAll">if set to <c>true</c> [search all].</param>
    /// <returns><c>true</c> if the specified key contains key; otherwise, <c>false</c>.</returns>
    bool ContainsKey(object key, bool searchAll);
}
