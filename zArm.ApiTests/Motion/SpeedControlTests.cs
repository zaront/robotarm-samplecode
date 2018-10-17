using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Motion;

namespace zArm.ApiTests.Motion
{
    [TestClass()]
    public class SpeedControlTests
    {
        [TestMethod()]
        public void GetPositionTest()
        {
            //forward
            var speedControl = new SpeedControl(30, 160, .0001, .02);
            Assert.AreEqual(30, speedControl.GetPosition(-1));
            Assert.AreEqual(30, speedControl.GetPosition(0));
            Assert.AreEqual(30.005, speedControl.GetPosition(10));
            Assert.AreEqual(30.5, speedControl.GetPosition(100));
            Assert.AreEqual(48, speedControl.GetPosition(1000));
            Assert.AreEqual(68, speedControl.GetPosition(2000));
            Assert.AreEqual(88, speedControl.GetPosition(3000));
            Assert.AreEqual(148, speedControl.GetPosition(6000));
            Assert.AreEqual(158, speedControl.GetPosition(6500));
            Assert.AreEqual(159.995, speedControl.GetPosition(6690));
            Assert.AreEqual(160, speedControl.GetPosition(7000));

            //reverse
            speedControl = new SpeedControl(160, 30, .0001, .02);
            Assert.AreEqual(160, speedControl.GetPosition(-1));
            Assert.AreEqual(160, speedControl.GetPosition(0));
            Assert.AreEqual(159.995, speedControl.GetPosition(10));
            Assert.AreEqual(159.5, speedControl.GetPosition(100));
            Assert.AreEqual(142, speedControl.GetPosition(1000));
            Assert.AreEqual(122, speedControl.GetPosition(2000));
            Assert.AreEqual(102, speedControl.GetPosition(3000));
            Assert.AreEqual(42, speedControl.GetPosition(6000));
            Assert.AreEqual(32, speedControl.GetPosition(6500));
            Assert.AreEqual(30.004999999999995, speedControl.GetPosition(6690));
            Assert.AreEqual(30, speedControl.GetPosition(7000));

        }
    }
}