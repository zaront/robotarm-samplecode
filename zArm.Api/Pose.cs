using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api
{
    public class Pose
    {
        public float?[] Servos = new float?[7];

        public Pose(params float?[] positions) : this(positions as IEnumerable<float?>)
        {
        }

        public Pose(IEnumerable<float?> positions)
        {
            //set fields
            if (positions != null)
            {
                int index = 0;
                foreach (var pos in positions)
                {
                    Servos[index] = pos;
                    index++;
                    if (index == Servos.Length)
                        break;
                }
            }
        }

        public Pose(IEnumerable<float> positions)
        {
            //set fields
            if (positions != null)
            {
                int index = 0;
                foreach (var pos in positions)
                {
                    Servos[index] = pos;
                    index++;
                    if (index == Servos.Length)
                        break;
                }
            }
        }

        public Pose()
        {
        }

        public float? this[int index]
        {
            get { return Servos[index]; }
            set { Servos[index] = value; }
        }

        public float? Get(int servoID)
        {
            return Servos[servoID - 1];
        }

        public void Set(int servoID, float? value)
        {
            Servos[servoID - 1] = value;
        }

        public float? Servo1_Position
        {
            get { return Servos[0]; }
            set { Servos[0] = value; }
        }

        public float? Servo2_Position
        {
            get { return Servos[1]; }
            set { Servos[1] = value; }
        }

        public float? Servo3_Position
        {
            get { return Servos[2]; }
            set { Servos[2] = value; }
        }

        public float? Servo4_Position
        {
            get { return Servos[3]; }
            set { Servos[3] = value; }
        }

        public float? Servo5_Position
        {
            get { return Servos[4]; }
            set { Servos[4] = value; }
        }

        public float? Servo6_Position
        {
            get { return Servos[5]; }
            set { Servos[5] = value; }
        }

        public float? Servo7_Position
        {
            get { return Servos[6]; }
            set { Servos[6] = value; }
        }
    }
}
