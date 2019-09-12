﻿//-----------------------------------------------------------------------------
// FILE:	    ClusterDiagnostics.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;

using Neon.Common;
using Neon.Kube;
using Neon.Net;

// $todo(jeff.lill): Verify that there are no unexpected nodes in the cluster.

namespace NeonCli
{
    /// <summary>
    /// Methods to verify that cluster nodes are configured and functioning properly.
    /// </summary>
    public static class ClusterDiagnostics
    {
        /// <summary>
        /// Verifies that a cluster master node is healthy.
        /// </summary>
        /// <param name="node">The master node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        public static void CheckMaster(SshProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(node != null);
            Covenant.Requires<ArgumentException>(node.Metadata.IsMaster);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            if (!node.IsFaulted)
            {
                CheckMasterNtp(node, clusterDefinition);
            }

            node.Status = "healthy";
        }

        /// <summary>
        /// Verifies that a cluster worker node is healthy.
        /// </summary>
        /// <param name="node">The server node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        public static void CheckWorker(SshProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(node != null);
            Covenant.Requires<ArgumentException>(node.Metadata.IsWorker);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            if (!node.IsFaulted)
            {
                CheckWorkerNtp(node, clusterDefinition);
            }

            node.Status = "healthy";
        }

        /// <summary>
        /// Verifies that a master node's NTP health.
        /// </summary>
        /// <param name="node">The master node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckMasterNtp(SshProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            // We're going to use [ntpq -pw] to query the configured time sources.
            // We should get something back that looks like
            //
            //      remote           refid      st t when poll reach   delay   offset  jitter
            //      ==============================================================================
            //       LOCAL(0).LOCL.          10 l  45m   64    0    0.000    0.000   0.000
            //      * clock.xmission. .GPS.            1 u  134  256  377   48.939 - 0.549  18.357
            //      + 173.44.32.10    18.26.4.105      2 u  200  256  377   96.981 - 0.623   3.284
            //      + pacific.latt.ne 44.24.199.34     3 u  243  256  377   41.457 - 8.929   8.497
            //
            // For master nodes, we're simply going to verify that we have at least one external 
            // time source answering.

            node.Status = "check: NTP";

            var retryDelay = TimeSpan.FromSeconds(30);
            var fault      = (string)null;

            for (int tryCount = 0; tryCount < 6; tryCount++)
            {
                var response = node.SudoCommand("/usr/bin/ntpq -pw", RunOptions.LogOutput);

                if (response.ExitCode != 0)
                {
                    Thread.Sleep(retryDelay);
                    continue;
                }

                using (var reader = response.OpenOutputTextReader())
                {
                    string line;

                    // Column header and table bar lines.

                    line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        fault = "NTP: Invalid [ntpq -pw] response.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line[0] != '=')
                    {
                        fault = "NTP: Invalid [ntpq -pw] response.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    // Count the lines starting that don't include [*.LOCL.*], 
                    // the local clock.

                    var sourceCount = 0;

                    for (line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        if (line.Length > 0 && !line.Contains(".LOCL."))
                        {
                            sourceCount++;
                        }
                    }

                    if (sourceCount == 0)
                    {
                        fault = "NTP: No external sources are answering.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    // Everything looks good.

                    break;
                }
            }

            if (fault != null)
            {
                node.Fault(fault);
            }
        }

        /// <summary>
        /// Verifies that a worker node's NTP health.
        /// </summary>
        /// <param name="node">The worker node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        private static void CheckWorkerNtp(SshProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            // We're going to use [ntpq -pw] to query the configured time sources.
            // We should get something back that looks like
            //
            //           remote           refid      st t when poll reach   delay   offset  jitter
            //           ==============================================================================
            //            LOCAL(0).LOCL.          10 l  45m   64    0    0.000    0.000   0.000
            //           * 10.0.1.5        198.60.22.240    2 u  111  128  377    0.062    3.409   0.608
            //           + 10.0.1.7        198.60.22.240    2 u  111  128  377    0.062    3.409   0.608
            //           + 10.0.1.7        198.60.22.240    2 u  111  128  377    0.062    3.409   0.608
            //
            // For worker nodes, we need to verify that each of the masters are answering
            // by confirming that their IP addresses are present.

            node.Status = "check: NTP";

            var retryDelay = TimeSpan.FromSeconds(30);
            var fault      = (string)null;
            var firstTry   = true;

        tryAgain:

            for (var tries = 0; tries < 6; tries++)
            {
                var output = node.SudoCommand("/usr/bin/ntpq -pw", RunOptions.LogOutput).OutputText;

                foreach (var master in clusterDefinition.SortedMasters)
                {
                    // We're going to check the for presence of the master's IP address
                    // or its name, the latter because [ntpq] appears to attempt a reverse
                    // IP address lookup which will resolve into one of the DNS names defined
                    // in the local [/etc/hosts] file.

                    if (!output.Contains(master.PrivateAddress.ToString()) && !output.Contains(master.Name.ToLower()))
                    {
                        fault = $"NTP: Manager [{master.Name}/{master.PrivateAddress}] is not answering.";

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    // Everything looks OK.

                    break;
                }
            }

            if (fault != null)
            {
                if (firstTry)
                {
                    // $hack(jeff.lill):
                    //
                    // I've seen the NTP check fail on worker nodes, complaining
                    // that the connection attempt was rejected.  I manually restarted
                    // the node and then it worked.  I'm not sure if the rejected connection
                    // was being made to the local NTP service or from the local service
                    // to NTP running on the master.
                    //
                    // I'm going to assume that it was to the local NTP service and I'm
                    // going to try mitigating this by restarting the local NTP service
                    // and then re-running the tests.  I'm only going to do this once.

                    node.SudoCommand("systemctl restart ntp", node.DefaultRunOptions & ~RunOptions.FaultOnError);

                    firstTry = false;
                    goto tryAgain;
                }

                node.Fault(fault);
            }
        }
    }
}