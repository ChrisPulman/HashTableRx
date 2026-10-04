// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Tests for Value API: get and set, and error conditions.</summary>
public class ValueApiTests
{
    /// <summary>Defines the InitialValue test input.</summary>
    private const int InitialValue = 5;

    /// <summary>Defines the NestedValue test input.</summary>
    private const int NestedValue = 6;

    /// <summary>Defines the SampleValue test input.</summary>
    private const int SampleValue = 42;

    /// <summary>Setting and getting values works for root and nested paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueSetAndGetWorks()
    {
        using var ht = new HashTableRx(false);
        ht["A"] = InitialValue;
        await Assert.That(ht.Value("A", static value => (int)value!)).IsEqualTo(InitialValue);
        ht["A.B"] = NestedValue;
        await Assert.That(ht.Value("A.B", static value => (int)value!)).IsEqualTo(NestedValue);
    }

    /// <summary>Setting a non-existent variable throws InvalidVariableException.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueThrowsOnInvalidVariable()
    {
        using var ht = new HashTableRx(false);
        await Assert.That(() => ht.Value("NotExisting", 1)).Throws<InvalidVariableException>();
    }

    /// <summary>Setting a value with mismatched type throws InvalidCastException.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueThrowsOnInvalidCast()
    {
        using var ht = new HashTableRx(false);
        ht["A"] = SampleValue;
        await Assert.That(() => ht.Value("A", "nope")).Throws<InvalidCastException>();
    }

    /// <summary>Converter delegates are validated before receiver and path lookup.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConverterDelegatesAreRequired()
    {
        IHashTableRx? table = null;
        Func<object?, int>? converter = null;
        using var concrete = new HashTableRx(false);
        await Assert.That(() => table!.Value(null, converter!)).Throws<ArgumentNullException>();
        await Assert.That(() => concrete.Observe("A", converter!)).Throws<ArgumentNullException>();
    }
}
