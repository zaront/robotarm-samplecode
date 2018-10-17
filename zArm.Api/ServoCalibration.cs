using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api
{
    public class ServoCalibration
    {
        public int MinRange { get; set; }
        public int MaxRange { get; set; }
        public float ServoLinearization { get; set; }
        public float ServoOffset { get; set; }
        public float FeedbackLinearization { get; set; }
        public float FeedbackOffset { get; set; }
        public int MaxSpeed { get; set; }
    }
}
