using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Gui;
using Urho.Resources;
using zArm.Simulation.Components;

namespace zArm.Simulation
{
    /// <summary>
    /// 3d simulation
    /// </summary>
    public class App : Urho.Application
    {
        public static ISimRegistry SimRegistry { get; set; }

        public Sim Sim { get; }
        public IUISupportWindow UISupportWindow { get; }
        public IUISupportInput UISupportInput { get; }

        public Node CameraNode { get; set; }
        public CameraRotation CameraRotation { get; set; }
        public Scene Scene { get; set; }
        public Viewport Viewport { get; protected set; }

        public App(ApplicationOptions options = null) : base(options)
        {
			//register with Sim
			var registry = SimRegistry;
            if (registry != null)
            {
                var reg = registry.GetRegisteredSim(options.ExternalWindow);
                if (reg != null)
                {
                    Sim = reg.Sim;
                    reg.Sim.App = this;
                    UISupportWindow = reg.UISupportWindow;
                    UISupportInput = reg.UISupportInput;
                }
            }
        }

        protected override void Stop()
        {
            base.Stop();
            Sim?.Stopped();
        }

        protected override void Start()
        {
            base.Start();

            InitScene();
            InitManagers();
            CreateScene();
            SetupViewport();

            Sim?.OnStarted();
        }

        protected virtual void InitScene()
        {
            //blank scene
            Scene = new Scene();
            Scene.CreateComponent<Octree>();

            //default camera
            CameraNode = Scene.CreateChild("camera");
            CameraNode.CreateComponent<Camera>();
            CameraNode.Position = new Vector3(0, 5, 0);
            CameraRotation = CameraNode.CreateComponent<CameraRotation>();
        }

        protected virtual void InitManagers()
        {
        }

        protected virtual void CreateScene()
        {
        }

        protected virtual void SetupViewport()
        {
            //simple single viewport
            var renderer = Renderer;
            var camera = CameraNode.GetComponent<Camera>();
            if (camera != null)
            {
                Viewport = new Viewport(Context, Scene, camera, null);
                renderer.SetViewport(0, Viewport);
            }  
        }
    }


    public interface ISimRegistry
    {
        RegisteredSim GetRegisteredSim(IntPtr renderWindow);
    }

    public class RegisteredSim
    {
        public Sim Sim { get; set; }
        public IUISupportWindow UISupportWindow { get; set; }
        public IUISupportInput UISupportInput { get; set; }
    }

}
