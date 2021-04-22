using UnityEditor;

namespace MaterialOverrides
{
    class SerializedShaderPropertyOverride
    {
        public SerializedProperty root { get; }
        public SerializedProperty pinnedState { get; }
        public SerializedProperty overrideState { get; }
        public SerializedShaderPropertyInfo propertyInfo { get; }
        public SerializedProperty colorValue { get; }
        public SerializedProperty vectorValue { get; }
        public SerializedProperty floatValue { get; }
        public SerializedProperty intValue { get; }
        public SerializedProperty textureValue { get; }

        public SerializedShaderPropertyOverride(SerializedProperty baseProperty)
        {
            root = baseProperty.Copy();
            pinnedState = baseProperty.FindPropertyRelative("m_PinnedState");
            overrideState = baseProperty.FindPropertyRelative("m_OverrideState");
            propertyInfo = new SerializedShaderPropertyInfo(baseProperty.FindPropertyRelative("m_PropertyInfo"));
            colorValue = baseProperty.FindPropertyRelative("m_ColorValue");
            vectorValue = baseProperty.FindPropertyRelative("m_VectorValue");
            floatValue = baseProperty.FindPropertyRelative("m_FloatValue");
            intValue = baseProperty.FindPropertyRelative("m_IntValue");
            textureValue = baseProperty.FindPropertyRelative("m_TextureValue");
        }
    }
}