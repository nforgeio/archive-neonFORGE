﻿//-----------------------------------------------------------------------------
// FILE:	    LineEnding.cs
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Neon.IO
{
    /// <summary>
    /// Enumerates the possible line ending modes.
    /// </summary>
    public enum LineEnding
    {
        /// <summary>
        /// Use platform specific line endings.
        /// </summary>
        Platform = 0,

        /// <summary>
        /// Windows style line endings using carriage return and line feed characters.
        /// </summary>
        CRLF,

        /// <summary>
        /// Unix/Linux style line endings using just a line feed.
        /// </summary>
        LF
    }
}
