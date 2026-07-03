// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Provides a compatibility copy of the trimming attribute for older target frameworks.</summary>
/// <param name="message">The diagnostic message.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Struct, Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute(string message) : Attribute
{
    /// <summary>Gets the diagnostic message.</summary>
    public string Message { get; } = message;

    /// <summary>Gets or sets the optional documentation URL.</summary>
    public string? Url { get; set; }
}
#endif
