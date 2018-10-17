using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using zArm.Simulation.Actions;
using zArm.Simulation.Components;
using zArm.Simulation.Entities;

namespace zArm.Simulation.Enviorment
{
    public class ArmApp : AppProduction
    {
        public SimzArmB1 SimArm;

        public ArmApp(ApplicationOptions options = null) : base(options) { }

        protected override void CreateScene()
        {
            base.CreateScene();

            //build arm
            SimArm = new SimzArmB1(Scene);

            //camera position
            CameraNode.Position = new Vector3(-40, 25, 12);
            CameraRotation.Pitch = 13;
            CameraRotation.Yaw = 40;

            //orbit arm
            var orbit = CameraNode.GetComponent<CameraOrbit>();
            orbit.Target = SimArm.Arm;

			//dock lights on camera
			OnUpdate(0); //force an update to get camera oriented before mounting lights
			var lights = Scene.GetChildrenWithComponent<Light>();
			if (lights != null)
			{
				foreach (var light in lights)
				{
					light.Parent = CameraNode;
					//lower the brightness a bit
					var component = light.GetComponent<Light>();
					component.Brightness = component.Brightness * .85f;
					component.SpecularIntensity = .5f;
				}
			}

			////add a flashlight to the camera
			//OnUpdate(0);
			//var lightNode = CameraNode.CreateChild("flashlight");
			//lightNode.SetWorldPosition(new Vector3(20, 50, -80));
			//lightNode.SetWorldDirection(Vector3.Zero - lightNode.Position);
			//var light = lightNode.CreateComponent<Light>();
			//light.LightType = LightType.Directional;
			//light.ShadowIntensity = .55f;
			//light.Brightness = .40f;

		}

	}
}
