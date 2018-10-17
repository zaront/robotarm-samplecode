using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using Urho.Physics;
using Urho.Resources;
using Urho.Urho2D;

namespace zArm.Simulation.Components
{
    public class Paper : Component
    {
        Texture2D _paperTexture;
        Image _paperImage;
        int _imageSize = 300;
        StaticModel _model;
        int _prevX = -1;
        int _prevY = -1;

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            //setup collision
            var model = node.GetComponent<StaticModel>();
            _model = model;
            if (model == null)
                throw new Exception("node must contain a model before adding a paper component");
            var c = node.CreateComponent<CollisionShape>();
            c.SetConvexHull(model.Model, 1, Vector3.One, Vector3.Zero, Quaternion.Identity);
            //var size = model.BoundingBox.Size;
            //c.SetBox(size, Vector3.Zero, Quaternion.Identity);
            var r = node.CreateComponent<RigidBody>();
            r.Trigger = true;
            r.CollisionLayer = 2;
            r.CollisionMask = 2; //don't collide with anything by layer 2
            r.Mass = 1;
            r.Kinematic = true;
            node.NodeCollision += OnCollision;

            //setup texture
            _paperImage = new Image();
            _paperImage.SetSize(_imageSize, _imageSize, 3);
            _paperImage.Clear(Color.White);
            _paperTexture = new Texture2D();
            _paperTexture.SetSize(10, 10, Urho.Graphics.RGBAFormat, TextureUsage.Static);
            _paperTexture.FilterMode = TextureFilterMode.Bilinear;
            _paperTexture.SetData(_paperImage, false);
            var paperMaterial = new Material();
            paperMaterial.SetTechnique(0, Application.ResourceCache.GetTechnique("Techniques/DiffUnlit.xml"), 0, 0);
            paperMaterial.SetTexture(TextureUnit.Diffuse, _paperTexture);
            model.SetMaterial(paperMaterial);
        }

        void OnCollision(NodeCollisionEventArgs e)
        {
            var marker = e.OtherNode.GetComponent<Marker>();
            if (marker == null)
                return;
            var contact = e.Contacts[0];
            var pos = Node.WorldToLocal(contact.ContactPosition);
            var size = (int)Math.Round(Math.Abs(contact.ContactDistance) * 5f);
            var x = (int)Math.Round((pos.Z + .5f) * _imageSize);
            var y = (int)Math.Round((pos.X + .5f) * _imageSize);
            Draw(x, y, marker.Color, size);
        }

        void Draw(int x, int y, Color color, int size = 1)
        {
            //validate - don't draw the same thing
            if (x == _prevX && y == _prevY)
                return;

            //add the pixel
            _paperImage.SetPixel(x, y, color);
            _paperTexture.SetData(_paperImage, false);

            _prevX = x;
            _prevY = y;
        }

        public void Clear()
        {
            _paperImage.Clear(Color.White);
            _paperTexture.SetData(_paperImage, false);
            _prevX = -1;
            _prevY = -1;
        }
    }
}
