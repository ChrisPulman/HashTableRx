// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Provides change notification and dotted-path access over values reflected from structured objects.</summary>
[Serializable]
public class HashTableRx : HashTable, IHashTableRx
{
    /// <summary>Stores the reflected structure data object in <see cref="Tag"/>.</summary>
    private const string DataKey = "Data";

    /// <summary>Stores the reflected fields in <see cref="Tag"/>.</summary>
    private const string FieldInfoKey = "FieldInfo";

    /// <summary>Stores the reflected properties in <see cref="Tag"/>.</summary>
    private const string PropertyInfoKey = "PropertyInfo";

    /// <summary>Initializes a new instance of the <see cref="HashTableRx"/> class.</summary>
    /// <param name="useUpperCase">A value indicating whether dotted paths are normalized to upper case.</param>
    public HashTableRx(bool useUpperCase)
    {
        UseUpperCase = useUpperCase;
        Tag = [];
    }

    /// <summary>Initializes a new instance of the <see cref="HashTableRx"/> class.</summary>
    /// <param name="source">The source observable that publishes table updates.</param>
    public HashTableRx(IObservable<(string key, object? value)> source)
        : base(source) => Tag = [];

    /// <summary>Initializes a new instance of the <see cref="HashTableRx"/> class.</summary>
    /// <param name="info">The serialization information.</param>
    /// <param name="context">The streaming context.</param>
    [SuppressMessage("Roslynator", "RCS1231:Make parameter ref read-only", Justification = "Required by serialization constructor shape.")]
    protected HashTableRx(SerializationInfo info, StreamingContext context) =>
        Tag = [];

    /// <summary>Occurs when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Occurs when a property value is changing.</summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>Gets an observable sequence of all changed values.</summary>
    public IObservable<(string key, object? value)> ObserveAll => Subject.DistinctUntilChanged();

    /// <summary>Gets the caller-owned metadata table associated with this instance.</summary>
    public HashTable Tag { get; }

    /// <summary>Gets or sets a value indicating whether all named paths are normalized to upper case.</summary>
    public bool UseUpperCase { get; set; }

    /// <summary>Gets the backing structured object after applying current table values.</summary>
    public object? Structure
    {
        [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
        get
        {
            var data = Tag[DataKey];
            if (data is null)
            {
                return null;
            }

            ApplyTableToObject(data, data.GetType(), this);
            return data;
        }
    }

    /// <summary>Gets or sets the value at the specified dotted path.</summary>
    /// <param name="fullName">The dotted value path.</param>
    /// <returns>The current value, or null when the path is not present.</returns>
    public object? this[string fullName]
    {
        get => GetFullName(fullName);
        set => SetFullName(fullName, value);
    }

    /// <summary>Adds an element with the specified key and value into the <see cref="HashTable"/>.</summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public new void Add(object key, object? value) => base.Add(key, value);

    /// <summary>Adds a nested hash table with the specified key into the <see cref="HashTable"/>.</summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The nested table to add.</param>
    public void Add(object key, HashTableRx value) => base.Add(key, value);

    /// <summary>Determines whether this table contains the key, optionally searching nested tables.</summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="searchAll">A value indicating whether nested tables should be searched.</param>
    /// <returns><c>true</c> when the key exists; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(object key, bool searchAll) =>
        searchAll ? ContainsKeyRecursive(key) : ContainsKey(key);

    /// <summary>Loads a structured object into the reactive table.</summary>
    /// <param name="value">The structured object to load.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    public void SetStructure(object? value)
    {
        if (value is null)
        {
            return;
        }

        ClearBaseValues();
        var type = value.GetType();
        Tag[DataKey] = value;
        Tag[FieldInfoKey] = type.GetFields();
        Tag[PropertyInfoKey] = type.GetProperties();
        LoadObject(value, type, this, null);
    }

    /// <summary>Applies all stored table values to an object instance.</summary>
    /// <param name="target">The object to update.</param>
    /// <param name="type">The reflected target type.</param>
    /// <param name="table">The table that stores current values.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private static void ApplyTableToObject(object target, Type type, HashTableRx table)
    {
        ApplyPropertiesToObject(target, type.GetProperties(), table);
        ApplyFieldsToObject(target, type.GetFields(), table);
    }

    /// <summary>Applies stored field values to an object instance.</summary>
    /// <param name="target">The object to update.</param>
    /// <param name="fields">The reflected fields.</param>
    /// <param name="table">The table that stores current values.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private static void ApplyFieldsToObject(object target, FieldInfo[] fields, HashTableRx table)
    {
        for (var index = 0; index < fields.Length; index++)
        {
            ApplyFieldToObject(target, fields[index], table);
        }
    }

    /// <summary>Applies a stored field value to an object instance.</summary>
    /// <param name="target">The object to update.</param>
    /// <param name="field">The reflected field.</param>
    /// <param name="table">The table that stores current values.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private static void ApplyFieldToObject(object target, FieldInfo field, HashTableRx table)
    {
        if (field.FieldType.IsPrimitiveArray())
        {
            _ = TrySetFieldValue(target, field, table.GetBaseValue(field.Name));
            return;
        }

        if (table.GetBaseValue(field.Name) is not HashTableRx childTable)
        {
            return;
        }

        var childData = field.GetValue(target);
        if (childData is null)
        {
            return;
        }

        ApplyTableToObject(childData, field.FieldType, childTable);
        _ = TrySetFieldValue(target, field, childData);
    }

    /// <summary>Applies stored property values to an object instance.</summary>
    /// <param name="target">The object to update.</param>
    /// <param name="properties">The reflected properties.</param>
    /// <param name="table">The table that stores current values.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private static void ApplyPropertiesToObject(object target, PropertyInfo[] properties, HashTableRx table)
    {
        for (var index = 0; index < properties.Length; index++)
        {
            ApplyPropertyToObject(target, properties[index], table);
        }
    }

    /// <summary>Applies a stored property value to an object instance.</summary>
    /// <param name="target">The object to update.</param>
    /// <param name="property">The reflected property.</param>
    /// <param name="table">The table that stores current values.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private static void ApplyPropertyToObject(object target, PropertyInfo property, HashTableRx table)
    {
        if (!CanUseProperty(property))
        {
            return;
        }

        if (property.PropertyType.IsPrimitiveArray())
        {
            _ = property.CanWrite && TrySetPropertyValue(target, property, table.GetBaseValue(property.Name));
            return;
        }

        if (table.GetBaseValue(property.Name) is not HashTableRx childTable)
        {
            return;
        }

        var childData = TryGetPropertyValue(target, property);
        if (childData is null)
        {
            return;
        }

        ApplyTableToObject(childData, property.PropertyType, childTable);
        _ = property.CanWrite && TrySetPropertyValue(target, property, childData);
    }

    /// <summary>Builds a dotted child path from the optional parent and current member name.</summary>
    /// <param name="parentPath">The optional parent path.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The combined dotted path.</returns>
    private static string CombineName(string? parentPath, string name) =>
        string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";

    /// <summary>Determines whether the property can be used for reflection mapping.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><c>true</c> when the property can be read and is not an indexer; otherwise, <c>false</c>.</returns>
    private static bool CanUseProperty(PropertyInfo property) =>
        property.CanRead && property.GetIndexParameters().Length == 0;

    /// <summary>Gets a reflected property value, returning null when the member cannot be read.</summary>
    /// <param name="source">The object that owns the property.</param>
    /// <param name="property">The property to read.</param>
    /// <returns>The property value, or null when reading fails.</returns>
    private static object? TryGetPropertyValue(object source, PropertyInfo property)
    {
        try
        {
            return property.GetValue(source, null);
        }
        catch (Exception ex) when (IsExpectedReflectionException(ex))
        {
            return null;
        }
    }

    /// <summary>Determines whether an exception is an expected reflection access failure.</summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns><c>true</c> when the exception is an expected reflection failure; otherwise, <c>false</c>.</returns>
    private static bool IsExpectedReflectionException(Exception ex) =>
        ex is ArgumentException or InvalidOperationException or MethodAccessException or NotSupportedException or TargetException or TargetInvocationException;

    /// <summary>Sets a reflected field value.</summary>
    /// <param name="target">The object that owns the field.</param>
    /// <param name="field">The field to update.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><c>true</c> when the value was written; otherwise, <c>false</c>.</returns>
    private static bool TrySetFieldValue(object target, FieldInfo field, object? value)
    {
        try
        {
            field.SetValue(target, value);
            return true;
        }
        catch (Exception ex) when (IsExpectedReflectionException(ex))
        {
            return false;
        }
    }

    /// <summary>Sets a reflected property value.</summary>
    /// <param name="target">The object that owns the property.</param>
    /// <param name="property">The property to update.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><c>true</c> when the value was written; otherwise, <c>false</c>.</returns>
    private static bool TrySetPropertyValue(object target, PropertyInfo property, object? value)
    {
        try
        {
            property.SetValue(target, value, null);
            return true;
        }
        catch (Exception ex) when (IsExpectedReflectionException(ex))
        {
            return false;
        }
    }

    /// <summary>Gets or creates a nested table for the specified member name.</summary>
    /// <param name="table">The table that owns the child.</param>
    /// <param name="name">The child member name.</param>
    /// <returns>The existing or newly created nested table.</returns>
    private static HashTableRx GetOrCreateChildTable(HashTableRx table, string name) =>
        table.GetBaseValue(name) is HashTableRx childTable ? childTable : new(table.UseUpperCase);

    /// <summary>Gets or creates a nested table and clears existing values for a structure reload.</summary>
    /// <param name="table">The table that owns the child.</param>
    /// <param name="name">The child member name.</param>
    /// <returns>The existing or newly created nested table.</returns>
    private static HashTableRx GetOrCreateReloadChildTable(HashTableRx table, string name)
    {
        var childTable = GetOrCreateChildTable(table, name);
        childTable.ClearBaseValues();
        return childTable;
    }

    /// <summary>Reads a primitive value and converts TwinCAT string wrappers when needed.</summary>
    /// <param name="source">The source object to read.</param>
    /// <param name="memberType">The reflected member type.</param>
    /// <param name="readValue">The value reader.</param>
    /// <param name="value">The primitive or converted value.</param>
    /// <returns><c>true</c> when the value was read; otherwise, <c>false</c>.</returns>
    [RequiresUnreferencedCode("Uses reflection to locate TwinCAT string conversion helpers.")]
    private static bool TryReadPrimitiveValue(object source, Type memberType, Func<object?> readValue, out object? value)
    {
        try
        {
            value = readValue();
            if (!memberType.IsTwinCATStringArray())
            {
                return true;
            }

            var converter = source.GetType().GetMethod("ToStringArray", BindingFlags.Public | BindingFlags.Static);
            value = converter?.Invoke(null, [value]) as string[];
            return true;
        }
        catch (Exception ex) when (IsExpectedReflectionException(ex))
        {
            value = null;
            return false;
        }
    }

    /// <summary>Gets the value at the specified dotted path.</summary>
    /// <param name="fullName">The dotted path to read.</param>
    /// <returns>The value, or null when the path is missing.</returns>
    private object? GetFullName(string? fullName)
    {
        var names = NormalizePath(fullName)?.Split('.');
        if (names is null || names.Length == 0)
        {
            return null;
        }

        var table = this;
        for (var index = 0; index < names.Length - 1; index++)
        {
            var name = names[index];
            if (table.GetBaseValue(name) is not HashTableRx childTable)
            {
                return null;
            }

            table = childTable;
        }

        return table.GetBaseValue(names[^1]);
    }

    /// <summary>Determines whether this table or any nested table contains the specified key.</summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><c>true</c> when the key exists; otherwise, <c>false</c>.</returns>
    private bool ContainsKeyRecursive(object key)
    {
        if (ContainsKey(key))
        {
            return true;
        }

        foreach (var tableKey in Keys)
        {
            if (GetBaseValue(tableKey) is HashTableRx table && table.ContainsKey(key, searchAll: true))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Loads all reflected fields into a table.</summary>
    /// <param name="source">The source object to read.</param>
    /// <param name="fields">The reflected fields.</param>
    /// <param name="table">The table to populate.</param>
    /// <param name="parentPath">The optional parent path.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private void LoadFields(object source, FieldInfo[] fields, HashTableRx table, string? parentPath)
    {
        for (var index = 0; index < fields.Length; index++)
        {
            LoadField(source, fields[index], table, parentPath);
        }
    }

    /// <summary>Loads one reflected field into a table.</summary>
    /// <param name="source">The source object to read.</param>
    /// <param name="field">The reflected field.</param>
    /// <param name="table">The table to populate.</param>
    /// <param name="parentPath">The optional parent path.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private void LoadField(object source, FieldInfo field, HashTableRx table, string? parentPath)
    {
        var name = field.Name;
        var fullName = CombineName(parentPath, name);
        if (field.FieldType.IsPrimitiveArray())
        {
            if (TryReadPrimitiveValue(source, field.FieldType, () => field.GetValue(source), out var value))
            {
                SetLeafValue(table, name, fullName, value);
            }

            return;
        }

        LoadNestedValue(field.GetValue(source), field.FieldType, table, name, fullName, field);
    }

    /// <summary>Loads a nested reflected member into a child table.</summary>
    /// <param name="value">The nested member value.</param>
    /// <param name="type">The nested member type.</param>
    /// <param name="table">The table that owns the child.</param>
    /// <param name="name">The child member name.</param>
    /// <param name="fullName">The full child path.</param>
    /// <param name="member">The reflected member metadata.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private void LoadNestedValue(object? value, Type type, HashTableRx table, string name, string fullName, MemberInfo member)
    {
        if (value is null)
        {
            return;
        }

        var childTable = GetOrCreateReloadChildTable(table, name);
        childTable.Tag[name] = member;
        LoadObject(value, type, childTable, fullName);
        if (childTable.Count == 0)
        {
            table.RemoveBaseValue(name);
            return;
        }

        table.SetBaseValue(name, childTable);
    }

    /// <summary>Loads all reflected members from an object into a table.</summary>
    /// <param name="source">The source object to read.</param>
    /// <param name="type">The reflected source type.</param>
    /// <param name="table">The table to populate.</param>
    /// <param name="parentPath">The optional parent path.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private void LoadObject(object source, Type type, HashTableRx table, string? parentPath)
    {
        LoadProperties(source, type.GetProperties(), table, parentPath);
        LoadFields(source, type.GetFields(), table, parentPath);
    }

    /// <summary>Loads all reflected properties into a table.</summary>
    /// <param name="source">The source object to read.</param>
    /// <param name="properties">The reflected properties.</param>
    /// <param name="table">The table to populate.</param>
    /// <param name="parentPath">The optional parent path.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private void LoadProperties(object source, PropertyInfo[] properties, HashTableRx table, string? parentPath)
    {
        for (var index = 0; index < properties.Length; index++)
        {
            LoadProperty(source, properties[index], table, parentPath);
        }
    }

    /// <summary>Loads one reflected property into a table.</summary>
    /// <param name="source">The source object to read.</param>
    /// <param name="property">The reflected property.</param>
    /// <param name="table">The table to populate.</param>
    /// <param name="parentPath">The optional parent path.</param>
    [RequiresUnreferencedCode("Uses reflection over fields and properties which may be trimmed in AOT.")]
    private void LoadProperty(object source, PropertyInfo property, HashTableRx table, string? parentPath)
    {
        if (!CanUseProperty(property))
        {
            return;
        }

        var name = property.Name;
        var fullName = CombineName(parentPath, name);
        if (property.PropertyType.IsPrimitiveArray())
        {
            if (TryReadPrimitiveValue(source, property.PropertyType, () => property.GetValue(source, null), out var value))
            {
                SetLeafValue(table, name, fullName, value);
            }

            return;
        }

        LoadNestedValue(TryGetPropertyValue(source, property), property.PropertyType, table, name, fullName, property);
    }

    /// <summary>Normalizes a dotted path based on the table casing mode.</summary>
    /// <param name="fullName">The dotted path to normalize.</param>
    /// <returns>The normalized dotted path.</returns>
    private string? NormalizePath(string? fullName) =>
        UseUpperCase ? fullName?.ToUpperInvariant() : fullName;

    /// <summary>Sets the value at the specified dotted path.</summary>
    /// <param name="fullName">The dotted path to set.</param>
    /// <param name="value">The value to store.</param>
    private void SetFullName(string? fullName, object? value)
    {
        if (NormalizePath(fullName) is not { Length: > 0 } normalizedName || value is null)
        {
            return;
        }

        ValueChanging(normalizedName);
        SetFullNameValue(normalizedName, value);
        ValueChanged(normalizedName, value);
    }

    /// <summary>Sets a normalized dotted-path value in the backing table hierarchy.</summary>
    /// <param name="fullName">The normalized dotted path.</param>
    /// <param name="value">The value to store.</param>
    private void SetFullNameValue(string fullName, object value)
    {
        var names = fullName.Split('.');
        var table = this;
        for (var index = 0; index < names.Length - 1; index++)
        {
            var childTable = GetOrCreateChildTable(table, names[index]);
            table.SetBaseValue(names[index], childTable);
            table = childTable;
        }

        table.SetBaseValue(names[^1], value);
    }

    /// <summary>Sets a reflected leaf value and publishes change events from the root table.</summary>
    /// <param name="table">The table that stores the leaf.</param>
    /// <param name="name">The leaf member name.</param>
    /// <param name="fullName">The full leaf path.</param>
    /// <param name="value">The value to store.</param>
    private void SetLeafValue(HashTableRx table, string name, string fullName, object? value)
    {
        ValueChanging(fullName);
        table.SetBaseValue(name, value);
        ValueChanged(fullName, value);
    }

    /// <summary>Publishes a property changed event and an observable update.</summary>
    /// <param name="fullName">The changed value path.</param>
    /// <param name="value">The changed value.</param>
    private void ValueChanged(string fullName, object? value)
    {
        Subject.OnNextHasObservers((fullName, value));
        PropertyChanged?.Invoke(this, new(fullName));
    }

    /// <summary>Publishes a property changing event.</summary>
    /// <param name="fullName">The changing value path.</param>
    private void ValueChanging(string fullName)
    {
        PropertyChanging?.Invoke(this, new(fullName));
    }
}
