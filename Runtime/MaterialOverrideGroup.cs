using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MaterialOverrides
{
    [ExecuteAlways]
    public class MaterialOverrideGroup : MonoBehaviour
    {
        [SerializeField]
        List<ShaderPropertyOverrideList> m_ShaderPropertyOverrides = new List<ShaderPropertyOverrideList>();
        public List<ShaderPropertyOverrideList> shaderPropertyOverrides => m_ShaderPropertyOverrides;

        [SerializeField]
        List<MaterialPropertyOverrideList> m_MaterialPropertyOverrides = new List<MaterialPropertyOverrideList>();
        public List<MaterialPropertyOverrideList> materialPropertyOverrides => m_MaterialPropertyOverrides;

        [SerializeField]
        List<Renderer> m_Renderers = new List<Renderer>();
        public List<Renderer> renderers => m_Renderers;

        [SerializeField]
        List<Shader> m_Shaders = new List<Shader>();
        public List<Shader> shaders => m_Shaders;

        [SerializeField]
        List<Material> m_Materials = new List<Material>();
        public List<Material> materials => m_Materials;

        struct RendererOverrideCacheEntry
        {
            public Renderer renderer;
            public ShaderPropertyOverrideList shaderOverrideList;
            public ShaderPropertyOverrideList materialOverrideList;
        }

        readonly Dictionary<Shader, ShaderPropertyOverrideList> m_ShaderToOverrideList = new Dictionary<Shader, ShaderPropertyOverrideList>();
        readonly Dictionary<Material, MaterialPropertyOverrideList> m_MaterialToOverrideList = new Dictionary<Material, MaterialPropertyOverrideList>();
        readonly List<RendererOverrideCacheEntry> m_RendererOverrideCache = new List<RendererOverrideCacheEntry>();
        
        MaterialPropertyBlock m_MaterialPropertyBlock;

        void OnEnable()
        {
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
            
            m_ShaderToOverrideList.Clear();
            foreach (var list in m_ShaderPropertyOverrides)
                m_ShaderToOverrideList.Add(list.shader, list);
            
            m_MaterialToOverrideList.Clear();
            foreach (var list in m_MaterialPropertyOverrides)
                m_MaterialToOverrideList.Add(list.material, list);

            ClearOverrides();
            RefreshOverrideCache();
            ApplyOverrides();
        }

        void OnDisable()
        {
            ClearOverrides();
        }
        
        public bool TryGetOverride(Shader shader, out ShaderPropertyOverrideList propertyOverrideList)
        {
            return m_ShaderToOverrideList.TryGetValue(shader, out propertyOverrideList);
        }

        public bool TryGetOverride(Material material, out MaterialPropertyOverrideList propertyOverrideList)
        {
            return m_MaterialToOverrideList.TryGetValue(material, out propertyOverrideList);
        }

        public bool TryGetOverridePropertyValue(Shader shader, int id, out ShaderPropertyOverride propertyOverride)
        { 
            if (m_ShaderToOverrideList.TryGetValue(shader, out var propertyOverrideList) &&
                propertyOverrideList.TryGetOverride(id, out propertyOverride))
                return true;

            propertyOverride = null;
            return false;
        }

        public bool TryGetOverridePropertyValue(Shader shader, string name, out ShaderPropertyOverride propertyOverride)
        {
            return TryGetOverridePropertyValue(shader, Shader.PropertyToID(name), out propertyOverride);
        }

        public bool TryGetOverridePropertyValue(Material material, int id, out ShaderPropertyOverride propertyOverride)
        { 
            if (m_MaterialToOverrideList.TryGetValue(material, out var propertyOverrideList) &&
                propertyOverrideList.TryGetOverride(id, out propertyOverride))
                return true;

            propertyOverride = null;
            return false;
        }

        public bool TryGetOverridePropertyValue(Material material, string name, out ShaderPropertyOverride propertyOverride)
        {
            return TryGetOverridePropertyValue(material, Shader.PropertyToID(name), out propertyOverride);
        }
        
        void Reset()
        {
            PopulateOverrides();
        }

        /// Resets all renderer properties. Call to remove applied overrides.
        public void ClearOverrides()
        {
            foreach (var renderer in m_Renderers)
            {
                if (renderer == null)
                    continue;

                renderer.SetPropertyBlock(null);
                for (int i = 0, count = renderer.sharedMaterials.Length; i < count; i++)
                    renderer.SetPropertyBlock(null, i);
            }
        }

        /// Populates the list of renders and available overrides.
        public void PopulateOverrides()
        {
            ClearOverrides();
            
            m_ShaderToOverrideList.Clear();
            m_MaterialToOverrideList.Clear();
            
            // Renderer list

            var shaders = new HashSet<Shader>();
            var materials = new HashSet<Material>();
            var childRenderers = GetComponentsInChildren<Renderer>();

            foreach (var renderer in childRenderers)
            {
                foreach (var sharedMaterial in renderer.sharedMaterials)
                {
                    if (sharedMaterial == null)
                        continue;

                    shaders.Add(sharedMaterial.shader);
                    materials.Add(sharedMaterial);
                }
            }

            m_Renderers.Clear();
            m_Renderers.AddRange(childRenderers);

            // Shader overrides

            foreach (var shader in shaders)
            {
                var overrideList = m_ShaderPropertyOverrides.Find(x => x.shader == shader);
                if (!overrideList)
                {
                    overrideList = ScriptableObject.CreateInstance<ShaderPropertyOverrideList>();
                    var shaderPropertyInfos = ShaderPropertyInfoCache.GetShaderPropertyInfoList(shader);
                    overrideList.Initialize(shader, ShaderPropertyInfoCache.CreatePropertyValues(shaderPropertyInfos));
                }
                
                m_ShaderToOverrideList.Add(shader, overrideList);
            }

            m_Shaders.Clear();
            m_Shaders.AddRange(shaders.ToArray());
            m_Shaders.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // For the inspector
            m_ShaderPropertyOverrides = m_ShaderToOverrideList.Values.ToList();

            // Material overrides

            foreach (var material in materials)
            {
                var overrideList = m_MaterialPropertyOverrides.Find(x => x.material == material);
                if (!overrideList)
                {
                    overrideList = ScriptableObject.CreateInstance<MaterialPropertyOverrideList>();
                    var shaderPropertyInfos = ShaderPropertyInfoCache.GetShaderPropertyInfoList(material.shader);
                    overrideList.Initialize(material, ShaderPropertyInfoCache.CreatePropertyValues(shaderPropertyInfos));
                }
                
                m_MaterialToOverrideList.Add(material, overrideList);
            }

            m_Materials.Clear();
            m_Materials.AddRange(materials.ToArray());
            m_Materials.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // For the inspector
            m_MaterialPropertyOverrides = m_MaterialToOverrideList.Values.ToList();

            RefreshOverrideCache();
            ApplyOverrides();
        }

        internal void RefreshOverrideCache()
        {
            foreach (var renderer in m_Renderers)
            {
                if (!renderer)
                    continue;

                var sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials.Length == 0)
                    continue;

                foreach (var material in sharedMaterials)
                {
                    if (!material || !material.shader)
                        continue;
                    
                    ShaderPropertyOverrideList shaderOverrideList = null;
                    MaterialPropertyOverrideList materialOverrideList = null;

                    bool hasShaderOverrides = m_ShaderToOverrideList.TryGetValue(material.shader, out shaderOverrideList);
                    bool hasMaterialOverrides = m_MaterialToOverrideList.TryGetValue(material, out materialOverrideList);
                    
                    if (!hasShaderOverrides && !hasMaterialOverrides)
                        continue;

                    m_RendererOverrideCache.Add(new RendererOverrideCacheEntry
                    {
                        renderer = renderer, 
                        shaderOverrideList = shaderOverrideList, 
                        materialOverrideList = materialOverrideList
                    });
                }
            }
        }

        /// Applies active and overriden properties to the list of renderers.
        public void ApplyOverrides()
        {
            if (m_MaterialPropertyBlock == null)
                m_MaterialPropertyBlock = new MaterialPropertyBlock();
            
            foreach (var value in m_RendererOverrideCache)
            {
                if (!value.renderer)
                    continue;
                
                m_MaterialPropertyBlock.Clear();
                
                if (value.shaderOverrideList && value.shaderOverrideList.active)
                    ApplyOverrides(m_MaterialPropertyBlock, value.shaderOverrideList.overrides);
                
                if (value.materialOverrideList && value.materialOverrideList.active)
                    ApplyOverrides(m_MaterialPropertyBlock, value.materialOverrideList.overrides);
                
                value.renderer.SetPropertyBlock(m_MaterialPropertyBlock);
            }
        }

        /// Applies a list of individual override values to an mpb
        static void ApplyOverrides(MaterialPropertyBlock mpb, List<ShaderPropertyOverride> propertyOverrides)
        {
            foreach (var o in propertyOverrides)
            {
                if (o.overrideState == false)
                    continue;

                switch (o.propertyInfo.type)
                {
                    case ShaderPropertyType.Color:
                        mpb.SetColor(o.propertyInfo.id, o.colorValue);
                        break;
                    case ShaderPropertyType.Vector:
                        mpb.SetVector(o.propertyInfo.id, o.vectorValue);
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        mpb.SetFloat(o.propertyInfo.id, o.floatValue);
                        break;
                    case ShaderPropertyType.Texture:
                        if (o.textureValue != null)
                            mpb.SetTexture(o.propertyInfo.id, o.textureValue);
                        else if (o.propertyInfo.defaultTextureValue != null)
                            mpb.SetTexture(o.propertyInfo.id, o.propertyInfo.defaultTextureValue);
                        break;
#if UNITY_2021_1_OR_NEWER
                    case ShaderPropertyType.Int:
                        mpb.SetInt(o.propertyInfo.id, o.intValue);
                        break;
#endif
                }
            }
        }
    }
}