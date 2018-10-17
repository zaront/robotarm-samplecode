using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using zArm.Simulation.Components;

namespace zArm.Simulation.Actions
{
    public class ServoTo : ChangeTo<float>
    {
        public ServoTo(float duration, float endPosition) : base(duration, endPosition, OnCurrentPosition, OnChange)
        { }

        static float OnCurrentPosition(BaseAction action, Node node)
        {
            var servo = node.GetComponent<Servo>();
            if (servo != null)
                return servo.Position;
            return 0;
        }

        static void OnChange(BaseAction action, Node node, float start, float end, float percentage)
        {
            var servo = node.GetComponent<Servo>();
            if (servo != null)
                servo.Position = MathHelper.Lerp(start, end, percentage);
        }
    }
}
