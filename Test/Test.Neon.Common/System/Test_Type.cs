﻿//-----------------------------------------------------------------------------
// FILE:	    Test_Type.cs
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
    internal interface IFoo
    {
        void Test();
    }

    internal class Foo : IFoo
    {
        public void Test()
        {
        }
    }

    internal class FooExtended : Foo
    {
    }

    internal class NotFoo
    {
        public void Test()
        {
        }
    }

    public class Test_Type
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Implements()
        {
            var fooType         = typeof(Foo);
            var fooExtendedType = typeof(FooExtended);
            var notFooType      = typeof(NotFoo);

            Assert.True(fooType.Implements<IFoo>());
            Assert.True(fooExtendedType.Implements<IFoo>());

            Assert.False(notFooType.Implements<IFoo>());

            Assert.Throws<ArgumentNullException>(() => ((Type)null).Implements<IFoo>());
            Assert.Throws<ArgumentException>(() => fooType.Implements<NotFoo>());
        }
    }
}
