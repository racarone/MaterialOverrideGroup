using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialOverrides
{
    class ShaderPropertyOverrideListEditor
    {
        static class Styles
        {
            public static GUIContent overrideSettingText { get; } = EditorGUIUtility.TrTextContent("", "Override this property.");
            public static GUIContent allText { get; } = EditorGUIUtility.TrTextContent("ALL", "Toggle to show all properties.");
            public static GUIContent activeText { get; } = EditorGUIUtility.TrTextContent("ACTIVE", "Toggle to only show overriden properties.");
        }

        public ShaderPropertyOverrideList target { get; }
        public Material material { get; }
        public SerializedObject serializedObject { get; }
        public SerializedProperty baseProperty { get; }
        public SerializedProperty active { get; }
        public SerializedProperty overrides { get; }
        
        public bool showHidden
        {
            get => overrides.isExpanded;
            set => overrides.isExpanded = value;
        }

        public bool showActive
        {
            get => active.isExpanded;
            set => active.isExpanded = value;
        }

        public bool showAll
        {
            get => !active.isExpanded;
            set => active.isExpanded = !value;
        }

        // Reference to the parent editor in the inspector
        Editor m_BaseEditor;

        public ShaderPropertyOverrideListEditor(ShaderPropertyOverrideList target, SerializedProperty property, Editor baseEditor,  Material material = null)
        {
            this.target = target;
            this.material = material;
            serializedObject = new SerializedObject(target);
            baseProperty = property;
            active = serializedObject.FindProperty("m_Active");
            overrides = serializedObject.FindProperty("m_Overrides");
            m_BaseEditor = baseEditor;
        }

        public void Repaint()
        {
            if (m_BaseEditor != null) // Can happen in tests.
                m_BaseEditor.Repaint();

#if UNITY_2020_2_OR_NEWER
            SettingsService.RepaintAllSettingsWindow();
#endif
        }

        public void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUILayout.VerticalScope())
            {
                DrawTopRow();
                EditorGUILayout.Space();

                // Draw list of override properties
                if (Selection.gameObjects.Length > 1)
                    EditorGUILayout.HelpBox("Multi editing not supported", MessageType.Info);
                else
                {
                    DrawProperties(overrides, showAll, showHidden, serializedObject.targetObject);
                }

                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawTopRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                showAll = GUILayout.Toggle(showAll, Styles.allText, CoreEditorStyles.miniLabelButton, GUILayout.ExpandWidth(false));
                showActive = GUILayout.Toggle(showActive, Styles.activeText, CoreEditorStyles.miniLabelButton, GUILayout.ExpandWidth(false));
            }
        }

        static void DrawHeader(GUIContent header)
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
            EditorGUI.LabelField(rect, header, EditorStyles.boldLabel);
            CoreEditorUtils.DrawSplitter();
        }

        static void DrawProperties(SerializedProperty overridesProperty, bool showAll, bool showHidden, Object target)
        {
            var singleLineHeightLayout = GUILayout.Height(EditorGUIUtility.singleLineHeight);

            for (int i = 0, count = overridesProperty.arraySize; i < count; ++i)
            {
                var property = new SerializedShaderPropertyOverride(overridesProperty.GetArrayElementAtIndex(i));
                if (!showAll && !property.overrideState.boolValue)
                    continue;
                
                var flags = (ShaderPropertyFlags) property.propertyInfo.flags.intValue;
                if (!showHidden && flags.HasFlag(ShaderPropertyFlags.HideInInspector))
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawOverrideCheckbox(property.overrideState);

                    // Property
                    using (new EditorGUI.DisabledScope(!property.overrideState.boolValue))
                    {
                        // Draw an overridden property. Offer change of value
                        Undo.RecordObject(target, "Shader Property Override");

                        var title = EditorGUIUtility.TrTextContent(property.propertyInfo.displayName.stringValue, property.propertyInfo.name.stringValue);
                        var type = (ShaderPropertyType) property.propertyInfo.type.enumValueIndex;

                        switch (type)
                        {
                            case ShaderPropertyType.Color:
                                var hdr = flags.HasFlag(ShaderPropertyFlags.HDR);
                                property.colorValue.colorValue = EditorGUILayout.ColorField(title, property.colorValue.colorValue, false, true, hdr);
                                break;
                            case ShaderPropertyType.Float:
                                property.floatValue.floatValue = EditorGUILayout.FloatField(title, property.floatValue.floatValue);
                                break;
                            case ShaderPropertyType.Range:
                                var range = property.propertyInfo.range.vector2Value;
                                property.floatValue.floatValue = EditorGUILayout.Slider(title, property.floatValue.floatValue, range.x, range.y);
                                break;
                            case ShaderPropertyType.Vector:
                                property.vectorValue.vector4Value = EditorGUILayout.Vector4Field(title, property.vectorValue.vector4Value);
                                break;
                            case ShaderPropertyType.Texture:
                                property.textureValue.objectReferenceValue = (Texture) EditorGUILayout.ObjectField(title, property.textureValue.objectReferenceValue, typeof(Texture), false, singleLineHeightLayout);
                                break;
                        }
                    }
                }
            }
        }

        static void DrawOverrideCheckbox(SerializedProperty overrideState)
        {
            // Create a rect the height + vspacing of the property that is being overriden
            var height = EditorGUI.GetPropertyHeight(overrideState) + EditorGUIUtility.standardVerticalSpacing;
            var overrideRect = GUILayoutUtility.GetRect(new GUIContent("ALL"), CoreEditorStyles.miniLabelButton, GUILayout.Height(height), GUILayout.ExpandWidth(false));

            // also center vertically the checkbox
            var overrideToggleSize = CoreEditorStyles.smallTickbox.CalcSize(Styles.overrideSettingText);
            overrideRect.yMin += height * 0.5f - overrideToggleSize.y * 0.5f;
            overrideRect.xMin += overrideToggleSize.x * 0.5f;

            overrideState.boolValue = GUI.Toggle(overrideRect, overrideState.boolValue, Styles.overrideSettingText, CoreEditorStyles.smallTickbox);
        }
    }
}