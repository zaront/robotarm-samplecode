using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using Urho.Physics;
using Urho.Resources;
using Urho.Shapes;
using zArm.Simulation;

namespace zArm.Simulation.Components
{
	public interface ITransformGimbal
	{
		event EventHandler<TranformGizmoEventArgs> ChangedPosition;
		event EventHandler<GrabbedEventArgs> Grabbed;
		bool IsVisible { get; set; }
		bool ShowPlanes { get; set; }
		void SetPosition(float x, float y, float z);
	}

	public class TransformGimbal : Component, ITransformGimbal
	{
		Vector3 _offset;
		IntVector2 _prevMousePos;
		bool _usingTouch;
		Node _transform;
		Urho.Plane _plane;

		public float MoveSpeed { get; set; } = 4.0f;
		public bool IsSelectable { get; set; } = true;
		public bool ShowPlanes { get; set; } = true;

		public event EventHandler<TranformGizmoEventArgs> ChangedPosition;
		public event EventHandler<GrabbedEventArgs> Grabbed;

		public override void OnAttachedToNode(Node node)
		{
			base.OnAttachedToNode(node);

			InsureTransformMesh();
		}

		private void InsureTransformMesh()
		{
			_transform = Node.GetChild("transform gimbal");
			if (_transform == null)
				_transform = CreateTransform(Node);
		}

		Node CreateTransform(Node parent)
		{
			var transform = parent.CreateChild("transform");
			var x = CreateArrow(transform, new Color(Color.Red, .5f), "x");
			x.Roll(90);
		
			var y = CreateArrow(transform, new Color(Color.Green, .5f), "y");
			
			var z = CreateArrow(transform, new Color(Color.Blue, .5f), "z");
			z.Roll(90);
			z.Pitch(90);

			if (ShowPlanes)
			{
				var xz = CreatePlane(transform, new Color(Color.Red, .12f), "xz");
				xz.Pitch(90);
				xz.Roll(90);
				xz.Yaw(180);

				var yx = CreatePlane(transform, new Color(Color.Green, .12f), "yx");

				var zy = CreatePlane(transform, new Color(Color.Blue, .12f), "zy");
				zy.Pitch(90);
				zy.Yaw(-90);
			}

			return transform;
		}

		Node CreateArrow(Node parent, Color color, string key, float length = 4f, float thickness = .25f, float arrowSize = 1f)
		{
			var node = parent.CreateChild(key);

			//line
			var cylinderNode = node.CreateChild();
			var cylinder = cylinderNode.CreateComponent<Cylinder>();
			cylinder.Color = color;
			cylinder.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
			cylinderNode.Scale = new Vector3(thickness, length, thickness);
			cylinderNode.Position = new Vector3(0, length / 2f, 0);
			cylinderNode.AddComponent(new TransformArrow(this, key));

			//arrow
			var coneNode = node.CreateChild();
			var cone = coneNode.CreateComponent<Cone>();
			cone.Color = color;
			cone.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
			coneNode.Scale = new Vector3(arrowSize / 2f, arrowSize, arrowSize / 2f);
			coneNode.Position = new Vector3(0, length + (arrowSize / 2f), 0);
			coneNode.AddComponent(new TransformArrow(this, key));

			return node;
		}

		Node CreatePlane(Node parent, Color color, string key, float length = 4f, float thickness = .1f, float size = 1.5f)
		{
			var node = parent.CreateChild(key);

			//box
			var boxNode = node.CreateChild();
			var box = boxNode.CreateComponent<Box>();
			box.Color = color;
			box.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
			boxNode.Scale = new Vector3(size, size, thickness);
			boxNode.Position = new Vector3(-(length / 2f - (size / 2f)), length - (size / 2f), 0);
			boxNode.AddComponent(new TransformArrow(this, key));

			return node;
		}

		public bool IsVisible
		{
			get { return Node.Enabled; }
			set { Node.Enabled = value; }
		}

		public void SetPosition(float x, float y, float z)
		{
			Node.Position = new Vector3(x, y, z);
		}

		void OnInputUpdate(float timeStep, IInputPermissions inputPermissions, string key)
		{
			if (EnabledEffective)
				MoveNode(timeStep, inputPermissions, key);
		}

		void MouseSelecting(MouseSelectingArgs e, string key)
		{
			//get selection plane for moving within
			if (e.Ray != null)
			{
				//select corect plane
				var cameraDir = (Application as App).CameraNode.Direction;
				switch (key)
				{
					case "x":
						if (Math.Abs(cameraDir.Y) > Math.Abs(cameraDir.X))
							_plane = new Urho.Plane(Vector3.Forward, Node.Position);
						else
							_plane = new Urho.Plane(Vector3.Up, Node.Position);
						break;
					case "y":
						if (Math.Abs(cameraDir.X) > Math.Abs(cameraDir.Z))
							_plane = new Urho.Plane(Vector3.Right, Node.Position);
						else
							_plane = new Urho.Plane(Vector3.Forward, Node.Position);
						break;
					case "z":
						if (Math.Abs(cameraDir.X) > Math.Abs(cameraDir.Y))
							_plane = new Urho.Plane(Vector3.Right, Node.Position);
						else
							_plane = new Urho.Plane(Vector3.Up, Node.Position);
						break;
					case "xz":
						_plane = new Urho.Plane(Vector3.Up, Node.Position);
						break;
					case "yx":
						_plane = new Urho.Plane(Vector3.Forward, Node.Position);
						break;
					case "zy":
						_plane = new Urho.Plane(Vector3.Right, Node.Position);
						break;
				}
				_offset = Node.Position - e.Ray.Value.Position;
			}

			//using touch
			_usingTouch = Application.Input.IsTouching();

			//fire event
			Grabbed?.Invoke(this, new GrabbedEventArgs() { IsGrabbed = true });
		}

		void MouseUnselected(string key)
		{
			//fire event
			Grabbed?.Invoke(this, new GrabbedEventArgs() { IsGrabbed = false });
		}

		private void MoveNode(float timeStep, IInputPermissions inputPermissions, string key)
		{
			var prevPos = Node.Position;

			//move by keyboard
			var input = Application.Input;
			if (inputPermissions.CanUseKeyboard)
			{
				if (input.GetKeyDown(Key.W)) Node.Translate(Vector3.UnitZ * MoveSpeed * timeStep, TransformSpace.Local);
				if (input.GetKeyDown(Key.S)) Node.Translate(-Vector3.UnitZ * MoveSpeed * timeStep, TransformSpace.Local);
				if (input.GetKeyDown(Key.A)) Node.Translate(-Vector3.UnitX * MoveSpeed * timeStep, TransformSpace.Local);
				if (input.GetKeyDown(Key.D)) Node.Translate(Vector3.UnitX * MoveSpeed * timeStep, TransformSpace.Local);
				if (input.GetKeyDown(Key.C)) Node.Translate(-Vector3.UnitY * MoveSpeed * timeStep, TransformSpace.Local);
				if (input.GetKeyDown(Key.E)) Node.Translate(Vector3.UnitY * MoveSpeed * timeStep, TransformSpace.Local);
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
					pos = pos + _offset;
					//constrain to selected axes
					switch (key)
					{
						case "x": pos = new Vector3(pos.X, Node.Position.Y, Node.Position.Z); break;
						case "y": pos = new Vector3(Node.Position.X, pos.Y, Node.Position.Z); break;
						case "z": pos = new Vector3(Node.Position.X, Node.Position.Y, pos.Z); break;
					}
					Node.Position = pos;
				}
			}

			//fire event
			if (prevPos != Node.Position)
				ChangedPosition?.Invoke(this, new TranformGizmoEventArgs() { X = Node.Position.X, Y = Node.Position.Y, Z = Node.Position.Z });
		}




		class TransformArrow : Component, IInputReceiver, ISelectable
		{
			TransformGimbal _transformGimbal;
			string _key;
			Color? _hilight;
			Color? _unHilight;

			internal TransformArrow(TransformGimbal transformGimbal, string key)
			{
				//set fields
				_transformGimbal = transformGimbal;
				_key = key;
			}

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
				_transformGimbal.OnInputUpdate(timeStep, inputPermissions, _key);
			}

			bool ISelectable.SelectSubObject()
			{
				return false;
			}

			void ISelectable.MouseEntered(MouseEnteredArgs e)
			{
				SetHilight(true);
			}

			void ISelectable.MouseExited()
			{
				SetHilight(false);
			}

			void ISelectable.MouseSelecting(MouseSelectingArgs e)
			{
				SetHilight(true);

				_transformGimbal.MouseSelecting(e, _key);
			}

			void ISelectable.MouseUnselected()
			{
				SetHilight(false);

				_transformGimbal.MouseUnselected(_key);
			}

			void SetHilight(bool hilight)
			{
				//get shapes
				var shapes = Node.Parent.GetChildrenWithComponent<Shape>().Select(i => i.GetComponent<Shape>());

				foreach (var shape in shapes)
				{
					//get hilight
					if (_hilight == null)
					{
						_unHilight = shape.Color;
						_hilight = new Color(shape.Color, 1);

					}

					if (hilight)
						shape.Color = _hilight.Value;
					else
						shape.Color = _unHilight.Value;
					shape.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
				}
			}
		}

	}




	public class TranformGizmoEventArgs : EventArgs
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
	}

}
