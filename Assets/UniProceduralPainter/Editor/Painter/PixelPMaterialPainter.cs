using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public partial class PixelPMaterialPainter : EditorWindow
    {
        #region ----Command----
        public static void InitWindow()
        {
            CurrentWin = GetWindowWithRect<PixelPMaterialPainter>(new Rect(0, 0, k_GUI_WindowWidth, k_GUI_WindowHeight));
            CurrentWin.titleContent = new GUIContent("PMatPainter", "Pixel PMaterial Painter");
            CurrentWin.Focus();
        }

        [OnOpenAsset()]
        public static bool OpenWindowFromAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            Type assetType = AssetDatabase.GetImporterType(assetPath);
            //
            if (assetType == typeof(AssetImporter))
            {
                Type uniAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (uniAssetType == typeof(PixelPMaterial))
                {
                    s_selectPMat = AssetDatabase.LoadAssetAtPath<PixelPMaterial>(assetPath);
                    PMatPipeline.Instance.BindingRenderingMaterial(s_selectPMat);
                    s_selectPMat.UpdatePMatProperties();
                    InitWindow();
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region ----Static Properties----
        public static PixelPMaterialPainter CurrentWin { get; private set; }

        private static PixelPMaterial s_selectPMat;
        #endregion

        #region ----GUI Constants----
        private const float k_GUI_WindowWidth = 1580;
        private const float k_GUI_WindowHeight = 730;
        private const float k_GUI_EditPanel_ScrollViewWidth = 290;

        private const int k_GUI_PreviewTex_Width = 1280;
        private const int k_GUI_PreviewTex_Height = 720;
        #endregion

        #region ----GUI Properties----
        private bool m_RequireRepaint;
        #endregion

        #region ----Unity----
        private void OnEnable()
        {
            SetupPreviewRTHandle();
            SetupPreviewMaterial();
            m_IsHoldingScrollWheel = false;
            m_RequireRepaint = true;
            m_Translation = Vector2.zero;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            DrawSetting();
            DrawPreviewView(out Rect previewRect);
            GUILayout.EndHorizontal();
            //
            if (m_RequireRepaint)
            {
                OnRenderingPreview();
                m_RequireRepaint = false;
                Repaint();
            }
            //
            OnControlPreviewCamera(previewRect);
        }

        private void OnDisable()
        {
            PMatPipeline.Instance.UnbindingRenderingMaterial();
            s_selectPMat.ForceSave();
            ReleasePreviewRTHandle();
            ReleasePreviewMaterial();
        }
        #endregion
    }
#endif
}