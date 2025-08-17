// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for case sensitivity behavior of HashTableRx.
/// </summary>
public class CaseSensitivityTests
{
    /// <summary>
    /// Verifies that keys are case sensitive when UseUpperCase is false.
    /// </summary>
    [Fact]
    public void KeysRespectUseUpperCaseFalse()
    {
        var ht = new HashTableRx(false);
        ht["Root.Child.Value"] = 42;

        Assert.Equal(42, ht.Value<int>("Root.Child.Value"));
        Assert.Null(ht.Value<int?>("ROOT.CHILD.VALUE"));
    }

    /// <summary>
    /// Verifies that keys are normalized to upper-case when UseUpperCase is true.
    /// </summary>
    [Fact]
    public void KeysRespectUseUpperCaseTrue()
    {
        var ht = new HashTableRx(true);
        ht["Root.Child.Value"] = 42;

        // Upper / lower should both resolve when UseUpperCase = true
        Assert.Equal(42, ht.Value<int>("ROOT.CHILD.VALUE"));
        Assert.Equal(42, ht.Value<int>("root.child.value"));

        // Observe should also normalize and emit
        int? observed = null;
        using var sub = ht.Observe<int>("root.child.value").Subscribe(v => observed = v);
        ht.Value("ROOT.CHILD.VALUE", 99);
        Assert.Equal(99, observed);
    }
}
