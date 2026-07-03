// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for case sensitivity behavior of HashTableRx.
/// </summary>
public class CaseSensitivityTests
{
    /// <summary>
    /// Verifies that keys are case sensitive when UseUpperCase is false.
    /// </summary>
    [Test]
    public async Task KeysRespectUseUpperCaseFalse()
    {
        var ht = new HashTableRx(false);
        ht["Root.Child.Value"] = 42;

        await Assert.That(ht.Value<int>("Root.Child.Value")).IsEqualTo(42);
        await Assert.That(ht.Value<int?>("ROOT.CHILD.VALUE")).IsNull();
    }

    /// <summary>
    /// Verifies that keys are normalized to upper-case when UseUpperCase is true.
    /// </summary>
    [Test]
    public async Task KeysRespectUseUpperCaseTrue()
    {
        var ht = new HashTableRx(true);
        ht["Root.Child.Value"] = 42;

        // Upper / lower should both resolve when UseUpperCase = true
        await Assert.That(ht.Value<int>("ROOT.CHILD.VALUE")).IsEqualTo(42);
        await Assert.That(ht.Value<int>("root.child.value")).IsEqualTo(42);

        // Observe should also normalize and emit
        int? observed = null;
        using var sub = ht.Observe<int>("root.child.value").Subscribe(new TestObserver<int>(v => observed = v));
        ht.Value("ROOT.CHILD.VALUE", 99);
        await Assert.That(observed).IsEqualTo(99);
    }
}
