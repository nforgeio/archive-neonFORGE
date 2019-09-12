﻿//-----------------------------------------------------------------------------
// FILE:	    ServiceTaskTemplate.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Neon.Docker
{
    /// <summary>
    /// User modifiable service task configuration.
    /// </summary>
    public class ServiceTaskTemplate : INormalizable
    {
        /// <summary>
        /// Service container settings.
        /// </summary>
        [JsonProperty(PropertyName = "ContainerSpec", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public ServiceContainerSpec ContainerSpec { get; set; }

        /// <summary>
        /// Specifies resource requirements for each service container.
        /// </summary>
        [JsonProperty(PropertyName = "Resources", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public ServiceResources Resources { get; set; }

        /// <summary>
        /// Restart policy for service containers.
        /// </summary>
        [JsonProperty(PropertyName = "RestartPolicy", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public ServiceRestartPolicy RestartPolicy { get; set; }

        /// <summary>
        /// Service container placement options.
        /// </summary>
        [JsonProperty(PropertyName = "Placement", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public ServicePlacement Placement { get; set; }

        /// <summary>
        /// Counter that triggers an update even if no relevant service properties have changed.
        /// </summary>
        [JsonProperty(PropertyName = "ForceUpdate", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(0)]
        public long ForceUpdate { get; set; }

        /// <summary>
        /// Specifies the runtime for the service task executor.
        /// </summary>
        [JsonProperty(PropertyName = "Runtime", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public string Runtime { get; set; }

        /// <summary>
        /// Specifies the networks to be attached to the service containers.
        /// </summary>
        [JsonProperty(PropertyName = "Networks", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public List<ServiceNetwork> Networks { get; set; }

        /// <summary>
        /// Optionally specifies the log driver to use for the service containers.
        /// </summary>
        [JsonProperty(PropertyName = "LogDriver", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public ServiceLogDriver LogDriver { get; set; }

        /// <summary>
        /// Optionally specifies the network endpoints for the service containers.
        /// </summary>
        [JsonProperty(PropertyName = "EndpointSpec", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public ServiceEndpointSpec EndpointSpec { get; set; }

        /// <inheritdoc/>
        public void Normalize()
        {
            ContainerSpec = ContainerSpec ?? new ServiceContainerSpec();
            Resources     = Resources ?? new ServiceResources();
            RestartPolicy = RestartPolicy ?? new ServiceRestartPolicy();
            Placement     = Placement ?? new ServicePlacement();
            Networks      = Networks ?? new List<ServiceNetwork>();
            EndpointSpec  = EndpointSpec ?? new ServiceEndpointSpec();

            ContainerSpec?.Normalize();
            Resources?.Normalize();
            RestartPolicy?.Normalize();
            Placement?.Normalize();
            LogDriver?.Normalize();
            EndpointSpec?.Normalize();

            foreach (var item in Networks)
            {
                item?.Normalize();
            }
        }
    }
}
