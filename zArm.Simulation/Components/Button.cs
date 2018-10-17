using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation.Components
{
    public class Button : Component
    {
        bool _pressed;

        public event EventHandler<ButtonChangedEventArgs> ButtonChanged;

        public bool Pressed
        {
            get { return _pressed; }
            set
            {
                if (_pressed == value)
                    return;

                _pressed = value;

                //fire events
                ButtonChanged?.Invoke(this, new ButtonChangedEventArgs() { Pressed = _pressed });
            }
        }

    }


    public class ButtonChangedEventArgs : EventArgs
    {
        public bool Pressed { get; set; }
    }
}
