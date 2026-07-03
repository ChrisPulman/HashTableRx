// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Disposables;
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>
/// UnitTest1.
/// </summary>
public class HashTableRxTest
{
    /// <summary>
    /// Test1s this instance.
    /// </summary>
    [Test]
    public async Task HashTableRxCanReadValuesDirectly()
    {
        var htRx = HashTableRxFixture.CreateHashTable();
        htRx["CalibrationDataValid"] = false;
        var t = (bool?)htRx["CalibrationDataValid"];
        await Assert.That(t).IsFalse();

        htRx["Casing.Temperature.PV.Value"] = 0.0f;
        var t2 = (float?)htRx["Casing.Temperature.PV.Value"];
        await Assert.That(t2).IsEqualTo(0.0f);
    }

    /// <summary>
    /// Hashes the table rx can write values.
    /// </summary>
    [Test]
    public async Task HashTableRxCanWriteValuesDirectly()
    {
        var htRx = HashTableRxFixture.CreateHashTable();
        htRx["CalibrationDataValid"] = true;
        var t = (bool?)htRx["CalibrationDataValid"];
        await Assert.That(t).IsTrue();

        htRx["Casing.Temperature.PV.Value"] = 1.0f;
        var t2 = (float?)htRx["Casing.Temperature.PV.Value"];
        await Assert.That(t2).IsEqualTo(1.0f);
    }

    /// <summary>
    /// Hashes the table rx can read values from observable.
    /// </summary>
    [Test]
    public async Task HashTableRxCanReadValuesFromObservable()
    {
        var htRx = HashTableRxFixture.CreateHashTable();
        var disposables = new MultipleDisposable();
        htRx["CalibrationDataValid"] = false;
        var t = (bool?)htRx["CalibrationDataValid"];
        await Assert.That(t).IsFalse();
        var boolResullt = default(bool?);
        disposables.Add(htRx.Observe<bool>("CalibrationDataValid").Subscribe(new TestObserver<bool>(x => boolResullt = x)));
        htRx["CalibrationDataValid"] = true;
        await Assert.That(boolResullt).IsTrue();

        var floatResult = default(float?);
        htRx["Casing.Temperature.PV.Value"] = 0.0f;
        var t2 = (float?)htRx["Casing.Temperature.PV.Value"];
        await Assert.That(t2).IsEqualTo(0.0f);
        disposables.Add(htRx.Observe<float>("Casing.Temperature.PV.Value").Subscribe(new TestObserver<float>(x => floatResult = x)));
        htRx["Casing.Temperature.PV.Value"] = 1.0f;
        await Assert.That(floatResult).IsEqualTo(1.0f);

        disposables.Dispose();
    }

    /// <summary>
    /// Hashes the table rx can read values.
    /// </summary>
    [Test]
    public async Task HashTableRxCanReadValues()
    {
        var htRx = HashTableRxFixture.CreateHashTable();
        htRx["CalibrationDataValid"] = false;
        var t = htRx.Value<bool>("CalibrationDataValid");
        await Assert.That(t).IsFalse();

        htRx["Casing.Temperature.PV.Value"] = 0.0f;
        var t2 = htRx.Value<float>("Casing.Temperature.PV.Value");
        await Assert.That(t2).IsEqualTo(0.0f);
    }

    /// <summary>
    /// Hashes the table rx can write values.
    /// </summary>
    [Test]
    public async Task HashTableRxCanWriteValues()
    {
        var htRx = HashTableRxFixture.CreateHashTable();
        htRx.Value("CalibrationDataValid", true);
        var t = htRx.Value<bool>("CalibrationDataValid");
        await Assert.That(t).IsTrue();

        htRx.Value("Casing.Temperature.PV.Value", 1.0f);
        var t2 = htRx.Value<float>("Casing.Temperature.PV.Value");
        await Assert.That(t2).IsEqualTo(1.0f);
    }
}
