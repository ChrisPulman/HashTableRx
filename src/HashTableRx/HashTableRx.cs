// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;

namespace CP.Collections;

/// <summary>
/// A Specialized HashTable providing on change notification and allowing reflection of objects
/// to a list. The values are then get-able and set-able via it's path i.e. thisClass.thatClass.thisValue.
/// </summary>
[Serializable]
public class HashTableRx : HashTable, IHashTableRx
{
    private const string PropertyInfo = nameof(PropertyInfo);
    private const string FieldInfo = nameof(FieldInfo);
    private const string Data = nameof(Data);

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTableRx" /> class.
    /// </summary>
    /// <param name="useUpperCase">if set to <c>true</c> [is upper case].</param>
    public HashTableRx(bool useUpperCase)
    {
        UseUpperCase = useUpperCase;
        Tag = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTableRx"/> class.
    /// </summary>
    /// <param name="source">The source.</param>
    public HashTableRx(IObservable<(string key, object? value)> source)
        : base(source) => Tag = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTableRx"/> class.
    /// </summary>
    /// <param name="info">The Serialization Info.</param>
    /// <param name="context">The Context.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1231:Make parameter ref read-only", Justification = "not required.")]
    protected HashTableRx(SerializationInfo info, StreamingContext context) =>
        Tag = [];

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when a property value is changing.
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// Gets or sets a value indicating whether this instance uses upper case.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance uses upper case; otherwise, <c>false</c>.
    /// </value>
    public bool UseUpperCase { get; set; }

    /// <summary>
    /// Gets the observe all.
    /// </summary>
    /// <value>The observe all.</value>
    public IObservable<(string key, object? value)> ObserveAll => Subject.DistinctUntilChanged().Publish().RefCount();

    /// <summary>
    /// Gets the tag.
    /// </summary>
    /// <value>The tag.</value>
    public HashTable? Tag { get; }

    /// <summary>
    /// Gets or sets the <see cref="object"/> with the specified full name.
    /// </summary>
    /// <value>The <see cref="object"/>.</value>
    /// <param name="fullName">The full name.</param>
    /// <returns>An object.</returns>
    public object? this[string fullName]
    {
        get => GetFullName(fullName);
        set => SetFullName(fullName, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="object"/> with the specified use reflection.
    /// </summary>
    /// <value>The <see cref="object"/>.</value>
    /// <param name="useReflection">if set to <c>true</c> [use reflection].</param>
    /// <returns>An object.</returns>
    public object? this[bool useReflection]
    {
        get
        {
            var exHashtable = this;
            return GetByReflection(ref exHashtable);
        }

        set
        {
            if (value != null)
            {
                var exHashtable = this;
                SetByReflection(ref exHashtable, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="HashTableRx" /> with the specified _default.
    /// </summary>
    /// <value>
    /// The <see cref="HashTableRx" />.
    /// </value>
    /// <param name="unused">if set to <c>true</c> [unused].</param>
    /// <param name="key">The key.</param>
    /// <returns>
    /// A reactive Hash Table.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "intended alternate")]
    private HashTableRx? this[bool unused, object? key]
    {
        get => (HashTableRx?)this[key!] ?? new HashTableRx(UseUpperCase);
        set => this[key!] = value;
    }

    /// <summary>
    /// Gets or sets the <see cref="object"/> with the specified key.
    /// </summary>
    /// <value>The <see cref="object"/>.</value>
    /// <param name="key">The key.</param>
    /// <param name="isEnd">if set to <c>true</c> [is end].</param>
    /// <returns>An object.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "intended alternate")]
    private object? this[object? key, bool isEnd]
    {
        get => key == null ? null : this[key];

        set
        {
            if (key != null)
            {
                this[key] = value;
            }
        }
    }

    /// <summary>
    /// Adds an element with the specified key and value into the <see cref="Hashtable"/>.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add. The value can be null.</param>
    public new void Add(object key, object? value) => base.Add(key, value);

    /// <summary>
    /// Adds an element with the specified key and value into the <see cref="Hashtable"/>.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add. The value can be null.</param>
    public void Add(object key, HashTableRx value) => base.Add(key, value);

    /// <summary>
    /// Determines whether the specified key contains key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="searchAll">if set to <c>true</c> [search all].</param>
    /// <returns>A Boolean.</returns>
    public bool ContainsKey(object key, bool searchAll) =>
        searchAll ? ContainsKey(key) || HtContainsKey(key, Keys) : ContainsKey(key);

    /// <summary>
    /// Gets the by reflection.
    /// </summary>
    /// <param name="htrx">The Reactive Hash Table.</param>
    /// <returns>An object.</returns>
    private static object? GetByReflection(ref HashTableRx htrx)
    {
        if (htrx.Tag?.Count == 0)
        {
            return null;
        }

        var data = htrx.Tag?[Data];
        try
        {
            if (htrx.Tag?[PropertyInfo] != null)
            {
                foreach (PropertyInfo propertyInfo in (IEnumerable?)htrx.Tag[PropertyInfo]!)
                {
                    if (propertyInfo?.PropertyType?.IsPrimativeArray() == true)
                    {
                        propertyInfo?.SetValue(data, htrx[propertyInfo.Name, true], null);
                    }
                }
            }
        }
        catch
        {
        }

        if (htrx.Tag?[FieldInfo] != null)
        {
            try
            {
                if (htrx.Tag[FieldInfo] != null)
                {
                    foreach (FieldInfo fieldInfo in (IEnumerable?)htrx.Tag[FieldInfo]!)
                    {
                        if (fieldInfo?.FieldType?.IsPrimativeArray() == true)
                        {
                            fieldInfo?.SetValue(data, htrx[fieldInfo.Name, true]);
                        }
                        else
                        {
                            var eHt = htrx;
                            var name = fieldInfo?.Name;
                            var item = eHt[true, name];
                            var obj = fieldInfo?.GetValue(data);
                            GetFieldByReflection(fieldInfo, ref item!, ref obj);
                            eHt[true, name] = item;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return data;
    }

    /// <summary>
    /// Gets the by reflection.
    /// </summary>
    /// <param name="fi">The field info.</param>
    /// <param name="htrx">The Reactive Hash Table.</param>
    /// <param name="data">The data.</param>
    private static void GetFieldByReflection(FieldInfo? fi, ref HashTableRx htrx, ref object? data)
    {
        var properties = fi?.FieldType.GetProperties();
        for (var i = 0; i < checked(properties?.Length); i++)
        {
            var propertyInfo = properties[i];
            if (propertyInfo?.PropertyType.IsPrimativeArray() == true)
            {
                propertyInfo.SetValue(data, htrx[propertyInfo.Name, true], null);
            }
        }

        var fields = fi?.FieldType.GetFields();
        for (var j = 0; j < checked(fields?.Length); j++)
        {
            var fieldInfo = fields[j];
            if (fieldInfo.FieldType.IsPrimativeArray())
            {
                fieldInfo.SetValue(data, htrx[fieldInfo.Name, true]);
            }
            else
            {
                var htRx = htrx;
                var name = fieldInfo.Name;
                var item = htRx[true, name];
                var subData = fieldInfo.GetValue(data);
                GetFieldByReflection(fieldInfo, ref item!, ref subData);
                htRx[true, name] = item;
            }
        }
    }

    /// <summary>
    /// Gets the full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <param name="htrx">The EHT.</param>
    /// <returns>An object.</returns>
    private static object? GetFullName(string? fullName, ref HashTableRx htrx)
    {
        if (htrx == null)
        {
            return null;
        }

        if (htrx.UseUpperCase)
        {
            fullName = fullName?.ToUpper();
        }

        var names = fullName?.Split('.');
        var firstName = names![0];
        if (checked(names.Length) <= 1)
        {
            return htrx[firstName, true];
        }

        htrx[true, firstName] ??= new(htrx.UseUpperCase);

        var str = fullName?.Remove(0, checked(fullName.IndexOf('.') + 1));
        var htRx = htrx;
        var item = htRx[true, firstName];
        var obj = GetFullName(str, ref item!);
        htRx[true, firstName] = item;
        return obj;
    }

    /// <summary>
    /// The Hash Table contains key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="hashTableBase">The Hash Table base.</param>
    /// <returns>A Boolean.</returns>
    private static bool HtContainsKey(object key, IEnumerable hashTableBase)
    {
        try
        {
            foreach (HashTable ht in hashTableBase)
            {
                if (ht.ContainsKey(key))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    /// <summary>
    /// Sets the full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <param name="htrx">The Reactive HashTable.</param>
    /// <param name="value">The value.</param>
    private static void SetFullName(string? fullName, ref HashTableRx htrx, object value)
    {
        if (htrx.UseUpperCase)
        {
            fullName = fullName?.ToUpper();
        }

        var names = fullName?.Split('.');
        var firstName = names![0];
        if (checked(names.Length) <= 1)
        {
            htrx[firstName, true] = value;
        }
        else
        {
            var remainingNames = fullName?.Remove(0, checked(fullName.IndexOf('.') + 1));
            var htRx = htrx;
            var item = htRx[true, firstName];
            SetFullName(remainingNames, ref item!, value);
            htRx[true, firstName] = item;
        }
    }

    /// <summary>
    /// Gets the full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <returns>An object.</returns>
    private object? GetFullName(string? fullName)
    {
        if (UseUpperCase)
        {
            fullName = fullName?.ToUpper();
        }

        var names = fullName?.Split('.');
        var firstName = names![0];
        if (checked(names.Length) <= 1)
        {
            return base[firstName];
        }

        var remainingNames = fullName?.Remove(0, checked(fullName.IndexOf('.') + 1));
        var item = (HashTableRx?)base[firstName];
        var obj = GetFullName(remainingNames, ref item!);
        base[firstName] = item;
        return obj;
    }

    /// <summary>
    /// Sets the by reflection.
    /// </summary>
    /// <param name="htrx">The EHT.</param>
    /// <param name="value">The value.</param>
    private void SetByReflection(ref HashTableRx htrx, object value)
    {
        if (value == null)
        {
            return;
        }

        htrx.Tag![Data] = value;
        htrx.Tag[FieldInfo] = value.GetType().GetFields();
        htrx.Tag[PropertyInfo] = value.GetType().GetProperties();
        try
        {
            if (htrx.Tag.Count > 0 && htrx.Tag[PropertyInfo] != null)
            {
                foreach (PropertyInfo propertyInfo in (IEnumerable?)htrx.Tag[PropertyInfo]!)
                {
                    var name = propertyInfo.Name;
                    if (propertyInfo.PropertyType?.IsPrimativeArray() == true)
                    {
                        ValueChanging(name);
                        var obj = propertyInfo.GetValue(value, null);
                        htrx[name, true] = obj;
                        ValueChanged(name, obj);
                    }
                    else
                    {
                        htrx[true, name]!.Tag![name] = propertyInfo;
                        var htRx = htrx;
                        var item = htRx[true, name];
                        SetPropertyByReflection(propertyInfo, ref item!, propertyInfo.GetValue(value)!, name);
                        htRx[true, name] = item;
                    }
                }
            }
        }
        catch
        {
        }

        try
        {
            if (htrx.Tag.Count > 0 && htrx.Tag[FieldInfo] != null)
            {
                foreach (FieldInfo fieldInfo in (IEnumerable?)htrx.Tag[FieldInfo]!)
                {
                    var name = fieldInfo.Name;
                    if (fieldInfo.FieldType?.IsPrimativeArray() == true)
                    {
                        ValueChanging(name);
                        var obj = fieldInfo.GetValue(value);
                        htrx[name, true] = obj;
                        ValueChanged(name, obj);
                    }
                    else
                    {
                        htrx[true, name]!.Tag![name] = fieldInfo;
                        var htRx = htrx;
                        var item = htRx[true, name];
                        SetFieldByReflection(fieldInfo, ref item!, fieldInfo.GetValue(value), name);
                        htRx[true, name] = item;
                    }
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Sets the by reflection.
    /// </summary>
    /// <param name="fi">The field info.</param>
    /// <param name="eht">The Reactive Hash Table.</param>
    /// <param name="value">The value.</param>
    /// <param name="fullName">The full name.</param>
    private void SetFieldByReflection(FieldInfo? fi, ref HashTableRx eht, object? value, string fullName)
    {
        var properties = fi?.FieldType.GetProperties();
        for (var i = 0; i < checked(properties?.Length); i++)
        {
            var propertyInfo = properties[i];
            var name = propertyInfo.Name;
            if (propertyInfo.PropertyType.IsPrimativeArray())
            {
                var nameOfChange = $"{fullName}.{name}";
                ValueChanging(nameOfChange);
                var obj = propertyInfo.GetValue(value, null);
                eht[propertyInfo.Name, true] = obj;
                ValueChanged(nameOfChange, obj);
            }
            else
            {
                eht[true, name]!.Tag![name] = propertyInfo;
                var eHt = eht;
                var item = eHt[true, name];
                SetPropertyByReflection(propertyInfo, ref item!, propertyInfo.GetValue(value)!, fullName + "." + name);
                eHt[true, name] = item;
            }
        }

        var fields = fi?.FieldType.GetFields();
        for (var j = 0; j < checked(fields?.Length); j++)
        {
            var fieldInfo = fields[j];
            var name = fieldInfo.Name;
            if (fieldInfo.FieldType.IsPrimativeArray())
            {
                var nameOfChange = $"{fullName}.{name}";
                ValueChanging(nameOfChange);
                var obj = fieldInfo.GetValue(value);
                eht[name, true] = obj;
                ValueChanged(nameOfChange, obj);
            }
            else
            {
                eht[true, name]!.Tag![name] = fieldInfo;
                var eHt = eht;
                var item = eHt[true, name];
                SetFieldByReflection(fieldInfo, ref item!, fieldInfo.GetValue(value), fullName + "." + name);
                eHt[true, name] = item;
            }
        }
    }

    /// <summary>
    /// Sets the full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <param name="value">The value.</param>
    private void SetFullName(string? fullName, object? value)
    {
        if (value == null)
        {
            return;
        }

        if (UseUpperCase)
        {
            fullName = fullName?.ToUpper();
        }

        ValueChanging(fullName);
        var names = fullName?.Split('.');
        var firstName = names![0];
        if (checked(names.Length) <= 1)
        {
            base[firstName] = value;
        }
        else
        {
            base[firstName] ??= new HashTableRx(Source!) { UseUpperCase = UseUpperCase };

            var str = fullName?.Remove(0, checked(fullName.IndexOf('.') + 1));
            var item = (HashTableRx?)base[firstName];
            SetFullName(str, ref item!, value);
            base[firstName] = item;
        }

        ValueChanged(fullName, value);
    }

    /// <summary>
    /// Sets the property by reflection.
    /// </summary>
    /// <param name="pi">The property info.</param>
    /// <param name="htrx">The Reactive Hash Table.</param>
    /// <param name="value">The value.</param>
    /// <param name="fullName">The full name.</param>
    private void SetPropertyByReflection(PropertyInfo pi, ref HashTableRx htrx, object value, string fullName)
    {
        var properties = pi.PropertyType.GetProperties();
        for (var i = 0; i < checked(properties.Length); i++)
        {
            var propertyInfo = properties[i];
            var name = propertyInfo.Name;
            if (propertyInfo.PropertyType.IsPrimativeArray())
            {
                var nameOfChange = $"{fullName}.{name}";
                ValueChanging(nameOfChange);
                var obj = propertyInfo.GetValue(value, null);
                htrx[name, true] = obj;
                ValueChanged(nameOfChange, obj);
            }
            else
            {
                htrx[true, name]!.Tag![name] = propertyInfo;
                var htRx = htrx;
                var item = htRx[true, name];
                SetPropertyByReflection(propertyInfo, ref item!, propertyInfo.GetValue(value)!, fullName + "." + name);
                htRx[true, name] = item;
            }
        }

        var fields = pi.PropertyType.GetFields();
        for (var j = 0; j < checked(fields.Length); j++)
        {
            var fieldInfo = fields[j];
            var name = fieldInfo.Name;
            if (fieldInfo.FieldType.IsPrimativeArray())
            {
                var nameOfChange = $"{fullName}.{name}";
                ValueChanging(nameOfChange);
                var obj = fieldInfo.GetValue(value);
                htrx[name, true] = obj;
                ValueChanged(nameOfChange, obj);
            }
            else
            {
                htrx[true, name]!.Tag![name] = fieldInfo;
                var htRx = htrx;
                var item = htRx[true, name];
                SetFieldByReflection(fieldInfo, ref item!, fieldInfo.GetValue(value), fullName + "." + name);
                htRx[true, name] = item;
            }
        }
    }

    /// <summary>
    /// Values the changed.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <param name="value">The value.</param>
    private void ValueChanged(string? fullName, object? value)
    {
        if (fullName != null)
        {
            Subject.OnNextHasObservers((fullName, value));
            PropertyChanged?.Invoke(this, new(fullName));
        }
    }

    /// <summary>
    /// Values the changing.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    private void ValueChanging(string? fullName)
    {
        if (fullName != null)
        {
            PropertyChanging?.Invoke(this, new(fullName));
        }
    }
}
