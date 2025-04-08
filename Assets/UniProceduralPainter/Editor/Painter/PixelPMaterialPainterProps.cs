using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public enum PMatTextureSourceType
    {
        Texture = 4,
        RTHandle = 5
    }

    public partial class PixelPMaterialPainter : EditorWindow
    {
        #region ----MatProps GUI Properties----
        private Vector2 m_ScrollPosition;
        private string m_ExportName;
        private string m_ExportPath;
        #endregion

        #region ----Text GUI Constants----
        //ref: https://www.rapidtables.com/web/color/RGB_Color.html
        private const string k_Text_ColorDarkVioletTag = "<color=#9400D3>";
        private const string k_Text_ColorEndTag = "</color>";
        #endregion

        #region ----Properties GUI----
        private string GetWarningErrorString(string str)
        {
            return $"{k_Text_ColorDarkVioletTag}{str}{k_Text_ColorEndTag}";
        }

        private void DrawPMatPropertyInt(PMaterialProperty prop)
        {
            prop.ValueInt = EditorGUILayout.IntField(prop.Description, prop.ValueInt);
        }

        private void DrawPMatPropertyFloat(PMaterialProperty prop)
        {
            prop.ValueFloat = EditorGUILayout.FloatField(prop.Description, prop.ValueFloat);
        }

        private void DrawPMatPropertyVector(PMaterialProperty prop)
        {
            prop.ValueVector = EditorGUILayout.Vector4Field(prop.Description, prop.ValueVector);
        }

        private void DrawPMatPropertyColor(PMaterialProperty prop)
        {
            prop.ValueColor = EditorGUILayout.ColorField(prop.Description, prop.ValueColor);
        }

        private void DrawPMatPropertyTexture(PMaterialProperty prop)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            //绘制
            prop.TextureType = (PMaterialPropertyType)EditorGUILayout.EnumPopup(new GUIContent("纹理类型: ", "Texture: Tex2D或是Cubemap; RTHandle: PMaterial渲染的结果"), (PMatTextureSourceType)((int)prop.Type));
            //
            if (prop.Type == PMaterialPropertyType.Texture)
            {
                prop.ValueTexture = EditorGUILayout.ObjectField(prop.Description, prop.ValueTexture, typeof(Texture), false) as Texture;
            }
            else if (prop.Type == PMaterialPropertyType.RTHandle)
            {
                EditorGUI.BeginChangeCheck();
                prop.TempChildPMat = EditorGUILayout.ObjectField(prop.Description, prop.TempChildPMat, typeof(PMaterial), false) as PMaterial;
                if (EditorGUI.EndChangeCheck())
                {
                    s_selectPMat.CheckChildPMaterial(prop.TempChildPMat, out bool isSelfInfLoop, out bool isChildHasChildren);
                    if (isSelfInfLoop)
                    {
                        Debug.LogWarning("禁止使用自己作为材质属性的纹理!会造成死循环!");
                        prop.TempChildPMat = prop.ValuePMaterial;
                    }
                    else if (isChildHasChildren)
                    {
                        Debug.LogWarning("禁止使用含有RTHandle属性的PMat作为RTHandle的值!");
                        prop.TempChildPMat = prop.ValuePMaterial;
                    }
                    else
                    {
                        prop.SetRTHandleValue(prop.TempChildPMat);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPMatPropertyBuffer(PMaterialProperty prop)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            prop.TempBufferDataAsset = EditorGUILayout.ObjectField(prop.Description, prop.TempBufferDataAsset, typeof(TextAsset), false) as TextAsset;
            prop.IsBufferSourceBinary = EditorGUILayout.Toggle(new GUIContent("是二进制文件", "文件格式有一定的规范, 因此应该通过提供的工具来生成"), prop.IsBufferSourceBinary);
            if (EditorGUI.EndChangeCheck())
            {
                //TODO:预检查数据是否合规
                prop.SetComputeBuffer(prop.TempBufferDataAsset, prop.IsBufferSourceBinary);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPMatProperty(PMaterialProperty prop)
        {
            switch (prop.Type)
            {
                case PMaterialPropertyType.Int:
                    DrawPMatPropertyInt(prop);
                    break;
                case PMaterialPropertyType.Float:
                    DrawPMatPropertyFloat(prop);
                    break;
                case PMaterialPropertyType.Vector:
                    DrawPMatPropertyVector(prop);
                    break;
                case PMaterialPropertyType.Color:
                    DrawPMatPropertyColor(prop);
                    break;
                case PMaterialPropertyType.Texture:
                case PMaterialPropertyType.RTHandle:
                    DrawPMatPropertyTexture(prop);
                    break;
                case PMaterialPropertyType.ComputeBuffer:
                    DrawPMatPropertyBuffer(prop);
                    break;
            }
        }

        private void DrawPMatOutputSetting()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("【输出设置】");
            GUILayout.Space(5);
            s_selectPMat.OutputSetting.OutputFormat = (GraphicsFormat)EditorGUILayout.EnumPopup(new GUIContent("输出纹理格式: "), s_selectPMat.OutputSetting.OutputFormat);
            s_selectPMat.OutputSetting.Dimension = (PMatOutputDimension)EditorGUILayout.EnumPopup(new GUIContent("纹理维度: ", "暂不支持Tex3D"), s_selectPMat.OutputSetting.Dimension);
            s_selectPMat.OutputSetting.TextureSize = EditorGUILayout.Vector2IntField("纹理尺寸: ", s_selectPMat.OutputSetting.TextureSize);
            //
            if (GUILayout.Button(new GUIContent("应用", "只有应用了才能真正修改输出的纹理设置")))
            {
                s_selectPMat.SetupOutputHandle();
                m_RequireRepaint = true;
            }
            GUILayout.Space(5);
            m_ExportName = EditorGUILayout.TextField(new GUIContent("文件名: ", "默认为PMat的名字+Output"), m_ExportName);
            GUILayout.Space(5);
            m_ExportPath = EditorGUILayout.TextField(new GUIContent("导出路径: ", "默认与PMat同路径"), m_ExportPath);
            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent("导出纹理")))
            {
                //暂不提供压缩格式的方案, 因为import的时候会被二压
                string pathName = AssetDatabase.GetAssetPath(s_selectPMat);
                int lastSlashIndex = pathName.LastIndexOf('/');
                string folderPath = pathName.Substring(0, lastSlashIndex);
                string filenameWithExtension = pathName.Substring(lastSlashIndex + 1);
                int lastDotIndex = filenameWithExtension.LastIndexOf('.');
                string filenameWithoutExtension = (lastDotIndex > 0) ? filenameWithExtension.Substring(0, lastDotIndex) : filenameWithExtension;
                filenameWithoutExtension += "_Output";
                //
                EditorFileUtility.AutoExportRTHandle(s_selectPMat.OutputHandle, ref m_ExportName, ref m_ExportPath, folderPath, filenameWithoutExtension);
            }
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("【预览设置】");
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            m_previewSettings.BackgroundColor = EditorGUILayout.ColorField("Preview BG Color: ", m_previewSettings.BackgroundColor);
            float psMin = 720f / Mathf.Max(s_selectPMat.OutputSetting.Width, s_selectPMat.OutputSetting.Height * 16 / 9f);
            float psMax = Mathf.Max(s_selectPMat.OutputSetting.Width / 128f, s_selectPMat.OutputSetting.Height / 72f);
            m_previewSettings.MinScale = psMin;
            m_previewSettings.MaxScale = psMax;
            m_previewSettings.Scale = EditorGUILayout.Slider("Preview Scale: ", m_previewSettings.Scale, psMin, psMax);
            if (EditorGUI.EndChangeCheck())
            {
                m_RequireRepaint = true;
            }
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawSetting()
        {
            GUILayout.Space(5);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(k_GUI_EditPanel_ScrollViewWidth));
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Shader: ", s_selectPMat.Shader, typeof(Shader), false);//只是方便用户选中以编辑shader
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            //
            DrawPMatOutputSetting();
            //
            DrawPreviewSettings();
            //
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (s_selectPMat.IsMaterialValid)
            {
                //Properties
                EditorGUI.BeginDisabledGroup(ShaderUtil.anythingCompiling);//编译时禁止修改
                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < s_selectPMat.PropertyList.Count; i++)
                {
                    DrawPMatProperty(s_selectPMat.PropertyList[i]);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_RequireRepaint = true;
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUILayout.Label(GetWarningErrorString("当前Pixel Procedural Material无效: " + ((PixelPMaterial)s_selectPMat).State), EditorGUIUtility.RichTextLabel);
            }
            EditorGUILayout.EndVertical();
            //
            EditorGUILayout.EndScrollView();
        }
        #endregion
    }
#endif
}