using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation.Components
{
    public class KnobGimbal : Hilight, IInputReceiver, ISelectable
    {
        IntVector2 _mousePosition;
        float _origKnobPos;
        Color _selectedColor = Color.Red;
        Color _hoverColor = new Color(1, .8f, .8f);
        bool _mouseEnteredButton;
        bool _selectedButton;
		bool _usingTouch;


		public bool IsSelectable { get; set; } = true;

        Knob GetKnob()
        {
            return Node.GetComponent<Knob>();
        }

        Button GetButton()
        {
            return Node.GetComponent<Button>();
        }

        bool ISelectable.SelectSubObject()
        {
            return true;
        }

        bool IsButton(uint subObject)
        {
            if (subObject == 0)
                return false;
            return true;
        }

        void ISelectable.MouseEntered(MouseEnteredArgs e)
        {
            //validate
            if (!IsSelectable)
                return;

            _mouseEnteredButton = IsButton(e.SubObject);
            if (_mouseEnteredButton)
                SetHilight(true, _hoverColor, 1);
            else
                SetHilight(true, _hoverColor, 0);
        }

        void ISelectable.MouseExited()
        {
            //validate
            if (!IsSelectable)
                return;

            if (_mouseEnteredButton)
                SetHilight(false, _hoverColor, 1);
            else
                SetHilight(false, _hoverColor, 0);
        }

        void ISelectable.MouseSelecting(MouseSelectingArgs e)
        {
            //validate
            if (!IsSelectable)
            {
                e.Select = false;
                return;
            }

            _selectedButton = IsButton(e.SubObject);
            if (_mouseEnteredButton)
            {
                SetHilight(true, _selectedColor, 1);

                //press the button
                GetButton().Pressed = true;
            }
            else
            {
                SetHilight(true, _selectedColor, 0);
                SetHilight(true, _selectedColor, 2);

                //get starting positions
                _mousePosition = Application.Input.GetMousePositionOrTouchPosition();
                _origKnobPos = GetKnob().Position;

				//using touch
				_usingTouch = Application.Input.IsTouching();
			}
        }

        void ISelectable.MouseUnselected()
        {
            if (_mouseEnteredButton)
            {
                SetHilight(false, _selectedColor, 1);

                //stop pressing the button
                GetButton().Pressed = false;
            }
            else
            {
                SetHilight(false, _selectedColor, 0);
                SetHilight(false, _selectedColor, 2);
            }
        }

        void IInputReceiver.ReceivedInputControl(IInputPermissions inputPermissions)
        {
            inputPermissions.RequestExclusiveMouse = true;
        }

        void IInputReceiver.RevokedInputControl(IInputPermissions inputPermissions)
        {
        }

        void IInputReceiver.OnInputUpdate(float timeStep, IInputPermissions inputPermissions)
        {
            if (EnabledEffective && !_selectedButton)
                MoveServo(inputPermissions);
        }

        private void MoveServo(IInputPermissions inputPermissions)
        {
            //validate
            if (!inputPermissions.CanUseMouse)
                return;

            //get diffrence
            var currentMousePosition = Application.Input.GetMousePositionOrTouchPosition(_usingTouch);
            var knob = GetKnob();
            var mouseDiff = IntVector2.Subtract(_mousePosition, currentMousePosition);
            var dif = ((float)mouseDiff.Y) * .1f;
            var newPos = _origKnobPos + dif;
            newPos = MathHelper.Clamp(newPos, -8000f, 8000f);
            var newPosInt = (int)Math.Round(newPos);

            //move knob in sim
            if (knob.Position != newPosInt)
                knob.Position = newPosInt;
        }
    }
}
