﻿//-----------------------------------------------------------------------------
// FILE:	    Test_HostsFixture.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Xunit;

using Xunit;

namespace TestCommon
{
    public class Test_HostsFixture : IClassFixture<HostsFixture>
    {
        private static readonly string HostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32", "drivers", "etc", "hosts");

        private HostsFixture fixture;

        public Test_HostsFixture(HostsFixture fixture)
        {
            this.fixture = fixture;

            fixture.Initialize(
                () =>
                {
                    // Add some entries using deferred commit.

                    fixture.AddHostAddress("www.foo.com", "1.2.3.4", deferCommit: true);
                    fixture.AddHostAddress("www.bar.com", "5.6.7.8", deferCommit: true);
                    fixture.Commit();

                    // Add an entry using auto commit.

                    fixture.AddHostAddress("www.foobar.com", "1.1.1.1");
                });
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Lookup()
        {
            Assert.Equal(new IPAddress[] { IPAddress.Parse("1.2.3.4") }, Dns.GetHostAddresses("www.foo.com"));
            Assert.Equal(new IPAddress[] { IPAddress.Parse("5.6.7.8") }, Dns.GetHostAddresses("www.bar.com"));
            Assert.Equal(new IPAddress[] { IPAddress.Parse("1.1.1.1") }, Dns.GetHostAddresses("www.foobar.com"));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Reset()
        {
            // Verify that we can reset the hosts.

            fixture.Reset();

            Assert.NotEqual(new IPAddress[] { IPAddress.Parse("1.2.3.4") }, Dns.GetHostAddresses("www.foo.com"));
            Assert.NotEqual(new IPAddress[] { IPAddress.Parse("5.6.7.8") }, Dns.GetHostAddresses("www.bar.com"));
            Assert.NotEqual(new IPAddress[] { IPAddress.Parse("1.1.1.1") }, Dns.GetHostAddresses("www.foobar.com"));

            // Restore the hosts so that other tests will work.

            fixture.AddHostAddress("www.foo.com", "1.2.3.4", deferCommit: true);
            fixture.AddHostAddress("www.bar.com", "5.6.7.8", deferCommit: true);
            fixture.AddHostAddress("www.foobar.com", "1.1.1.1", deferCommit: true);
            fixture.Commit();
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void NoDuplicates()
        {
            // Ensure that duplicate host/IP mappings are iognored and are 
            // not added to the fixture.

            try
            {
                fixture.Reset();
                fixture.AddHostAddress("www.foobar.com", "1.1.1.1");
                fixture.AddHostAddress("www.foobar.com", "1.1.1.1");

                Assert.Equal(new IPAddress[] { IPAddress.Parse("1.1.1.1") }, Dns.GetHostAddresses("www.foobar.com"));
            }
            finally
            {
                // Restore the hosts so the remaining tests won't be impacted.

                fixture.Reset();
                fixture.AddHostAddress("www.foo.com", "1.2.3.4", deferCommit: true);
                fixture.AddHostAddress("www.bar.com", "5.6.7.8", deferCommit: true);
                fixture.AddHostAddress("www.foobar.com", "1.1.1.1", deferCommit: true);
                fixture.Commit();
            }
        }
    }
}
