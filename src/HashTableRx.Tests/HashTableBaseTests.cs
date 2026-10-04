// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if REACTIVE_TESTS
using ImmediateSequencer = System.Reactive.Concurrency.ImmediateScheduler;
#else
using ReactiveUI.Primitives.Concurrency;
#endif
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Tests for the HashTable base class API.</summary>
public class HashTableBaseTests
{
    /// <summary>Defines the IntegerValue test input.</summary>
    private const int IntegerValue = 123;

    /// <summary>Defines the PairCount test input.</summary>
    private const int PairCount = 2;

    /// <summary>Add and reactive Get should return the key and value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddAndGetThroughObservableGet()
    {
        using var ht = new HashTable(ImmediateSequencer.Instance);
        ht.Add("K", IntegerValue);
        var value = await ht.Get("K").FirstAsync();
        await Assert.That(value).IsEqualTo(("K", (object)IntegerValue));
    }

    /// <summary>Remove and Clear should not throw and the table becomes empty (operations are scheduled).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RemoveAndClearDoNotThrow()
    {
        using var ht = new HashTable(ImmediateSequencer.Instance);
        ht.Add("K1", 1);
        ht.Add("K2", PairCount);
        ht.Remove("K1");
        ht.Clear();
        await Assert.That(ht.Count).IsEqualTo(0);
    }
}
