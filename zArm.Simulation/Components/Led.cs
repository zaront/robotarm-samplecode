using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation.Components
{
    public class Led : Component
    {
        Color? _color;
        Light _light;
        float _brightness;

        public Vector3 LightPosition { get; set; }

        public Color? Color
        {
            get { return _color; }
            set
            {
                SetLed(_brightness, value);
            }
        }

        public float Brightness
        {
            get { return _brightness; }
            set
            {
                SetLed(value, _color);
            }
        }

        public void SetLed(float brightness, Color? color = null)
        {
            //set fields
            _brightness = MathHelper.Clamp(brightness, 0f, 1f);
            _color = color;

            //hilight
            var model = Node.GetComponent<StaticModel>();
            if (_color == null)
                SetHilight(false, model, Urho.Color.White);
            else
                SetHilight(true, model, _color.Value);

            //create light
            if (_light == null)
            {
                var lightNode = Node.CreateChild();
                _light = lightNode.CreateComponent<Light>();
                lightNode.Position = LightPosition;
                _light.LightType = LightType.Point;
                _light.FadeDistance = .5f;
                _light.Range = 6f;
                //_light.CastShadows = true;
            }

            //set light color
            if (_color == null || Urho.Color.Black.Equals(_color) || _brightness == 0)
                _light.Enabled = false;
            else
            {
                _light.Enabled = true;
                _light.Color = _color.Value;
                _light.Brightness = _brightness * 2f;
            }
        }

        void SetHilight(bool hilight, StaticModel mesh, Color color)
        {
            for (uint i = 0; i < 5; i++)
            {
                var mat = mesh.GetMaterial(i);
                if (mat == null)
                    break;
                var matCopy = mat.Clone(mat.Name + "_hilight");
                matCopy.SetShaderParameter("MatDiffColor", hilight ? color : Urho.Color.White);
                mesh.SetMaterial(i, matCopy);
            }
        }
    }
}
