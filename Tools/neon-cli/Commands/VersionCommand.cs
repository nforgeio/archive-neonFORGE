﻿//-----------------------------------------------------------------------------
// FILE:	    VersionCommand.cs
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
    /// Implements the <b>version</b> command.
    /// </summary>
    public class VersionCommand : CommandBase
    {
        private const string usage = @"
Prints the actual [neon-cli] version, ignoring any [--version] option.

USAGE:

    neon version [-n] [--get]

OPTIONS:

    -n      - Don't write a newline after version.
    --git   - Include the Git branch/commit information.
";
        /// <inheritdoc/>
        public override string[] Words
        {
            get { return new string[] { "version" }; }
        }

        /// <inheritdoc/>
        public override string[] ExtendedOptions
        {
            get { return new string[] { "-n", "--git" }; }
        }

        /// <inheritdoc/>
        public override void Help()
        {
            Console.WriteLine(usage);
        }

        /// <inheritdoc/>
        public override void Run(CommandLine commandLine)
        {
            if (commandLine.HasHelpOption)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            if (commandLine.HasOption("--git"))
            {
                Console.Write($"{Program.Version}/{Program.GitVersion}");
            }
            else
            {
                Console.Write(Program.Version);
            }

            if (!commandLine.HasOption("-n"))
            {
                Console.WriteLine();
            }
        }

        /// <inheritdoc/>
        public override DockerShimInfo Shim(DockerShim shim)
        {
            return new DockerShimInfo(shimability: DockerShimability.Optional);
        }
    }
}
