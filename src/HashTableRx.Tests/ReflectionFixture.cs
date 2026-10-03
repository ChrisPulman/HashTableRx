// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace CP.Collections.Tests;

/// <summary>Creates runtime field-bearing structures for exercising PLC reflection contracts.</summary>
internal static class ReflectionFixture
{
    /// <summary>Creates a field-bearing copy of a fixture whose field members are named explicitly.</summary>
    /// <param name="model">The property-based fixture template.</param>
    /// <returns>A runtime structure containing equivalent values and public fields.</returns>
    internal static object Create(object model)
    {
        var template = model.GetType();
        if (!HasFields(template))
        {
            return model;
        }

        var module = AssemblyBuilder.DefineDynamicAssembly(new($"{nameof(ReflectionFixture)}_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect)
            .DefineDynamicModule("Fixtures");
        var builder = module.DefineType(template.Name, TypeAttributes.Public | TypeAttributes.Sealed);
        var values = new Dictionary<string, object?>();
        foreach (var property in template.GetProperties())
        {
            var value = property.GetValue(model);
            if (value is not null && HasFields(value.GetType()))
            {
                value = Create(value);
            }

            values[property.Name] = value;
            var memberType = value?.GetType() ?? property.PropertyType;
            if (IsField(property.Name))
            {
                _ = builder.DefineField(property.Name, memberType, FieldAttributes.Public);
            }
            else
            {
                DefineProperty(builder, property.Name, memberType);
            }
        }

        var converter = template.GetMethod("ToStringArray", BindingFlags.Public | BindingFlags.Static);
        if (converter is not null)
        {
            var method = builder.DefineMethod(converter.Name, MethodAttributes.Public | MethodAttributes.Static, converter.ReturnType, [converter.GetParameters()[0].ParameterType]);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, converter);
            il.Emit(OpCodes.Ret);
        }

        var instance = Activator.CreateInstance(builder.CreateType()!)!;
        foreach (var (name, value) in values)
        {
            Set(instance, name, value);
        }

        return instance;
    }

    /// <summary>Copies a runtime structure back into its typed template for assertions.</summary>
    /// <typeparam name="T">The property-based fixture type.</typeparam>
    /// <param name="instance">The runtime structure containing values written by the table.</param>
    /// <returns>A typed fixture containing the current structure values.</returns>
    internal static T Read<T>(object instance)
        where T : class, new()
    {
        return (T)Read(instance, typeof(T));
    }

    /// <summary>Replaces a runtime member to exercise missing or null nested-member guards.</summary>
    /// <param name="instance">The runtime structure.</param>
    /// <param name="name">The member name.</param>
    /// <param name="value">The replacement value.</param>
    internal static void Set(object instance, string name, object? value)
    {
        var type = instance.GetType();
        if (type.GetField(name) is { } field)
        {
            field.SetValue(instance, value);
        }
        else
        {
            type.GetProperty(name)!.SetValue(instance, value);
        }
    }

    /// <summary>Creates a structure with a setter-only property.</summary>
    /// <returns>A structure whose property cannot be read.</returns>
    internal static object CreateWriteOnly()
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new("WriteOnlyFixture"), AssemblyBuilderAccess.RunAndCollect)
            .DefineDynamicModule("Fixtures");
        var builder = module.DefineType("WriteOnlyRoot", TypeAttributes.Public | TypeAttributes.Sealed);
        var property = builder.DefineProperty("WriteOnly", PropertyAttributes.None, typeof(int), null);
        var setter = builder.DefineMethod("set_WriteOnly", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, [typeof(int)]);
        setter.GetILGenerator().Emit(OpCodes.Ret);
        property.SetSetMethod(setter);
        return Activator.CreateInstance(builder.CreateType()!)!;
    }

    /// <summary>Copies runtime values into a property-based fixture recursively.</summary>
    /// <param name="instance">The runtime structure.</param>
    /// <param name="template">The requested fixture type.</param>
    /// <returns>The typed fixture or the original compatible value.</returns>
    private static object Read(object instance, Type template)
    {
        if (template.IsInstanceOfType(instance))
        {
            return instance;
        }

        var result = Activator.CreateInstance(template)!;
        foreach (var property in template.GetProperties())
        {
            var type = instance.GetType();
            var value = type.GetField(property.Name) is { } field
                ? field.GetValue(instance)
                : type.GetProperty(property.Name)!.GetValue(instance);
            if (value is not null && !property.PropertyType.IsInstanceOfType(value))
            {
                value = Read(value, property.PropertyType);
            }

            property.SetValue(result, value);
        }

        return result;
    }

    /// <summary>Identifies templates containing members that must become public fields.</summary>
    /// <param name="type">The fixture template type.</param>
    /// <returns>true when the fixture requires runtime fields; otherwise, false.</returns>
    private static bool HasFields(Type type) => Array.Exists(type.GetProperties(), static property => IsField(property.Name));

    /// <summary>Identifies the explicit field-member naming convention of reflection fixtures.</summary>
    /// <param name="name">The member name.</param>
    /// <returns>true for field fixture members; otherwise, false.</returns>
    private static bool IsField(string name) => name.StartsWith("Field", StringComparison.Ordinal) || name.EndsWith("Field", StringComparison.Ordinal);

    /// <summary>Defines an ordinary readable and writable property on a runtime fixture.</summary>
    /// <param name="builder">The runtime fixture type.</param>
    /// <param name="name">The property name.</param>
    /// <param name="type">The property value type.</param>
    private static void DefineProperty(TypeBuilder builder, string name, Type type)
    {
        var field = builder.DefineField($"_{name}", type, FieldAttributes.Private);
        var property = builder.DefineProperty(name, PropertyAttributes.None, type, null);
        const MethodAttributes Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        var getter = builder.DefineMethod($"get_{name}", Attributes, type, Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, field);
        getterIl.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        var setter = builder.DefineMethod($"set_{name}", Attributes, null, [type]);
        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, field);
        setterIl.Emit(OpCodes.Ret);
        property.SetSetMethod(setter);
    }
}
