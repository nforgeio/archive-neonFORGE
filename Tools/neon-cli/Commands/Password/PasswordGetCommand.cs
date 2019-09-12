﻿//-----------------------------------------------------------------------------
// FILE:	    PasswordGetCommand.cs
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
using Neon.Cryptography;
using Neon.Kube;

namespace NeonCli
{
    /// <summary>
    /// Implements the <b>password get</b> command.
    /// </summary>
    public class PasswordGetCommand : CommandBase
    {
        private const string usage = @"
Returns the current value of an named password.

USAGE:

    neon password get NAME

ARGUMENTS:

    NAME        - The password name
";

        /// <inheritdoc/>
        public override string[] Words
        {
            get { return new string[] { "password", "get" }; }
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

            var nameArg = commandLine.Arguments.ElementAtOrDefault(0);

            if (nameArg == null)
            {
                Console.Error.WriteLine($"*** ERROR: NAME argument is required.");
                Program.Exit(1);
            }

            var passwordName = NeonVault.ValidatePasswordName(nameArg);
            var path         = Path.Combine(KubeHelper.PasswordsFolder, passwordName);

            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"*** ERROR: Password [{passwordName}] not found.");
                Program.Exit(1);
            }

            Console.Write(File.ReadAllText(path));
            Program.Exit(0);
        }

        /// <inheritdoc/>
        public override DockerShimInfo Shim(DockerShim shim)
        {
            return new DockerShimInfo(shimability: DockerShimability.None, ensureConnection: false);
        }
    }
}
