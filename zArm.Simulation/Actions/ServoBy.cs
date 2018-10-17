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
    public class ServoBy : ChangeBy<float>
    {
        public ServoBy(float duration, float changePosition) : base(duration, changePosition, OnCurrentPosition, OnAddPosition, OnChange)
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

        static float OnAddPosition(float start, float change)
        {
            return start + change;
        }
    }
}
