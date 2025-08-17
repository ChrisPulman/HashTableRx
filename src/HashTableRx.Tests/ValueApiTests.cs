// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.Collections.Tests;

/// <summary>
/// Tests for Value API: get and set, and error conditions.
/// </summary>
public class ValueApiTests
{
    /// <summary>
    /// Setting and getting values works for root and nested paths.
    /// </summary>
    [Fact]
    public void ValueSetAndGetWorks()
    {
        var ht = new HashTableRx(false);
        ht["A"] = 5;
        Assert.Equal(5, ht.Value<int>("A"));

        ht["A.B"] = 6;
        Assert.Equal(6, ht.Value<int>("A.B"));
    }

    /// <summary>
    /// Setting a non-existent variable throws InvalidVariableException.
    /// </summary>
    [Fact]
    public void ValueThrowsOnInvalidVariable()
    {
        var ht = new HashTableRx(false);
        Assert.Throws<InvalidVariableException>(() => ht.Value("NotExisting", 1));
    }

    /// <summary>
    /// Setting a value with mismatched type throws InvalidCastException.
    /// </summary>
    [Fact]
    public void ValueThrowsOnInvalidCast()
    {
        var ht = new HashTableRx(false);
        ht["A"] = 42;
        Assert.Throws<InvalidCastException>(() => ht.Value("A", "nope"));
    }
}
