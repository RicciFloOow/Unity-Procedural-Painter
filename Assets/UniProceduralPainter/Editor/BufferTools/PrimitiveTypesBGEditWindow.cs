using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public class PrimitiveTypesBGEditWindow : EditorWindow
    {
        #region ----Command----
        public static void InitWindow()
        {
            CurrentWin = GetWindowWithRect<PrimitiveTypesBGEditWindow>(new Rect(0, 0, k_GUI_WindowWidth, k_GUI_WindowHeight));
            CurrentWin.titleContent = new GUIContent("Primitive Data Type Buffer Data Generator", "用于将基础数据类型的数据转换为可用于PMat中ComputeBuffer数据源的工具");
            CurrentWin.Focus();
        }

        [MenuItem("UniPMaterial/BufferDataGenerator/PrimitiveDataType")]
        public static void OpenWindowFromMenu()
        {
            InitWindow();
        }
        #endregion

        #region ----Static Properties----
        public static PrimitiveTypesBGEditWindow CurrentWin { get; private set; }
        #endregion

        #region ----GUI Properties----
        public enum DataType
        {
            Float,
            Int,
            Uint
        }

        private Vector2 m_ScrollPosition;
        private DataType m_SourceDataType;
        private bool m_UseBinaryFile;
        private string m_ExportName;
        private string m_ExportPath;
        private string m_InputText;
        #endregion

        #region ----GUI Constants----
        private const float k_GUI_WindowWidth = 460;
        private const float k_GUI_WindowHeight = 300;
        private const float k_GUI_EditPanel_ScrollViewWidth = 450;

        private const string k_Default_ExportPath = "Assets/ExportRes/BufferData";
        private const string k_Default_ExportName = "PrimitiveTypesBufferData";
        #endregion

        #region ----Generator----
        private bool TryGenerateBufferData()
        {
            if (string.IsNullOrEmpty(m_InputText))
            {
                return false;
            }
            string[] splits = m_InputText.Split(new[] { ",", "，", " ", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<uint> data = new List<uint>(splits.Length);
            switch (m_SourceDataType)
            {
                case DataType.Float:
                    for (int i = 0; i < splits.Length; i++)
                    {
                        if (float.TryParse(splits[i], out float v))
                        {
                            data.Add(TypeConvertUtility.FloatToUint(v));
                        }
                    }
                    break;
                case DataType.Int:
                    for (int i = 0; i < splits.Length; i++)
                    {
                        if (int.TryParse(splits[i], out int v))
                        {
                            data.Add((uint)v);
                        }
                    }
                    break;
                case DataType.Uint:
                    for (int i = 0; i < splits.Length; i++)
                    {
                        if (uint.TryParse(splits[i], out uint v))
                        {
                            data.Add(v);
                        }
                    }
                    break;
            }
            if (m_UseBinaryFile)
            {
                byte[] output = new byte[data.Count * sizeof(uint)];
                uint[] dataArray = data.ToArray();
                CopyUtility.CopyUintArrayToByteArray(ref dataArray, ref output);
                //
                EditorFileUtility.ExportBytesToBinFile(output, ref m_ExportPath, ref m_ExportName, k_Default_ExportPath + "/Binary", k_Default_ExportName);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Count; i++)
                {
                    sb.Append(data[i].ToString());
                    if (i < data.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                string output = sb.ToString();
                EditorFileUtility.ExportStringToTextFile(output, ref m_ExportPath, ref m_ExportName, k_Default_ExportPath + "/Text", k_Default_ExportName);
            }
            return true;
        }
        #endregion

        #region ----GUI----
        private void DrawPanel()
        {
            GUILayout.Space(5);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(k_GUI_EditPanel_ScrollViewWidth));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            //
            {
                m_SourceDataType = (DataType)EditorGUILayout.EnumPopup("原始数据类型: ", m_SourceDataType);
                m_UseBinaryFile = EditorGUILayout.Toggle("输出二进制文件: ", m_UseBinaryFile);
                m_ExportName = EditorGUILayout.TextField("文件名: ", m_ExportName);
                m_ExportPath = EditorGUILayout.TextField("导出路径: ", m_ExportPath);
            }
            {
                if (GUILayout.Button("导出"))
                {
                    if (!TryGenerateBufferData())
                    {
                        Debug.Log("输入的数据不能为空!");
                    }
                }
            }
            EditorGUILayout.EndVertical();
            //
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                //
                EditorGUILayout.LabelField("Buffer Data: ");
                m_InputText = EditorGUILayout.TextArea(m_InputText);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            GUILayout.Space(5);
        }
        #endregion

        #region ----Unity----
        private void OnEnable()
        {
            m_InputText = string.Empty;
        }

        private void OnGUI()
        {
            DrawPanel();
        }

        private void OnDisable()
        {
            
        }
        #endregion
    }
#endif
}