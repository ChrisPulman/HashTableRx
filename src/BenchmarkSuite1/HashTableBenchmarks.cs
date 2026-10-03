// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using CP.Collections;

namespace Benchmarks;

/// <summary>Benchmarks read and write operations on <see cref="HashTable"/>.</summary>
[MemoryDiagnoser]
public class HashTableBenchmarks : IDisposable
{
    /// <summary>Stores the table instance under test.</summary>
    private HashTable _ht = [];

    /// <summary>Stores the rotating benchmark keys.</summary>
    private string[] _keys = [];

    /// <summary>Stores the current key index.</summary>
    private int _idx;

    /// <summary>Tracks whether cleanup already ran.</summary>
    private bool _disposedValue;

    /// <summary>Gets or sets the number of distinct keys to pre-populate and iterate over.</summary>
    [Params(1000, 10_000)]
    public int N { get; set; }

    /// <summary>Populates the table with <see cref="N"/> keys and values.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _ht = [];
        _keys = new string[N];
        for (var i = 0; i < N; i++)
        {
            var key = $"K{i}";
            _keys[i] = key;
            _ht[key] = i;
        }

        _idx = 0;
        _disposedValue = false;
    }

    /// <summary>Disposes the table after the benchmark run completes.</summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <summary>Reads a value via the indexer for a rotating key.</summary>
    /// <returns>The value for the current key.</returns>
    [Benchmark]
    public object? Read_Indexer()
    {
        var key = NextKey();
        return _ht[key];
    }

    /// <summary>Writes a value via the indexer for a rotating key.</summary>
    [Benchmark]
    public void Write_Indexer()
    {
        var key = NextKey();
        _ht[key] = _idx;
    }

    /// <summary>Releases the benchmark resources.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the managed resources for this benchmark when requested.</summary>
    /// <param name="disposing">true to release managed resources; otherwise, false.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue || !disposing)
        {
            return;
        }

        _disposedValue = true;
        _ht.Dispose();
    }

    /// <summary>Returns the next key in the benchmark rotation.</summary>
    /// <returns>The next key.</returns>
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
