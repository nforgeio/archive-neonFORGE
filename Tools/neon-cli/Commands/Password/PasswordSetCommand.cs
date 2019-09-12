﻿//-----------------------------------------------------------------------------
// FILE:	    PasswordSetCommand.cs
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
    /// Implements the <b>password set</b> command.
    /// </summary>
    public class PasswordSetCommand : CommandBase
    {
        private const string usage = @"
Creates or modifies a named password.

USAGE:

    neon password set NAME          - Generates and sets a password
    neon password set NAME PATH     - Sets a password from a file
    neon password set NAME -        - Sets a password from STDIN

ARGUMENTS:

    NAME        - the password name
    PATH        - path to the source file
    -           - read password from STDIN

REMARKS:

This command creates or updates a named password.
";

        /// <inheritdoc/>
        public override string[] Words
        {
            get { return new string[] { "password", "set" }; }
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

            var nameArg   = commandLine.Arguments.ElementAtOrDefault(0);
            var sourceArg = commandLine.Arguments.ElementAtOrDefault(1);

            if (nameArg == null)
            {
                Console.Error.WriteLine($"*** ERROR: NAME argument is required.");
                Program.Exit(1);
            }

            var passwordName = NeonVault.ValidatePasswordName(nameArg);
            var password     = string.Empty;

            if (sourceArg == null)
            {
                // Generate a 20 character password.

                password = NeonHelper.GetRandomPassword(20);
            }
            else if (sourceArg == "-")
            {
                // Read the password from STDIN and trim.

                using (var stdin = Console.OpenStandardInput())
                {
                    using (var reader = new StreamReader(stdin))
                    {
                        password = reader.ReadLine().Trim();
                    }
                }
            }
            else
            {
                // Read the first line from the file.

                using (var input = new FileStream(sourceArg, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new StreamReader(input))
                    {
                        password = reader.ReadLine().Trim();
                    }
                }
            }

            if (password.Length == 0)
            {
                Console.Error.WriteLine($"*** ERROR: The password cannot be blank.");
                Program.Exit(1);
            }

            File.WriteAllText(Path.Combine(KubeHelper.PasswordsFolder, passwordName), password);
            Program.Exit(0);
        }

        /// <inheritdoc/>
        public override DockerShimInfo Shim(DockerShim shim)
        {
            return new DockerShimInfo(shimability: DockerShimability.None, ensureConnection: false);
        }
    }
}
