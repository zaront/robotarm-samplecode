using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation.Components
{
    public class CameraFly : Component, IInputReceiver
    {
        bool _captureMouse = false;
        
        IInputPermissions _inputPermissions;
        public float MoveSpeed { get; set; } = 10.0f;
        public float MouseSensitivity { get; set; } = .1f;

        void IInputReceiver.ReceivedInputControl(IInputPermissions inputPermissions)
        {
            _inputPermissions = inputPermissions;
        }

        void IInputReceiver.RevokedInputControl(IInputPermissions inputPermissions)
        {
            _inputPermissions = null;
        }

        void IInputReceiver.OnInputUpdate(float timeStep, IInputPermissions inputPermissions)
        {
            if (EnabledEffective)
                MoveCamera(timeStep, inputPermissions);
        }

        void MoveCamera(float timeStep, IInputPermissions inputPermissions)
        {
            var input = Application.Input;
            var moveSpeed = MoveSpeed;

            if (Application.UI.FocusElement != null)
                return;

            if (inputPermissions.CanUseMouse)
            {
                if (CaptureMouse)
                {
                    input.CenterMousePosition();

                    var cameraRotation = Node.GetOrCreateComponent<CameraRotation>();
                    var mouseMove = input.MouseMove;
                    cameraRotation.Yaw += MouseSensitivity * mouseMove.X;
                    cameraRotation.Pitch += MouseSensitivity * mouseMove.Y;
                    Node.Rotation = new Quaternion(cameraRotation.Pitch, cameraRotation.Yaw, 0);

                    //mouse click to exit
                    if (input.GetMouseButtonPress(MouseButton.Left))
                        CaptureMouse = false;
                }
                else
                {
                    //mouse click to capture
                    if (input.GetMouseButtonPress(MouseButton.Left))
                        CaptureMouse = true;
                }

                //mouse wheel
                var wheel = input.MouseMoveWheel;
                Node.Translate(Vector3.UnitZ * moveSpeed * timeStep * wheel * 20f);

            }
            else
            {
                inputPermissions.MouseVisible = true;
            }

            if (inputPermissions.CanUseKeyboard)
            {
                if (input.GetKeyDown(Key.LeftShift))
                    moveSpeed = moveSpeed / 10f;
                if (input.GetKeyDown(Key.W)) Node.Translate(Vector3.UnitZ * moveSpeed * timeStep);
                if (input.GetKeyDown(Key.S)) Node.Translate(-Vector3.UnitZ * moveSpeed * timeStep);
                if (input.GetKeyDown(Key.A)) Node.Translate(-Vector3.UnitX * moveSpeed * timeStep);
                if (input.GetKeyDown(Key.D)) Node.Translate(Vector3.UnitX * moveSpeed * timeStep);
                if (input.GetKeyDown(Key.X)) Node.Translate(-Vector3.UnitY * moveSpeed * timeStep);
                if (input.GetKeyDown(Key.E)) Node.Translate(Vector3.UnitY * moveSpeed * timeStep);
            }
        }

        public bool CaptureMouse
        {
            get { return _captureMouse; }
            set
            {
                _captureMouse = value;
                if (_inputPermissions != null)
                {
                    _inputPermissions.MouseVisible = !value;
                    _inputPermissions.RequestExclusiveMouse = value;
                }
            }

        }
    }
}
