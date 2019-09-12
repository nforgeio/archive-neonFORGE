﻿//-----------------------------------------------------------------------------
// FILE:	    KubeTestHelper.cs
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
    /// Misc local unit test helpers.
    /// </summary>
    public static class KubeTestHelper
    {
        /// <summary>
        /// <b>nhive/test</b> image test user name.
        /// </summary>
        public const string TestUsername = "test";

        /// <summary>
        /// <b>nhive/test</b> image test user ID.
        /// </summary>
        public const string TestUID = "5555";

        /// <summary>
        /// <b>nhive/test</b> image test group ID.
        /// </summary>
        public const string TestGID = "6666";

        /// <summary>
        /// Creates and optionally populates a temporary test folder with test files.
        /// </summary>
        /// <param name="files">
        /// The files to be created.  The first item in each tuple entry will be 
        /// the local file name and the second the contents of the file.
        /// </param>
        /// <returns>The <see cref="TempFolder"/>.</returns>
        /// <remarks>
        /// <note>
        /// Ensure that the <see cref="TempFolder"/> returned is disposed so it and
        /// any files within will be deleted.
        /// </note>
        /// </remarks>
        public static TempFolder CreateTestFolder(params Tuple<string, string>[] files)
        {
            var folder = new TempFolder();

            if (files != null)
            {
                foreach (var file in files)
                {
                    File.WriteAllText(Path.Combine(folder.Path, file.Item1), file.Item2 ?? string.Empty);
                }
            }

            return folder;
        }

        /// <summary>
        /// Creates and populates a temporary test folder with a test file.
        /// </summary>
        /// <param name="data">The file name</param>
        /// <param name="filename">The file data.</param>
        /// <returns>The <see cref="TempFolder"/>.</returns>
        /// <remarks>
        /// <note>
        /// Ensure that the <see cref="TempFolder"/> returned is disposed so it and
        /// any files within will be deleted.
        /// </note>
        /// </remarks>
        public static TempFolder TempFolder(string filename, string data)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(filename));

            var folder = new TempFolder();

            File.WriteAllText(Path.Combine(folder.Path, filename), data ?? string.Empty);

            return folder;
        }

        /// <summary>
        /// Starts and returns a <see cref="KubeTestManager"/> instance.  This will put <see cref="KubeHelper"/>
        /// into test mode.  You must dispose the instance before the tests complete to revert back
        /// to normal mode.
        /// </summary>
        /// <returns>The <see cref="KubeTestManager"/>.</returns>
        public static KubeTestManager KubeTestManager()
        {
            return new KubeTestManager();
        }

        /// <summary>
        /// Returns the path to the <b>neon-cli</b> executable.
        /// </summary>
        private static string NeonExePath
        {
            get
            {
                // We're going to run the command from the NF_BUILD directory.

                var buildFolder = Environment.GetEnvironmentVariable("NF_BUILD");

                if (string.IsNullOrEmpty(buildFolder))
                {
                    throw new Exception("The NF_BUILD environment variable is not defined.");
                }

                if (NeonHelper.IsWindows)
                {
                    return Path.Combine(buildFolder, "neon.cmd");
                }
                else
                {
                    return Path.Combine(buildFolder, "neon");
                }
            }
        }

        /// <summary>
        /// Executes <b>neon-cli</b> passing optional individual arguments.
        /// </summary>
        /// <param name="args">The command arguments.</param>
        /// <returns>The <see cref="ExecuteResult"/>.</returns>
        public static ExecuteResult NeonExec(params object[] args)
        {
            return NeonExec(NeonHelper.NormalizeExecArgs(args));
        }

        /// <summary>
        /// Executes <b>neon-cli</b> passing all arguments as a string.
        /// </summary>
        /// <param name="args">The command arguments.</param>
        /// <returns>The <see cref="ExecuteResult"/>.</returns>
        public static ExecuteResult NeonExec(string args)
        {
            var neonPath = NeonExePath;

            if (!File.Exists(neonPath))
            {
                throw new Exception($"The [neon-cli] executable does not exist at [{neonPath}].");
            }

            return NeonHelper.ExecuteCapture(neonPath, args);
        }

        /// <summary>
        /// Executes <b>neon-cli</b> passing text as STDIN and also passing all 
        /// arguments as a string.
        /// </summary>
        /// <param name="stdIn">The text to be passed as STDIN.</param>
        /// <param name="args">The command arguments.</param>
        /// <returns>The <see cref="ExecuteResult"/>.</returns>
        public static ExecuteResult NeonExecStdin(string stdIn, params object[] args)
        {
            return NeonExecStdin(stdIn, NeonHelper.NormalizeExecArgs(args));
        }

        /// <summary>
        /// Executes <b>neon-cli</b> passing text as STDIN and also passing all 
        /// arguments as a string.
        /// </summary>
        /// <param name="inputText">The text to be passed as STDIN.</param>
        /// <param name="args">The command arguments.</param>
        /// <returns>The <see cref="ExecuteResult"/>.</returns>
        public static ExecuteResult NeonExecStdin(string inputText, string args)
        {
            Covenant.Requires<ArgumentNullException>(inputText != null);

            using (var tempFolder = new TempFolder())
            {
                var inputPath = Path.Combine(tempFolder.Path, "input");

                File.WriteAllText(inputPath, inputText);

                if (NeonHelper.IsWindows)
                {
                    var scriptPath = Path.Combine(tempFolder.Path, "script.cmd");

                    File.WriteAllText(scriptPath, $"cat \"{inputPath}\" | \"{NeonExePath}\" {args}");

                    return NeonHelper.ExecuteCapture(scriptPath, new object[0]);
                }
                else
                {
                    var scriptPath = Path.Combine(tempFolder.Path, "script.sh");

                    File.WriteAllText(scriptPath, $"cat \"{inputPath}\" | \"{NeonExePath}\" {args}");

                    return NeonHelper.ExecuteCapture($"bash {scriptPath}", new object[0]);
                }
            }
        }
    }
}
