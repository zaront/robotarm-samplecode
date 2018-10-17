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
    public class KinamaticTests
    {
        [TestMethod()]
        public void GetDistanceUsingAccelerationTest()
        {
            //basic
            Assert.AreEqual(2.5, Kinamatic.GetDistanceUsingAcceleration(0, .2, 5));
            Assert.AreEqual(1, Kinamatic.GetVelocityUsingTime(0, .2, 5));
            Assert.AreEqual(2.5, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 5));
            Assert.AreEqual(0, Kinamatic.GetVelocityUsingTime(1, -.2, 5));

            //speed up
            Assert.AreEqual(0, Kinamatic.GetDistanceUsingAcceleration(0, .2, 0));
            Assert.AreEqual(.1, Kinamatic.GetDistanceUsingAcceleration(0, .2, 1));
            Assert.AreEqual(.4, Kinamatic.GetDistanceUsingAcceleration(0, .2, 2));
            Assert.AreEqual(.9, Kinamatic.GetDistanceUsingAcceleration(0, .2, 3));
            Assert.AreEqual(1.6, Kinamatic.GetDistanceUsingAcceleration(0, .2, 4));
            Assert.AreEqual(2.5, Kinamatic.GetDistanceUsingAcceleration(0, .2, 5));

            //slow down
            Assert.AreEqual(0, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 0));
            Assert.AreEqual(.9, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 1));
            Assert.AreEqual(1.6, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 2));
            Assert.AreEqual(2.1, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 3));
            Assert.AreEqual(2.4, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 4));
            Assert.AreEqual(2.5, Kinamatic.GetDistanceUsingAcceleration(1, -.2, 5));

        }
    }
}
