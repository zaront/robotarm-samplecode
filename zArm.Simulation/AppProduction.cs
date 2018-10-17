using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using zArm.Simulation.Components;

namespace zArm.Simulation
{
    public class AppProduction : AppBasic
    {
        public CameraOrbit CameraOrbit { get; protected set; }

        public AppProduction(ApplicationOptions options = null) : base(options){ }

        protected override void InitScene()
        {
            base.InitScene();

            //enable orbit Camera
            CameraOrbit = CameraNode.CreateComponent<CameraOrbit>();
            InputManager.Add(CameraOrbit);
        }

        protected override void CreateScene()
        {
            base.CreateScene();

            ScaleWorld();
            CreatePhysicsWorld();
            CreateLight().GetComponent<Light>().CastShadows = true;
        }

        protected override void SetupViewport()
        {
            base.SetupViewport();
            
            Viewport.SetClearColor(new Color(37f / 255f, 37f / 255f, 37f / 255f)); //background color
        }
    }
}
