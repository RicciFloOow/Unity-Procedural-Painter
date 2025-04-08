using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using UnityEngine;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public class PixelPMaterialCreator : UnityEditor.Editor
    {
        internal const string K_PixelPMatTemplatePath = "Assets/UniProceduralPainter/Editor/TemplateHelper/TemplateRes/NewPixelPMatShader.txt";

        [MenuItem(EditorConstantsUtil.MENU_CREAT_PIXELPMAT, false, EditorConstantsUtil.MENU_UNIPMAT_ORDER)]
        public static void CreatePixelPMaterial()
        {
            //创建PixelPMaterial, 对应的Shader在确认名字后创建
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            CreateInstance<PixelPMaterialCreator_Asset>(),
            GetSelectedPathOrFallback() + "/New PixelPMat.asset",
            (Texture2D)UnityEditor.EditorGUIUtility.Load(EditorConstantsUtil.EDIT_ICON_PMAT),//注意，在这里EditorGUIUtility.FindTexture()并不能获取到built-in editor的纹理
            K_PixelPMatTemplatePath);
        }

        internal static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }

        internal class PixelPMaterialCreator_Asset : EndNameEditAction
        {
            internal static UnityEngine.Object CreatePixelProceduralMaterial(string pathName, string resourceFile)
            {
                string fileName = Path.GetFileNameWithoutExtension(pathName);
                //读取shader模板
                StreamReader reader = new StreamReader(resourceFile);
                string shaderText = reader.ReadToEnd();
                reader.Close();
                //
                shaderText = Regex.Replace(shaderText, "Hidden/New PixelPMat", "UniPMaterial/Custom/" + fileName);
                //写入文件
                UTF8Encoding encoding = new UTF8Encoding(true, false);
                string shaderPath = pathName.Substring(0, pathName.Length - 6) + "_PS.shader";//一定以.asset结尾
                string fullShaderPath = Path.GetFullPath(shaderPath);
                StreamWriter writer = new StreamWriter(fullShaderPath, false, encoding);
                writer.Write(shaderText);
                writer.Close();
                AssetDatabase.ImportAsset(shaderPath);
                //
                PixelPMaterial pMat = ScriptableObject.CreateInstance<PixelPMaterial>();
                AssetDatabase.CreateAsset(pMat, pathName);
                AssetDatabase.ImportAsset(pathName);
                pMat.Shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                pMat.OutputSetting = new PMaterialOutputSetting(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, PMatOutputDimension.Tex2D, new Vector2Int(1024, 1024));
                pMat.ForceSave();
                return pMat;
            }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                UnityEngine.Object o = CreatePixelProceduralMaterial(pathName, resourceFile);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }
    }
#endif
}