using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using zArm.Simulation.Actions;
using zArm.Simulation.Components;

namespace zArm.Simulation.Enviorment
{
    public class MotionStudioSim : ArmSim<MotionStudioApp>
    {
		static FiniteTimeAction _hilight = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(true, Color.Blue));
		static FiniteTimeAction _stopHilight = new CallFuncN((n) => n.GetOrCreateComponent<Hilight>().SetHilight(false, Color.Blue));

		protected internal override void Started()
        {
            base.Started();

            App.CameraOrbit.AutoCenter = false;
		}

		public ITrails Trails
		{
			get { return App?.Trails; }
		}

		public void Hilight(int[] servoIDs = null)
		{
			//validate
			if (!IsRunning || SimArm == null)
				return;

			//stop flashing
			if (servoIDs == null || servoIDs.Length == 0)
			{
				foreach (var arm in SimArm.ArmSegments)
					arm.Node.RunActions(_stopHilight);
			}
			
			//flash arm parts
			else
			{
				foreach(var servoID in servoIDs)
					SimArm.ArmSegments[servoID - 1].Node.RunActions(_hilight);
			}
		}
		
		public ITransformGimbal TransformGimbal
		{
			get { return App?.TransformGimbal; }
		}
	}



	


	public class MotionStudioApp : ArmApp
    {
		public ITransformGimbal TransformGimbal { get; private set; }
		public ITrails Trails { get; private set; }

		public MotionStudioApp(ApplicationOptions options = null) : base(options) { }

		protected override void CreateScene()
		{
			base.CreateScene();

			//add transform gizmo
			var node = Scene.CreateChild("transform gizmo");
			TransformGimbal = node.CreateComponent<TransformGimbal>();
			TransformGimbal.IsVisible = false;

			//create trails control
			var trailsNode = Scene.CreateChild("trails");
			Trails = trailsNode.CreateComponent<Trails>();
		}
	}

    
}
