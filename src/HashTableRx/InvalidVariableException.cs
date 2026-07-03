// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace CP.Collections.Reactive;
#else
namespace CP.Collections;
#endif

/// <summary>Represents an invalid PLC variable lookup.</summary>
[Serializable]
public class InvalidVariableException : Exception
{
    /// <summary>Stores the invalid variable name.</summary>
    private readonly string? _variable;

    /// <summary>Initializes a new instance of the <see cref="InvalidVariableException"/> class.</summary>
    public InvalidVariableException() => _variable = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="InvalidVariableException"/> class.</summary>
    /// <param name="variable">The invalid variable name.</param>
    public InvalidVariableException(string? variable) => _variable = variable;

    /// <summary>Initializes a new instance of the <see cref="InvalidVariableException"/> class.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public InvalidVariableException(string message, Exception innerException)
        : base(message, innerException) => _variable = message;

    /// <summary>Gets a message that describes the current exception.</summary>
    public override string Message => $"The variable - {_variable} - does not exist in the PLC";
}
