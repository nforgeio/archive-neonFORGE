﻿//-----------------------------------------------------------------------------
// FILE:	    KubeConfigContextProperties.cs
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
    /// Describes a Kubernetes context properties.
    /// </summary>
    public class KubeConfigContextProperties
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public KubeConfigContextProperties()
        {
        }

        /// <summary>
        /// The optional cluster nickname.
        /// </summary>
        [JsonProperty(PropertyName = "cluster", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "cluster", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Cluster { get; set; }

        /// <summary>
        /// The optional namespace.
        /// </summary>
        [JsonProperty(PropertyName = "namespace", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "namespace", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Namespace { get; set; }

        /// <summary>
        /// The optional user nickname.
        /// </summary>
        [JsonProperty(PropertyName = "user", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "user", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string User { get; set; }
    }
}
