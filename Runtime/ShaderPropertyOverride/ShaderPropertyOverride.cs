using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialOverrides
{
    [Serializable]
    public sealed class ShaderPropertyOverride
    {
        [SerializeField]
        bool m_PinnedState;

        [SerializeField]
        bool m_OverrideState;
        public bool overrideState
        {
            get => m_OverrideState;
            set => m_OverrideState = value;
        }
        
        [SerializeField]
        ShaderPropertyInfo m_PropertyInfo;
        public ShaderPropertyInfo propertyInfo => m_PropertyInfo;

        [SerializeField]
        Color m_ColorValue;
        public Color colorValue
        {
            get
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Color);
                return m_ColorValue;
            }
            set
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Color);
                m_ColorValue = value;
            }
        }

        [SerializeField]
        Vector4 m_VectorValue;
        public Vector4 vectorValue
        {
            get
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Vector);
                return m_VectorValue;
            }
            set
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Vector);
                m_VectorValue = value;
            }
        }

        [SerializeField]
        float m_FloatValue;
        public float floatValue
        {
            get
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Float || propertyInfo.type == ShaderPropertyType.Range);
                return m_FloatValue;
            }
            set
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Float || propertyInfo.type == ShaderPropertyType.Range);
                m_FloatValue = value;
            }
        }

#if UNITY_2021_1_OR_NEWER
        [SerializeField]
        int m_IntValue;
        public int intValue
        {
            get
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Int);
                return m_IntValue;
            }
            set
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Int);
                m_IntValue = value;
            }
        }
#endif

        [SerializeField]
        Texture m_TextureValue;
        public Texture textureValue
        {
            get
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Texture);
                return m_TextureValue;
            }
            set
            {
                Debug.Assert(propertyInfo.type == ShaderPropertyType.Texture);
                m_TextureValue = value;
            }
        }

        public ShaderPropertyOverride(ShaderPropertyInfo info)
        {
            m_PropertyInfo = info;

            // Initialize with default values
            switch (m_PropertyInfo.type)
            {
                case ShaderPropertyType.Color:
                case ShaderPropertyType.Vector:
                    m_ColorValue = m_PropertyInfo.defaultVectorValue;
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    m_FloatValue = m_PropertyInfo.defaultFloatValue;
                    break;
                case ShaderPropertyType.Texture:
                    m_TextureValue = m_PropertyInfo.defaultTextureValue;
                    break;
            }
        }

        public void ApplyTo(MaterialPropertyBlock mpb)
        {
            if (m_OverrideState)
            {
                switch (m_PropertyInfo.type)
                {
                    case ShaderPropertyType.Color:
                        mpb.SetColor(m_PropertyInfo.id, m_ColorValue);
                        break;
                    case ShaderPropertyType.Vector:
                        mpb.SetVector(m_PropertyInfo.id, m_VectorValue);
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        mpb.SetFloat(m_PropertyInfo.id, m_FloatValue);
                        break;
                    case ShaderPropertyType.Texture:
                        if (m_TextureValue != null)
                            mpb.SetTexture(m_PropertyInfo.id, m_TextureValue);
                        else if (m_PropertyInfo.defaultTextureValue != null)
                            mpb.SetTexture(m_PropertyInfo.id, m_PropertyInfo.defaultTextureValue);
                        break;
#if UNITY_2021_1_OR_NEWER
                    case ShaderPropertyType.Int:
                        mpb.SetInt(m_PropertyInfo.id, m_IntValue);
                        break;
#endif
                }
            }
        }

        public void Reset()
        {
            switch (m_PropertyInfo.type)
            {
                case ShaderPropertyType.Color:
                    m_ColorValue = m_PropertyInfo.defaultVectorValue;
                    break;
                case ShaderPropertyType.Vector:
                    m_VectorValue = m_PropertyInfo.defaultVectorValue;
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    m_FloatValue = m_PropertyInfo.defaultFloatValue;
                    break;
                case ShaderPropertyType.Texture:
                    m_TextureValue = m_PropertyInfo.defaultTextureValue;
                    break;
#if UNITY_2021_1_OR_NEWER
                case ShaderPropertyType.Int:
                    m_IntValue = 0;
                    break;
#endif
            }
        }
    }
}