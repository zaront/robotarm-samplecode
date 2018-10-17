using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Gui;
using Urho.Physics;
using Urho.Resources;
using zArm.Simulation.Components;

namespace zArm.Simulation
{
    /// <summary>
    /// adds debuging abilities to a 3d simulation like WASD camera, framerate and such
    /// </summary>
    public class AppDebug : AppBasic
    {
        protected Node SelectedNode { get; private set; }
        protected MonoDebugHud MonoDebugHud { get; set; }
        protected bool DrawDebug { get; set; }
        protected bool DrawBoxes { get; set; }

        public AppDebug(ApplicationOptions options = null) : base(options){}

        protected override void Start()
        {
            base.Start();

            MonoDebugHud = new MonoDebugHud(this);
            MonoDebugHud.Show();
            Input.KeyDown += HandleKeyDown;
            Engine.PostRenderUpdate += RenderDebug;
        }

        protected override void InitScene()
        {
            base.InitScene();

            //enable fly Camera
            var cameraFly = CameraNode.CreateComponent<CameraFly>();
            InputManager.Add(cameraFly);
            cameraFly.CaptureMouse = true;
        }

        [Flags]
        enum DebugMode
        {
            DBG_NoDebug = 0,
            DBG_DrawWireframe = 1,
            DBG_DrawAabb = 2,
            DBG_DrawFeaturesText = 4,
            DBG_DrawContactPoints = 8,
            DBG_NoDeactivation = 16,
            DBG_NoHelpText = 32,
            DBG_DrawText = 64,
            DBG_ProfileTimings = 128,
            DBG_EnableSatComparison = 256,
            DBG_DisableBulletLCP = 512,
            DBG_EnableCCD = 1024,
            DBG_DrawConstraints = (1 << 11),
            DBG_DrawConstraintLimits = (1 << 12),
            DBG_FastWireframe = (1 << 13),
            DBG_DrawNormals = (1 << 14),
            DBG_DrawFrames = (1 << 15),
            DBG_MAX_DEBUG_DRAW_MODE
        }

        void RenderDebug(PostRenderUpdateEventArgs args)
        {
            // If draw debug mode is enabled, draw viewport debug geometry, which will show eg. drawable bounding boxes and skeleton
            // bones. Note that debug geometry has to be separately requested each frame. Disable depth test so that we can see the
            // bones properly
            if (DrawDebug)
            {
                Scene.GetOrCreateComponent<DebugRenderer>();
                var pw = Scene?.GetComponent<PhysicsWorld>();
                pw?.setDebugMode((int)DebugMode.DBG_MAX_DEBUG_DRAW_MODE);
                pw?.DrawDebugGeometry(true);
            }
            if (DrawBoxes)
            {
                Scene.GetOrCreateComponent<DebugRenderer>();
                Renderer.DrawDebugGeometry(true);
            }
        }

        protected override void SetupViewport()
        {
            base.SetupViewport();

            //add flashlight to camera
            var flashlight = CameraNode.CreateChild("flashlight");
            var light = flashlight.CreateComponent<Light>();
            light.LightType = LightType.Point;
            light.ShadowIntensity = .55f;
            light.Enabled = false;
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);
            SelectedNode = SelectionManager.Selection as Node;
            if (SelectedNode == null && SelectionManager.Selection is Component)
                SelectedNode = (SelectionManager.Selection as Component).Node;

        }

        void HandleKeyDown(KeyDownEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Esc:
                    Exit();
                    return;
                //case Key.F1:
                //    _console.Toggle();
                //    return;
                //case Key.F2:
                //    _debugHud.ToggleAll();
                //    return;
                case Key.F1:
                    DrawDebug = !DrawDebug;
                    return;
                case Key.F2:
                    DrawBoxes = !DrawBoxes;
                    return;
            }

            var renderer = Renderer;
            switch (e.Key)
            {
                case Key.N1:
                    var quality = renderer.TextureQuality;
                    ++quality;
                    if (quality > 2)
                        quality = 0;
                    renderer.TextureQuality = quality;
                    break;

                case Key.N2:
                    var mquality = renderer.MaterialQuality;
                    ++mquality;
                    if (mquality > 2)
                        mquality = 0;
                    renderer.MaterialQuality = mquality;
                    break;

                case Key.N3:
                    renderer.SpecularLighting = !renderer.SpecularLighting;
                    break;

                case Key.N4:
                    renderer.DrawShadows = !renderer.DrawShadows;
                    break;

                case Key.N5:
                    var shadowMapSize = renderer.ShadowMapSize;
                    shadowMapSize *= 2;
                    if (shadowMapSize > 2048)
                        shadowMapSize = 512;
                    renderer.ShadowMapSize = shadowMapSize;
                    break;

                // shadow depth and filtering quality
                case Key.N6:
                    var q = (int)renderer.ShadowQuality++;
                    if (q > 3)
                        q = 0;
                    renderer.ShadowQuality = (ShadowQuality)q;
                    break;

                // occlusion culling
                case Key.N7:
                    var o = !(renderer.MaxOccluderTriangles > 0);
                    renderer.MaxOccluderTriangles = o ? 5000 : 0;
                    break;

                // instancing
                case Key.N8:
                    renderer.DynamicInstancing = !renderer.DynamicInstancing;
                    break;

                case Key.N9:
                    Image screenshot = new Image();
                    Graphics.TakeScreenShot(screenshot);
                    screenshot.SavePNG(FileSystem.ProgramDir + $"Data/Screenshot_{GetType().Name}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}.png");
                    break;

                //flashlight
                case Key.N0:
                    var light = CameraNode?.GetChild("flashlight", false)?.GetComponent<Light>();
                    if (light != null)
                        light.Enabled = !light.Enabled;
                    break;
            }
        }

        
    }
}
