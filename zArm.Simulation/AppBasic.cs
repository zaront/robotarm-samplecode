using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Physics;

namespace zArm.Simulation
{
    public class AppBasic : App
    {
        protected InputManager InputManager;
        protected SelectionManager SelectionManager;

        public AppBasic(ApplicationOptions options = null) : base(options) {}

        protected override void Start()
        {
            InputManager = new InputManager(Input, UISupportInput);

            base.Start();
        }

        protected override void InitManagers()
        {
            base.InitManagers();

            SelectionManager = new SelectionManager(InputManager, UISupportWindow, CameraNode.GetComponent<Camera>(), Scene.GetComponent<Octree>(), this);

            //add the selection manager as input - after the camera is added
            InputManager.Add(SelectionManager);
        }

        protected override void Stop()
        {
            base.Stop();

            if (InputManager != null)
            {
                InputManager.Clear();
                InputManager.MouseVisible = true;
            }
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            //update inputs
            InputManager?.UpdateInput(timeStep);
        }

        protected Node CreateLight()
        {
            // Create a directional light to the world so that we can see something. The light scene node's orientation controls the
            // light direction; we will use the SetDirection() function which calculates the orientation from a forward direction vector.
            // The light will use default settings (white light, no shadows)
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Position = new Vector3(-50, 50, -50);
            lightNode.SetDirection(Vector3.Zero - lightNode.Position);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.ShadowIntensity = .55f;
            light.Brightness = 1f;
            //lightNode.CreateComponent<Box>();

            //backlight
            var backlightNode = Scene.CreateChild("DirectionalLight");
            backlightNode.Position = new Vector3(40, 50, 50);
            backlightNode.SetDirection(Vector3.Zero - backlightNode.Position);
            var backlight = backlightNode.CreateComponent<Light>();
            backlight.LightType = LightType.Directional;
            backlight.ShadowIntensity = .55f;
			backlight.Brightness = .27f;
            //backlightNode.CreateComponent<Box>();

            return lightNode;
        }

        protected void ScaleWorld()
        {
            //convert physics to centimeters

            //scale physics to centimeters per sec (default is meters & kilograms)
            var physics = Scene.GetOrCreateComponent<PhysicsWorld>();
            physics.SetGravity(new Vector3(0, -98.1f, 0));//centimeters

            //mass should be set in 10's of grams
            /*
            servo = 16g  (1.6 10's of grams)
            shoulder = 10.7g (1.07)
            upperArm = 9.4g (.94)
            foreArm = 8.4g (.84)
            hand = 10g (1)
            finger = 5.7g (.57)
            */
        }

        protected PhysicsWorld CreatePhysicsWorld()
        {
            var physics = Scene.GetOrCreateComponent<PhysicsWorld>();
            physics.Fps = 240; //speed up physics engine to get contraints to work well
            return physics;
        }

        protected Node CreateFloor()
        {
            var cache = ResourceCache;
            // Create a child scene node (at world origin) and a StaticModel component into it. Set the StaticModel to show a simple
            // plane mesh with a "stone" material. Note that naming the scene nodes is optional. Scale the scene node larger
            // (100 x 100 world units)
            // Create a floor object, 500 x 500 world units. Adjust position so that the ground is at zero Y
            Node floorNode = Scene.CreateChild("Floor");
            floorNode.Position = new Vector3(0.0f, -0.5f, 0.0f);
            floorNode.Scale = new Vector3(500.0f, 1.0f, 500.0f);
            StaticModel floorObject = floorNode.CreateComponent<StaticModel>();
            floorObject.Model = cache.GetModel("Models/Box.mdl");
            var m = cache.GetMaterial("Materials/StoneTiled.xml");

            floorObject.SetMaterial(m);

            return floorNode;
        }

        protected Node CreatePhysicsFloor()
        {
            var floorNode = CreateFloor();
            // Make the floor physical by adding RigidBody and CollisionShape components
            RigidBody body = floorNode.CreateComponent<RigidBody>();
            // We will be spawning spherical objects in this sample. The ground also needs non-zero rolling friction so that
            // the spheres will eventually come to rest
            body.RollingFriction = 0.15f;
            CollisionShape shape = floorNode.CreateComponent<CollisionShape>();
            // Set a box shape of size 1 x 1 x 1 for collision. The shape will be scaled with the scene node scale, so the
            // rendering and physics representation sizes should match (the box model is also 1 x 1 x 1.)
            shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
            body.Restitution = 0.3f;
			return floorNode;
        }
    }
}
