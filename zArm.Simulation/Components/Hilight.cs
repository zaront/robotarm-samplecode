using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Resources;

namespace zArm.Simulation.Components
{
    public class Hilight : Component
    {
		protected virtual StaticModel[] GetHilightModels()
        {
            return new StaticModel[] { Node.GetComponent<StaticModel>() };
        }

        /// <summary>
        /// Hilight a mesh
        /// </summary>
        /// <param name="materialIndex">hilight only that specific material</param>
        public void SetHilight(bool hilight, Color color, uint? materialIndex = null)
        {
            var models = GetHilightModels();
            foreach (var model in models)
                SetHilight(hilight, model, color, materialIndex);
        }

        void SetHilight(bool hilight, StaticModel mesh, Color color, uint? materialIndex = null)
        {
            //hilight all materials
            if (materialIndex == null)
            {
                for (uint i = 0; i < 10; i++)
                {
                    var mat = mesh.GetMaterial(i);
                    if (mat == null)
                        break;

                    SetHilightMaterial(hilight, mesh, mat, i, color);
                }
            }
            
            //hilight the specific material
            else
            {
                var material = mesh.GetMaterial(materialIndex.Value);
                if (material == null)
                    return;

                SetHilightMaterial(hilight, mesh, material, materialIndex.Value, color);
            }
        }

        void SetHilightMaterial(bool hilight, StaticModel mesh, Material material, uint materialIndex, Color color)
        {
            var matCopy = material.Clone(material.Name + "_hilight");
            matCopy.SetShaderParameter("MatDiffColor", hilight ? color : Color.White);
            mesh.SetMaterial(materialIndex, matCopy);
        }
	}
}
