using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Physics;

namespace zArm.Simulation.Components
{
    public class Knob : Component
    {
        int _position;

        public int TotalPositions { get; set; } = 20;

        public event EventHandler<KnobChangedEventArgs> KnobChanged;

        public int Position
        {
            get { return _position; }
            set
            {

                if (_position == value)
                    return;

                var change = value - _position;
                _position = value;

                //get angle
                var rotations = _position % TotalPositions;
                if (rotations < 0)
                    rotations += TotalPositions;
                var angle = rotations * (360f / TotalPositions);

                var hinge = Node.GetComponent<Constraint>();
                hinge.HighLimit = new Vector2(angle, 0.0f);
                hinge.LowLimit = new Vector2(angle, 0.0f);
                hinge.CFM = 0f;
                hinge.ERP = 2f;

                hinge.OwnBody.Activate();
                hinge.OtherBody.Activate();

                //fire event
                KnobChanged?.Invoke(this, new KnobChangedEventArgs() { Position = _position, Amount = change });
            }
        }
    }


    public class KnobChangedEventArgs : EventArgs
    {
        public int Position { get; set; }
        public int Amount { get; set; }
    }
}
