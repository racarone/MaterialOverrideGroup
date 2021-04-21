using UnityEditor;

namespace MaterialOverrides
{
    class SerializedShaderPropertyInfo
    {
        public SerializedProperty root { get; }
        public SerializedProperty displayName { get; }
        public SerializedProperty name { get; }
        public SerializedProperty id { get; }
        public SerializedProperty type { get; }
        public SerializedProperty flags { get; }
        public SerializedProperty range { get; }
        public SerializedProperty textureDimension { get; }

        public SerializedShaderPropertyInfo(SerializedProperty baseProperty)
        {
            root = baseProperty.Copy();
            name = baseProperty.FindPropertyRelative("m_Name");
            id = baseProperty.FindPropertyRelative("m_Id");
            displayName = baseProperty.FindPropertyRelative("m_DisplayName");
            type = baseProperty.FindPropertyRelative("m_Type");
            flags = baseProperty.FindPropertyRelative("m_Flags");
            range = baseProperty.FindPropertyRelative("m_Range");
            textureDimension = baseProperty.FindPropertyRelative("m_TextureDimension");
        }
    }
}