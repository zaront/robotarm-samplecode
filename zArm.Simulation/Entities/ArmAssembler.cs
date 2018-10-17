using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Physics;
using Urho.Resources;
using zArm.Simulation.Components;

namespace zArm.Simulation.Entities
{
    public class ArmAssembler
    {
        Dictionary<string, XmlElement> _xmlNodes;
        Node _parent;
        Node _node;
        Node _mount;
        Node _childMount;
        RigidBody _parentBody;

        public ArmAssembler(string scenePath, Node parent) : this(GetRootNodes(scenePath), parent, null)
        {
        }

        ArmAssembler(Dictionary<string, XmlElement> xmlNodes, Node parent, Node node)
        {
            _xmlNodes = xmlNodes;
            _parent = parent;
            _node = node;
        }

        static Dictionary<string, XmlElement> GetRootNodes(string xmlScene)
        {
            var result = new Dictionary<string, XmlElement>();
            var file = Urho.Application.Current.ResourceCache.GetXmlFile(xmlScene);
            var root = file.GetRoot(null);
            XmlElement node = null;
            if (root.HasChild("node"))
            {
                node = root.GetChild("node");
                XmlElement attribute = null;
                while (node.NotNull())
                {
                    attribute = node.GetChild("attribute");
                    while (attribute.NotNull())
                    {
                        if (attribute.GetAttribute("name") == "Name")
                        {
                            var name = attribute.GetAttribute("value");
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                if (!result.ContainsKey(name))
                                    result.Add(name, node);
                                break;
                            }
                        }
                        attribute = attribute.GetNext("attribute");
                    }
                    node = node.GetNext("node");
                }
            }
            return result;
        }

        void Position(Node parent, Node child, string mountName, out Node parentMount, out Node childMount)
        {
            parentMount = parent.GetChild(mountName, false);
            childMount = child.GetChild(mountName, false);
            if (parentMount == null || childMount == null)
                return;
            child.SetWorldRotation(parentMount.WorldRotation * childMount.Rotation);
            child.SetWorldPosition(parentMount.WorldPosition + (child.WorldPosition - childMount.WorldPosition));
        }

        public ArmAssembler Create(string nodeName)
        {
            var node = _parent.CreateChild(nodeName);
            node.LoadXml(_xmlNodes[nodeName], false);
            node.Position = Vector3.Zero;

            return new ArmAssembler(_xmlNodes, _parent, node);
        }

        public Node Node
        {
            get { return _node; }
        }

        public Node Mount
        {
            get { return _mount; }
        }

        public Node ChildMount
        {
            get { return _childMount; }
        }

        public ArmAssembler Attach(string nodeName, string mountName = null)
        {
            return Attach(nodeName, Vector3.Zero, mountName);
        }

        public ArmAssembler Attach(string nodeName, Vector3 rotationAxis, string mountName = null)
        {
            return Attach(nodeName, rotationAxis, Quaternion.Identity, mountName);
        }

        public ArmAssembler Attach(string nodeName, Vector3 rotationAxis, Quaternion rotationOffset, string mountName = null)
        {
            if (mountName == null)
            {
                if (_node.Name == "servo")
                    mountName = "servo-horn";
                else
                    mountName = nodeName + "-mount";
            }
            var child = Create(nodeName);
            Position(_node, child.Node, mountName, out _childMount, out child._mount);

            //rotation offset
            if (rotationOffset != Quaternion.Identity)
                child.Node.RotateAround(child.Mount.WorldPosition, rotationOffset, TransformSpace.World);

            var childBody = child.Node.GetComponent<RigidBody>(false);
            var parentBody = _node.GetComponent<RigidBody>(false);
            child._parentBody = _parentBody;
            if (parentBody != null)
                child._parentBody = parentBody;
            if (child._parentBody != childBody && childBody != null && child._parentBody != null && !childBody.Kinematic)
            {
                //hinge
                var hinge = child.Node.CreateComponent<Constraint>();
                hinge.ConstraintType = ConstraintType.Hinge;
                hinge.DisableCollision = true;
                hinge.OtherBody = _parentBody;
                hinge.SetWorldPosition(child.Mount.WorldPosition);
                var axis = (rotationAxis == Vector3.Zero) ? child.Mount.WorldDirection : rotationAxis;
                hinge.SetAxis(axis);
                hinge.SetOtherAxis(axis);
                hinge.HighLimit = Vector2.Zero;
                hinge.LowLimit = Vector2.Zero;
            }
            else
            {
                //nest parent child
                child.Node.Parent = _node;

                //combine nodes
                if (childBody == null)
                    foreach (var c in child.Node.Components.OfType<CollisionShape>())
                    {
                        var cCopy = _node.CreateComponent<CollisionShape>();
                        cCopy.ShapeType = c.ShapeType;
                        cCopy.LodLevel = c.LodLevel;
                        cCopy.Margin = c.Margin;
                        cCopy.Model = c.Model;
                        cCopy.Size = c.Size;
                        cCopy.Rotation = c.Node.Rotation;
                        cCopy.Position = c.Node.Position;
                        c.Remove();
                    }
            }

            return child;
        }

        public Tuple<Servo,ServoGimbal> AttachServo(int servoID, bool invertServo = false)
        {
            var servo = Node.CreateComponent<Servo>();
            servo.Invert = invertServo;
            servo.ServoID = servoID;
            var gimbal = Node.CreateComponent<ServoGimbal>();
            return new Tuple<Servo, ServoGimbal>(servo, gimbal);
        }

        public ServoGimbal AttachServoGimbal()
        {
            return Node.CreateComponent<ServoGimbal>();
        }

        public Tuple<Knob,Button,KnobGimbal> AttachKnob()
        {
            var knob = Node.CreateComponent<Knob>();
            var button = Node.CreateComponent<Button>();
            var gimbal = Node.CreateComponent<KnobGimbal>();
            return new Tuple<Knob, Button, KnobGimbal>(knob, button, gimbal);
        }

        public Gripper AttachPrimaryGripper(Node secondaryGripper)
        {
            var g1 = Node.CreateComponent<Gripper>();
            var g2 = secondaryGripper.CreateComponent<Gripper>();
            g1.OtherGripper = g2;
            g2.OtherGripper = g1;
            g1.ProducesConstraint = true;
            return g1;
        }

        public Led AttachLed()
        {
            var led = Node.CreateComponent<Led>();
            led.LightPosition = ChildMount?.Position??new Vector3() + new Vector3(0, 2, 0);
            return led;
        }

        public void Immovable()
        {
            var body = _node.GetComponent<RigidBody>(false);
            if (body != null)
                body.Mass = 0;
        }
    }
}
