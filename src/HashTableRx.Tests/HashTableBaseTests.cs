// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if REACTIVE_TESTS
using ImmediateSequencer = System.Reactive.Concurrency.ImmediateScheduler;
#else
using ReactiveUI.Primitives.Concurrency;
#endif
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for the HashTable base class API.
/// </summary>
public class HashTableBaseTests
{
    /// <summary>
    /// Add and reactive Get should return the key and value.
    /// </summary>
    [Test]
    public async Task AddAndGetThroughObservableGet()
    {
        var ht = new HashTable(ImmediateSequencer.Instance);
        ht.Add("K", 123);
        var value = await ht.Get("K").FirstAsync();
        await Assert.That(value).IsEqualTo(("K", (object)123));
    }

    /// <summary>
    /// Remove and Clear should not throw and the table becomes empty (operations are scheduled).
    /// </summary>
    [Test]
    public async Task RemoveAndClearDoNotThrow()
    {
        var ht = new HashTable(ImmediateSequencer.Instance);
        ht.Add("K1", 1);
        ht.Add("K2", 2);
        ht.Remove("K1");
        ht.Clear();

        await Assert.That(ht.Count).IsEqualTo(0);
    }
}
