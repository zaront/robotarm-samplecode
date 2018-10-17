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
    public class Gripper : Component
    {
        Dictionary<Node, NodeCollisionStartEventArgs> _touchingNodes = new Dictionary<Node, NodeCollisionStartEventArgs>();
        Node _gripping;
        RigidBody _grippingBody;
        bool _grippingKinematic;
        Node _grippingParent;

        public Gripper OtherGripper { get; set; }
        public bool ProducesConstraint { get; set; }
        public CollisionShape CollisionShape { get; set; }

        public event Action<Node> Gripping;
        public event Action<Node> Releasing;

        public bool IsGripping
        {
            get { return _gripping != null;  }
        }

		public Node GrippingNode
		{
			get { return _gripping; }
		}

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            node.NodeCollisionStart += OnCollisionStart;
            node.NodeCollisionEnd += OnCollisionEnd;
        }

        private bool IsTouchingNode(Node node)
        {
            return _touchingNodes.ContainsKey(node);
        }

        private void OnCollisionStart(NodeCollisionStartEventArgs e)
        {
            //validate
            if (IsGripping)
                return;
            if (e.OtherNode == Node.Parent)
                return;
            if (OtherGripper != null && (e.OtherNode == OtherGripper.Node || e.OtherNode == OtherGripper.Node.Parent))
                return;

            //register the node as touching
            if (!_touchingNodes.ContainsKey(e.OtherNode))
                _touchingNodes.Add(e.OtherNode, e);

            //determine if other gripper is touching the item too
            if (OtherGripper != null)
            {
                if (e.OtherBody.Mass != 0) // don't grip immovable objects
                {
                    if (OtherGripper.IsTouchingNode(e.OtherNode))
                    {
                        Grip(e);
                        OtherGripper.Grip(e);
                    }
                }
            }

        }

        private void OnCollisionEnd(NodeCollisionEndEventArgs e)
        {
            //unregister the node as touching
            if (_touchingNodes.ContainsKey(e.OtherNode))
                _touchingNodes.Remove(e.OtherNode);

            //release grip
            if (IsGripping && !ProducesConstraint && (e.OtherNode == _gripping || _gripping.IsDeleted))
            {
                ReleaseGrip();
                OtherGripper?.ReleaseGrip();
            }
        }

        private void Grip(NodeCollisionStartEventArgs e)
        {
            if (!IsGripping)
            {
                _gripping = e.OtherNode;

                //create constraint
                if (ProducesConstraint)
                {
                    //store gripped objects values
                    _grippingBody = e.OtherBody;
                    _grippingKinematic = e.OtherBody.Kinematic;
                    _grippingParent = e.OtherNode.Parent;

                    //connect to the gripper
                    e.OtherNode.Parent = Node;
                    e.OtherBody.Kinematic = true;

                    //call event
                    Gripping?.Invoke(_gripping);
                }
            }
        }

        private void ReleaseGrip()
        {
            if (IsGripping)
            {
                //release the constraint
                if (ProducesConstraint)
                {
					//restore gripped objects values
					if (!_gripping.IsDeleted)
					{
						_grippingBody.Kinematic = _grippingKinematic;
						_gripping.Parent = _grippingParent;
					}
                    _grippingBody = null;
                    _grippingParent = null;

                    //call event
                    Releasing?.Invoke(_gripping);
                }

                _gripping = null;
            }
        }

        public Vector3 GripCenterPosition
        {
            get
            {
                if (OtherGripper == null)
                    return Node.Position;
                return Vector3.Lerp(Node.WorldPosition, OtherGripper.Node.WorldPosition, .5f);
            }
        }

        public Quaternion GripCenterRotation
        {
            get
            {
                return Node.Parent.WorldRotation;
            }
        }

    }
}
