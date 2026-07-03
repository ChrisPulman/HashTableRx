// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Provides a compatibility copy of the unconditional suppress-message attribute.</summary>
/// <param name="category">The diagnostic category.</param>
/// <param name="checkId">The diagnostic identifier.</param>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute(string category, string checkId) : Attribute
{
    /// <summary>Gets the diagnostic category.</summary>
    public string Category { get; } = category;

    /// <summary>Gets the diagnostic identifier.</summary>
    public string CheckId { get; } = checkId;

    /// <summary>Gets or sets the optional justification.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets the optional message identifier.</summary>
    public string? MessageId { get; set; }

    /// <summary>Gets or sets the optional diagnostic scope.</summary>
    public string? Scope { get; set; }

    /// <summary>Gets or sets the optional diagnostic target.</summary>
    public string? Target { get; set; }
}
#endif
