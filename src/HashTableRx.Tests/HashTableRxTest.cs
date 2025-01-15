// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CP.Collections.Tests;

/// <summary>
/// UnitTest1.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HashTableRxTest"/> class.
/// </remarks>
/// <param name="fixture">The hash table rx fixture.</param>
public class HashTableRxTest(HashTableRxFixture fixture) : IClassFixture<HashTableRxFixture>
{
    /// <summary>
    /// Test1s this instance.
    /// </summary>
    [Fact]
    public void HashTableRxCanReadValuesDirectly()
    {
        fixture.HtRx["CalibrationDataValid"] = false;
        var t = (bool?)fixture.HtRx["CalibrationDataValid"];
        Assert.False(t);

        fixture.HtRx["Casing.Temperature.PV.Value"] = 0.0f;
        var t2 = (float?)fixture.HtRx["Casing.Temperature.PV.Value"];
        Assert.Equal(0.0f, t2);
    }

    /// <summary>
    /// Hashes the table rx can write values.
    /// </summary>
    [Fact]
    public void HashTableRxCanWriteValuesDirectly()
    {
        fixture.HtRx["CalibrationDataValid"] = true;
        var t = (bool?)fixture.HtRx["CalibrationDataValid"];
        Assert.True(t);

        fixture.HtRx["Casing.Temperature.PV.Value"] = 1.0f;
        var t2 = (float?)fixture.HtRx["Casing.Temperature.PV.Value"];
        Assert.Equal(1.0f, t2);
    }

    /// <summary>
    /// Hashes the table rx can read values from observable.
    /// </summary>
    [Fact]
    public void HashTableRxCanReadValuesFromObservable()
    {
        var disposables = new CompositeDisposable();
        fixture.HtRx["CalibrationDataValid"] = false;
        var t = (bool?)fixture.HtRx["CalibrationDataValid"];
        Assert.False(t);
        disposables.Add(fixture.HtRx.Observe<bool>("CalibrationDataValid").Skip(1).Subscribe(x => Assert.True(x)));
        fixture.HtRx["CalibrationDataValid"] = true;

        fixture.HtRx["Casing.Temperature.PV.Value"] = 0.0f;
        var t2 = (float?)fixture.HtRx["Casing.Temperature.PV.Value"];
        Assert.Equal(0.0f, t2);
        disposables.Add(fixture.HtRx.Observe<float>("Casing.Temperature.PV.Value").Skip(1).Subscribe(x => Assert.Equal(1.0f, x)));
        fixture.HtRx["Casing.Temperature.PV.Value"] = 1.0f;

        disposables.Dispose();
    }

    /// <summary>
    /// Hashes the table rx can read values.
    /// </summary>
    [Fact]
    public void HashTableRxCanReadValues()
    {
        fixture.HtRx["CalibrationDataValid"] = false;
        var t = fixture.HtRx.Value<bool>("CalibrationDataValid");
        Assert.False(t);

        fixture.HtRx["Casing.Temperature.PV.Value"] = 0.0f;
        var t2 = fixture.HtRx.Value<float>("Casing.Temperature.PV.Value");
        Assert.Equal(0.0f, t2);
    }

    /// <summary>
    /// Hashes the table rx can write values.
    /// </summary>
    [Fact]
    public void HashTableRxCanWriteValues()
    {
        fixture.HtRx.Value("CalibrationDataValid", true);
        var t = fixture.HtRx.Value<bool>("CalibrationDataValid");
        Assert.True(t);

        fixture.HtRx.Value("Casing.Temperature.PV.Value", 1.0f);
        var t2 = fixture.HtRx.Value<float>("Casing.Temperature.PV.Value");
        Assert.Equal(1.0f, t2);
    }
}
