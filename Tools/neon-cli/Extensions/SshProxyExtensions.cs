﻿//-----------------------------------------------------------------------------
// FILE:	    SshProxyExtension.cs
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
using System.Text;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Kube;
using Neon.IO;
using Neon.Net;

namespace NeonCli
{
    /// <summary>
    /// <see cref="SshProxy{T}"/> extension methods.
    /// </summary>
    public static class SshProxyExtension
    {
        /// <summary>
        /// Converts a string into a value suitable for use in a Bash script.
        /// </summary>
        /// <param name="value">The value to be made safe,</param>
        /// <returns>The safe value.</returns>
        private static string BashSafeValue(object value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            if (value is bool)
            {
                return NeonHelper.ToBoolString((bool)value);
            }
            else if (value is int)
            {
                return value.ToString();
            }
            else
            {
                // We need to escape single and double quotes.

                var stringValue = value.ToString();

                stringValue = stringValue.Replace("'", "\\'");
                stringValue = stringValue.Replace("\"", "\\\"");

                return $"\"{stringValue}\"";
            }
        }

        /// <summary>
        /// Sets a variable in a <see cref="PreprocessReader"/> such that the value will be safe
        /// to be included in a Bash variable set statement.
        /// </summary>
        /// <param name="preprocessReader">The reader.</param>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        private static void SetBashVariable(PreprocessReader preprocessReader, string name, object value)
        {
            Covenant.Requires<ArgumentNullException>(preprocessReader != null);
            Covenant.Requires<ArgumentNullException>(name != null);

            if (value == null)
            {
                preprocessReader.Set(name, value);
            }
            else
            {
                if (value is bool)
                {
                    value = NeonHelper.ToBoolString((bool)value);
                }
                else if (value is int)
                {
                    value = value.ToString();
                }
                else
                {
                    // We need to escape single and double quotes.

                    var stringValue = value.ToString();

                    stringValue = stringValue.Replace("'", "\\'");
                    stringValue = stringValue.Replace("\"", "\\\"");

                    value = $"\"{stringValue}\"";
                }

                preprocessReader.Set(name, value);
            }
        }

        /// <summary>
        /// Returns the IP address for a node suitable for including in the
        /// <b>/etc/hosts</b> file.  
        /// </summary>
        /// <param name="nodeDefinition">The target node definition.</param>
        /// <returns>
        /// The IP address, left adjusted with necessary spaces so that the
        /// host definitions will align nicely.
        /// </returns>
        private static string GetHostsFormattedAddress(NodeDefinition nodeDefinition)
        {
            const string ip4Max = "255.255.255.255";

            var address = nodeDefinition.PrivateAddress.ToString();

            if (address.Length < ip4Max.Length)
            {
                address += new string(' ', ip4Max.Length - address.Length);
            }

            return address;
        }

        /// <summary>
        /// Sets cluster definition related variables for a <see cref="PreprocessReader"/>.
        /// </summary>
        /// <param name="preprocessReader">The reader.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <param name="kubeSetupInfo">The Kubernetes setup details.</param>
        /// <param name="nodeDefinition">The target node definition.</param>
        private static void SetClusterVariables(PreprocessReader preprocessReader, ClusterDefinition clusterDefinition, KubeSetupInfo kubeSetupInfo, NodeDefinition nodeDefinition)
        {
            Covenant.Requires<ArgumentNullException>(preprocessReader != null);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);
            Covenant.Requires<ArgumentNullException>(kubeSetupInfo != null);

            // Generate the master node variables in sorted order.  The variable 
            // names will be formatted as:
            //
            //      NEON_MASTER_#
            //
            // where [#] is the zero-based index of the node.  This is compatible
            // with the [getmaster] function included the script.
            //
            // Each variable defines an associative array with [name] and [address]
            // properties.
            //
            // Then generate the NEON_MASTER_NAMES and NEON_MASTER_ADDRESSES arrays.
            //
            // NOTE: We need to use Linux-style line endings.

            var sbMasters                  = new StringBuilder();
            var sbMasterNamesArray         = new StringBuilder();
            var sbMasterAddressesArray     = new StringBuilder();
            var sbPeerMasterAddressesArray = new StringBuilder();
            var sbMasterNodesSummary       = new StringBuilder();
            var index                      = 0;
            var masterNameWidth            = 0;

            sbMasterNamesArray.Append("(");
            sbMasterAddressesArray.Append("(");
            sbPeerMasterAddressesArray.Append("(");

            foreach (var master in clusterDefinition.SortedMasters)
            {
                sbMasters.Append($"declare -x -A NEON_MASTER_{index}\n");
                sbMasters.Append($"NEON_MASTER_{index}=( [\"name\"]=\"{master.Name}\" [\"address\"]=\"{master.PrivateAddress}\" )\n");
                sbMasters.Append("\n");
                index++;

                sbMasterNamesArray.Append($" \"{master.Name}\"");
                sbMasterAddressesArray.Append($" \"{master.PrivateAddress}\"");

                if (master != nodeDefinition)
                {
                    sbPeerMasterAddressesArray.Append($" \"{master.PrivateAddress}\"");
                }

                masterNameWidth = Math.Max(master.Name.Length, masterNameWidth);
            }

            sbMasterNamesArray.Append(" )");
            sbMasterAddressesArray.Append(" )");
            sbPeerMasterAddressesArray.Append(" )");

            foreach (var master in clusterDefinition.SortedMasters)
            {
                var nameField = master.Name;

                if (nameField.Length < masterNameWidth)
                {
                    nameField += new string(' ', masterNameWidth - nameField.Length);
                }

                // The blanks below are just enough so that the "=" sign lines up
                // with the summary output from [cluster.conf.sh].

                if (sbMasterNodesSummary.Length == 0)
                {
                    sbMasterNodesSummary.Append($"    echo \"NEON_MASTER_NODES                 = {nameField}: {master.PrivateAddress}\" 1>&2\n");
                }
                else
                {
                    sbMasterNodesSummary.Append($"    echo \"                                     {nameField}: {master.PrivateAddress}\" 1>&2\n");
                }
            }

            foreach (var master in clusterDefinition.SortedMasters)
            {
                sbMasters.Append($"declare -x -A NEON_MASTER_{index}\n");
                sbMasters.Append($"NEON_MASTER_{index}=( [\"name\"]=\"{master.Name}\" [\"address\"]=\"{master.PrivateAddress}\" )\n");
                index++;
            }

            sbMasters.Append("\n");
            sbMasters.Append($"declare -x NEON_MASTER_NAMES={sbMasterNamesArray}\n");
            sbMasters.Append($"declare -x NEON_MASTER_ADDRESSES={sbMasterAddressesArray}\n");

            sbMasters.Append("\n");

            // Generate the master and worker NTP time sources.

            var masterTimeSources = string.Empty;
            var workerTimeSources = string.Empty;

            if (clusterDefinition.TimeSources != null)
            {
                foreach (var source in clusterDefinition.TimeSources)
                {
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        continue;
                    }

                    if (masterTimeSources.Length > 0)
                    {
                        masterTimeSources += " ";
                    }

                    masterTimeSources += $"\"{source}\"";
                }
            }

            foreach (var master in clusterDefinition.SortedMasters)
            {
                if (workerTimeSources.Length > 0)
                {
                    workerTimeSources += " ";
                }

                workerTimeSources += $"\"{master.PrivateAddress}\"";
            }

            if (string.IsNullOrWhiteSpace(masterTimeSources))
            {
                // Default to a reasonable public time source.

                masterTimeSources = "\"pool.ntp.org\"";
            }

            // Set the variables.

            preprocessReader.Set("load-cluster-conf", KubeHostFolders.Config + "/cluster.conf.sh --echo-summary");
            preprocessReader.Set("load-cluster-conf-quiet", KubeHostFolders.Config + "/cluster.conf.sh");

            SetBashVariable(preprocessReader, "cluster.provisioner", clusterDefinition.Provisioner);

            SetBashVariable(preprocessReader, "node.driveprefix", clusterDefinition.DrivePrefix);

            SetBashVariable(preprocessReader, "neon.folders.archive", KubeHostFolders.Archive);
            SetBashVariable(preprocessReader, "neon.folders.bin", KubeHostFolders.Bin);
            SetBashVariable(preprocessReader, "neon.folders.exec", KubeHostFolders.Exec);
            SetBashVariable(preprocessReader, "neon.folders.config", KubeHostFolders.Config);
            SetBashVariable(preprocessReader, "neon.folders.setup", KubeHostFolders.Setup);
            SetBashVariable(preprocessReader, "neon.folders.state", KubeHostFolders.State);
            SetBashVariable(preprocessReader, "neon.folders.tmpfs", KubeHostFolders.Tmpfs);
            SetBashVariable(preprocessReader, "neon.folders.tools", KubeHostFolders.Bin);

            SetBashVariable(preprocessReader, "nodes.master.count", clusterDefinition.Masters.Count());
            preprocessReader.Set("nodes.masters", sbMasters);
            preprocessReader.Set("nodes.masters.summary", sbMasterNodesSummary);
            
            SetBashVariable(preprocessReader, "ntp.master.sources", masterTimeSources);
            NewMethod(preprocessReader, workerTimeSources);

            SetBashVariable(preprocessReader, "docker.packageuri", kubeSetupInfo.DockerPackageUbuntuUri);

            SetBashVariable(preprocessReader, "neon.kube.kubeadm.package_version", kubeSetupInfo.KubeAdmPackageUbuntuVersion);
            SetBashVariable(preprocessReader, "neon.kube.kubectl.package_version", kubeSetupInfo.KubeCtlPackageUbuntuVersion);
            SetBashVariable(preprocessReader, "neon.kube.kubelet.package_version", kubeSetupInfo.KubeletPackageUbuntuVersion);

            //-----------------------------------------------------------------
            // Configure the variables for the [setup-disk.sh] script.

            switch (clusterDefinition.Hosting.Environment)
            {
                case HostingEnvironments.Aws:

                    throw new NotImplementedException("$todo(jeff.lill)");

                case HostingEnvironments.Azure:

                    // The primary Azure data drive is [/dev/sdb] so any mounted drive will be [/dev/sdc].

                    if (nodeDefinition.Azure.HardDriveCount == 0)
                    {
                        SetBashVariable(preprocessReader, "data.disk", "PRIMARY");
                    }
                    else
                    {
                        SetBashVariable(preprocessReader, "data.disk", "/dev/sdc");
                    }
                    break;

                case HostingEnvironments.Google:

                    throw new NotImplementedException("$todo(jeff.lill)");

                case HostingEnvironments.HyperV:
                case HostingEnvironments.HyperVLocal:
                case HostingEnvironments.Machine:
                case HostingEnvironments.Unknown:
                case HostingEnvironments.XenServer:

                    // VMs for all of these environments simply host their data on the
                    // primary OS disk only for now, the idea being that this disk
                    // can be sized up as necessary.  There are valid scenarios where
                    // folks would like the data on a different drive (e.g. for better
                    // performance).  I'm putting support for that on the backlog.

                    SetBashVariable(preprocessReader, "data.disk", "PRIMARY");
                    break;

                default:

                    throw new NotImplementedException($"The [{clusterDefinition.Hosting.Environment}] hosting environment is not implemented.");
            }
        }

        private static void NewMethod(PreprocessReader preprocessReader, string workerTimeSources)
        {
            SetBashVariable(preprocessReader, "ntp.worker.sources", workerTimeSources);
        }

        /// <summary>
        /// Uploads a resource file to the remote server after performing any necessary preprocessing.
        /// </summary>
        /// <typeparam name="TMetadata">The node metadata type.</typeparam>
        /// <param name="node">The remote node.</param>
        /// <param name="clusterDefinition">The cluster definition or <c>null</c>.</param>
        /// <param name="kubeSetupInfo">The Kubernetes setup details.</param>
        /// <param name="file">The resource file.</param>
        /// <param name="targetPath">The target path on the remote server.</param>
        private static void UploadFile<TMetadata>(this SshProxy<TMetadata> node, ClusterDefinition clusterDefinition, KubeSetupInfo kubeSetupInfo, ResourceFiles.File file, string targetPath)
            where TMetadata : class
        {
            using (var input = file.ToStream())
            {
                if (file.HasVariables)
                {
                    // We need to expand any variables.  Note that if we don't have a
                    // cluster definition or for undefined variables, we're going to 
                    // have the variables expand to the empty string.

                    using (var msExpanded = new MemoryStream())
                    {
                        using (var writer = new StreamWriter(msExpanded))
                        {
                            var preprocessReader =
                                new PreprocessReader(new StreamReader(input))
                                {
                                    DefaultVariable = string.Empty,
                                    ExpandVariables = true,
                                    ProcessStatements = false,
                                    StripComments   = false
                                };

                            if (clusterDefinition != null)
                            {
                                SetClusterVariables(preprocessReader, clusterDefinition, kubeSetupInfo, node.Metadata as NodeDefinition);
                            }

                            foreach (var line in preprocessReader.Lines())
                            {
                                writer.WriteLine(line);
                            }

                            writer.Flush();

                            msExpanded.Position = 0;
                            node.UploadText(targetPath, msExpanded, tabStop: 4, outputEncoding: Encoding.UTF8);
                        }
                    }
                }
                else
                {
                    node.UploadText(targetPath, input, tabStop: 4, outputEncoding: Encoding.UTF8);
                }
            }
        }

        /// <summary>
        /// Uploads the configuration files for the target operating system to the server.
        /// </summary>
        /// <typeparam name="Metadata">The node metadata type.</typeparam>
        /// <param name="node">The remote node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <param name="kubeSetupInfo">The Kubernetes setup details.</param>
        public static void UploadConfigFiles<Metadata>(this SshProxy<Metadata> node, ClusterDefinition clusterDefinition, KubeSetupInfo kubeSetupInfo)
            where Metadata : class
        {
            Covenant.Requires<ArgumentNullException>(node != null);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);
            Covenant.Requires<ArgumentNullException>(kubeSetupInfo != null);

            // Clear the contents of the configuration folder.

            node.Status = $"clear: {KubeHostFolders.Config}";
            node.SudoCommand($"rm -rf {KubeHostFolders.Config}/*.*");

            // Upload the files.

            node.Status = "upload: config files";

            foreach (var file in Program.LinuxFolder.GetFolder("conf").Files())
            {
                node.UploadFile(clusterDefinition, kubeSetupInfo, file, $"{KubeHostFolders.Config}/{file.Name}");
            }

            // Secure the files and make the scripts executable.

            node.SudoCommand($"chmod 644 {KubeHostFolders.Config}/*.*");
            node.SudoCommand($"chmod 744 {KubeHostFolders.Config}/*.sh");

            node.Status = "copied";
        }

        /// <summary>
        /// Uploads the setup and other scripts and tools for the target operating system to the server.
        /// </summary>
        /// <typeparam name="TMetadata">The server's metadata type.</typeparam>
        /// <param name="server">The remote server.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <param name="kubeSetupInfo">The Kubernetes setup details.</param>
        public static void UploadResources<TMetadata>(this SshProxy<TMetadata> server, ClusterDefinition clusterDefinition, KubeSetupInfo kubeSetupInfo)
            where TMetadata : class
        {
            Covenant.Requires<ArgumentNullException>(server != null);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);
            Covenant.Requires<ArgumentNullException>(kubeSetupInfo != null);

            //-----------------------------------------------------------------
            // Upload resource files to the setup folder.

            server.Status = $"clear: {KubeHostFolders.Setup}";
            server.SudoCommand($"rm -rf {KubeHostFolders.Setup}/*.*");

            // Upload the setup files.

            server.Status = "upload: setup scripts";

            foreach (var file in Program.LinuxFolder.GetFolder("setup").Files())
            {
                server.UploadFile(clusterDefinition, kubeSetupInfo, file, $"{KubeHostFolders.Setup}/{file.Name}");
            }

            // Make the setup scripts executable.

            server.SudoCommand($"chmod 744 {KubeHostFolders.Setup}/*");

            //-----------------------------------------------------------------
            // Upload files to the bin folder.

            server.Status = $"clear: {KubeHostFolders.Bin}";
            server.SudoCommand($"rm -rf {KubeHostFolders.Bin}/*.*");

            // Upload the tool files.  Note that we're going to strip out the [.sh] 
            // file type to make these easier to run.
        
            server.Status = "upload: binary files";

            foreach (var file in Program.LinuxFolder.GetFolder("binary").Files())
            {
                server.UploadFile(clusterDefinition, kubeSetupInfo, file, $"{KubeHostFolders.Bin}/{file.Name.Replace(".sh", string.Empty)}");
            }

            // Make the scripts executable.

            server.SudoCommand($"chmod 744 {KubeHostFolders.Bin}/*");
        }
    }
}
