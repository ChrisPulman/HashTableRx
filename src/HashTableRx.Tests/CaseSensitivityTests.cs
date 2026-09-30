// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Tests for case sensitivity behavior of HashTableRx.</summary>
public class CaseSensitivityTests
{
    /// <summary>Defines the RootChildValuePath test input.</summary>
    private const string RootChildValuePath = "Root.Child.Value";

    /// <summary>Defines the SampleValue test input.</summary>
    private const int SampleValue = 42;

    /// <summary>Defines the ROOTCHILDVALUEPath test input.</summary>
    private const string ROOTCHILDVALUEPath = "ROOT.CHILD.VALUE";

    /// <summary>Defines the ReplacementValue test input.</summary>
    private const int ReplacementValue = 99;

    /// <summary>Verifies that keys are case sensitive when UseUpperCase is false.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task KeysRespectUseUpperCaseFalse()
    {
        using var ht = new HashTableRx(false);
        ht[RootChildValuePath] = SampleValue;
        await Assert.That(ht.Value(RootChildValuePath, static value => (int)value!)).IsEqualTo(SampleValue);
        await Assert.That(ht.Value(ROOTCHILDVALUEPath, static value => (int?)value)).IsNull();
    }

    /// <summary>Verifies that keys are normalized to upper-case when UseUpperCase is true.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task KeysRespectUseUpperCaseTrue()
    {
        using var ht = new HashTableRx(true);
        ht[RootChildValuePath] = SampleValue;

        // Upper / lower should both resolve when UseUpperCase = true
        await Assert.That(ht.Value(ROOTCHILDVALUEPath, static value => (int)value!)).IsEqualTo(SampleValue);
        await Assert.That(ht.Value("root.child.value", static value => (int)value!)).IsEqualTo(SampleValue);

        // Observe should also normalize and emit
        int? observed = null;
        using var sub = ht.Observe("root.child.value", static value => (int)value!).Subscribe(new TestObserver<int>(v => observed = v));
        _ = ht.Value(ROOTCHILDVALUEPath, ReplacementValue);
        await Assert.That(observed).IsEqualTo(ReplacementValue);
    }
}
