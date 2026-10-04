// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveUI.Primitives.Disposables;
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Verifies calibration and temperature access through the reactive table APIs.</summary>
public class HashTableRxTest
{
    /// <summary>Defines the CalibrationDataValidPath test input.</summary>
    private const string CalibrationDataValidPath = "CalibrationDataValid";

    /// <summary>Defines the CasingTemperaturePVValuePath test input.</summary>
    private const string CasingTemperaturePVValuePath = "Casing.Temperature.PV.Value";

    /// <summary>Verifies that indexer reads return the stored calibration and temperature values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableRxCanReadValuesDirectly()
    {
        using var table = HashTableRxFixture.CreateHashTable();
        table[CalibrationDataValidPath] = false;
        var t = (bool?)table[CalibrationDataValidPath];
        await Assert.That(t).IsFalse();
        table[CasingTemperaturePVValuePath] = 0.0F;
        var t2 = (float?)table[CasingTemperaturePVValuePath];
        await Assert.That(t2).IsEqualTo(0.0F);
    }

    /// <summary>Verifies that indexer writes replace calibration and temperature values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableRxCanWriteValuesDirectly()
    {
        using var table = HashTableRxFixture.CreateHashTable();
        table[CalibrationDataValidPath] = true;
        var t = (bool?)table[CalibrationDataValidPath];
        await Assert.That(t).IsTrue();
        table[CasingTemperaturePVValuePath] = 1.0F;
        var t2 = (float?)table[CasingTemperaturePVValuePath];
        await Assert.That(t2).IsEqualTo(1.0F);
    }

    /// <summary>Verifies that subscriptions receive calibration and temperature changes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableRxCanReadValuesFromObservable()
    {
        using var table = HashTableRxFixture.CreateHashTable();
        using var disposables = new MultipleDisposable();
        table[CalibrationDataValidPath] = false;
        var t = (bool?)table[CalibrationDataValidPath];
        await Assert.That(t).IsFalse();
        var boolResullt = default(bool?);
        disposables.Add(table.Observe(CalibrationDataValidPath, static value => (bool)value!).Subscribe(new TestObserver<bool>(x => boolResullt = x)));
        table[CalibrationDataValidPath] = true;
        await Assert.That(boolResullt).IsTrue();
        var floatResult = default(float?);
        table[CasingTemperaturePVValuePath] = 0.0F;
        var t2 = (float?)table[CasingTemperaturePVValuePath];
        await Assert.That(t2).IsEqualTo(0.0F);
        disposables.Add(table.Observe(CasingTemperaturePVValuePath, static value => (float)value!).Subscribe(new TestObserver<float>(x => floatResult = x)));
        table[CasingTemperaturePVValuePath] = 1.0F;
        await Assert.That(floatResult).IsEqualTo(1.0F);
        disposables.Dispose();
    }

    /// <summary>Verifies that value conversion returns the stored calibration and temperature values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableRxCanReadValues()
    {
        using var table = HashTableRxFixture.CreateHashTable();
        table[CalibrationDataValidPath] = false;
        var t = table.Value(CalibrationDataValidPath, static value => (bool)value!);
        await Assert.That(t).IsFalse();
        table[CasingTemperaturePVValuePath] = 0.0F;
        var t2 = table.Value(CasingTemperaturePVValuePath, static value => (float)value!);
        await Assert.That(t2).IsEqualTo(0.0F);
    }

    /// <summary>Verifies that value setters replace calibration and temperature values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableRxCanWriteValues()
    {
        using var table = HashTableRxFixture.CreateHashTable();
        _ = table.Value(CalibrationDataValidPath, true);
        var t = table.Value(CalibrationDataValidPath, static value => (bool)value!);
        await Assert.That(t).IsTrue();
        _ = table.Value(CasingTemperaturePVValuePath, 1.0F);
        var t2 = table.Value(CasingTemperaturePVValuePath, static value => (float)value!);
        await Assert.That(t2).IsEqualTo(1.0F);
    }
}
