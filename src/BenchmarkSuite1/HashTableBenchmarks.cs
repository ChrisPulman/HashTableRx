// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using CP.Collections;

namespace Benchmarks;

/// <summary>
/// Benchmarks for read and write operations on CP.Collections.HashTable.
/// </summary>
[MemoryDiagnoser]
public class HashTableBenchmarks : IDisposable
{
    private HashTable _ht = null!;
    private string[] _keys = null!;
    private int _idx;
    private bool _disposedValue;

    /// <summary>
    /// Gets or sets the number of distinct keys to pre-populate and iterate over.
    /// </summary>
    [Params(1000, 10000)]
    public int N { get; set; }

    /// <summary>
    /// Global setup: populate the table with N keys and values.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _ht = [];
        _keys = new string[N];
        for (var i = 0; i < N; i++)
        {
            var k = "K" + i.ToString();
            _keys[i] = k;
            _ht[k] = i;
        }

        _idx = 0;
    }

    /// <summary>
    /// Global cleanup: dispose the table.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <summary>
    /// Reads a value via the indexer for a rotating key.
    /// </summary>
    /// <returns>The value for the current key.</returns>
    [Benchmark]
    public object? Read_Indexer()
    {
        var k = NextKey();
        return _ht[k];
    }

    /// <summary>
    /// Writes a value via the indexer for a rotating key.
    /// </summary>
    [Benchmark]
    public void Write_Indexer()
    {
        var k = NextKey();
        _ht[k] = _idx; // overwrite existing
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _ht.Dispose();
            }

            _disposedValue = true;
        }
    }

    private string NextKey()
    {
        _idx++;
        if (_idx >= _keys.Length)
        {
            _idx = 0;
        }

        return _keys[_idx];
    }
}
