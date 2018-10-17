using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation
{
	public class InputManager
	{
		Input _input;
		IUISupportInput _uiSupportInput;
		Dictionary<IInputReceiver, InputPermissions> _permissions = new Dictionary<IInputReceiver, InputPermissions>();
		bool _mouseVisible = true;
		Tuple<int, int> _mousePos;

		public InputManager(Input input, IUISupportInput uiSupportInput)
		{
			//set fields
			_input = input;
			_uiSupportInput = uiSupportInput;

			_input.Enabled = true; //enable input
		}

		public void Add(IInputReceiver input)
		{
			//validate
			if (input == null || _permissions.ContainsKey(input))
				return;

			//add
			var permission = new InputPermissions(this, input);
			_permissions.Add(input, permission);

			UpdatePermissions();
			input.ReceivedInputControl(permission);
		}

		public void Remove(IInputReceiver input)
		{
			//validate
			if (input == null || !_permissions.ContainsKey(input))
				return;

			var permission = _permissions[input];
			_permissions.Remove(input);

			UpdatePermissions();
			permission.CanUseMouse = false;
			permission.CanUseKeyboard = false;
			input.RevokedInputControl(permission);
		}

		public void Clear()
		{
			foreach (var key in _permissions.Keys.ToArray())
				Remove(key);
		}

		public bool MouseVisible
		{
			get { return _mouseVisible; }
			set {
				if (_mouseVisible == value)
					return;
				_mouseVisible = value;
				if (!_mouseVisible)
				{
					_input.SetMouseGrabbed(true, false);
					_input.SetMouseVisible(false, false);
					_mousePos = _uiSupportInput?.HideMouse();
				}
				else
				{
					_input.SetMouseGrabbed(false, false);
					_input.SetMouseVisible(true, false);
					_uiSupportInput?.ShowMouse(_mousePos);
				}
			}
		}

		public void SetMouseCursor(CursorType cursor)
		{
			_uiSupportInput?.SetCursor(cursor);
		}

		void UpdatePermissions()
		{
			var exclusiveMouse = _permissions.Values.Reverse().FirstOrDefault(i => i.RequestExclusiveMouse);
			var exclusiveKeyboard = _permissions.Values.Reverse().FirstOrDefault(i => i.RequestExclusiveKeyboard);
			foreach (var key in _permissions)
			{
				var permission = key.Value;

				//mouse
				permission.CanUseMouse = (exclusiveMouse == null) ? true : exclusiveMouse == permission;

				//keyboard
				permission.CanUseKeyboard = (exclusiveKeyboard == null) ? true : exclusiveKeyboard == permission;
			}
		}

		public void UpdateInput(float timeStep)
		{
			//call each inputUpdate method - in the right order
			foreach (var key in _permissions.Reverse()) //last has highest priority
				key.Key.OnInputUpdate(timeStep, key.Value);
		}



		public class InputPermissions : IInputPermissions
		{
			InputManager _inputManager;
			IInputReceiver _input;
			bool _requestExclusiveMouse;
			bool _requestExclusiveKeyboard;

			public bool CanUseMouse { get; internal set; }
			public bool CanUseKeyboard { get; internal set; }

			internal InputPermissions(InputManager inputManager, IInputReceiver input)
			{
				//set fields
				_inputManager = inputManager;
				_input = input;
			}

			public bool RequestExclusiveMouse
			{
				get { return _requestExclusiveMouse; }
				set
				{
					if (_requestExclusiveMouse == value)
						return;
					_requestExclusiveMouse = value;
					_inputManager.UpdatePermissions();
				}
			}

			public bool RequestExclusiveKeyboard
			{
				get { return _requestExclusiveKeyboard; }
				set
				{
					if (_requestExclusiveKeyboard == value)
						return;
					_requestExclusiveKeyboard = value;
					_inputManager.UpdatePermissions();
				}
			}

			public bool MouseVisible
			{
				get { return _inputManager.MouseVisible; }
				set { _inputManager.MouseVisible = value; }
			}

			public void SetMouseCursor(CursorType cursor)
			{
				_inputManager.SetMouseCursor(cursor);
			}

			public void RevokeInputControl()
			{
				_inputManager.Remove(_input);
			}
		}
	}



	public interface IInputPermissions
	{
		bool CanUseMouse { get; }
		bool RequestExclusiveMouse { get; set; }
		bool CanUseKeyboard { get; }
		bool RequestExclusiveKeyboard { get; set; }
		void RevokeInputControl();
		bool MouseVisible { get; set; }
		void SetMouseCursor(CursorType cusor);
	}




	public interface IInputReceiver
	{
		void ReceivedInputControl(IInputPermissions inputPermissions);
		void RevokedInputControl(IInputPermissions inputPermissions);
		void OnInputUpdate(float timeStep, IInputPermissions inputPermissions);

	}


	public static class InputHelper
	{
		private static bool _isPinching;
		private static bool _isTouching;
		private static IntVector2 _lastTouchPos;

		public static bool GetMouseDownOrTouching(this Input input, out IntVector2 delta)
		{
			//mouse down
			if(input.GetMouseButtonDown(MouseButton.Left))
			{
				delta = input.MouseMove;
				return true;
			}

			//touch
			if (input.NumTouches > 0 && !_isPinching)
			{
				delta = input.GetTouch(0).Delta;
				return true;
			}

			//default value
			delta = IntVector2.Zero;
			return false;
		}

		public static bool GetMouseWheelOrPinch(this Input input, out float delta)
		{
			//mouse wheel changed
			if (input.MouseMoveWheel != 0)
			{
				delta = input.MouseMoveWheel;
				return true;
			}

			//pinch
			_isPinching = false;
			if (input.NumTouches >= 2)
			{
				_isPinching = true;
				var t1 = input.GetTouch(0);
				var t2 = input.GetTouch(1);
				var startDist = Math.Abs(IntVector2.Distance(t1.LastPosition, t2.LastPosition));
				var endDist = Math.Abs(IntVector2.Distance(t1.Position, t2.Position));
				var deltaDist = endDist- startDist;
				if (deltaDist != 0)
				{
					delta = (float)deltaDist * .02f;
					return true;
				}
			}

			//default value
			delta = 0;
			return false;
		}

		public static bool GetMouseClickOrTouch(this Input input)
		{
			//mouse click
			if (input.GetMouseButtonPress(MouseButton.Left))
				return true;

			//touched
			if (input.NumTouches > 0 && !_isTouching)
			{
				return true;
			}
			_isTouching = input.NumTouches > 0;

			//default value
			return false;
		}

		public static IntVector2 GetMousePositionOrTouchPosition(this Input input, bool touchOnly = false)
		{
			//touch
			if ((input.NumTouches > 0 || touchOnly) && !_isPinching)
			{
				if (input.NumTouches > 0)
				{
					_lastTouchPos = input.GetTouch(0).Position;
					return input.GetTouch(0).Position;
				}
				else
					return _lastTouchPos;
			}

			//mouse position
			return input.MousePosition;
		}

		public static bool IsTouching(this Input input)
		{
			return input.NumTouches > 0;
		}
	}
}
