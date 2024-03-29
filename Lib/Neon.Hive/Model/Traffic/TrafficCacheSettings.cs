﻿//-----------------------------------------------------------------------------
// FILE:	    TrafficCacheSettings.cs
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
using Neon.Net;

namespace Neon.Hive
{
    /// <summary>
    /// Included in the traffic manager configuration ZIP archive generated by <b>neon-proxy-manager</b> 
    /// as the <b>cache-settings.json</b> file.  This will be consumed by the <b>neon-proxy-cache</b>
    /// services to implement cache warming and perhaps other features in the future.
    /// </summary>
    public class TrafficCacheSettings
    {
        /// <summary>
        /// Specifies the backend objects to be proactively loaded to implement cache warming.
        /// </summary>
        [JsonProperty(PropertyName = "WarmTargets", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public List<TrafficWarmTarget> WarmTargets { get; set; } = new List<TrafficWarmTarget>();

        /// <summary>
        /// Normalizes the instance.
        /// </summary>
        public void Normalize()
        {
            WarmTargets = WarmTargets ?? new List<TrafficWarmTarget>();
        }
    }
}
