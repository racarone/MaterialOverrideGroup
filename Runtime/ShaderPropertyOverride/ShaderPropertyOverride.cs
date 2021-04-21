using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialOverrides
{
    [Serializable]
    public class ShaderPropertyOverride
    {
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
            switch (info.type)
            {
                case ShaderPropertyType.Color:
                case ShaderPropertyType.Vector:
                    m_ColorValue = info.defaultVectorValue;
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    m_FloatValue = info.defaultFloatValue;
                    break;
                case ShaderPropertyType.Texture:
                    m_TextureValue = info.defaultTextureValue;
                    break;
            }
        }
    }
}