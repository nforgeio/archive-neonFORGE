﻿//-----------------------------------------------------------------------------
// FILE:	    VpnCredentials.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

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

using Neon.Common;
using Neon.Cryptography;

namespace Neon.Hive
{
    /// <summary>
    /// Holds the VPN certificates required for an operator to connect to the
    /// hive VPN..
    /// </summary>
    public class VpnCredentials
    {
        /// <summary>
        /// The hive's CA certificate.
        /// </summary>
        [JsonProperty(PropertyName = "CaCert", Required = Required.Always)]
        public string CaCert { get; set; }

        /// <summary>
        /// The user's certificate.
        /// </summary>
        [JsonProperty(PropertyName = "UserCert", Required = Required.Always)]
        public string UserCert { get; set; }

        /// <summary>
        /// The user's private key.
        /// </summary>
        [JsonProperty(PropertyName = "UserKey", Required = Required.Always)]
        public string UserKey { get; set; }

        /// <summary>
        /// The shared TLS authentication key used to provide
        /// firewall-style security over and above normal TLS
        /// security.
        /// </summary>
        [JsonProperty(PropertyName = "TaKey", Required = Required.Always)]
        public string TaKey { get; set; }

        /// <summary>
        /// The key used to encrypt the VPN certificate authority files stored
        /// in the hive Vault.  Operators that have this key will be able
        /// to create and revoke client VPN certificates for other users.
        /// </summary>
        [JsonProperty(PropertyName = "CaZipKey", Required = Required.AllowNull)]
        public string CaZipKey { get; set; }

        /// <summary>
        /// This temporarily holds the certificate authority ZIP archive between
        /// the time when the archive was generated by the <b>neon hive prepare</b>
        /// command until it can be persisted to the hive's Vault by <b>neon hive setup</b>.
        /// </summary>
        [JsonProperty(PropertyName = "CaZip", Required = Required.AllowNull)]
        public byte[] CaZip { get; set; }
    }
}
