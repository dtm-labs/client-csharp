using DtmCommon;
using System;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class BranchIDGenTests
    {
        [Fact]
        public void TestNewSubBranchID()
        {
            var b = new BranchIDGen("");

            // 01,...,09
            for (int i = 0; i < 9; i++)
            {
                var n = b.NewSubBranchID();
                Assert.Equal($"0{i + 1}", n);
            }

            // 10~98
            for (int i = 9; i < 99; i++)
            {
                var n = b.NewSubBranchID();
                Assert.Equal($"{i + 1}", n);
            }

            // 99~
            Assert.Throws<ArgumentException>(() => b.NewSubBranchID());
        }

        [Fact]
        public void NewSubBranchID_With_BranchId_Should_Succeed()
        {
            var b = new BranchIDGen("ss");
            var n = b.NewSubBranchID();
            Assert.Equal($"ss01", n);
        }

        [Fact]
        public void NewSubBranchID_With_BranchId_Should_Throw_Exception()
        {
            var b = new BranchIDGen("sssssssssssssssssssss");
            Assert.Throws<ArgumentException>(() => b.NewSubBranchID());
        }
    }
}
