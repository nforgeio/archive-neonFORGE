﻿//-----------------------------------------------------------------------------
// FILE:	    KubeLogin.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

using Neon.Common;
using Neon.Cryptography;

namespace Neon.Kube
{
    /// <summary>
    /// Holds all of the information required to import/export a cluster
    /// login.  This includes the Kubernetes cluster, login, and neonKUBE
    /// extensions.
    /// </summary>
    public class KubeLogin
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public KubeLogin()
        {
        }

        /// <summary>
        /// The Kubernetes cluster.
        /// </summary>
        [JsonProperty(PropertyName = "Cluster", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "Cluster", ApplyNamingConventions = false)]
        public KubeConfigCluster Cluster { get; set; }

        /// <summary>
        /// The Kubernetes context.
        /// </summary>
        [JsonProperty(PropertyName = "Context", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "Context", ApplyNamingConventions = false)]
        public KubeConfigContext Context { get; set; }

        /// <summary>
        /// The context extension information.  This may be <c>null</c>.
        /// </summary>
        [JsonProperty(PropertyName = "Extensions", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "Extensions", ApplyNamingConventions = false)]
        public KubeContextExtension Extensions { get; set; }

        /// <summary>
        /// The Kubernetes user.
        /// </summary>
        [JsonProperty(PropertyName = "User", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "User", ApplyNamingConventions = false)]
        public KubeConfigUser User { get; set; }

        /// <summary>
        /// Ensures that the login information is valid.
        /// </summary>
        /// <exception cref="KubeException">Thrown if the instance is invalid.</exception>
        public void Validate()
        {
            if (Context == null || User == null)
            {
                throw new KubeException("Invalid login.");
            }
        }
    }
}
