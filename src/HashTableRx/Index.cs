// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETFRAMEWORK
namespace System;

/// <summary>Provides compiler support for from-end indexing on older target frameworks.</summary>
internal readonly struct Index
{
    /// <summary>The encoded index value.</summary>
    private readonly int _value;

    /// <summary>Initializes a new instance of the <see cref="Index"/> struct.</summary>
    /// <param name="value">The index value.</param>
    /// <param name="fromEnd">A value indicating whether the index is from the end of the collection.</param>
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        _value = fromEnd ? ~value : value;
    }

    /// <summary>Gets a value indicating whether the index is from the end of the collection.</summary>
    public bool IsFromEnd => _value < 0;

    /// <summary>Gets the index value.</summary>
    public int Value => IsFromEnd ? ~_value : _value;

    /// <summary>Converts an integer to an index from the start of a collection.</summary>
    /// <param name="value">The index value.</param>
    /// <returns>The index.</returns>
    public static implicit operator Index(int value) => new(value);

    /// <summary>Calculates the zero-based offset for a collection length.</summary>
    /// <param name="length">The collection length.</param>
    /// <returns>The zero-based offset.</returns>
    public int GetOffset(int length) => IsFromEnd ? length - Value : Value;
}
#endif
