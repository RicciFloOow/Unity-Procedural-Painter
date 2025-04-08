using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public enum PreviewUpdateFrequency
    {
        High,
        Low
    }

    public partial class PixelPMaterialPainter : EditorWindow
    {
        #region ----Preview GUI Setting----
        public struct PreviewHandleSettings
        {
            public Color BackgroundColor;
            public float Scale;
            public float MinScale;
            public float MaxScale;
            //TODO:PreviewUpdateFrequency
        }
        private PreviewHandleSettings m_previewSettings;
        #endregion

        #region ----Preview RTHandle----
        private RTHandle m_previewHandle;

        private void SetupPreviewRTHandle()
        {
            m_previewHandle = new RTHandle(k_GUI_PreviewTex_Width, k_GUI_PreviewTex_Height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        }

        private void ReleasePreviewRTHandle()
        {
            m_previewHandle?.Release();
            m_previewHandle = null;
        }
        #endregion

        #region ----Preview Material----
        private Material m_previewMaterial;

        private void SetupPreviewMaterial()
        {
            m_previewMaterial = new Material(Shader.Find("UniPMaterial/Editor/PMatPreview"));
        }

        private void ReleasePreviewMaterial()
        {
            if (m_previewMaterial != null)
            {
                DestroyImmediate(m_previewMaterial);
            }
        }
        #endregion

        #region ----Preview GUI----
        private void DrawPreviewView(out Rect previewRect)
        {
            //
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(k_GUI_PreviewTex_Width + 10), GUILayout.MaxHeight(k_GUI_PreviewTex_Height + 10));
            previewRect = GUILayoutUtility.GetRect(k_GUI_PreviewTex_Width, k_GUI_PreviewTex_Height, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(previewRect, m_previewHandle, null, ScaleMode.StretchToFill);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region ----Preview User Operation----
        private float m_ZoomVelocity;
        private bool m_IsHoldingScrollWheel;
        private Vector2 m_LastFrameMousePosition;
        private Vector2 m_Translation;

        private void OnControlPreviewCamera(Rect previewRect)
        {
            Vector2 mousePosition = Event.current.mousePosition;
            bool isCursorInRect = previewRect.Contains(mousePosition);
            //zoom in/out
            if (Event.current.type == EventType.ScrollWheel && isCursorInRect)
            {
                ZoomAt(-Event.current.delta.y > 0 ? 0.02f : -0.02f);
            }
            //
            if (Event.current.button == 2)
            {
                //鼠标中键
                if (Event.current.type == EventType.MouseDown)
                {
                    m_IsHoldingScrollWheel = true;
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    m_IsHoldingScrollWheel = false;
                }
                if (m_IsHoldingScrollWheel)
                {
                    Vector2 offset = mousePosition - m_LastFrameMousePosition;
                    m_Translation += offset * (m_previewSettings.MinScale / m_previewSettings.Scale);
                    m_LastFrameMousePosition = mousePosition;
                    m_RequireRepaint = true;
                }
                else
                {
                    m_LastFrameMousePosition = mousePosition;
                }
            }
        }

        private void ZoomAt(float delta)
        {
            m_previewSettings.Scale = Mathf.SmoothDamp(m_previewSettings.Scale, Mathf.Clamp(m_previewSettings.Scale + delta, m_previewSettings.MinScale, m_previewSettings.MaxScale), ref m_ZoomVelocity, 0.08f);
            m_RequireRepaint = true;
        }
        #endregion

        #region ----Preview Shader Helper----
        private static readonly int k_ShaderProperty_Float_PreviewScale = Shader.PropertyToID("_PreviewScale");
        private static readonly int k_ShaderProperty_Vector_PreviewTranslation = Shader.PropertyToID("_PreviewTranslation");
        private static readonly int k_ShaderProperty_Vector_TargetOutputScale = Shader.PropertyToID("_TargetOutputScale");
        private static readonly int k_ShaderProperty_Color_PreviewBGColor = Shader.PropertyToID("_PreviewBGColor");
        private static readonly int k_ShaderProperty_Tex_PMatOutputHandle = Shader.PropertyToID("_PMatOutputHandle");
        #endregion

        #region ----Rendering Preview----
        //TODO: 区分高频低频更新
        //TODO: 提供更好的Cubemap的预览

        private void OnRenderingPreview()
        {
            //TODO: 区分目标是2D的还是cubemap的
            PMatPipeline.Instance.OnPipelineRendering();
            //
            CommandBuffer cmd = new CommandBuffer()
            {
                name = "Procedural Material Preview Pass"
            };
            {
                cmd.SetRenderTarget(m_previewHandle);
                MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();
                matPropertyBlock.SetFloat(k_ShaderProperty_Float_PreviewScale, m_previewSettings.Scale);
                matPropertyBlock.SetVector(k_ShaderProperty_Vector_PreviewTranslation, new Vector2(m_Translation.x / 1280f, m_Translation.y / 720f));
                matPropertyBlock.SetVector(k_ShaderProperty_Vector_TargetOutputScale, s_selectPMat.OutputSetting.Scale);
                matPropertyBlock.SetColor(k_ShaderProperty_Color_PreviewBGColor, m_previewSettings.BackgroundColor);
                matPropertyBlock.SetTexture(k_ShaderProperty_Tex_PMatOutputHandle, s_selectPMat.OutputTexture);
                //TODO: 更多的混合模式+单通道查看
                cmd.DrawProcedural(Matrix4x4.identity, m_previewMaterial, 0, MeshTopology.Triangles, 3, 1, matPropertyBlock);
            }
            Graphics.ExecuteCommandBuffer(cmd);
        }
        #endregion
    }
#endif
}