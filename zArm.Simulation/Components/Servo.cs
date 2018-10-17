using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Physics;

namespace zArm.Simulation.Components
{
    public class Servo : Component
    {
        float _position;
        float? _minRenderRange;
        float? _maxRenderRange;

        public int ServoID { get; set; }
        public bool Invert { get; set; }
        public float MinRange { get; private set; } = -90f;
        public float MaxRange { get; private set; } = 90f;
		public ServoEvent FireEvent { get; }

		public event EventHandler<ServoChangedEventArgs> ServoChanged;
        public event EventHandler<float> RenderUpdate;
        public event EventHandler<GrabbedEventArgs> Grabbed;

        public Servo()
        {
            ReceiveSceneUpdates = true;
			FireEvent = new ServoEvent(this);
		}

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            //fire event
            RenderUpdate?.Invoke(this, timeStep);
        }

        private void SliderChanged(object sender, float slider)
        {
            Position = slider;
        }

        public float Position
        {
            get { return _position; }
            set
            {

                if (_position == value)
                    return;

                _position = value;
                var hinge = Node.GetComponent<Constraint>();
                var position = _position;
                if (_minRenderRange != null && position < _minRenderRange.Value)
                    position = _minRenderRange.Value;
                if (_maxRenderRange != null && position > _maxRenderRange.Value)
                    position = _maxRenderRange.Value;
                position = (Invert) ? -position : position;
                hinge.HighLimit = new Vector2(position, 0.0f);
                hinge.LowLimit = new Vector2(position, 0.0f);
                hinge.CFM = 0f;
                hinge.ERP = 2f;

                hinge.OwnBody.Activate();
                hinge.OtherBody.Activate();

                //fire events
                ServoChanged?.Invoke(this, new ServoChangedEventArgs() { ServoID = ServoID, Position = _position });
            }
        }

        public Vector3 HingeLocation
        {
            get
            {
                var hinge = Node.GetComponent<Constraint>();
                return hinge.WorldPosition;
            }
        }

        public Quaternion HingeNormal
        {
            get
            {
                var hinge = Node.GetComponent<Constraint>();
                var normal = hinge.Node.WorldRotation * hinge.Rotation;
                return normal;
            }
        }

        public void SetLimits(float minRange, float maxRange)
        {
            //set limits
            MinRange = minRange;
            MaxRange = maxRange;

            //clamp to new limits
            var newPos = MathHelper.Clamp(Position, MinRange, MaxRange);
            if (newPos != Position)
                Position = newPos;
        }

        public void SetRenderLimits(float? minRenderRange, float? maxRenderRange)
        {
            //set limits
            _minRenderRange = minRenderRange;
            _maxRenderRange = maxRenderRange;
        }

		public class ServoEvent
		{
			Servo _servo;

			internal ServoEvent(Servo servo)
			{
				_servo = servo;
			}

			public void SetGrabbed(bool isGrabbed)
			{
				_servo.Grabbed?.Invoke(this, new GrabbedEventArgs() { IsGrabbed = isGrabbed });
			}
		}
    }



    public class ServoChangedEventArgs : EventArgs
    {
        public int ServoID { get; set; }
        public float Position { get; set; }
    }

    public class GrabbedEventArgs : EventArgs
    {
        public bool IsGrabbed { get; set; }
    }
}
