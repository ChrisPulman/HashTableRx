// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace CP.Collections.Tests;

/// <summary>
/// Tests focused on Observe and ObserveAll behavior.
/// </summary>
public class ObserveBehaviorTests
{
    /// <summary>
    /// Observe emits only on change and is distinct.
    /// </summary>
    [Fact]
    public void ObserveEmitsOnChangeOnlyAndDistinct()
    {
        var ht = new HashTableRx(false);
        var results = new List<int?>();

        using var sub = ht.Observe<int>("A.B.C").Select(x => (int?)x).Subscribe(results.Add);

        // Create the variable first via indexer
        ht["A.B.C"] = 1;

        // Duplicate set via Value should not emit due to DistinctUntilChanged
        ht.Value("A.B.C", 1);

        // Change value
        ht.Value("A.B.C", 2);

        Assert.Equal(new int?[] { 1, 2 }, results);
    }

    /// <summary>
    /// ObserveAll emits key/value tuples for any change.
    /// </summary>
    [Fact]
    public void ObserveAllEmitsTuples()
    {
        var ht = new HashTableRx(false);
        var results = new List<(string key, object? value)>();
        using var sub = ht.ObserveAll.Subscribe(results.Add);

        // Create variables via indexer
        ht["X.Y"] = 3.14f;
        ht["Z"] = true;

        Assert.Contains(results, r => r.key == "X.Y" && (float)r.value! == 3.14f);
        Assert.Contains(results, r => r.key == "Z" && (bool)r.value!);
    }
}
