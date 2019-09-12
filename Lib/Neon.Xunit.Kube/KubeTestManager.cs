﻿//-----------------------------------------------------------------------------
// FILE:	    KubeTestManager.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Kube;
using Neon.IO;
using Neon.Xunit;
using Neon.Xunit.Kube;

using Xunit;

namespace Neon.Xunit.Kube
{
    /// <summary>
    /// Creates a temporary folder and puts <see cref="KubeHelper"/> into test mode
    /// to support <b>neon-cli</b> unit testing.  <see cref="Dispose"/> reverts the 
    /// test mode and deletes the temporary folder.
    /// </summary>
    public sealed class KubeTestManager : IDisposable
    {
        //---------------------------------------------------------------------
        // Static members

        private static object syncLock = new object();

        /// <summary>
        /// Returns the current test manager.
        /// </summary>
        public static KubeTestManager Current { get; private set; }

        //---------------------------------------------------------------------
        // Instance members

        private TempFolder tempFolder;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if another test manager instance is active.</exception>
        public KubeTestManager()
        {
            lock (syncLock)
            {
                if (Current != null)
                {
                    throw new InvalidOperationException("Another test manager is active.");
                }

                try
                {
                    tempFolder = new TempFolder();
                    Current    = this;

                    KubeHelper.SetTestMode(tempFolder.Path);
                    Environment.SetEnvironmentVariable(KubeConst.TestModeFolderVar, tempFolder.Path);
                }
                catch
                {
                    Environment.SetEnvironmentVariable(KubeConst.TestModeFolderVar, null);
                    Current = null;
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (tempFolder != null)
            {
                KubeHelper.ResetTestMode();
                tempFolder.Dispose();

                tempFolder = null;
                Current    = null;
            }
        }

        /// <summary>
        /// Returns the path to the temporary test folder.
        /// </summary>
        public string TestFolder => tempFolder?.Path;
    }
}
