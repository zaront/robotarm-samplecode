using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation.Components
{
    public class CameraRotation : Component
    {
        public float Pitch { get; set; }
        public float Yaw { get; set; }
    }
}
