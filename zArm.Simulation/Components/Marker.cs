using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using Urho.Physics;

namespace zArm.Simulation.Components
{
    public class Marker : Component
    {
        public Color Color { get; set; } = Color.Black;
        public Node Tip { get; private set; }

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            //setup collision
            var model = node.GetComponent<StaticModel>();
            if (model == null)
                throw new Exception("node must contain a model before adding a paper component");
            var c = node.CreateComponent<CollisionShape>();
            c.SetConvexHull(model.Model, 1, Vector3.One, Vector3.Zero, Quaternion.Identity);
            var r = node.CreateComponent<RigidBody>();
            r.Mass = 1;
            r.Kinematic = true;
            r.Trigger = true;
            r.CollisionLayer = 2;

            //add tip
            Tip = Node.CreateChild("marker-tip");
            Tip.Position = new Vector3(0, .5f, 0);
        }

    }
}
