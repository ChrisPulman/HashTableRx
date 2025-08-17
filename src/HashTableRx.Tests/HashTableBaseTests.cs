// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Threading;

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
    /// Remove and Clear should not throw and the table becomes empty (operations are scheduled).
    /// </summary>
    [Fact]
    public void RemoveAndClearDoNotThrow()
    {
        var ht = new HashTable();
        ht.Add("K1", 1);
        ht.Add("K2", 2);
        ht.Remove("K1");
        ht.Clear();

        // Operations are scheduled on a background scheduler, wait for completion.
        var emptied = SpinWait.SpinUntil(() => ht.Count == 0, TimeSpan.FromSeconds(1));
        Assert.True(emptied, $"Expected table to be empty but count was {ht.Count}");
    }
}
