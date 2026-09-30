// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

namespace BenchmarkSuite1;

/// <summary>Runs the benchmark suite in this assembly.</summary>
internal static class Program
{
    /// <summary>Runs all discovered benchmarks in this assembly.</summary>
    private static void Main() => _ = BenchmarkRunner.Run(typeof(Program).Assembly);
}
