// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.Collections.Tests;

/// <summary>
/// HashTableRxFixture.
/// </summary>
public static class HashTableRxFixture
{
    public static HashTableRx CreateHashTable()
    {
        // get the current directory
        var currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        // load the assembly MockLibraryWithFields.dll
        var assembly = System.Reflection.Assembly.LoadFrom(currentDirectory + "\\MockLibraryWithFields.dll");
        if (assembly is null)
        {
            throw new InvalidOperationException("Unable to load MockLibraryWithFields.dll.");
        }

        // Create an instance of the MockLibraryWithFields.MockClassWithFields class
        var obj = assembly.CreateInstance("TwinCATRx.RigSTRUCT");
        if (obj is null)
        {
            throw new InvalidOperationException("Unable to create TwinCATRx.RigSTRUCT.");
        }

        var hashTable = new HashTableRx(false);
        hashTable.SetStructure(obj);
        return hashTable;
    }
}
