using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using Urho.Shapes;
using zArm.Simulation.Actions;

namespace zArm.Simulation.Components
{
	public interface ITrails
	{
		void SetTrails(params Trail[] trails);
		void UpdateTrail(int trailIndex, Trail update);
	}

	public class Trails : Component, ITrails
	{
		ConcurrentDictionary<int, int> _safetyIndex = new ConcurrentDictionary<int, int>();

		public void SetTrails(params Trail[] trails)
		{
			//remove exisiting trails
			Node.RemoveAllChildren();
			_safetyIndex.Clear();

			//create each trail
			if (trails == null || trails.Length == 0)
				return;
			var totalDelay = 0f;

			var decorationRoot = Node.CreateChild();
			var safetyIndex = 0;
			var safetyPositions = new Dictionary<Vector3, int>();
			foreach (var trail in trails.Where(i => i.Path != null && i.Path.Length != 0))
			{
				//create ribbon
				var ribbonNode = Node.CreateChild();
				var ribbon = ribbonNode.CreateComponent<RibbonTrail>();
				ribbon.ViewMask = 1; //invisible to Selection Manager
				ribbon.CastShadows = false;
				ribbon.Material = Material.FromColor(new Color(trail.R / 255f, trail.G / 255f, trail.B / 255f, trail.A / 255f));
				ribbon.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
				ribbon.Lifetime = 10000f;
				ribbon.Width = (trail.Width == 0) ? .1f : trail.Width;
				ribbonNode.SetWorldPosition(new Vector3(trail.Path[0].X, trail.Path[0].Y, trail.Path[0].Z));

				//create decoration
				var decorationNode = decorationRoot.CreateChild();
				if (trail.Decoration != null)
				{
					var decoration = trail.Decoration.Value;
					var start = new Vector3(decoration.Start.X, decoration.Start.Y, decoration.Start.Z);
					var end = new Vector3(decoration.End.X, decoration.End.Y, decoration.End.Z);

					//Safety
					if (decoration.Type == TrailDecorationType.Safety)
					{
						if (!safetyPositions.ContainsKey(start))
						{
							var width = .7f;
							var top = CreateShape<Cone>(decorationNode, decoration, new Vector3(0, width / 2f, 0), new Vector3(width, width, width));
							var bottom = CreateShape<Cone>(decorationNode, decoration, new Vector3(0, -width / 2f, 0), new Vector3(width, width, width));
							bottom.Roll(180);
							decorationNode.SetWorldPosition(start);
							safetyPositions.Add(start, safetyIndex);
						}
						else
							_safetyIndex.AddOrUpdate(safetyIndex, safetyPositions[start], (i, e) => safetyPositions[start]); //if one is at the same location, then reuse it
					}

					//pick
					if (decoration.Type == TrailDecorationType.Pick)
					{
						CreatePointer(decorationNode, .4f, false, decoration, start, end);
					}

					//place
					if (decoration.Type == TrailDecorationType.Place)
					{
						CreatePointer(decorationNode, .4f, true, decoration, start, end);
					}

				}
				safetyIndex++;

				//create action
				var speed = (trail.Speed == 0) ? 50f : trail.Speed;
				var actions = trail.Path.Skip(1).Select((i, index) =>
				{
					var pos = new Vector3(i.X, i.Y, i.Z);
					var prevPos = new Vector3(trail.Path[index].X, trail.Path[index].Y, trail.Path[index].Z);
					var dist = Vector3.Distance(pos, prevPos);
					var time = dist / speed;
					totalDelay += time;
					return new MoveTo(time, pos);
				});

				//animate ribbon
				FiniteTimeAction[] actionsArray;
				if (totalDelay != 0)
				{
					var time = totalDelay;
					var a = actions.ToList<FiniteTimeAction>();
					a.Insert(0, new DelayTime(time));
					actionsArray = a.ToArray();
				}
				else
					actionsArray = actions.ToArray();
				ribbonNode.RunActions(new Sequence(actionsArray));

				//animate decoration
				if (trail.Decoration != null)
				{
					float fadeTime = .2f;
					var nodes = decorationNode.GetChildrenWithComponent<Shape>(true);
					foreach(var node in nodes)
					{
						var target = GetDecorationAlpha(null, node);
						ChangeDecorationAlpha(null, node, 0, 0, 0);
						var action = new ChangeTo<float>(fadeTime, target, GetDecorationAlpha, ChangeDecorationAlpha);
						node.RunActions(new Sequence(new FiniteTimeAction[] { new DelayTime(totalDelay), action }));
					}
					if (nodes.Length != 0)
						totalDelay += fadeTime;
				}
			}
		}

		float GetDecorationAlpha(BaseAction action, Node node)
		{
			return node.GetComponent<Shape>().Color.A;
		}

		void ChangeDecorationAlpha(BaseAction action, Node node, float start, float end, float percentage)
		{
			var shape = node.GetComponent<Shape>();
			var newColor = new Color(shape.Color, MathHelper.Lerp(start, end, percentage));
			shape.Color = newColor;
			shape.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
		}


		Node CreateShape<T>(Node parent, TrailDecoration decoration, Vector3 position, Vector3 scale)
			where T : Shape
		{
			var node = parent.CreateChild();
			var shape = node.CreateComponent<T>();
			node.Scale = scale;
			node.Position = position;
			shape.Color = new Color(decoration.R / 255f, decoration.G / 255f, decoration.B / 255f, decoration.A / 255f);
			shape.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
			shape.ViewMask = 1; //invisible to Selection Manager
			return node;
		}

		Node CreatePointer(Node parent, float width, bool arrowDown, TrailDecoration decoration, Vector3 start, Vector3 end)
		{
			var length = Vector3.Distance(start, end);
			var minLength = width * 6;
			var arrowWidth = width * 2;
			var arrowHeight = width * 4;
			var lineHeight = length - arrowHeight;
			if (length < minLength)
				lineHeight = minLength - arrowHeight;
			var plateheight = .05f;
			var plateWidth = 2.2f;
			var pointer = parent.CreateChild();
			var line = CreateShape<Cylinder>(pointer, decoration, new Vector3(0, arrowDown ? arrowHeight + (lineHeight / 2f) : lineHeight / 2f, 0), new Vector3(width, lineHeight, width));
			var arrow = CreateShape<Cone>(pointer, decoration, new Vector3(0, arrowDown ? arrowHeight / 2f : lineHeight + (arrowHeight / 2f), 0), new Vector3(arrowWidth, arrowHeight, arrowWidth));
			if (arrowDown)
				arrow.Roll(180);
			var plate = CreateShape<Cylinder>(parent, decoration, new Vector3(0, -plateheight / 2f, 0), new Vector3(plateWidth, plateheight, plateWidth));
			parent.SetWorldPosition(end);
			pointer.LookAt(start, Vector3.Up, TransformSpace.World);
			pointer.Pitch(90);
			return pointer;
		}

		public void UpdateTrail(int trailIndex, Trail update)
		{
			//find ribbon
			if (trailIndex + 1 >= Node.Children.Count)
				return;
			var trailNode = Node.Children[trailIndex + 1];
			var ribbon = trailNode.GetComponent<RibbonTrail>();
			if (ribbon == null)
				return;

			//change properties
			ribbon.Width = (update.Width == 0) ? .1f : update.Width;
			ribbon.Material = Material.FromColor(new Color(update.R / 255f, update.G / 255f, update.B / 255f, update.A / 255f));
			ribbon.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating

			//update decoration
			if (update.Decoration != null)
			{
				var decoration = update.Decoration.Value;
				var decorationRoot = Node.Children[0];
				var decorationNode = decorationRoot.Children[trailIndex];
				if (_safetyIndex.TryGetValue(trailIndex, out var replacementIndex))
					decorationNode = decorationRoot.Children[replacementIndex];
				foreach (var shapeNode in decorationNode.GetChildrenWithComponent<Shape>(true))
				{
					var shape = shapeNode.GetComponent<Shape>();
					shape.Color = new Color(decoration.R / 255f, decoration.G / 255f, decoration.B / 255f, decoration.A / 255f);
					shape.Material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitAlpha); //self illuminating
				}
			}
		}

	}




	public struct Trail
	{
		public float Width;
		public float Speed;
		public byte R;
		public byte G;
		public byte B;
		public byte A;
		public TrailPos[] Path;
		public TrailDecoration? Decoration;
	}

	public struct TrailPos
	{
		public float X;
		public float Y;
		public float Z;
	}

	public struct TrailDecoration
	{
		public TrailDecorationType Type;
		public float Width;
		public byte R;
		public byte G;
		public byte B;
		public byte A;
		public TrailPos Start;
		public TrailPos End;
	}

	public enum TrailDecorationType
	{
		Safety,
		Pick,
		Place
	}
}
