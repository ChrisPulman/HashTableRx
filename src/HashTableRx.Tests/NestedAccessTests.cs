// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for nested indexer access and observation.
/// </summary>
public class NestedAccessTests
{
    /// <summary>
    /// Indexer can set and get using dotted full path.
    /// </summary>
    [Fact]
    public void IndexerSetAndGetUsingFullPath()
    {
        var ht = new HashTableRx(false);
        ht["A.B.C"] = 10;
        Assert.Equal(10, (int?)ht["A.B.C"]);
    }

    /// <summary>
    /// Observe can subscribe to nested path updates.
    /// </summary>
    [Fact]
    public void ObserveNestedPath()
    {
        var ht = new HashTableRx(false);
        int? seen = null;
        using var sub = ht.Observe<int>("A.B.C").Subscribe(v => seen = v);
        ht["A.B.C"] = 7;
        Assert.Equal(7, seen);
    }
}
