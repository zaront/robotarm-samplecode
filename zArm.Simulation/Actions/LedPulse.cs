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
    public class LedPulse : ChangeTo<float>
    {
        Color _color;
        Action<bool> _isOn;
        bool _sendIsOn;

        public LedPulse(float duration, float endPosition, Color color, Action<bool> IsOn) : base(duration, endPosition, OnCurrentPosition, OnChange)
        {
            //set fields
            _color = color;
            _isOn = IsOn;
        }

        static float OnCurrentPosition(BaseAction action, Node node)
        {
            var led = node.GetComponent<Led>();
            if (led != null)
                return led.Brightness;
            return 0;
        }

        static void OnChange(BaseAction action, Node node, float start, float end, float percentage)
        {
            var led = node.GetComponent<Led>();
            if (led != null)
            {
                var instance = (action as LedPulse);
                var brightness = MathHelper.Lerp(start, end, percentage) * 2f;
                if (brightness > end)
                    brightness = end - brightness;
                led.SetLed(brightness, instance._color);

                if (percentage >= .5f && !instance._sendIsOn)
                {
                    instance._sendIsOn = true;
                    instance._isOn?.Invoke(true);
                }
                if (percentage == 1)
                    instance._isOn?.Invoke(false);
            }
        }
    }
}
