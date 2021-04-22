﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaterialOverrides
{
    [ExecuteAlways]
    public class MaterialOverrideGroup : MonoBehaviour
    {
        [SerializeField]
        ShaderPropertyOverrideList[] m_ShaderPropertyOverrides = new ShaderPropertyOverrideList[0];
        public ShaderPropertyOverrideList[] shaderPropertyOverrides => m_ShaderPropertyOverrides;

        [SerializeField]
        MaterialPropertyOverrideList[] m_MaterialPropertyOverrides = new MaterialPropertyOverrideList[0];
        public MaterialPropertyOverrideList[] materialPropertyOverrides => m_MaterialPropertyOverrides;

        [SerializeField]
        Renderer[] m_Renderers = new Renderer[0];
        public Renderer[] renderers => m_Renderers;

        [SerializeField]
        Shader[] m_Shaders = new Shader[0];
        public Shader[] shaders => m_Shaders;

        [SerializeField]
        Material[] m_Materials = new Material[0];
        public Material[] materials => m_Materials;

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
            Apply();
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
        
        public void Reset()
        {
            m_ShaderPropertyOverrides = new ShaderPropertyOverrideList[0];
            m_MaterialPropertyOverrides = new MaterialPropertyOverrideList[0];
            
            ClearOverrides();
            Populate();
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

        static readonly List<Renderer> s_ChildRenderers = new List<Renderer>();

        /// Populates the list of renders and available overrides.
        public void Populate()
        {
            // Renderer list

            var newShaders = new HashSet<Shader>();
            var newMaterials = new HashSet<Material>();
            
            s_ChildRenderers.Clear();
            GetComponentsInChildren(false, s_ChildRenderers);

            foreach (var renderer in s_ChildRenderers)
            {
                foreach (var sharedMaterial in renderer.sharedMaterials)
                {
                    if (sharedMaterial)
                    {
                        newShaders.Add(sharedMaterial.shader);
                        newMaterials.Add(sharedMaterial);
                    }
                }
            }

            m_Renderers = s_ChildRenderers.ToArray();

            // Shader overrides

            m_ShaderToOverrideList.Clear();
            
            foreach (var shader in newShaders)
            {
                var overrideList = Array.Find(m_ShaderPropertyOverrides, x => x.shader == shader);
                if (!overrideList)
                {
                    overrideList = ScriptableObject.CreateInstance<ShaderPropertyOverrideList>();
                    var shaderPropertyInfos = ShaderPropertyInfoCache.GetShaderPropertyInfoList(shader);
                    overrideList.Initialize(shader, ShaderPropertyInfoCache.CreatePropertyValues(shaderPropertyInfos));
                }
                
                m_ShaderToOverrideList.Add(shader, overrideList);
            }
            

            m_Shaders = newShaders.ToArray();
            Array.Sort(m_Shaders, (a, b) => string.CompareOrdinal(a.name, b.name)); // For the inspector
            m_ShaderPropertyOverrides = m_ShaderToOverrideList.Values.ToArray();

            // Material overrides
            
            m_MaterialToOverrideList.Clear();

            foreach (var material in newMaterials)
            {
                var overrideList = Array.Find(m_MaterialPropertyOverrides, x => x.material == material);
                if (!overrideList)
                {
                    overrideList = ScriptableObject.CreateInstance<MaterialPropertyOverrideList>();
                    var shaderPropertyInfos = ShaderPropertyInfoCache.GetShaderPropertyInfoList(material.shader);
                    overrideList.Initialize(material, ShaderPropertyInfoCache.CreatePropertyValues(shaderPropertyInfos));
                }
                
                m_MaterialToOverrideList.Add(material, overrideList);
            }

            m_Materials = newMaterials.ToArray();
            Array.Sort(m_Materials, (a, b) => string.CompareOrdinal(a.name, b.name)); // For the inspector
            m_MaterialPropertyOverrides = m_MaterialToOverrideList.Values.ToArray();

            RefreshOverrideCache();
        }

        /// Applies active and overriden properties to the list of renderers.
        public void Apply()
        {
            if (m_MaterialPropertyBlock == null)
                m_MaterialPropertyBlock = new MaterialPropertyBlock();
            
            foreach (var value in m_RendererOverrideCache)
            {
                if (!value.renderer)
                    continue;
                
                m_MaterialPropertyBlock.Clear();
                
                if (value.shaderOverrideList)
                    value.shaderOverrideList.ApplyTo(m_MaterialPropertyBlock);
                
                if (value.materialOverrideList)
                    value.materialOverrideList.ApplyTo(m_MaterialPropertyBlock);
                
                value.renderer.SetPropertyBlock(m_MaterialPropertyBlock);
            }
        }

        internal void RefreshOverrideCache()
        {
            foreach (var renderer in m_Renderers)
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
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
    }
}