﻿//-----------------------------------------------------------------------------
// FILE:	    Test_EnvironmentParser.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Xunit;

using Xunit;

// $todo(jeff.lill): Verify logging behaviors.

namespace TestCommon
{
    public partial class Test_EnvironmentParser
    {
        private string Var(string name)
        {
            return $"E00AA4A9-77FE-42D5-BF2F-D919A6E26643_{name}";
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void String()
        {
            var parser = new EnvironmentParser();

            Assert.Null(parser.Get(Var("DOESNT_EXIST"), null));
            Assert.Equal("hello", parser.Get(Var("DOESNT_EXIST"), "hello"));

            Assert.Throws<KeyNotFoundException>(() => parser.Get(Var("DOESNT_EXIST"), null, required: true));
            Assert.Throws<KeyNotFoundException>(() => parser.Get(Var("DOESNT_EXIST"), "hello", required: true));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "test");
            Assert.Equal("test", parser.Get(Var("EXISTS"), null));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Integer()
        {
            var parser = new EnvironmentParser();

            Assert.Equal(55, parser.Get(Var("DOESNT_EXIST"), 55));

            Assert.Throws<KeyNotFoundException>(() => parser.Get(Var("DOESNT_EXIST"), 55, required: true));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "55");
            Assert.Equal(55, parser.Get(Var("EXISTS"), 77));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "bad");
            Assert.Equal(77, parser.Get(Var("EXISTS"), 77));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Double()
        {
            var parser = new EnvironmentParser();

            Assert.Equal(123.4, parser.Get(Var("DOESNT_EXIST"), 123.4));

            Assert.Throws<KeyNotFoundException>(() => parser.Get(Var("DOESNT_EXIST"), 123.4, required: true));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "123.4");
            Assert.Equal(123.4, parser.Get(Var("EXISTS"), 567.8));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "bad");
            Assert.Equal(567.8, parser.Get(Var("EXISTS"), 567.8));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Bool()
        {
            var parser = new EnvironmentParser();

            Assert.True(parser.Get(Var("DOESNT_EXIST"), true));
            Assert.False(parser.Get(Var("DOESNT_EXIST"), false));

            Assert.Throws<KeyNotFoundException>(() => parser.Get(Var("DOESNT_EXIST"), true, required: true));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "true");
            Assert.True(parser.Get(Var("EXISTS"), false));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "TRUE");
            Assert.True(parser.Get(Var("EXISTS"), false));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "on");
            Assert.True(parser.Get(Var("EXISTS"), false));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "ON");
            Assert.True(parser.Get(Var("EXISTS"), false));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "yes");
            Assert.True(parser.Get(Var("EXISTS"), false));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "YES");
            Assert.True(parser.Get(Var("EXISTS"), false));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "1");
            Assert.True(parser.Get(Var("EXISTS"), false));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "false");
            Assert.False(parser.Get(Var("EXISTS"), true));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "FALSE");
            Assert.False(parser.Get(Var("EXISTS"), true));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "off");
            Assert.False(parser.Get(Var("EXISTS"), true));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "OFF");
            Assert.False(parser.Get(Var("EXISTS"), true));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "no");
            Assert.False(parser.Get(Var("EXISTS"), true));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "NO");
            Assert.False(parser.Get(Var("EXISTS"), true));
            Environment.SetEnvironmentVariable(Var("EXISTS"), "0");
            Assert.False(parser.Get(Var("EXISTS"), true));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "bad");
            Assert.True(parser.Get(Var("EXISTS"), true));
            Assert.False(parser.Get(Var("EXISTS"), false));
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void TimeSpan()
        {
            var parser = new EnvironmentParser();

            Assert.Equal(System.TimeSpan.FromSeconds(5), parser.Get(Var("DOESNT_EXIST"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "10");
            Assert.Equal(System.TimeSpan.FromSeconds(10), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "20s");
            Assert.Equal(System.TimeSpan.FromSeconds(20), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "20ms");
            Assert.Equal(System.TimeSpan.FromMilliseconds(20), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "20m");
            Assert.Equal(System.TimeSpan.FromMinutes(20), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "20h");
            Assert.Equal(System.TimeSpan.FromHours(20), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "20d");
            Assert.Equal(System.TimeSpan.FromDays(20), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));

            Environment.SetEnvironmentVariable(Var("EXISTS"), "20.5s");
            Assert.Equal(System.TimeSpan.FromSeconds(20.5), parser.Get(Var("EXISTS"), System.TimeSpan.FromSeconds(5)));
        }
    }
}
