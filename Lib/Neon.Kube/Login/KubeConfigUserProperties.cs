﻿//-----------------------------------------------------------------------------
// FILE:	    KubeConfigUserProperties.cs
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
using YamlDotNet.Core;
using YamlDotNet.Serialization;

using Neon.Common;
using Neon.Cryptography;

namespace Neon.Kube
{
    /// <summary>
    /// Describes a Kubernetes user's credentials.
    /// </summary>
    public class KubeConfigUserProperties
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public KubeConfigUserProperties()
        {
        }

        /// <summary>
        /// The optional authentication token (or <c>null</c>).
        /// </summary>
        [JsonProperty(PropertyName = "token", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "token", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Token { get; set; }

        /// <summary>
        /// The optional path to the client certificate (or <c>null</c>).
        /// </summary>
        [JsonProperty(PropertyName = "client-certificate-data", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "client-certificate-data", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string ClientCertificateData { get; set; }

        /// <summary>
        /// The optional client key data.
        /// </summary>
        [JsonProperty(PropertyName = "client-key-data", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "client-key-data", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string ClientKeyData { get; set; }

        /// <summary>
        /// The optional username (or <c>null</c>).
        /// </summary>
        [JsonProperty(PropertyName = "username", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "username", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Username { get; set; }

        /// <summary>
        /// The optional password (or <c>null</c>).
        /// </summary>
        [JsonProperty(PropertyName = "password", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "password", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Password { get; set; }
    }
}
