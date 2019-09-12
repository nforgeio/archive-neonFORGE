﻿//-----------------------------------------------------------------------------
// FILE:	    Test_PolledTimer.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

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
    public class Test_PolledTimer
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Normal()
        {
            PolledTimer timer;
            DateTime    sysNow;

            timer = new PolledTimer(TimeSpan.FromSeconds(1.0));
            Assert.False(timer.HasFired);
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);
            Assert.True(timer.HasFired);

            sysNow = SysTime.Now;
            timer.Reset();
            Assert.False(timer.HasFired);
            Assert.Equal(TimeSpan.FromSeconds(1.0), timer.Interval);
            Assert.True(timer.FireTime >= sysNow + timer.Interval);
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);
            Assert.True(timer.HasFired);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task Async()
        {
            var timer  = new PolledTimer(TimeSpan.FromSeconds(1.0));
            var sysNow = SysTime.Now;

            timer.Reset();
            Assert.False(timer.HasFired);

            await timer.WaitAsync(TimeSpan.FromMilliseconds(500));

            Assert.True(timer.HasFired);
            Assert.True(SysTime.Now + TimeSpan.FromMilliseconds(50) - sysNow > TimeSpan.FromSeconds(1));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void ResetImmediate()
        {
            PolledTimer timer;
            DateTime    sysNow;

            timer = new PolledTimer(TimeSpan.FromSeconds(1.0));
            timer.ResetImmediate();
            Assert.True(timer.HasFired);
            Assert.True(timer.HasFired);

            sysNow = SysTime.Now;
            timer.Reset();
            Assert.Equal(TimeSpan.FromSeconds(1.0), timer.Interval);
            Assert.True(timer.FireTime >= sysNow + timer.Interval);
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);
            Assert.True(timer.HasFired);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void AutoReset()
        {
            PolledTimer timer;
            DateTime    sysNow;

            sysNow = SysTime.Now;
            timer = new PolledTimer(TimeSpan.FromSeconds(1.0), true);
            Assert.False(timer.HasFired);
            Assert.Equal(TimeSpan.FromSeconds(1.0), timer.Interval);
            Assert.True(timer.FireTime >= sysNow + timer.Interval);
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);
            Assert.False(timer.HasFired);

            Assert.True(timer.FireTime >= SysTime.Now + timer.Interval);
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);
            Assert.False(timer.HasFired);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void FireNow()
        {
            PolledTimer timer;

            timer = new PolledTimer(TimeSpan.FromSeconds(10), true);
            Assert.False(timer.HasFired);
            timer.FireNow();
            Assert.True(timer.HasFired);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Disable()
        {
            PolledTimer timer;
            DateTime    sysNow;

            sysNow   = SysTime.Now;
            timer = new PolledTimer(TimeSpan.FromSeconds(1.0));
            Assert.False(timer.HasFired);
            Assert.Equal(TimeSpan.FromSeconds(1.0), timer.Interval);
            Assert.True(timer.FireTime >= sysNow + timer.Interval);
            Thread.Sleep(2000);
            timer.Disable();
            Assert.False(timer.HasFired);
            timer.Reset();
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);

            sysNow = SysTime.Now;
            timer.Reset();
            Assert.False(timer.HasFired);
            Assert.Equal(TimeSpan.FromSeconds(1.0), timer.Interval);
            Assert.True(timer.FireTime >= sysNow + timer.Interval);
            Thread.Sleep(2000);
            timer.Disable();
            Assert.False(timer.HasFired);
            timer.Interval = TimeSpan.FromSeconds(1.0);
            Thread.Sleep(2000);
            Assert.True(timer.HasFired);
        }
    }
}
