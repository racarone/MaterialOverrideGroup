using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using Object = UnityEngine.Object;

namespace MaterialOverrides
{
    [CustomEditor(typeof(MaterialOverrideGroup))]
    class MaterialOverrideGroupEditor : Editor
    {
        MaterialOverrideGroup m_Target;

        SerializedProperty m_ApplyMode;
        SerializedProperty m_ShaderPropertyOverrides;
        SerializedProperty m_MaterialPropertyOverrides;
        SerializedProperty m_Renderers;

        Dictionary<ShaderPropertyOverrideList, ShaderPropertyOverrideListEditor> m_ShaderOverrideEditors = new Dictionary<ShaderPropertyOverrideList, ShaderPropertyOverrideListEditor>();
        EditorPrefBool m_ShaderOverridesFoldout;
        EditorPrefBool m_MaterialOverridesFoldout;

        void OnEnable()
        {
            m_ApplyMode = serializedObject.FindProperty("m_ApplyMode");
            m_ShaderPropertyOverrides = serializedObject.FindProperty("m_ShaderPropertyOverrides");
            m_MaterialPropertyOverrides = serializedObject.FindProperty("m_MaterialPropertyOverrides");
            m_Renderers = serializedObject.FindProperty("m_Renderers");

            m_Target = (MaterialOverrideGroup) target;
            m_Target.ClearOverrides();
            m_Target.Populate();
            m_Target.Apply();
            RefreshEditors();

            m_ShaderOverridesFoldout = new EditorPrefBool($"{typeof(MaterialOverrideGroupEditor)}:ShaderOverridesFoldout", true);
            m_MaterialOverridesFoldout = new EditorPrefBool($"{typeof(MaterialOverrideGroupEditor)}:MaterialOverridesFoldout", true);

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        void OnHierarchyChanged()
        {
            if (!m_Target)
                return;

            m_Target.ClearOverrides();
            m_Target.Populate();
            m_Target.SetDirty();

            RefreshEditors();
        }

        void RefreshEditors()
        {
            m_ShaderOverrideEditors.Clear();

            serializedObject.Update();

            RefreshShaderEditors();
            RefreshMaterialEditors();
        }

        void RefreshShaderEditors()
        {
            for (int i = 0, count = m_ShaderPropertyOverrides.arraySize; i < count; i++)
            {
                var property = m_ShaderPropertyOverrides.GetArrayElementAtIndex(i);
                var overrideList = (ShaderPropertyOverrideList) property.objectReferenceValue;

                if (!m_ShaderOverrideEditors.TryGetValue(overrideList, out var editor))
                {
                    editor = new ShaderPropertyOverrideListEditor(overrideList, property, this);
                    m_ShaderOverrideEditors.Add(overrideList, editor);
                }
            }
        }

        void RefreshMaterialEditors()
        {
            for (int i = 0, count = m_MaterialPropertyOverrides.arraySize; i < count; i++)
            {
                var property = m_MaterialPropertyOverrides.GetArrayElementAtIndex(i);
                var overrideList = (MaterialPropertyOverrideList) property.objectReferenceValue;

                if (!m_ShaderOverrideEditors.TryGetValue(overrideList, out var editor))
                {
                    editor = new ShaderPropertyOverrideListEditor(overrideList, property, this);
                    m_ShaderOverrideEditors.Add(overrideList, editor);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (!m_Target)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Populate"))
                    m_Target.Populate();

                if (GUILayout.Button("Apply"))
                    m_Target.Apply();

                if (GUILayout.Button("Reset"))
                {
                    m_Target.Reset();
                    RefreshEditors();
                    Repaint();
                }
            }

            serializedObject.Update();

            if (m_Target.renderers.Length == 0)
            {
                EditorGUILayout.HelpBox($"{typeof(MaterialOverrideGroup)} contains no renderers.", MessageType.Info);
                return;
            }

            // Find null renderers
            var oldRenderers = new List<Renderer>();
            foreach (var renderer in m_Target.renderers)
            {
                if (renderer == null)
                    continue;

                oldRenderers.Add(renderer);
            }

            // Remove null renderers
            if (oldRenderers.Count != m_Renderers.arraySize)
            {
                m_Renderers.ClearArray();
                foreach (var renderer in oldRenderers)
                {
                    m_Renderers.arraySize += 1;
                    m_Renderers.GetArrayElementAtIndex(m_Renderers.arraySize - 1).objectReferenceValue = (Object) renderer;
                }
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            m_Target.applyMode = (MaterialOverrideGroup.ApplyMode) EditorGUILayout.EnumPopup((MaterialOverrideGroup.ApplyMode) m_Target.applyMode);
            EditorGUILayout.Space();

            CoreEditorUtils.DrawSplitter();
            m_ShaderOverridesFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShaderOverridesFoldout.value, "Shader Overrides");
            if (m_ShaderOverridesFoldout.value)
            {
                var targetShaders = m_Target.shaders;

                for (int i = 0, count = targetShaders.Length; i < count; i++)
                {
                    if (!m_Target.TryGetOverride(targetShaders[i], out var propertyOverrideList))
                        continue;

                    if (!m_ShaderOverrideEditors.TryGetValue(propertyOverrideList, out var editor))
                        continue;

                    // Draw material header
                    CoreEditorUtils.DrawSplitter();
                    bool displayContent = CoreEditorUtils.DrawHeaderToggle(
                        targetShaders[i].name,
                        editor.baseProperty,
                        editor.active,
                        null,
                        () => editor.showHidden,
                        () => editor.showHidden = !editor.showHidden
                    );

                    if (displayContent)
                    {
                        using (new EditorGUI.DisabledScope(!editor.active.boolValue))
                        {
                            editor.OnInspectorGUI();
                        }
                    }
                }

                if (m_Target.shaders.Length == 0)
                    EditorGUILayout.HelpBox($"{typeof(MaterialOverrideGroup)} contains no shaders.", MessageType.Info);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            CoreEditorUtils.DrawSplitter();
            m_MaterialOverridesFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_MaterialOverridesFoldout.value, "Material Overrides");
            if (m_MaterialOverridesFoldout.value)
            {
                var targetMaterials = m_Target.materials;

                // Draw materials
                for (int i = 0, count = targetMaterials.Length; i < count; i++)
                {
                    if (!m_Target.TryGetOverride(targetMaterials[i], out var propertyOverrideList))
                        continue;

                    if (!m_ShaderOverrideEditors.TryGetValue(propertyOverrideList, out var editor))
                        continue;

                    // Draw material header
                    CoreEditorUtils.DrawSplitter();
                    bool displayContent = CoreEditorUtils.DrawHeaderToggle(
                        targetMaterials[i].name,
                        editor.baseProperty,
                        editor.active,
                        null,
                        () => editor.showHidden,
                        () => editor.showHidden = !editor.showHidden
                    );

                    if (displayContent)
                    {
                        using (new EditorGUI.DisabledScope(!editor.active.boolValue))
                        {
                            editor.OnInspectorGUI();
                        }
                    }
                }

                if (m_Target.materials.Length == 0)
                    EditorGUILayout.HelpBox($"{typeof(MaterialOverrideGroup)} contains no materials.", MessageType.Info);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                m_Target.SetDirty();
            }
        }
    }
}