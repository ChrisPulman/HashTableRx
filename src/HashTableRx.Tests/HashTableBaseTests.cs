// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for the HashTable base class API.
/// </summary>
public class HashTableBaseTests
{
    /// <summary>
    /// Add and reactive Get should return the key and value.
    /// </summary>
    [Fact]
    public void AddAndGetThroughObservableGet()
    {
        var ht = new HashTable();
        ht.Add("K", 123);
        var value = ht.Get("K").Wait();
        Assert.Equal(("K", (object)123), value);
    }

    /// <summary>
    /// Remove and Clear should not throw and the table is empty.
    /// </summary>
    [Fact]
    public void RemoveAndClearDoNotThrow()
    {
        var ht = new HashTable();
        ht.Add("K1", 1);
        ht.Add("K2", 2);
        ht.Remove("K1");
        ht.Clear();
        Assert.Empty(ht);
    }
}
