﻿//-----------------------------------------------------------------------------
// FILE:	    XenServerHostingManager.cs
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
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Neon.Common;
using Neon.Net;
using Neon.Xen;

namespace Neon.Kube
{
    /// <summary>
    /// Manages cluster provisioning on the XenServer hypervisor.
    /// </summary>
    [HostingProvider(HostingEnvironments.XenServer)]
    public partial class XenServerHostingManager : HostingManager
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Used to persist information about downloaded VHD template files.
        /// </summary>
        public class DriveTemplateInfo
        {
            /// <summary>
            /// The downloaded file ETAG.
            /// </summary>
            [JsonProperty(PropertyName = "ETag", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [YamlMember(Alias = "ETag", ApplyNamingConventions = false)]
            [DefaultValue(null)]
            public string ETag { get; set; }

            /// <summary>
            /// The downloaded file length used as a quick verification that
            /// the complete file was downloaded.
            /// </summary>
            [JsonProperty(PropertyName = "Length", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [YamlMember(Alias = "Length", ApplyNamingConventions = false)]
            [DefaultValue(-1)]
            public long Length { get; set; }
        }

        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Ensures that the assembly hosting this hosting manager is loaded.
        /// </summary>
        public static void Load()
        {
            // We don't have to do anything here because the assembly is loaded
            // as a byproduct of calling this.
        }

        //---------------------------------------------------------------------
        // Instance members

        private ClusterProxy                cluster;
        private string                      logFolder;
        private List<XenClient>             xenHosts;
        private SetupController<XenClient>  controller;
        private int                         maxVmNameWidth;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cluster">The cluster being managed.</param>
        /// <param name="logFolder">
        /// The folder where log files are to be written, otherwise or <c>null</c> or 
        /// empty if logging is disabled.
        /// </param>
        public XenServerHostingManager(ClusterProxy cluster, string logFolder = null)
        {
            this.cluster                = cluster;
            this.cluster.HostingManager = this;
            this.logFolder              = logFolder;
            this.maxVmNameWidth         = cluster.Definition.Nodes.Max(n => n.Name.Length) + cluster.Definition.Hosting.GetVmNamePrefix(cluster.Definition).Length;
        }

        /// <inheritdoc/>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (xenHosts != null)
                {
                    foreach (var xenHost in xenHosts)
                    {
                        xenHost.Dispose();
                    }

                    xenHosts = null;
                }

                GC.SuppressFinalize(this);
            }

            xenHosts = null;
        }

        /// <inheritdoc/>
        public override bool IsProvisionNOP
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public override void Validate(ClusterDefinition clusterDefinition)
        {
            // Identify the OSD Bluestore block device for OSD nodes.

            if (cluster.Definition.Ceph.Enabled)
            {
                foreach (var node in cluster.Definition.Nodes.Where(n => n.Labels.CephOSD))
                {
                    node.Labels.CephOSDDevice = "xvdb";
                }
            }
        }

        /// <inheritdoc/>
        public override bool Provision(bool force)
        {
            // $todo(jeff.lill):
            //
            // I'm not implementing [force] here.  I'm not entirely sure
            // that this makes sense for production clusters.
            //
            // Perhaps it would make more sense to replace this with a
            // [neon cluster remove] command.
            //
            //      https://github.com/jefflill/NeonForge/issues/156

            if (IsProvisionNOP)
            {
                // There's nothing to do here.

                return true;
            }

            // Update the node labels with the actual capabilities of the 
            // virtual machines being provisioned.

            foreach (var node in cluster.Definition.Nodes)
            {
                if (string.IsNullOrEmpty(node.Labels.PhysicalMachine))
                {
                    node.Labels.PhysicalMachine = node.VmHost;
                }

                if (node.Labels.ComputeCores == 0)
                {
                    node.Labels.ComputeCores = node.GetVmProcessors(cluster.Definition);
                }

                if (node.Labels.ComputeRam == 0)
                {
                    node.Labels.ComputeRam = (int)(node.GetVmMemory(cluster.Definition) / ByteUnits.MebiBytes);
                }

                if (string.IsNullOrEmpty(node.Labels.StorageSize))
                {
                    node.Labels.StorageSize = ByteUnits.ToGiBString(node.GetVmDisk(cluster.Definition));
                }
            }

            // Build a list of [SshProxy] instances that map to the specified XenServer
            // hosts.  We'll use the [XenClient] instances as proxy metadata.

            var sshProxies = new List<SshProxy<XenClient>>();

            xenHosts = new List<XenClient>();

            foreach (var host in cluster.Definition.Hosting.VmHosts)
            {
                var hostAddress  = host.Address;
                var hostname     = host.Name;
                var hostUsername = host.Username ?? cluster.Definition.Hosting.VmHostUsername;
                var hostPassword = host.Password ?? cluster.Definition.Hosting.VmHostPassword;

                if (string.IsNullOrEmpty(hostname))
                {
                    hostname = host.Address;
                }

                var xenHost = new XenClient(hostAddress, hostUsername, hostPassword, name: host.Name, logFolder: logFolder);

                xenHosts.Add(xenHost);
                sshProxies.Add(xenHost.SshProxy);
            }

            // We're going to provision the XenServer hosts in parallel to
            // speed up cluster setup.  This works because each XenServer
            // is essentially independent from the others.

            controller = new SetupController<XenClient>($"Provisioning [{cluster.Definition.Name}] cluster", sshProxies)
            {
                ShowStatus  = this.ShowStatus,
                MaxParallel = this.MaxParallel
            };

            controller.AddWaitUntilOnlineStep();

            controller.AddStep("sudo config", 
                (node, stepDelay) =>
                {
                    using (var sshClient = node.CloneSshClient())
                    {
                        // We're going to rewrite [/etc/sudoers.d/nopasswd] so that client
                        // connections won't require a TTY and also that SUDO password
                        // prompting will be disabled for all users.
                        //
                        // The file will end up looking like:
                        //
                        //      Defaults !requiretty
                        //      %sudo    ALL=NOPASSWD: ALL

                        var response = sshClient.RunCommand("echo \"Defaults !requiretty\" >> /etc/sudoers.d/nopasswd");

                        if (response.ExitStatus != 0)
                        {
                            node.Fault($"Cannot update [/etc/sudoers.d/nopasswd]: {response.Result}");
                            return;
                        }

                        response = sshClient.RunCommand("echo \"%sudo    ALL=NOPASSWD: ALL\" >> /etc/sudoers.d/nopasswd");

                        if (response.ExitStatus != 0)
                        {
                            node.Fault($"Cannot update [/etc/sudoers.d/nopasswd]: {response.Result}");
                            return;
                        }
                    }
                });

            controller.AddStep("node folders", (node, stepDelay) => node.CreateHostFolders());
            controller.AddStep("verify readiness", (node, stepDelay) => VerifyReady(node));
            controller.AddStep("virtual machine template", (node, stepDelay) => CheckVmTemplate(node));
            controller.AddStep("virtual machines", (node, stepDelay) => ProvisionVirtualMachines(node));
            controller.AddGlobalStep(string.Empty, () => Finish(), quiet: true);

            if (!controller.Run())
            {
                Console.Error.WriteLine("*** ERROR: One or more configuration steps failed.");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override string DrivePrefix
        {
            get { return "xvd"; }
        }

        /// <summary>
        /// Returns the list of <see cref="NodeDefinition"/> instances describing which cluster
        /// nodes are to be hosted by a specific XenServer.
        /// </summary>
        /// <param name="xenHost">The target XenServer.</param>
        /// <returns>The list of nodes to be hosted on the XenServer.</returns>
        private List<SshProxy<NodeDefinition>> GetHostedNodes(XenClient xenHost)
        {
            var nodeDefinitions = cluster.Definition.NodeDefinitions.Values;

            return cluster.Nodes.Where(n => n.Metadata.VmHost.Equals(xenHost.Name, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Returns the name to use when naming the virtual machine hosting the node.
        /// </summary>
        /// <param name="node">The target node.</param>
        /// <returns>The virtual machine name.</returns>
        private string GetVmName(SshProxy<NodeDefinition> node)
        {
            return $"{cluster.Definition.Hosting.GetVmNamePrefix(cluster.Definition)}{node.Name}";
        }

        /// <summary>
        /// Verify that the XenServer is ready to provision the cluster virtual machines.
        /// </summary>
        /// <param name="xenSshProxy">The XenServer SSH proxy.</param>
        private void VerifyReady(SshProxy<XenClient> xenSshProxy)
        {
            // $todo(jeff.lill):
            //
            // It would be nice to verify that XenServer actually has enough 
            // resources (RAM, DISK, and perhaps CPU) here as well.

            var xenHost = xenSshProxy.Metadata;
            var nodes   = GetHostedNodes(xenHost);

            xenSshProxy.Status = "check: virtual machines";

            var vmNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var vm in xenHost.Machine.List())
            {
                vmNames.Add(vm.NameLabel);
            }

            foreach (var hostedNode in nodes)
            {
                var vmName = GetVmName(hostedNode);

                if (vmNames.Contains(vmName))
                {
                    xenSshProxy.Fault($"XenServer [{xenHost.Name}] already hosts a virtual machine named [{vmName}].");
                    return;
                }
            }
        }

        /// <summary>
        /// Install the virtual machine template on the XenServer if it's not already present.
        /// </summary>
        /// <param name="xenSshProxy">The XenServer SSH proxy.</param>
        private void CheckVmTemplate(SshProxy<XenClient> xenSshProxy)
        {
            var xenHost      = xenSshProxy.Metadata;
            var templateName = cluster.Definition.Hosting.XenServer.TemplateName;

            xenSshProxy.Status = "check: template";

            if (xenHost.Template.Find(templateName) == null)
            {
                xenSshProxy.Status = "download: vm template (slow)";
                xenHost.Template.Install(cluster.Definition.Hosting.XenServer.HostXvaUri, templateName, cluster.Definition.Hosting.XenServer.StorageRepository);
            }
        }

        /// <summary>
        /// Formats a nice node status message.
        /// </summary>
        /// <param name="vmName">The name of the virtual machine used to host the cluster node.</param>
        /// <param name="message">The status message.</param>
        /// <returns>The formatted status message.</returns>
        private string FormatVmStatus(string vmName, string message)
        {
            var namePart     = $"[{vmName}]:";
            var desiredWidth = maxVmNameWidth + 3;
            var actualWidth  = namePart.Length;

            if (desiredWidth > actualWidth)
            {
                namePart += new string(' ', desiredWidth - actualWidth);
            }

            return $"{namePart} {message}";
        }

        /// <summary>
        /// Provision the virtual machines on the XenServer.
        /// </summary>
        /// <param name="xenSshProxy">The XenServer SSH proxy.</param>
        private void ProvisionVirtualMachines(SshProxy<XenClient> xenSshProxy)
        {
            var xenHost = xenSshProxy.Metadata;

            foreach (var node in GetHostedNodes(xenHost))
            {
                var vmName      = GetVmName(node);
                var processors  = node.Metadata.GetVmProcessors(cluster.Definition);
                var memoryBytes = node.Metadata.GetVmMemory(cluster.Definition);
                var diskBytes   = node.Metadata.GetVmDisk(cluster.Definition);

                xenSshProxy.Status = FormatVmStatus(vmName, "create: virtual machine");

                // We need to create a raw drive if the node hosts a Ceph OSD.

                var extraDrives = new List<XenVirtualDrive>();

                if (node.Metadata.Labels.CephOSD)
                {
                    extraDrives.Add(
                        new XenVirtualDrive()
                        {
                            Size = node.Metadata.GetCephOSDDriveSize(cluster.Definition)
                        });
                }

                var vm = xenHost.Machine.Create(vmName, cluster.Definition.Hosting.XenServer.TemplateName,
                    processors:                 processors,
                    memoryBytes:                memoryBytes,
                    diskBytes:                  diskBytes,
                    snapshot:                   cluster.Definition.Hosting.XenServer.Snapshot,
                    extraDrives:                extraDrives,
                    primaryStorageRepository:   cluster.Definition.Hosting.XenServer.StorageRepository,
                    extraStorageRespository:    cluster.Definition.Hosting.XenServer.OsdStorageRepository);

                xenSshProxy.Status = FormatVmStatus(vmName, "start: virtual machine");

                xenHost.Machine.Start(vm);

                // We need to wait for the virtual machine to start and obtain
                // and IP address via DHCP.

                var address = string.Empty;

                xenSshProxy.Status = FormatVmStatus(vmName, "discover: ip address");

                try
                {
                    NeonHelper.WaitFor(
                        () =>
                        {
                            while (true)
                            {
                                vm = xenHost.Machine.Find(vmName);

                                if (!string.IsNullOrEmpty(vm.Address))
                                {
                                    address = vm.Address;
                                    return true;
                                }

                                Thread.Sleep(1000);
                            }
                        },
                        TimeSpan.FromMinutes(3));
                }
                catch (TimeoutException)
                {
                    xenSshProxy.Fault("Timeout waiting for virtual machine to start and set an IP address.");
                }

                // SSH into the VM using the DHCP address, configure the static IP
                // address and extend the primary partition and file system to fill
                // the drive and then reboot.

                var subnet = NetworkCidr.Parse(cluster.Definition.Network.PremiseSubnet);

                // We're going to temporarily set the node to the current VM address
                // so we can connect via SSH.

                var nodePrivateAddress = node.PrivateAddress;

                try
                {
                    node.PrivateAddress = IPAddress.Parse(address);

                    using (var nodeProxy = cluster.GetNode(node.Name))
                    {
                        xenSshProxy.Status = FormatVmStatus(vmName, "connect");
                        nodeProxy.WaitForBoot();

                        // Configure the node's network stack to the static IP address
                        // and upstream nameservers.

                        node.Status = $"network config [IP={nodePrivateAddress}]";

                        var primaryInterface = node.GetNetworkInterface(node.PrivateAddress);

                        node.ConfigureNetwork(
                            networkInterface:   primaryInterface,
                            address:            nodePrivateAddress,
                            gateway:            IPAddress.Parse(cluster.Definition.Network.Gateway),
                            subnet:             NetworkCidr.Parse(cluster.Definition.Network.PremiseSubnet),
                            nameservers:        cluster.Definition.Network.Nameservers.Select(ns => IPAddress.Parse(ns)));

                        // Extend the primary partition and file system to fill 
                        // the virtual the drive. 

                        xenSshProxy.Status = FormatVmStatus(vmName, $"resize: primary drive");

                        // $hack(jeff.lill):
                        //
                        // I've seen a transient error here but can't reproduce it.  I'm going
                        // to assume for now that the file system might not be quite ready for
                        // this operation directly after the VM has been rebooted, so we're going
                        // to delay for a few seconds before performing the operations.

                        Thread.Sleep(TimeSpan.FromSeconds(5));
                        nodeProxy.SudoCommand("growpart /dev/xvda 2");
                        nodeProxy.SudoCommand("resize2fs /dev/xvda2");

                        // Reboot to pick up the changes.

                        xenSshProxy.Status = FormatVmStatus(vmName, "restarting...");
                        nodeProxy.Reboot(wait: false);
                    }
                }
                finally
                {
                    // Restore the node's IP address.

                    node.PrivateAddress = nodePrivateAddress;
                }
            }
        }

        /// <summary>
        /// Perform any necessary global post Hyper-V provisioning steps.
        /// </summary>
        private void Finish()
        {
            // Recreate the node proxies because we disposed them above.
            // We need to do this so subsequent prepare steps will be
            // able to connect to the nodes via the correct addresses.

            cluster.CreateNodes();
        }

        /// <inheritdoc/>
        public override (string Address, int Port) GetSshEndpoint(string nodeName)
        {
            return (Address: cluster.GetNode(nodeName).PrivateAddress.ToString(), Port: NetworkPorts.SSH);
        }

        /// <inheritdoc/>
        public override void AddPostProvisionSteps(SetupController<NodeDefinition> controller)
        {
        }
    }
}
