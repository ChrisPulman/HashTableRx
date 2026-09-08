// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for nested indexer access and observation.
/// </summary>
public class NestedAccessTests
{
    /// <summary>
    /// Flat and nested writes preserve normalization and notification ordering.
    /// </summary>
    [Test]
    [Arguments("Value", false)]
    [Arguments("Value", true)]
    [Arguments("Root.Value", false)]
    [Arguments("Root.Value", true)]
    public async Task IndexerPreservesNotificationOrder(string path, bool useUpperCase)
    {
        using var table = new HashTableRx(useUpperCase);
        table[path] = 1;
        var normalizedPath = useUpperCase ? path.ToUpperInvariant() : path;
        var notifications = new List<(string kind, string? path, object? value)>();
        table.PropertyChanging += (_, args) => notifications.Add(("changing", args.PropertyName, table[path]));
        using var subscription = table.ObserveAll.Subscribe(
            new TestObserver<(string key, object? value)>(change => notifications.Add(("observable", change.key, change.value))));
        table.PropertyChanged += (_, args) => notifications.Add(("changed", args.PropertyName, table[path]));
        notifications.Clear();

        table[path] = 2;

        await Assert.That(notifications.Count).IsEqualTo(3);
        await Assert.That(notifications[0]).IsEqualTo(("changing", (string?)normalizedPath, (object?)1));
        await Assert.That(notifications[1]).IsEqualTo(("observable", (string?)normalizedPath, (object?)2));
        await Assert.That(notifications[2]).IsEqualTo(("changed", (string?)normalizedPath, (object?)2));
        await Assert.That(table[normalizedPath]).IsEqualTo((object?)2);
    }

    /// <summary>
    /// Empty path segments remain valid dictionary keys during dotted traversal.
    /// </summary>
    [Test]
    [Arguments(".")]
    [Arguments(".Value")]
    [Arguments("Root.")]
    [Arguments("Root..Value")]
    public async Task IndexerPreservesEmptyPathSegments(string path)
    {
        using var table = new HashTableRx(false);

        table[path] = 7;

        await Assert.That(table[path]).IsEqualTo((object?)7);
        await Assert.That(table["Missing.Value"]).IsNull();
    }

    /// <summary>
    /// Empty keys added through the base API can be read, while empty-path writes remain ignored.
    /// </summary>
    [Test]
    public async Task IndexerPreservesEmptyKeyAndNullPathBehavior()
    {
        using var table = new HashTableRx(false);
        table.Add(string.Empty, 5);

        table[string.Empty] = 6;
        table[null!] = 7;

        await Assert.That(table[string.Empty]).IsEqualTo((object?)5);
        await Assert.That(table[null!]).IsNull();
        await Assert.That(table["Missing"]).IsNull();
        await Assert.That(table.Count).IsEqualTo(1);
    }

    /// <summary>
    /// Indexer can set and get using dotted full path.
    /// </summary>
    [Test]
    public async Task IndexerSetAndGetUsingFullPath()
    {
        var ht = new HashTableRx(false);
        ht["A.B.C"] = 10;
        await Assert.That((int?)ht["A.B.C"]).IsEqualTo(10);
    }

    /// <summary>
    /// Observe can subscribe to nested path updates.
    /// </summary>
    [Test]
    public async Task ObserveNestedPath()
    {
        var ht = new HashTableRx(false);
        int? seen = null;
        using var sub = ht.Observe("A.B.C", static value => (int)value!).Subscribe(new TestObserver<int>(v => seen = v));
        ht["A.B.C"] = 7;
        await Assert.That(seen).IsEqualTo(7);
    }
}
