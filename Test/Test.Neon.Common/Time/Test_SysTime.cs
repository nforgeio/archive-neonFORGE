﻿//-----------------------------------------------------------------------------
// FILE:	    Test_SysTime.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Time;
using Neon.Xunit;

using Xunit;

namespace TestCommon
{
    public class Test_SysTime
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void InitialValue()
        {
            SysTime.Reset();
            Assert.True(SysTime.Now >= DateTime.MinValue + TimeSpan.FromDays(365 / 2));
            Assert.True(SysTime.Now <= DateTime.MinValue + TimeSpan.FromDays(365 * 1.5));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Delta()
        {
            DateTime start;
            DateTime end;
            TimeSpan delta;

            start = SysTime.Now;

            Thread.Sleep(1000);

            end = SysTime.Now;
            delta = end - start;
            Assert.True(delta >= TimeSpan.FromSeconds(1) - SysTime.Resolution);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Infinite()
        {
            Assert.True(SysTime.Now + SysTime.Infinite >= DateTime.MaxValue - TimeSpan.FromDays(366));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Resolution()
        {
            Assert.True(SysTime.Resolution > TimeSpan.Zero);
            Assert.True(SysTime.Resolution <= TimeSpan.FromMilliseconds(100));
        }
    }
}
