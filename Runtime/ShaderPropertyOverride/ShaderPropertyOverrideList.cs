using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialOverrides
{
    public class ShaderPropertyOverrideList : ScriptableObject
    {
        [SerializeField]
        bool m_Active = true;
        public bool active
        {
            get => m_Active;
            set => m_Active = value;
        }
        
        [SerializeField]
        Shader m_Shader;
        public Shader shader => m_Shader;

        [SerializeField]
        List<ShaderPropertyOverride> m_Overrides = new List<ShaderPropertyOverride>();
        public List<ShaderPropertyOverride> overrides => m_Overrides;

        readonly Dictionary<int, ShaderPropertyOverride> m_IdToOverride = new Dictionary<int, ShaderPropertyOverride>();

        internal void Initialize(Shader shader, List<ShaderPropertyOverride> overrides)
        {
            m_Shader = shader;
            m_Overrides = overrides;
            RefreshIds();
        }

        void OnEnable()
        {
            RefreshIds();
        }

        void Reset()
        {
            RefreshIds();
        }

        void RefreshIds()
        {
            m_IdToOverride.Clear();
            foreach (var propertyOverride in m_Overrides)
            {
                if (!m_IdToOverride.ContainsKey(propertyOverride.propertyInfo.id))
                    m_IdToOverride.Add(propertyOverride.propertyInfo.id, propertyOverride);
            }
        }

        public bool TryGetOverride(int id, out ShaderPropertyOverride propertyOverride)
        {
            return m_IdToOverride.TryGetValue(id, out propertyOverride);
        }

        public bool TryGetOverride(string name, out ShaderPropertyOverride propertyOverride)
        {
            return TryGetOverride(Shader.PropertyToID(name), out propertyOverride);
        }

        public void ApplyTo(MaterialPropertyBlock mpb)
        {
            if (m_Active)
            {
                foreach (var propertyOverride in m_Overrides)
                    propertyOverride.ApplyTo(mpb);
            }
        }
    }
}