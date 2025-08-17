// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace CP.Collections;

internal static class ReflectionAccessors
{
    private static readonly ConcurrentDictionary<Type, TypeAccessors> Cache = new();

    [RequiresUnreferencedCode("Builds expression-based accessors; members may be trimmed in AOT.")]
    public static TypeAccessors Get(Type type) => Cache.GetOrAdd(type, Build);

    [RequiresUnreferencedCode("Builds expression-based accessors; members may be trimmed in AOT.")]
    private static TypeAccessors Build(Type type)
    {
        var primitive = new List<MemberAccessor>();
        var complex = new List<MemberAccessor>();

        foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!p.CanRead)
            {
                continue;
            }

            var acc = CreateAccessorForProperty(p);
            (acc.IsPrimitiveLike ? primitive : complex).Add(acc);
        }

        foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            var acc = CreateAccessorForField(f);
            (acc.IsPrimitiveLike ? primitive : complex).Add(acc);
        }

        return new TypeAccessors(primitive, complex);
    }

    private static MemberAccessor CreateAccessorForProperty(PropertyInfo p)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valParam = Expression.Parameter(typeof(object), "val");
        var typedObj = Expression.Convert(objParam, p.DeclaringType!);
        var member = Expression.Property(typedObj, p);

        // Getter: (object o) => (object) ((TDecl)o).Prop
        var getBody = Expression.Convert(member, typeof(object));
        var getter = Expression.Lambda<Func<object, object?>>(getBody, objParam).Compile();

        Action<object, object?>? setter = null;
        if (p.CanWrite)
        {
            var converted = Expression.Convert(valParam, p.PropertyType);
            var assign = Expression.Assign(member, converted);

            var setLambda = Expression.Lambda<Action<object, object?>>(assign, objParam, valParam);
            setter = setLambda.Compile();
        }

        return new MemberAccessor(p.Name, p.PropertyType, getter, setter);
    }

    private static MemberAccessor CreateAccessorForField(FieldInfo f)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valParam = Expression.Parameter(typeof(object), "val");
        var typedObj = Expression.Convert(objParam, f.DeclaringType!);
        var member = Expression.Field(typedObj, f);

        var getBody = Expression.Convert(member, typeof(object));
        var getter = Expression.Lambda<Func<object, object?>>(getBody, objParam).Compile();

        var converted = Expression.Convert(valParam, f.FieldType);
        var assign = Expression.Assign(member, converted);
        var setter = Expression.Lambda<Action<object, object?>>(assign, objParam, valParam).Compile();

        return new MemberAccessor(f.Name, f.FieldType, getter, setter);
    }
}
