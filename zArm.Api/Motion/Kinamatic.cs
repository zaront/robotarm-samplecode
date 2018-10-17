using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Motion
{
    public static class Kinamatic
    {
        public static double GetDistanceUsingAcceleration(double initialVelocity, double acceleration, double time)
        {
            return (initialVelocity * time) + (.5 * acceleration * (time * time));
        }

        public static double GetDistanceUsingVelocity(double initialVelocity, double finalVelocity, double time)
        {
            return ((initialVelocity + finalVelocity) / 2) * time;
        }


        public static double GetVelocityUsingDistance(double initialVelocity, double acceleration, double distance)
        {
            return Math.Sqrt((initialVelocity * initialVelocity) + (2 * acceleration * distance));
        }

        public static double GetVelocityUsingTime(double initialVelocity, double acceleration, double time)
        {
            return initialVelocity + (acceleration * time);
        }


        public static double GetTimeUsingVelocity(double initialVelocity, double finalVelocity, double acceleration)
        {
            if (initialVelocity == finalVelocity || acceleration == 0)
                return 0;
            return (finalVelocity - initialVelocity) / acceleration;
        }

        public static double GetTimeUsingDistance(double initialVelocity, double acceleration, double distance)
        {
            if (acceleration == 0)
                return distance / initialVelocity;

            var finalVelocity = GetVelocityUsingDistance(initialVelocity, acceleration, distance);
            return GetTimeUsingVelocity(initialVelocity, finalVelocity, acceleration);
        }

        public static double GetAccelerationUsingVelocityDistance(double initialVelocity, double finalVelocity, double distance)
        {
            return (finalVelocity * finalVelocity) - (initialVelocity * initialVelocity) / (distance * 2);
        }
    }
}
