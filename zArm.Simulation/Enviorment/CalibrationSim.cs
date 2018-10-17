using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho.Shapes;
using Urho;
using System.IO;
using zArm.Simulation.Entities;
using zArm.Simulation.Components;
using zArm.Simulation.Actions;
using Urho.Actions;

namespace zArm.Simulation.Enviorment
{
    public class CalibrationSim : ArmSim
    {
        //animations
        static FiniteTimeAction _flashMaterial1 = new RepeatForever(new Flash(1f, 1, (a, n, h) => { n.GetOrCreateComponent<Hilight>().SetHilight(h, Color.Red, 0); }));
        static FiniteTimeAction _flashMaterial2 = new RepeatForever(new Flash(1f, 1, (a, n, h) => { n.GetOrCreateComponent<Hilight>().SetHilight(h, Color.Red, 1); }));
        static FiniteTimeAction _flashMaterial3 = new RepeatForever(new Flash(1f, 1, (a, n, h) => { n.GetOrCreateComponent<Hilight>().SetHilight(h, Color.Red, 2); }));
        static FiniteTimeAction _stopFlash = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(false, Color.Red));
        static FiniteTimeAction _verticalPosition = new ServoTo(.7f, 0);
        static FiniteTimeAction _flash = new Flash(1f, 2, (a, n, h) => n.GetOrCreateComponent<Hilight>().SetHilight(h, Color.Red));
        static FiniteTimeAction _flashForever = new RepeatForever(_flash);
		static FiniteTimeAction _flashBlue = new Flash(1f, 2, (a, n, h) => n.GetOrCreateComponent<Hilight>().SetHilight(h, Color.Blue));
		static FiniteTimeAction _flashForeverBlue = new RepeatForever(_flashBlue);
		static FiniteTimeAction _hilight = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(true, Color.Red));
        static FiniteTimeAction _openPosition = new ServoTo(2, 35);
        static FiniteTimeAction _unhilight = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(false, Color.Red));
        static FiniteTimeAction _halfOpen = new Sequence(new FiniteTimeAction[] { _verticalPosition, _flash, _hilight, _openPosition, _unhilight });
        static FiniteTimeAction _halfForward = new ServoBy(.5f, 7);
        static FiniteTimeAction _forward = new ServoBy(1f, 15);
        static FiniteTimeAction _backward = new ServoBy(1f, -15);
        static FiniteTimeAction _swing = new Sequence(new FiniteTimeAction[] { _flash, _hilight, new EaseInOut(_halfForward,2), new RepeatForever(new EaseInOut(_backward,2), new EaseInOut(_forward,2)) });
        static FiniteTimeAction _stopSwing = new Sequence(new FiniteTimeAction[] { _unhilight, _verticalPosition });
        static FiniteTimeAction _forwardFull = new ServoTo(2f, 80);
        static FiniteTimeAction _backwardFull = new ServoTo(2f, -80);
        static FiniteTimeAction _forwardFullGripper = new ServoTo(2f, 70);
        static FiniteTimeAction _backwardFullGripper = new ServoTo(2f, -6);
        static FiniteTimeAction _backwardFullGripperLight = new ServoTo(2f, 1);
        static FiniteTimeAction _range = new Sequence(new FiniteTimeAction[] { _flash, _hilight, new RepeatForever(new EaseIn(_forwardFull, 2), new DelayTime(1), new EaseInOut(_backward, 2), new EaseIn(_forward, 2), new DelayTime(1), new EaseIn(_backwardFull, 2), new DelayTime(1), new EaseInOut(_forward, 2), new EaseIn(_backward, 2), new DelayTime(1))});
        static FiniteTimeAction _rangeGripper = new Sequence(new FiniteTimeAction[] { _flash, _hilight, new RepeatForever(new EaseIn(_forwardFullGripper, 2), new DelayTime(1), new EaseInOut(_backward, 2), new EaseIn(_forward, 2), new DelayTime(1), new EaseIn(_backwardFullGripper, 2), new DelayTime(1), new EaseInOut(_forward, 2), new EaseIn(_backward, 2), new DelayTime(1)) });
        static FiniteTimeAction _rangeGripperLight = new Sequence(new FiniteTimeAction[] { _flash, _hilight, new RepeatForever(new EaseIn(_forwardFullGripper, 2), new DelayTime(1), new EaseInOut(_backward, 2), new EaseIn(_forward, 2), new DelayTime(1), new EaseIn(_backwardFullGripperLight, 2), new DelayTime(1), new EaseInOut(_forward, 2), new EaseIn(_backward, 2), new DelayTime(1)) });
        static FiniteTimeAction _stopRange = new Sequence(new FiniteTimeAction[] { _unhilight, _verticalPosition });

        protected internal override void Started()
        {
            base.Started();

            //disable ability to select
            SimArm.IsSelectable = false;
        }

        public void FocusOnArm()
        {
            //validate
            if (!IsRunning)
                return;

            App.CameraOrbit.AutoCenter = true;
            App.CameraOrbit.StartTransition();
            App.CameraOrbit.SetTarget(SimArm.Arm, 43, 13, 40);
        }

        public void FocusOnArmSide()
        {
            //validate
            if (!IsRunning)
                return;

            App.CameraOrbit.AutoCenter = true;
            App.CameraOrbit.StartTransition();
            App.CameraOrbit.SetTarget(SimArm.Arm, 43, 13, 90);
        }

        public void FocusOnArmTop()
        {
            //validate
            if (!IsRunning)
                return;

            App.CameraOrbit.AutoCenter = true;
            App.CameraOrbit.StartTransition();
            App.CameraOrbit.SetTarget(SimArm.Arm, 43, 90, 0);
        }

        public void FocusOnHand()
        {
            //validate
            if (!IsRunning)
                return;

            App.CameraOrbit.AutoCenter = true;
            App.CameraOrbit.StartTransition();
            App.CameraOrbit.SetTarget(SimArm.Hand.Node, 20, 10, 0);
        }

        public void FocusOnKnob()
        {
            //validate
            if (!IsRunning)
                return;

            App.CameraOrbit.AutoCenter = true;
            App.CameraOrbit.StartTransition();
            App.CameraOrbit.SetTarget(SimArm.Knob.Node, 10, 10, 30);
        }

        public void FreezeCameraCentering(bool enable)
        {
            //validate
            if (!IsRunning)
                return;

            App.CameraOrbit.AutoCenter = !enable;
        }

        public void FlashKnob(bool enable)
        {
            Flash(SimArm?.KnobGimbal.Node, enable, _flashMaterial1, _stopFlash);
        }

        public void FlashButton(bool enable)
        {
            Flash(SimArm?.KnobGimbal.Node, enable, _flashMaterial2, _stopFlash);
        }

        public void FlashPower(bool enable)
        {
            Flash(SimArm?.Base, enable, _flashMaterial2, _stopFlash);
            Flash(SimArm?.Base, enable, _flashMaterial3, _stopFlash);
        }

        public void FlashBase(bool enable)
        {
            FreezeCameraCentering(enable);
            Flash(SimArm?.Base, enable, _flashForeverBlue, _stopFlash);
        }

        public void FlashUpperArm(bool enable)
        {
            FreezeCameraCentering(enable);
            Flash(SimArm?.UpperArm.Node, enable, _flashForeverBlue, _stopFlash);
        }

        public void FlashForeArm(bool enable)
        {
            FreezeCameraCentering(enable);
            Flash(SimArm?.ForeArm.Node, enable, _flashForeverBlue, _stopFlash);
        }

        void Flash(Node node, bool enable, FiniteTimeAction flash, FiniteTimeAction stopFlash)
        {
            //validate
            if (!IsRunning)
                return;

            if (enable)
                node.RunActions(flash);
            else
            {
                node.RemoveAction(flash);
                node.RunActions(stopFlash);
            }
        }

        public void VerticalPostion(bool gripperHalfOpen = false)
        {
            //validate
            if (!IsRunning)
                return;

            SimArm.Shoulder.Node.RunActions(_verticalPosition);
            SimArm.UpperArm.Node.RunActions(_verticalPosition);
            SimArm.ForeArm.Node.RunActions(_verticalPosition);
            SimArm.Hand.Node.RunActions(_verticalPosition);
            if (!gripperHalfOpen)
                SimArm.Finger.Node.RunActions(_verticalPosition);
            else
                SimArm.Finger.Node.RunActions(_halfOpen);
        }

        public void SwingServo(ArmPart part)
        {
            //validate
            if (!IsRunning)
                return;

            part.Node.RemoveAllActions();
            part.Node.RunActions(_swing);

            //disabled recentering
            App.CameraOrbit.AutoCenter = false;
        }

        public void StopSwingServo(ArmPart part)
        {
            //validate
            if (!IsRunning)
                return;

            part.Node.RemoveAllActions();
            part.Node.RunActions(_stopSwing);

            //enable recentering
            App.CameraOrbit.AutoCenter = true;
        }

        public void RangeServo(ArmPart part, bool isGripper = false, bool lightTouch = false)
        {
            //validate
            if (!IsRunning)
                return;

            if (isGripper && lightTouch)
                part.Node.RunActions(_rangeGripperLight);
            else if (isGripper)
                part.Node.RunActions(_rangeGripper);
            else
                part.Node.RunActions(_range);

            //disabled recentering
            App.CameraOrbit.AutoCenter = false;
        }

        public void StopRangeServo(ArmPart part, bool isGripper = false, bool lightTouch = false)
        {
            //validate
            if (!IsRunning)
                return;

            if (isGripper && lightTouch)
                part.Node.RemoveAction(_rangeGripperLight);
            else if (isGripper)
                part.Node.RemoveAction(_rangeGripper);
            else
                part.Node.RemoveAction(_range);
            part.Node.RunActions(_stopRange);

            //enable recentering
            App.CameraOrbit.AutoCenter = true;
        }

        public void SetPose(float?[] positions, int servoIndexHilight)
        {
            //validate
            if (!IsRunning)
                return;

            var index = 0;
            foreach (var part in SimArm.ArmSegments)
            {
                //hilight
                if (index == servoIndexHilight)
                    part.Node.RunActions(_hilight);
                //postition
                if (positions[index] != null)
                    part.Node.RunActions(new EaseInOut(new ServoTo(2, positions[index].Value), 2));
                index++;
            }
        }

        public void StopPose()
        {
            //validate
            if (!IsRunning)
                return;

            foreach (var part in SimArm.ArmSegments)
            {
                part.Node.RemoveAllActions();
                part.Node.RunActions(_unhilight);
            }

            VerticalPostion(false);
        }
    }
}
