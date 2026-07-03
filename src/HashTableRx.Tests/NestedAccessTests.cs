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
        using var sub = ht.Observe<int>("A.B.C").Subscribe(new TestObserver<int>(v => seen = v));
        ht["A.B.C"] = 7;
        await Assert.That(seen).IsEqualTo(7);
    }
}
