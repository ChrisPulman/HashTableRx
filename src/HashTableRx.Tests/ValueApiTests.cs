// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>
/// Tests for Value API: get and set, and error conditions.
/// </summary>
public class ValueApiTests
{
    /// <summary>
    /// Setting and getting values works for root and nested paths.
    /// </summary>
    [Test]
    public async Task ValueSetAndGetWorks()
    {
        var ht = new HashTableRx(false);
        ht["A"] = 5;
        await Assert.That(ht.Value<int>("A")).IsEqualTo(5);

        ht["A.B"] = 6;
        await Assert.That(ht.Value<int>("A.B")).IsEqualTo(6);
    }

    /// <summary>
    /// Setting a non-existent variable throws InvalidVariableException.
    /// </summary>
    [Test]
    public async Task ValueThrowsOnInvalidVariable()
    {
        var ht = new HashTableRx(false);
        await Assert.That(() => ht.Value("NotExisting", 1)).Throws<InvalidVariableException>();
    }

    /// <summary>
    /// Setting a value with mismatched type throws InvalidCastException.
    /// </summary>
    [Test]
    public async Task ValueThrowsOnInvalidCast()
    {
        var ht = new HashTableRx(false);
        ht["A"] = 42;
        await Assert.That(() => ht.Value("A", "nope")).Throws<InvalidCastException>();
    }
}
