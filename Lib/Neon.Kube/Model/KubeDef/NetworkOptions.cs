﻿//-----------------------------------------------------------------------------
// FILE:	    NetworkOptions.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

using Neon.Common;
using Neon.Net;

namespace Neon.Kube
{
    /// <summary>
    /// Describes the network options for a cluster.
    /// </summary>
    public class NetworkOptions
    {
        private const string defaultPodSubnet     = "10.254.0.0/16";
        private const string defaultServiceSubnet = "10.253.0.0/16";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NetworkOptions()
        {
        }

        /// <summary>
        /// Specifies the subnet for entire host network for on-premise environments like
        /// <see cref="HostingEnvironments.Machine"/>, <see cref="HostingEnvironments.HyperVLocal"/> and
        /// <see cref="HostingEnvironments.XenServer"/>.  This is required for those environments.
        /// </summary>
        [JsonProperty(PropertyName = "PremiseSubnet", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "PremiseSubnet", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string PremiseSubnet { get; set; }

        /// <summary>
        /// <para>
        /// The subnet where the cluster nodes reside.
        /// </para>
        /// <note>
        /// This property must be configured for the on-premise providers (<see cref="HostingEnvironments.Machine"/>, 
        /// <b>HyperV</b>, and <b>XenServer</b>".  This is computed automatically by the <b>neon</b> tool when
        /// provisioning in a cloud environment.
        /// </note>
        /// <note>
        /// For on-premise clusters, the statically assigned IP addresses assigned 
        /// to the nodes must reside within the this subnet.  The network gateway
        /// will be assumed to be the second address in this subnet and the broadcast
        /// address will assumed to be the last address.
        /// </note>
        /// </summary>
        [JsonProperty(PropertyName = "NodesSubnet", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "NodesSubnet", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string NodeSubnet { get; set; }

        /// <summary>
        /// <para>
        /// Specifies the pod subnet to be used for the cluster.  This subnet will be
        /// split so that each node will be allocated its own subnet.  This defaults
        /// to <b>10.254.0.0/16</b>.
        /// </para>
        /// <note>
        /// <b>WARNING:</b> This subnet must not conflict with any other subnets
        /// provisioned within the premise network.
        /// </note>
        /// </summary>
        [JsonProperty(PropertyName = "PodSubnet", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "PodSubnet", ApplyNamingConventions = false)]
        [DefaultValue(defaultPodSubnet)]
        public string PodSubnet { get; set; } = defaultPodSubnet;

        /// <summary>
        /// Specifies the subnet subnet to be used for the allocating service addresses
        /// within the cluster.  This defaults to <b>10.253.0.0/16</b>.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceSubnet", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "ServiceSubnet", ApplyNamingConventions = false)]
        [DefaultValue(defaultServiceSubnet)]
        public string ServiceSubnet { get; set; } = defaultServiceSubnet;

        /// <summary>
        /// The IP addresses of the upstream DNS nameservers to be used by the cluster.  This defaults to the 
        /// Google Public DNS servers: <b>[ "8.8.8.8", "8.8.4.4" ]</b> when the property is <c>null</c> or empty.
        /// </summary>
        [JsonProperty(PropertyName = "Nameservers", Required = Required.Default)]
        [YamlMember(Alias = "Nameservers", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string[] Nameservers { get; set; } = null;

        /// <summary>
        /// Specifies the default network gateway address to be configured for hosts.  This defaults to the 
        /// first usable address in the <see cref="PremiseSubnet"/>.  For example, for the <b>10.0.0.0/24</b> 
        /// subnet, this will be set to <b>10.0.0.1</b>.  This is ignored for cloud hosting 
        /// environments.
        /// </summary>
        [JsonProperty(PropertyName = "Gateway", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "Gateway", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Gateway { get; set; } = null;

        /// <summary>
        /// Specifies the cluster network provider.  This currently defaults to <see cref="NetworkCni.Calico"/>
        /// but this will change to the <see cref="NetworkCni.Istio"/> integrated provider once that stablizes.
        /// </summary>
        [JsonProperty(PropertyName = "Cni", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "Cni", ApplyNamingConventions = false)]
        [DefaultValue(NetworkCni.Calico)]
        public NetworkCni Cni { get; set; } = NetworkCni.Calico;

        /// <summary>
        /// <para>
        /// Specifies the cluster network provider version.  This defaults to <b>default</b> which will install
        /// a reasonable supported version.
        /// </para>
        /// <note>
        /// This is ignored for the <see cref="NetworkCni.Istio"/> integrated provider.
        /// </note>
        /// </summary>
        [JsonProperty(PropertyName = "CniVersion", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "CniVersion", ApplyNamingConventions = false)]
        [DefaultValue("default")]
        public string CniVersion { get; set; } = "default";

        /// <summary>
        /// Specifies the version  of Istio to be installed.  This defaults to <b>default</b> which
        /// will install a reasonable supported version.
        /// </summary>
        [JsonProperty(PropertyName = "IstioVersion", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "IstioVersion", ApplyNamingConventions = false)]
        [DefaultValue("default")]
        public string IstioVersion { get; set; } = "default";

        /// <summary>
        /// Enables global mutual TLS for communication between pods via the Istio sidecar proxies.
        /// This defaults to <c>true</c>
        /// </summary>
        [JsonProperty(PropertyName = "IstioMutualTls", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "IstioMutualTls", ApplyNamingConventions = false)]
        [DefaultValue(true)]
        public bool IstioMutualTls { get; set; } = true;

        /// <summary>
        /// Used for checking subnet conflicts below.
        /// </summary>
        private class SubnetDefinition
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Subnet name.</param>
            /// <param name="cidr">Subnet CIDR.</param>
            public SubnetDefinition(string name, NetworkCidr cidr)
            {
                this.Name = $"{nameof(NetworkOptions)}.{name}";
                this.Cidr = cidr;
            }

            /// <summary>
            /// Identifies the subnet.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The subnet CIDR.
            /// </summary>
            public NetworkCidr Cidr { get; set; }
        }

        /// <summary>
        /// Validates the options and also ensures that all <c>null</c> properties are
        /// initialized to their default values.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ClusterDefinitionException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            // Nameservers

            var subnets = new List<SubnetDefinition>();

            if (Nameservers == null || Nameservers.Length == 0)
            {
                Nameservers = new string[] { "8.8.8.8", "8.8.4.4" };
            }

            foreach (var nameserver in Nameservers)
            {
                if (!IPAddress.TryParse(nameserver, out var address))
                {
                    throw new ClusterDefinitionException($"[{nameserver}] is not a valid [{nameof(NetworkOptions)}.{nameof(Nameservers)}] IP address.");
                }
            }

            // Istio version.

            if (IstioVersion != "default" && !Version.TryParse(IstioVersion, out var vIstio))
            {
                throw new ClusterDefinitionException($"The [{nameof(NetworkOptions)}.{nameof(IstioVersion)}={IstioVersion}] is invalid.");
            }

            // Network provider.

            if (Cni == NetworkCni.Istio)
            {
                throw new ClusterDefinitionException($"The [{nameof(NetworkOptions)}.{nameof(Cni)}={Cni}] network provider is not implemented.");
            }

            if (Cni != NetworkCni.Istio)
            {
                if (CniVersion != "default" && !Version.TryParse(CniVersion, out var vCni))
                {
                    throw new ClusterDefinitionException($"The [{nameof(NetworkOptions)}.{nameof(CniVersion)}={CniVersion}] is invalid.");
                }
            }

            // Verify [PremiseSubnet].

            if (!NetworkCidr.TryParse(PremiseSubnet, out var premiseSubnet))
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(PremiseSubnet)}={PremiseSubnet}] is not a valid IPv4 subnet.");
            }

            subnets.Add(new SubnetDefinition(nameof(PremiseSubnet), premiseSubnet));

            // Verify [PodSubnet].

            if (!NetworkCidr.TryParse(PodSubnet, out var podSubnet))
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(PodSubnet)}={PodSubnet}] is not a valid IPv4 subnet.");
            }

            subnets.Add(new SubnetDefinition(nameof(PodSubnet), podSubnet));

            // Verify [ServiceSubnet].

            if (!NetworkCidr.TryParse(ServiceSubnet, out var serviceSubnet))
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(ServiceSubnet)}={ServiceSubnet}] is not a valid IPv4 subnet.");
            }

            subnets.Add(new SubnetDefinition(nameof(ServiceSubnet), serviceSubnet));

            // Verify [Gateway]

            if (string.IsNullOrEmpty(Gateway))
            {
                // Default to the first valid address of the cluster nodes subnet 
                // if this isn't already set.

                Gateway = premiseSubnet.FirstUsableAddress.ToString();
            }

            if (!IPAddress.TryParse(Gateway, out var gateway) || gateway.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(Gateway)}={Gateway}] is not a valid IPv4 address.");
            }

            if (!premiseSubnet.Contains(gateway))
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(Gateway)}={Gateway}] address is not within the [{nameof(NetworkOptions)}.{nameof(NetworkOptions.NodeSubnet)}={NodeSubnet}] subnet.");
            }

            // Verify [NodeSubnet].

            if (!NetworkCidr.TryParse(NodeSubnet, out var nodesSubnet))
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(NodeSubnet)}={NodeSubnet}] is not a valid IPv4 subnet.");
            }

            if (!premiseSubnet.Contains(nodesSubnet))
            {
                throw new ClusterDefinitionException($"[{nameof(NetworkOptions)}.{nameof(NodeSubnet)}={NodeSubnet}] is not within [{nameof(NetworkOptions)}.{nameof(PremiseSubnet)}={PremiseSubnet}].");
            }

            // Verify that none of the major subnets conflict.

            foreach (var subnet in subnets)
            {
                foreach (var subnetTest in subnets)
                {
                    if (subnet == subnetTest)
                    {
                        continue;   // Don't test against self.[
                    }

                    if (subnet.Cidr.Overlaps(subnetTest.Cidr))
                    {
                        throw new ClusterDefinitionException($"[{subnet.Name}={subnet.Cidr}] and [{subnetTest.Name}={subnetTest.Cidr}] overlap.");
                    }
                }
            }
        }
    }
}
