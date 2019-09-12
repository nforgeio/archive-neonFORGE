﻿//-----------------------------------------------------------------------------
// FILE:        Test_Block.cs
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

namespace TestCommon
{
    public class Test_Block
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Construction()
        {
            Block block;

            block = new Block(new byte[10], 0, 10);
            Assert.Equal(10, block.Buffer.Length);
            Assert.Equal(0, block.Offset);
            Assert.Equal(10, block.Length);

            block = new Block(new byte[10], 2, 8);
            Assert.Equal(10, block.Buffer.Length);
            Assert.Equal(2, block.Offset);
            Assert.Equal(8, block.Length);

            block = new Block(new byte[10]);
            Assert.Equal(0, block.Offset);
            Assert.Equal(10, block.Length);

            block = new Block(10);
            Assert.Equal(0, block.Offset);
            Assert.Equal(10, block.Length);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void Indexing()
        {
            Block block;

            block = new Block(10);
            for (int i = 0; i < 10; i++)
                Assert.Equal(0, block[i]);

            for (int i = 0; i < block.Length; i++)
                block[i] = (byte)i;

            for (int i = 0; i < block.Length; i++)
                Assert.Equal((byte)i, block[i]);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void CopyTo()
        {
            Block block;
            byte[] arr = new byte[10];

            block = new Block(20);
            for (int i = 0; i < block.Length; i++)
                block[i] = (byte)i;

            for (int i = 0; i < arr.Length; i++)
                arr[i] = 0;

            block.CopyTo(0, arr, 0, 10);
            Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, arr);

            block.CopyTo(5, arr, 0, 5);
            Assert.Equal(new byte[] { 5, 6, 7, 8, 9, 5, 6, 7, 8, 9 }, arr);

            block.SetRange(10, 10);
            block.CopyTo(0, arr, 0, 10);
            Assert.Equal(new byte[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }, arr);

            block.CopyTo(5, arr, 0, 5);
            Assert.Equal(new byte[] { 15, 16, 17, 18, 19, 15, 16, 17, 18, 19 }, arr);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public void CopyFrom()
        {
            Block block;
            byte[] arr = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            block = new Block(20);
            for (int i = 0; i < 20; i++)
                Assert.Equal(0, block[i]);

            block.CopyFrom(arr, 0, 0, 10);
            for (int i = 0; i < 20; i++)
            {
                if (i < 10)
                    Assert.Equal(arr[i], block[i]);
                else
                    Assert.Equal(0, block[i]);
            }

            block.CopyFrom(arr, 0, 10, 10);
            for (int i = 0; i < 20; i++)
            {
                if (i < 10)
                    Assert.Equal(arr[i], block[i]);
                else
                    Assert.Equal(i - 10, block[i]);
            }

            block = new Block(new byte[20], 10, 10);

            block.CopyFrom(arr, 0, 5, 5);
            for (int i = 0; i < 10; i++)
            {
                if (i < 5)
                    Assert.Equal(0, block[i]);
                else
                    Assert.Equal(i - 5, block[i]);
            }
        }
    }
}

