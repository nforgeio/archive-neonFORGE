﻿//-----------------------------------------------------------------------------
// FILE:	    StaticBytesDataSource.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
    /// <summary>
    /// Implements a <see cref="IStaticDataSource"/> that wraps an in-memory byte array
    /// into a form suitable for adding to a <see cref="ZipFile"/>.
    /// </summary>
    public class StaticBytesDataSource : IStaticDataSource
    {
        private byte[] data;

        /// <summary>
        /// Constructs a source from raw bytes.
        /// </summary>
        /// <param name="data">The data array or <c>null</c>.</param>
        public StaticBytesDataSource(byte[] data)
        {
            this.data = data;
        }

        /// <summary>
        /// Constructs a source from a UTG-8 encopded string.
        /// </summary>
        /// <param name="data">The data string</param>
        public StaticBytesDataSource(string data)
        {
            this.data = Encoding.UTF8.GetBytes(data);
        }

        /// <inheritdoc/>
        public Stream GetSource()
        {
            if (data == null)
            {
                return new MemoryStream();
            }
            else
            {
                return new MemoryStream(data);
            }
        }
    }
}
