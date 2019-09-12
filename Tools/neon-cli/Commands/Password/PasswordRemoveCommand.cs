﻿//-----------------------------------------------------------------------------
// FILE:	    PasswordRemoveCommand.cs
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
    /// Implements the <b>password remove</b> command.
    /// </summary>
    public class PasswordRemoveCommand : CommandBase
    {
        private const string usage = @"
Removes a specific named password or all passwords.

USAGE:

    neon password remove|rm [--force] NAME  - Removes the named password
    neon password remove|rm [--force] *     - Removes all named passwords

ARGUMENTS:

    NAME        - the password name
    PATH        - path to the source file
    -           - read password from STDIN

OPTIONS:

    --force     - don't prompt to confirm removal.

REMARKS:

This command removes a named password or all passwords.
";

        /// <inheritdoc/>
        public override string[] Words
        {
            get { return new string[] { "password", "remove" }; }
        }

        /// <inheritdoc/>
        public override string[] AltWords
        {
            get { return new string[] { "password", "rm" }; }
        }

        /// <inheritdoc/>
        public override string[] ExtendedOptions
        {
            get { return new string[] { "--force" }; }
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
            var force   = commandLine.HasOption("--force");

            if (nameArg == null)
            {
                Console.Error.WriteLine($"*** ERROR: NAME argument is required.");
                Program.Exit(1);
            }

            if (nameArg == "*")
            {
                if (!force && !Program.PromptYesNo("Are you sure you want to remove all passwords?"))
                {
                    Program.Exit(0);
                }

                foreach (var path in Directory.GetFiles(KubeHelper.PasswordsFolder))
                {
                    File.Delete(path);
                }
            }
            else
            {
                var passwordName = NeonVault.ValidatePasswordName(nameArg);
                var passwordPath = Path.Combine(KubeHelper.PasswordsFolder, passwordName);

                if (!File.Exists(passwordPath))
                {
                    Console.Error.WriteLine($"*** ERROR: The [{passwordName}] password does not exist.");
                    Program.Exit(1);
                }
                else
                {
                    if (!force && !Program.PromptYesNo($"Are you sure you want to remove the [{passwordName}] password?"))
                    {
                        Program.Exit(0);
                    }

                    File.Delete(passwordPath);
                }
            }

            Program.Exit(0);
        }

        /// <inheritdoc/>
        public override DockerShimInfo Shim(DockerShim shim)
        {
            return new DockerShimInfo(shimability: DockerShimability.None, ensureConnection: false);
        }
    }
}
