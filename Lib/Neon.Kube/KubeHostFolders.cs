﻿//-----------------------------------------------------------------------------
// FILE:	    KubeHostFolders.cs
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
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;

using Renci.SshNet;

namespace Neon.Kube
{
    /// <summary>
    /// Enumerates the paths of important directories on cluster 
    /// host servers.
    /// </summary>
    /// <remarks>
    /// <note>
    /// Although these constants are referenced by C# code, Linux scripts 
    /// are likely to hardcode these strings.  You should do a search and
    /// replace whenever you change any of these values.
    /// </note>
    /// <note>
    /// Changing any of these will likely break [neon-cli] interactions
    /// with existing clusters that use the previous folder path.  Be
    /// ver sure you know what you're doing when you make changes.
    /// </note>
    /// </remarks>
    public static class KubeHostFolders
    {
        /// <summary>
        /// Path to the cluster archive directory.
        /// </summary>
        public const string Archive = "${HOME}/.archive";

        /// <summary>
        /// Path to the cluster executable files directory.
        /// </summary>
        public const string Bin = "/lib/neonkube/bin";

        /// <summary>
        /// Path to the cluster configuration directory.
        /// </summary>
        public const string Config = "/etc/neonkube";

        /// <summary>
        /// The folder where cluster tools can upload, unpack, and then
        /// execute <see cref="CommandBundle"/>s as well as store temporary
        /// command output files.
        /// </summary>
        public const string Exec = "${HOME}/.exec";

        /// <summary>
        /// Path to the cluster management scripts directory.
        /// </summary>
        public const string Scripts = "/lib/neonkube/scripts";

        /// <summary>
        /// Path to the cluster setup scripts directory.
        /// </summary>
        public const string Setup = "/lib/neonkube/setup";

        /// <summary>
        /// Path to the cluster setup state directory.
        /// </summary>
        public const string State = "/var/local/neonkube";

        /// <summary>
        /// Root folder on the local tmpfs (shared memory) folder where 
        /// cluster will persist misc temporary files.
        /// </summary>
        public const string Tmpfs = "/dev/shm/neonkube";
    }
}
