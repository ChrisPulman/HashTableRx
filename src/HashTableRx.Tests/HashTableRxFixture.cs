// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace CP.Collections.Tests;

/// <summary>Creates independent calibration and temperature structures for table tests.</summary>
public static class HashTableRxFixture
{
    /// <summary>Creates a populated table containing calibration and temperature values.</summary>
    /// <returns>A table owned by the calling test.</returns>
    public static HashTableRx CreateHashTable()
    {
        var hashTable = new HashTableRx(false);
        hashTable.SetStructure(new CalibrationModel());
        return hashTable;
    }

    /// <summary>Defines the calibration and casing members used by fixture tests.</summary>
    private sealed class CalibrationModel
    {
        /// <summary>Gets or sets whether the calibration values are valid.</summary>
        public bool CalibrationDataValid { get; set; }

        /// <summary>Gets or sets the casing temperature structure.</summary>
        public CasingModel Casing { get; set; } = new();
    }

    /// <summary>Contains the casing temperature structure.</summary>
    private sealed class CasingModel
    {
        /// <summary>Gets or sets the casing temperature measurement.</summary>
        public TemperatureModel Temperature { get; set; } = new();
    }

    /// <summary>Contains the process-value measurement.</summary>
    private sealed class TemperatureModel
    {
        /// <summary>Gets or sets the temperature process value.</summary>
        public ProcessValueModel PV { get; set; } = new();
    }

    /// <summary>Stores the temperature process value.</summary>
    private sealed class ProcessValueModel
    {
        /// <summary>Gets or sets the temperature reading.</summary>
        public float Value { get; set; }
    }
}
