using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using Urho.Physics;
using Urho.Resources;
using zArm.Simulation;

namespace zArm.Simulation.Components
{
    public class Gimbal : Hilight, IInputReceiver, ISelectable
    {
        Plane _plane;
        Vector3 _offset;
        IntVector2 _prevMousePos;
        Color _origColor;
        bool _origKinematic;
		bool _usingTouch;

        public float MoveSpeed { get; set; } = 4.0f;
        public bool IsSelectable { get; set; } = true;

        void IInputReceiver.ReceivedInputControl(IInputPermissions inputPermissions)
        {
            inputPermissions.RequestExclusiveMouse = true;
            inputPermissions.RequestExclusiveKeyboard = true;
        }

        void IInputReceiver.RevokedInputControl(IInputPermissions inputPermissions)
        {
        }

        void IInputReceiver.OnInputUpdate(float timeStep, IInputPermissions inputPermissions)
        {
            if (EnabledEffective)
                MoveNode(timeStep, inputPermissions);
        }

        bool ISelectable.SelectSubObject()
        {
            return false;
        }

        void ISelectable.MouseEntered(MouseEnteredArgs e)
        {
            //validate
            if (!IsSelectable)
                return;

        }

        void ISelectable.MouseExited()
        {
            //validate
            if (!IsSelectable)
                return;

        }

        void ISelectable.MouseSelecting(MouseSelectingArgs e)
        {
            //validate
            if (!IsSelectable)
            {
                e.Select = false;
                return;
            }

            //set to hilight color
            _origColor = Node.GetComponent<Urho.Shapes.Shape>().Color;
            var color = Color.Red;
            if (_origColor.ToUInt() == color.ToUInt())
                color = Color.Yellow;
            Node.RunActionsAsync(new TintTo(.2f, color.R, color.G, color.B));

            //get selection plane for moving within
            if (e.Ray != null)
            {
                _plane = new Plane(e.Ray.Value.Normal, e.Ray.Value.Position);
                _offset = e.Ray.Value.Node.Position - e.Ray.Value.Position;
            }

            //change to kinamatic
            var r = Node.GetComponent<RigidBody>();
            if (r != null)
            {
                _origKinematic = r.Kinematic;
                r.Kinematic = true;
            }

			//using touch
			_usingTouch = Application.Input.IsTouching();
		}

        void ISelectable.MouseUnselected()
        {
            //set to orig color
            var color = _origColor;
            Node.RunActionsAsync(new TintTo(.2f, color.R, color.G, color.B));

            //change to non kinamatic
            var r = Node.GetComponent<RigidBody>();
            if (r != null)
                r.Kinematic = _origKinematic;
        }

        private void MoveNode(float timeStep, IInputPermissions inputPermissions)
        {
            var input = Application.Input;
            if (inputPermissions.CanUseKeyboard)
            {
                using (var n = new Node())
                {

                    n.SetDirection(_plane.Normal);
                    if (input.GetKeyDown(Key.W)) n.Translate(Vector3.UnitY * MoveSpeed * timeStep, TransformSpace.Local);
                    if (input.GetKeyDown(Key.S)) n.Translate(-Vector3.UnitY * MoveSpeed * timeStep, TransformSpace.Local);
                    if (input.GetKeyDown(Key.A)) n.Translate(Vector3.UnitX * MoveSpeed * timeStep, TransformSpace.Local);
                    if (input.GetKeyDown(Key.D)) n.Translate(-Vector3.UnitX * MoveSpeed * timeStep, TransformSpace.Local);
                    if (input.GetKeyDown(Key.C)) n.Translate(Vector3.UnitZ * MoveSpeed * timeStep, TransformSpace.Local);
                    if (input.GetKeyDown(Key.E)) n.Translate(-Vector3.UnitZ * MoveSpeed * timeStep, TransformSpace.Local);
                    var move = n.Position;
                    Node.Translate(move, TransformSpace.Local);
                }
            }

            //move by mouse
            var mousePos = input.GetMousePositionOrTouchPosition(_usingTouch);
            if (_prevMousePos != mousePos)
            {
                _prevMousePos = mousePos;
                if (inputPermissions.CanUseMouse)
                {
                    var camera = (Application as App).CameraNode.GetComponent<Camera>();
                    var graphics = Application.Graphics;
                    Ray ray = camera.GetScreenRay((float)mousePos.X / graphics.Width, (float)mousePos.Y / graphics.Height);
                    var dist = ray.HitDistance(_plane);
                    var pos = (ray.Direction * dist) + ray.Origin;
                    Node.Position = pos + _offset;
                }
            }
        }

	}
}
