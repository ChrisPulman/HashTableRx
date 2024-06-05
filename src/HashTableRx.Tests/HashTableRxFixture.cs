// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace CP.Collections.Tests
{
    /// <summary>
    /// HashTableRxFixture.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class HashTableRxFixture : IDisposable
    {
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashTableRxFixture"/> class.
        /// </summary>
        public HashTableRxFixture()
        {
            // get the current directory
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // load the assembly MockLibraryWithFields.dll
            var assembly = Assembly.LoadFrom(currentDirectory + "\\MockLibraryWithFields.dll");
            Assert.NotNull(assembly);

            // Create an instance of the MockLibraryWithFields.MockClassWithFields class
            var obj = assembly.CreateInstance("TwinCATRx.RigSTRUCT");
            Assert.NotNull(obj);

            HtRx = new(false);
            HtRx.SetStucture(obj);
        }

        /// <summary>
        /// Gets the HashTable Rx.
        /// </summary>
        /// <value>
        /// The ht rx.
        /// </value>
        public HashTableRx HtRx { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
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
                    HtRx.Dispose();
                }

                _disposedValue = true;
            }
        }
    }
}
