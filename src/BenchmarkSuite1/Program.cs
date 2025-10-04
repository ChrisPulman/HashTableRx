// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

namespace BenchmarkSuite1
{
    internal class Program
    {
        private static void Main(string[] args) => _ = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
