// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.Collections;

internal sealed class TypeAccessors(IReadOnlyList<MemberAccessor> primitiveLike, IReadOnlyList<MemberAccessor> complex)
{
    public IReadOnlyList<MemberAccessor> PrimitiveLike { get; } = primitiveLike;

    public IReadOnlyList<MemberAccessor> Complex { get; } = complex;
}
