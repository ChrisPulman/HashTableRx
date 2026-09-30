// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if REACTIVE_TESTS
using System.Reactive.Linq;
#else
using ReactiveUI.Primitives;
#endif
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Tests focused on Observe and ObserveAll behavior.</summary>
public class ObserveBehaviorTests
{
    /// <summary>Defines the ABCPath test input.</summary>
    private const string ABCPath = "A.B.C";

    /// <summary>Defines the PairCount test input.</summary>
    private const int PairCount = 2;

    /// <summary>Defines the Sample314FValue test input.</summary>
    private const float Sample314FValue = 3.14F;

    /// <summary>Observe emits only on change and is distinct.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObserveEmitsOnChangeOnlyAndDistinct()
    {
        using var ht = new HashTableRx(false);
        var results = new List<int?>();
        using var sub = ht.Observe(ABCPath, static value => (int)value!).Select(static x => (int?)x).Subscribe(new TestObserver<int?>(results.Add));

        // Create the variable first via indexer
        ht[ABCPath] = 1;

        // Duplicate set via Value should not emit due to DistinctUntilChanged
        _ = ht.Value(ABCPath, 1);

        // Change value
        _ = ht.Value(ABCPath, PairCount);
        await Assert.That(results.Count).IsEqualTo(PairCount);
        await Assert.That(results[0]).IsEqualTo(1);
        await Assert.That(results[1]).IsEqualTo(PairCount);
    }

    /// <summary>ObserveAll emits key/value tuples for any change.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObserveAllEmitsTuples()
    {
        using var ht = new HashTableRx(false);
        var results = new List<(string key, object? value)>();
        using var sub = ht.ObserveAll.Subscribe(new TestObserver<(string key, object? value)>(results.Add));

        // Create variables via indexer
        ht["X.Y"] = Sample314FValue;
        ht["Z"] = true;
        await Assert.That(results.Exists(static r => r.key == "X.Y" && Sample314FValue.Equals(r.value))).IsTrue();
        await Assert.That(results.Exists(static r => r.key == "Z" && (bool)r.value!)).IsTrue();
    }
}
