// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using CP.Collections.Reactive;
using TUnit.Assertions;
using TUnit.Core;
using HashTableRx = CP.Collections.Reactive.HashTableRx;

namespace CP.Collections.Tests;

/// <summary>Exercises reactive shim tests contracts.</summary>
public class ReactiveShimTests
{
    /// <summary>Defines the initial structure value.</summary>
    private const int InitialPropertyValue = 7;

    /// <summary>Defines the UpdatedPropertyValue test input.</summary>
    private const int UpdatedPropertyValue = 8;

    /// <summary>Verifies reactive shim compiles under reactive namespace and shares structure api.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReactiveShimCompilesUnderReactiveNamespaceAndSharesStructureApi()
    {
        using var table = new HashTableRx(false);
        var model = new ReactiveShimRoot
        {
            Value = InitialPropertyValue
        };
        table.SetStructure(ReflectionFixture.Create(model));
        await Assert.That(table.GetType().Namespace).IsEqualTo("CP.Collections.Reactive");
        await Assert.That(table.Value("Value", static value => (int)value!)).IsEqualTo(InitialPropertyValue);
        _ = table.Value("Value", UpdatedPropertyValue);
        var updated = (ReactiveShimRoot)table.Structure!;
        await Assert.That(updated.Value).IsEqualTo(UpdatedPropertyValue);
    }

    /// <summary>Exercises reactive shim root contracts.</summary>
    private sealed class ReactiveShimRoot
    {
        /// <summary>Gets or sets the value fixture value.</summary>
        public int Value { get; set; }
    }
}
