// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.Collections;

internal sealed class MemberAccessor(string name, Type memberType, Func<object, object?> getter, Action<object, object?>? setter)
{
    public string Name { get; } = name;

    public Type MemberType { get; } = memberType;

    public Func<object, object?> Getter { get; } = getter;

    public Action<object, object?>? Setter { get; } = setter;

    public bool IsPrimitiveLike => MemberType.IsPrimativeArray();
}
