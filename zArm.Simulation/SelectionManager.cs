using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Gui;

namespace zArm.Simulation
{
    public class SelectionManager : IInputReceiver
    {
        ISelectable _selection;
        ISelectable _mouseOver;
        MouseEnteredArgs _mouseOverArg;
        InputManager _inputManager;
        IUISupportWindow _uiSuportWindow;
        MouseSelectingArgs _selectionArg;
        Camera _camera;
        Octree _octree;
        Application _app;
        IInputPermissions _inputPermissions;

        public SelectionManager(InputManager inputManager, IUISupportWindow uiSuportWindow, Camera camera, Octree octree, Application app)
        {
            //set fields
            _inputManager = inputManager;
            _uiSuportWindow = uiSuportWindow;
            _camera = camera;
            _octree = octree;
            _app = app;
        }

        public ISelectable Selection
        {
            get { return _selection; }
            set { TrySetSelection(value, null); }
        }

        bool TrySetSelection(ISelectable selection, RayQueryResult? ray)
        {
            //validate
            if (selection == _selection)
            {
                if (selection == null || !selection.SelectSubObject() || (ray != null && _selectionArg != null && _selectionArg.SubObject == ray.Value.SubObject))
                    return false;
            }

            //see if selectable is OK with selection
            var e = new MouseSelectingArgs() { Ray = ray };
            if (ray.HasValue)
                e.SubObject = ray.Value.SubObject;
            selection?.MouseSelecting(e);
            if (!e.Select) //selection not allowed
                return false;

            //fire selection events
            _selection?.MouseUnselected();
            _selectionArg = e;
            var prevSelection = _selection;
            _selection = selection;

            //swap inputReceivers
            var inputReceiver = prevSelection as IInputReceiver;
            if (inputReceiver != null)
                _inputManager.Remove(inputReceiver);
            inputReceiver = _selection as IInputReceiver;
            if (inputReceiver != null)
                _inputManager.Add(inputReceiver);

            //request excusive mouse access while selected
            _inputPermissions.RequestExclusiveMouse = _selection != null;

            return true;
        }

        bool TrySetMouseOver(ISelectable selection, RayQueryResult? ray)
        {
            //validate
            if (selection == _mouseOver)
            {
                if (selection == null || !selection.SelectSubObject() || (ray != null && _mouseOverArg  != null && _mouseOverArg.SubObject == ray.Value.SubObject))
                    return false;
            }

            var e = new MouseSelectingArgs() { Ray = ray };
            if (ray.HasValue)
                e.SubObject = ray.Value.SubObject;

            //fire events
            _mouseOver?.MouseExited();
            _mouseOver = selection;
            _mouseOverArg = e;
            _mouseOver?.MouseEntered(e);

            return true;
        }

        RayQueryResult? GetMouseOver(Camera camera, Octree octree, Application app)
        {
            //cast ray to mouse position
            var graphics = app.Graphics;
            var ui = app.UI;
			//var pos = ui.CursorPosition;
			var pos = _app.Input.GetMousePositionOrTouchPosition();
            Ray cameraRay = camera.GetScreenRay((float)pos.X / graphics.Width, (float)pos.Y / graphics.Height);
            var results = octree.RaycastSingle(cameraRay, RayQueryLevel.Triangle, 250f, DrawableFlags.Geometry, 2 /*anything with a viewmask of 1 will not be detected by ray*/);
            return results;
        }

        void ProcessSelection(IInputPermissions inputPermissions)
        {
            var input = _app.Input;

            //unselect on mouse up
            if (_selection != null && _selectionArg != null && !_selectionArg.LockSelectionUntilClick)
            {
                if (!input.GetMouseDownOrTouching(out var delta))
                {
                    TrySetSelection(null, null); //unselect
                }
            }

            //validate
            if (!inputPermissions.CanUseMouse)
                return;

            var ray = GetMouseOver(_camera, _octree, _app);
            ISelectable selectable = null;
			if (ray != null && ray.Value.Node != null)
			{
				try { selectable = ray.Value.Node.Components.OfType<ISelectable>().FirstOrDefault(); }
				catch { }
			}

            //mouse over
            TrySetMouseOver((Selection == null) ? selectable : null, ray);


            //mouse click
            if (input.GetMouseClickOrTouch())
            {
                //if currently has locked selection then release it
                if (Selection != null && _selectionArg != null && _selectionArg.LockSelectionUntilClick)
                {
                    if (TrySetSelection(null, null)) //unselect
                        return;
                }

                //select it, if its selectable
                if (selectable != null)
                {
                    if (TrySetSelection(selectable, ray))
                        return;
                }

                //bug fix - set window focus
                _uiSuportWindow?.SetFocus();
            }
        }

        void IInputReceiver.ReceivedInputControl(IInputPermissions inputPermissions)
        {
            _inputPermissions = inputPermissions;
        }

        void IInputReceiver.RevokedInputControl(IInputPermissions inputPermissions)
        {
        }

        void IInputReceiver.OnInputUpdate(float timeStep, IInputPermissions inputPermissions)
        {
            ProcessSelection(inputPermissions);
        }
    }


    public class MouseSelectingArgs : MouseEnteredArgs
    {
        public bool Select { get; set; } = true;
        public bool LockSelectionUntilClick { get; set; }
		public bool IsTouch { get; set; }
    }

    public class MouseEnteredArgs
    {
        public RayQueryResult? Ray { get; set; }
        public uint SubObject { get; set; }
    }


    public interface ISelectable
    {
        void MouseEntered(MouseEnteredArgs e);
        void MouseExited();
        void MouseSelecting(MouseSelectingArgs e);
        void MouseUnselected();
        bool SelectSubObject();
    }
}
