using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public static class EditorFileUtility
    {

        #region ----Texture----
        private static bool IsGraphicsFormatNeedToExportAsExr(GraphicsFormat format)
        {
            switch (format)
            {
                case GraphicsFormat.R32G32B32A32_SFloat:
                case GraphicsFormat.R16G16B16A16_SFloat:
                case GraphicsFormat.R16G16B16A16_UInt:
                case GraphicsFormat.B10G11R11_UFloatPack32:
                case GraphicsFormat.R32G32_SFloat:
                case GraphicsFormat.R16G16_SFloat:
                case GraphicsFormat.R16G16_UInt:
                case GraphicsFormat.R32_SFloat:
                case GraphicsFormat.R16_SFloat:
                case GraphicsFormat.R16_UInt:
                    return true;
            }
            return false;
        }

        public static void ExportTextureToPNG(Texture2D texture, ref string exportPath, ref string exportName, string defaultExportPath, string defaultExportName)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = defaultExportPath;
            }
            if (string.IsNullOrEmpty(exportName))
            {
                exportName = defaultExportName;
            }
            //
            System.IO.Directory.CreateDirectory(exportPath);
            string filePath = exportPath + "/" + exportName + EditorConstantsUtil.FILE_EXTENSION_PNG;
            System.IO.File.WriteAllBytes(filePath, texture.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log("成功导出至:" + filePath);
        }

        public static void ExportTextureToEXR(Texture2D texture, ref string exportPath, ref string exportName, string defaultExportPath, string defaultExportName, bool useFloat32 = false)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = defaultExportPath;
            }
            if (string.IsNullOrEmpty(exportName))
            {
                exportName = defaultExportName;
            }
            //
            System.IO.Directory.CreateDirectory(exportPath);
            string filePath = exportPath + "/" + exportName + EditorConstantsUtil.FILE_EXTENSION_EXR;
            System.IO.File.WriteAllBytes(filePath, texture.EncodeToEXR(useFloat32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
            AssetDatabase.Refresh();
            Debug.Log("成功导出至:" + filePath);
        }

        public static void AutoExportRTHandle(RTHandle src, ref string path, ref string name, string defaultExportPath, string defaultExportName)
        {
            //TODO: 处理cubemap
            //TODO: 用更好的回读方案
            Texture2D tex = new Texture2D(src.Width, src.Height, src.Format, TextureCreationFlags.None);
            RenderTexture.active = src;
            tex.ReadPixels(new Rect(0, 0, src.Width, src.Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            if (IsGraphicsFormatNeedToExportAsExr(src.Format))
            {
                ExportTextureToEXR(tex, ref path, ref name, defaultExportPath, defaultExportName, true);
            }
            else
            {
                ExportTextureToPNG(tex, ref path, ref name, defaultExportPath, defaultExportName);
            }
            tex = null;
        }
        #endregion
    }
#endif
}