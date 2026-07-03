// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CP.Collections.Reactive;
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

public class ReactiveShimTests
{
    [Test]
    public async Task ReactiveShimCompilesUnderReactiveNamespaceAndSharesStructureApi()
    {
        var table = new CP.Collections.Reactive.HashTableRx(false);
        var model = new ReactiveShimRoot { Value = 7 };

        table.SetStructure(model);

        await Assert.That(table.GetType().Namespace).IsEqualTo("CP.Collections.Reactive");
        await Assert.That(table.Value<int>("Value")).IsEqualTo(7);

        table.Value("Value", 8);
        var updated = (ReactiveShimRoot)table.Structure!;

        await Assert.That(updated.Value).IsEqualTo(8);
    }

    private sealed class ReactiveShimRoot
    {
        public int Value { get; set; }
    }
}
