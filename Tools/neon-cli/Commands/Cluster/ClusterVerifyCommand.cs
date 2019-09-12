﻿//-----------------------------------------------------------------------------
// FILE:	    ClusterVerifyCommand.cs
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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;

using Neon.Common;
using Neon.Kube;

namespace NeonCli
{
    /// <summary>
    /// Implements the <b>cluster verify</b> command.
    /// </summary>
    public class ClusterVerifyCommand : CommandBase
    {
        private const string usage = @"
Verifies a cluster definition file.

USAGE:

    neon cluster verify CLUSTER-DEF

ARGUMENTS:

    CLUSTER-DEF     - Path to the cluster definition file.
";

        /// <inheritdoc/>
        public override string[] Words
        {
            get { return new string[] { "cluster", "verify" }; }
        }

        /// <inheritdoc/>
        public override void Help()
        {
            Console.WriteLine(usage);
        }

        /// <inheritdoc/>
        public override void Run(CommandLine commandLine)
        {
            if (commandLine.Arguments.Length < 1)
            {
                Console.Error.WriteLine("*** ERROR: CLUSTER-DEF is required.");
                Program.Exit(1);
            }

            // Parse and validate the cluster definition.

            ClusterDefinition.FromFile(commandLine.Arguments[0], strict: true);

            Console.WriteLine("");
            Console.WriteLine("*** The cluster definition is OK.");
        }

        /// <inheritdoc/>
        public override DockerShimInfo Shim(DockerShim shim)
        {
            shim.AddFile(shim.CommandLine.Arguments.LastOrDefault());

            return new DockerShimInfo(shimability: DockerShimability.Optional);
        }
    }
}
