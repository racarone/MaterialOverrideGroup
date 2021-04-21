using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialOverrides
{
    /// Immutable shader property info
    [Serializable]
    public struct ShaderPropertyInfo
    {
        [SerializeField]
        string m_DisplayName;
        public string displayName => m_DisplayName;

        [SerializeField]
        string m_Name;
        public string name => m_Name;

        [SerializeField]
        int m_Id;
        public int id => m_Id;

        [SerializeField]
        ShaderPropertyType m_Type;
        public ShaderPropertyType type => m_Type;

        [SerializeField]
        ShaderPropertyFlags m_Flags;
        public ShaderPropertyFlags flags => m_Flags;

        [SerializeField]
        Vector2 m_Range;
        public Vector2 range
        {
            get
            {
                Debug.Assert(type == ShaderPropertyType.Range);
                return m_Range;
            }
        }

        [SerializeField]
        TextureDimension m_TextureDimension;
        public TextureDimension textureDimension
        {
            get
            {
                Debug.Assert(type == ShaderPropertyType.Texture);
                return m_TextureDimension;
            }
        }

        [SerializeField]
        float m_DefaultFloatValue;
        public float defaultFloatValue
        {
            get
            {
                Debug.Assert(type == ShaderPropertyType.Float || type == ShaderPropertyType.Range);
                return m_DefaultFloatValue;
            }
        }
        
        [SerializeField]
        Vector4 m_DefaultVectorValue;
        public Vector4 defaultVectorValue
        {
            get
            {
                Debug.Assert(type == ShaderPropertyType.Vector || type == ShaderPropertyType.Color);
                return m_DefaultVectorValue;
            }
        }
        
        [SerializeField]
        string m_DefaultTexureName;
        public string defaultTextureName
        {
            get
            {
                Debug.Assert(type == ShaderPropertyType.Texture);
                return m_DefaultTexureName;
            }
        }
        
        [SerializeField]
        Texture m_DefaultTexureValue;
        public Texture defaultTextureValue
        {
            get
            {
                Debug.Assert(type == ShaderPropertyType.Texture);
                return m_DefaultTexureValue;
            }
        }

        public ShaderPropertyInfo(Shader shader, int propertyIndex)
        {
            m_Name = shader.GetPropertyName(propertyIndex);
            m_Id = Shader.PropertyToID(m_Name);
            m_DisplayName = shader.GetPropertyDescription(propertyIndex);
            m_Type = shader.GetPropertyType(propertyIndex);
            m_Flags = shader.GetPropertyFlags(propertyIndex);
            
            m_Range = default;
            if (m_Type == ShaderPropertyType.Range)
            {
                m_Range = shader.GetPropertyRangeLimits(propertyIndex);
            }
            
            m_TextureDimension = TextureDimension.None;
            m_DefaultFloatValue = 0;
            m_DefaultVectorValue = default;
            m_DefaultTexureName = string.Empty;
            m_DefaultTexureValue = null;
            switch (shader.GetPropertyType(propertyIndex))
            {
                case ShaderPropertyType.Color:
                case ShaderPropertyType.Vector:
                    m_DefaultVectorValue = shader.GetPropertyDefaultVectorValue(propertyIndex);
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    m_DefaultFloatValue = shader.GetPropertyDefaultFloatValue(propertyIndex);
                    break;
                case ShaderPropertyType.Texture:
                    m_TextureDimension = shader.GetPropertyTextureDimension(propertyIndex);
                    m_DefaultTexureName = shader.GetPropertyTextureDefaultName(propertyIndex);
                    m_DefaultTexureValue = m_TextureDimension switch
                    {
                        TextureDimension.Tex2D => m_DefaultTexureName switch
                        {
                            "white" => Texture2D.whiteTexture,
                            "black" => Texture2D.blackTexture,
                            "gray"  => Texture2D.grayTexture,
                            "bump"  => Texture2D.normalTexture,
                            _       => null
                        },
                        TextureDimension.Tex3D     => CoreUtils.blackVolumeTexture,
                        TextureDimension.Cube      => CoreUtils.blackCubeTexture,
                        TextureDimension.CubeArray => CoreUtils.magentaCubeTextureArray,
                        _                          => null
                    };

                    break;
            }
        }
    }
    
    public static class ShaderPropertyInfoCache
    {
        static readonly Dictionary<int, List<ShaderPropertyInfo>> s_Cache = new Dictionary<int, List<ShaderPropertyInfo>>();

        public static List<ShaderPropertyInfo> GetShaderPropertyInfoList(Shader shader)
        {
            if (s_Cache.TryGetValue(shader.GetInstanceID(), out var propertyInfoList))
                return propertyInfoList;

            propertyInfoList = new List<ShaderPropertyInfo>();
            for (int i = 0, count = shader.GetPropertyCount(); i < count; i++)
                propertyInfoList.Add(new ShaderPropertyInfo(shader, i));

            return s_Cache[shader.GetInstanceID()] = propertyInfoList;
        }

        public static List<ShaderPropertyOverride> CreatePropertyValues(IEnumerable<ShaderPropertyInfo> infos)
        {
            List<ShaderPropertyOverride> propertyOverrides = new List<ShaderPropertyOverride>();
            
            foreach (var info in infos)
                propertyOverrides.Add(new ShaderPropertyOverride(info));
            
            return propertyOverrides;
        }
    }
}