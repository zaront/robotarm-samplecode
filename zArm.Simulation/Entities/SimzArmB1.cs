using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using zArm.Simulation.Actions;
using zArm.Simulation.Components;

namespace zArm.Simulation.Entities
{
    public class SimzArmB1
    {
        static FiniteTimeAction _hilightMaterial1 = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(true, Color.Red, 0));
        static FiniteTimeAction _unhilightMaterial1 = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(false, Color.Red, 0));
        static FiniteTimeAction _hilightMaterial2 = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(true, Color.Red, 1));
        static FiniteTimeAction _unhilightMaterial2 = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(false, Color.Red, 1));
        static FiniteTimeAction _hilightMaterial3 = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(true, Color.Red, 2));
        static FiniteTimeAction _unhilightMaterial3 = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(false, Color.Red, 2));
        static FiniteTimeAction _flashKnob = new Sequence(new FiniteTimeAction[] { _hilightMaterial1, _hilightMaterial3, new DelayTime(.7f), _unhilightMaterial1, _unhilightMaterial3 });

        bool _isSelectable = true;

        public Node Arm { get; }
        public Node Base { get; }
        public ArmPart Shoulder { get; }
        public ArmPart UpperArm { get; }
        public ArmPart ForeArm { get; }
        public ArmPart Hand { get; }
        public ArmPart Finger { get; }
        public Gripper Gripper { get; }
        public Led Led { get; }
        public Knob Knob { get; }
        public Button Button { get; }
        public KnobGimbal KnobGimbal { get; }
        public ArmPart[] ArmSegments { get; }

        public SimzArmB1(Scene scene)
        {
            //construct the arm
            Arm = scene.CreateChild("arm");

            //base
            var baseBuilder = new ArmAssembler("Scenes/zArmB1.xml", Arm).Create("base");
            baseBuilder.Immovable();
            Base = baseBuilder.Node;

            //knob & Led
            var knobLight = baseBuilder.Attach("knob-light");
            var knob = knobLight.Attach("knob", new Vector3(0, 1, 0));
            Led = knobLight.AttachLed();
            var knobParts = knob.AttachKnob();
            Knob = knobParts.Item1;
            Button = knobParts.Item2;
            KnobGimbal = knobParts.Item3;

            //shoulder
            var shoulder = baseBuilder.Attach("servo").Attach("shoulder", new Vector3(1, 0, 0), new Quaternion(0, 50, 0));
            var servoPart = shoulder.AttachServo(0);
            var servo = shoulder.Attach("servo");
            var extraGimbal = servo.AttachServoGimbal();
            Shoulder = new ArmPart(shoulder, servoPart, extraGimbal);

            //upper arm
            var upperArm = servo.Attach("upperarm", new Vector3(0, 0, 1), new Quaternion(-90, 0, 0));
            servoPart = upperArm.AttachServo(1);
            servo = upperArm.Attach("servo");
            extraGimbal = servo.AttachServoGimbal();
            UpperArm = new ArmPart(upperArm, servoPart, extraGimbal);

            //forearm
            var foreArm = servo.Attach("forearm", new Vector3(0, 0, 1));
            servoPart = foreArm.AttachServo(2);
            servo = foreArm.Attach("servo");
            extraGimbal = servo.AttachServoGimbal();
            ForeArm = new ArmPart(foreArm, servoPart, extraGimbal);

            //hand
            var hand = servo.Attach("hand", new Vector3(0, 0, 1));
            servoPart = hand.AttachServo(3);
            servo = hand.Attach("servo");
            extraGimbal = servo.AttachServoGimbal();
            Hand = new ArmPart(hand, servoPart, extraGimbal);

            //finger
            var finger = servo.Attach("finger", new Vector3(0, 1, 0));
            servoPart = finger.AttachServo(4, true);
            Finger = new ArmPart(finger, servoPart, null);
            Finger.Servo.SetRenderLimits(-6f, null); //limit finger to not allow folding in on itself

            //grippers
            var handGrip = hand.Attach("grip");
            var fingerGrip = finger.Attach("grip");
            Gripper = hand.AttachPrimaryGripper(fingerGrip.Node);

            //all servos
            ArmSegments = new ArmPart[]
            {
                Shoulder,
                UpperArm,
                ForeArm,
                Hand,
                Finger
            };
        }

        public bool IsSelectable
        {
            get { return _isSelectable; }
            set
            {
                _isSelectable = value;
                KnobGimbal.IsSelectable = _isSelectable;
                foreach (var segment in ArmSegments)
                {
                    segment.Gimbal.IsSelectable = _isSelectable;
                    if (segment.ExtraGimbal != null)
                        segment.ExtraGimbal.IsSelectable = _isSelectable;
                }
            }
        }

        public void FlashKnob()
        {
            KnobGimbal.Node.RunActions(_flashKnob);
        }

        public void HilightButton(bool on)
        {
            KnobGimbal.Node.RunActions(on ? _hilightMaterial2 : _unhilightMaterial2);
        }
            
    }


    public class ArmPart
    {
        public Node Node { get; }
        public Servo Servo { get; }
		public ServoGimbal Gimbal { get; }
        public ServoGimbal ExtraGimbal { get; }
        public Node Mount { get; }
        public Node ChildMount { get; }

        public ArmPart (ArmAssembler part, Tuple<Servo,ServoGimbal> servo, ServoGimbal extraGimbal)
        {
            Node = part.Node;
            Mount = part.Mount;
            ChildMount = part.ChildMount;
            Servo = servo.Item1;
            Gimbal = servo.Item2;
            ExtraGimbal = extraGimbal;
        }
    }
}
