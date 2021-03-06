﻿//-----------------------------------------------------------------------------
// FILE:	    PreprocessReader.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Neon.Common;

namespace Neon.IO
{
    /// <summary>
    /// Generates a globally unique temporary file name and then 
    /// ensures that the file is removed when the instance is 
    /// disposed.
    /// </summary>
    public sealed class TempFile : IDisposable
    {
        private bool isDisposed = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="suffix">
        /// Optioonally specifies the file suffix (including the leading period) to be
        /// appended to the generated file name.  This defaults to <b>.tmp</b>.
        /// </param>
        /// <param name="folder">
        /// Optionaly specifies the target folder.  This defaults to the standard
        /// temporary directory for the current user.
        /// </param>
        public TempFile(string suffix = null, string folder = null)
        {
            if (suffix == null)
            {
                suffix = ".tmp";
            }
            else if (suffix.Length > 0 && !suffix.StartsWith("."))
            {
                throw new ArgumentException($"Non-empty [{nameof(suffix)}] arguments must be prefixed with a period.");
            }

            if (string.IsNullOrEmpty(folder))
            {
                folder = System.IO.Path.GetTempPath();
            }

            Directory.CreateDirectory(folder);

            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, Guid.NewGuid().ToString("D") + suffix));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch
            {
                // We're going to ignore any errors deleting the file.
            }

            isDisposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the fully qualified path to the temporary file.
        /// </summary>
        public string Path { get; private set; }
    }
}
