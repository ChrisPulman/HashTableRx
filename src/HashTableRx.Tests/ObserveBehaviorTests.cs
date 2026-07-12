// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if REACTIVE_TESTS
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>
/// Tests focused on Observe and ObserveAll behavior.
/// </summary>
public class ObserveBehaviorTests
{
    /// <summary>
    /// Observe emits only on change and is distinct.
    /// </summary>
    [Test]
    public async Task ObserveEmitsOnChangeOnlyAndDistinct()
    {
        var ht = new HashTableRx(false);
        var results = new List<int?>();

        using var sub = ht.Observe<int>("A.B.C").Select(x => (int?)x).Subscribe(new TestObserver<int?>(results.Add));

        // Create the variable first via indexer
        ht["A.B.C"] = 1;

        // Duplicate set via Value should not emit due to DistinctUntilChanged
        ht.Value("A.B.C", 1);

        // Change value
        ht.Value("A.B.C", 2);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0]).IsEqualTo(1);
        await Assert.That(results[1]).IsEqualTo(2);
    }

    /// <summary>
    /// ObserveAll emits key/value tuples for any change.
    /// </summary>
    [Test]
    public async Task ObserveAllEmitsTuples()
    {
        var ht = new HashTableRx(false);
        var results = new List<(string key, object? value)>();
        using var sub = ht.ObserveAll.Subscribe(new TestObserver<(string key, object? value)>(results.Add));

        // Create variables via indexer
        ht["X.Y"] = 3.14f;
        ht["Z"] = true;

        await Assert.That(results.Exists(r => r.key == "X.Y" && (float)r.value! == 3.14f)).IsTrue();
        await Assert.That(results.Exists(r => r.key == "Z" && (bool)r.value!)).IsTrue();
    }
}
