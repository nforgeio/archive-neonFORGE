﻿//-----------------------------------------------------------------------------
// FILE:	    KubeException.cs
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
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Neon.Common;

namespace Neon.Kube
{
    /// <summary>
    /// Describes a cluster related related error.
    /// </summary>
    public class KubeException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public KubeException()
        {
        }

        /// <summary>
        /// Constructs and instance with a message and an optional inner exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The optional inner exception.</param>
        public KubeException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
