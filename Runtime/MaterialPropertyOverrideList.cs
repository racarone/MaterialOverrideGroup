using System.Collections.Generic;
using UnityEngine;

namespace MaterialOverrides
{
    public class MaterialPropertyOverrideList : ShaderPropertyOverrideList
    {
        [SerializeField]
        Material m_Material;
        public Material material => m_Material;

        internal void Initialize(Material material, List<ShaderPropertyOverride> overrides)
        {
            base.Initialize(material.shader, overrides);
            m_Material = material;
        }
    }
}