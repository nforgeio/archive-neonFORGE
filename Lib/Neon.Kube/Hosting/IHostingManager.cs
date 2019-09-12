﻿//-----------------------------------------------------------------------------
// FILE:	    IHostingManager.cs
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
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Cryptography;
using Neon.Diagnostics;
using Neon.IO;
using Neon.Net;
using Neon.Retry;
using Neon.Time;

using ICSharpCode.SharpZipLib.Zip;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Neon.Kube
{
    /// <summary>
    /// Interface describing the hosting environment managers.
    /// </summary>
    public interface IHostingManager : IDisposable
    {
        /// <summary>
        /// Returns <c>true</c> if the provisioning operation actually does nothing.
        /// </summary>
        bool IsProvisionNOP { get; }

        /// <summary>
        /// Verifies that a cluster is valid for the hosting manager, customizing 
        /// properties as required.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ClusterDefinitionException">Thrown if any problems were detected.</exception>
        void Validate(ClusterDefinition clusterDefinition);

        /// <summary>
        /// Creates and initializes the cluster resources such as the virtual machines,
        /// networks, load balancers, network security groups, public IP addresses etc.
        /// </summary>
        /// <param name="force">
        /// Indicates that any existing resources (such as virtual machines) 
        /// are to be replaced or overwritten during privisioning.  The actual interpretation
        /// of this parameter is specific to each hosting manager implementation.
        /// </param>
        /// <returns><c>true</c> if the operation was successful.</returns>
        bool Provision(bool force);

        /// <summary>
        /// Returns the FQDN or IP address (as a string) and the port to use
        /// to establish a SSH connection to a node while provisioning is in
        /// progress.
        /// </summary>
        /// <param name="nodeName">The target node's name.</param>
        /// <returns>A <b>(string Address, int Port)</b> tuple.</returns>
        /// <remarks>
        /// Hosting platforms such as Azure that may not assign public IP addresses
        /// to cluster nodes will return the IP address of the traffic manager and
        /// a temporary NAT port for the node.
        /// </remarks>
        (string Address, int Port) GetSshEndpoint(string nodeName);

        /// <summary>
        /// Adds any necessary post-provisioning steps to the step controller.
        /// </summary>
        /// <param name="controller">The target setup controller.</param>
        void AddPostProvisionSteps(SetupController<NodeDefinition> controller);

        /// <summary>
        /// <para>
        /// Returns the prefix for block devices that will be attached to
        /// the host machines.  For many hosting environments this will be
        /// <b>sd</b>, indicating that drives will be attached like: 
        /// <b>/dev/sda</b>, <b>/dev/sdb</b>, <b>/dev/sdc</b>...
        /// </para>
        /// <para>
        /// This may be different though for some hosting environment.
        /// XenServer for example, uses the <b>xvd</b> prefix and attaches
        /// drives as <b>/dev/sda</b>, <b>/dev/sdb</b>, <b>/dev/sdc</b>...
        /// </para>
        /// </summary>
        string DrivePrefix { get; }

        /// <summary>
        /// Returns <c>true</c> if provisoning requires that the user has
        /// administrator privileges.
        /// </summary>
        bool RequiresAdminPrivileges { get; }
    }
}
