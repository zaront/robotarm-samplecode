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
    public class LedBlink : Flash
    {
        Color _color;
        Action<bool> _isOn;
        bool _prevOn;

        public LedBlink(float duration, int count, Color color, Action<bool> IsOn) : base(duration, (uint)count, OnFlash)
        {
            //set fields
            _color = color;
            _isOn = IsOn;
        }

        static void OnFlash(BaseAction action, Node node, bool on)
        {
            if (node == null)
                return;
            var led = node.GetComponent<Led>();
            if (led == null)
                return;

            var instance = (action as LedBlink);
            if (on)
                led.SetLed(1, instance._color);
            else
                led.SetLed(0);
            if (instance._prevOn != on)
                instance._isOn?.Invoke(on);
            instance._prevOn = on;
        }
    }
}
