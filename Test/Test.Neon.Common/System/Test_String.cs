﻿//-----------------------------------------------------------------------------
// FILE:	    Test_String.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Retry;
using Neon.Xunit;

using Xunit;

namespace TestCommon
{
    public class Test_String
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void ToLines()
        {
            Assert.Empty(((string)null).ToLines());

            Assert.Equal(new string[] { "" }, "".ToLines());
            Assert.Equal(new string[] { "    " }, "    ".ToLines());
            Assert.Equal(new string[] { "one" }, "one".ToLines());

            Assert.Equal(new string[] { "one" }, "one\r\n".ToLines());
            Assert.Equal(new string[] { "one", "two" }, "one\r\ntwo".ToLines());
            Assert.Equal(new string[] { "one", "two", "three" }, "one\r\ntwo\r\nthree".ToLines());

            Assert.Equal(new string[] { "one" }, "one\n".ToLines());
            Assert.Equal(new string[] { "one", "two" }, "one\ntwo".ToLines());
            Assert.Equal(new string[] { "one", "two", "three" }, "one\ntwo\r\nthree".ToLines());
        }
    }
}
