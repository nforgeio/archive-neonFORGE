﻿//-----------------------------------------------------------------------------
// FILE:	    ServiceEndpointMode.cs
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
using System.Runtime.Serialization;
using System.Text;

namespace Neon.Docker
{
    /// <summary>
    /// Service endpoint mode.
    /// </summary>
    public enum ServiceEndpointMode
    {
        /// <summary>
        /// Assign a virtual IP address to the service and provide a load balancer.
        /// </summary>
        [EnumMember(Value = "vip")]
        Vip = 0,

        /// <summary>
        /// Returns DNS resource records for the active service instances.
        /// </summary>
        [EnumMember(Value = "dnsrr")]
        DnsRR
    }
}
