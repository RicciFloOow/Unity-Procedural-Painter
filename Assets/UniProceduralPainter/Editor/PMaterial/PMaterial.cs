using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public enum PMatOutputDimension
    {
        Tex2D,
        //Tex3D,//这里还是不提供3D纹理的支持比较好，比较万一手残错误设置纹理的大小，显存直接炸了
        Cube
    }

    [Serializable]
    public struct PMaterialOutputSetting
    {
        public GraphicsFormat OutputFormat;
        public PMatOutputDimension Dimension;
        public Vector2Int TextureSize;

        public int Width
        {
            get
            {
                return Mathf.Max(1, TextureSize.x);
            }
        }

        public int Height
        {
            get
            {
                return Mathf.Max(1, TextureSize.y);
            }
        }

        public Vector2 Scale
        {
            get
            {
                return Width > Height ? new Vector2(1, (float)Width / Height) : new Vector2((float)Height / Width, 1);
            }
        }

        public PMaterialOutputSetting(GraphicsFormat format, PMatOutputDimension dim, Vector2Int size)
        {
            OutputFormat = format;
            Dimension = dim;
            TextureSize = size;
        }
    }


    public class PMaterial : ScriptableObject
    {
        [HideInInspector]
        public PMaterialOutputSetting OutputSetting;

        public virtual bool IsMaterialValid { get; }

        #region ----RT Handle----
        public Texture OutputTexture
        {
            get
            {
                if (m_outputHandle == null)
                {
                    return Texture2D.whiteTexture;
                }
                else
                {
                    return m_outputHandle;
                }
            }
        }

        public RTHandle OutputHandle => m_outputHandle;

        protected RTHandle m_outputHandle;

        public virtual void SetupOutputHandle() { }

        protected virtual void ReleaseOutputHandle()
        {
            m_outputHandle?.Release();
            m_outputHandle = null;
        }

        public virtual void OnRenderingProceduralMaterial() { }
        #endregion

        #region ----Custom Editor----
        public virtual void OnEnterPMaterialEditor() { }
        public virtual void OnExitPMaterialEditor() { }
        #endregion

        #region ----Properties----
        [HideInInspector]
        public List<PMaterialPropertyData> PropertyDataList;

        public IReadOnlyList<PMaterialProperty> PropertyList
        {
            get
            {
                if (m_PropertyList == null)
                {
                    PropertiesDeserialization();
                }
                return m_PropertyList.AsReadOnly();
            }
        }

        protected List<PMaterialProperty> m_PropertyList;

        public void PropertiesSerialization()
        {
            if (PropertyDataList == null)
            {
                PropertyDataList = new List<PMaterialPropertyData>();
            }
            PropertyDataList.Clear();
            if (m_PropertyList != null)
            {
                foreach (var property in m_PropertyList)
                {
                    PropertyDataList.Add(property.ToSerializedData());
                }
            }
        }

        public void PropertiesDeserialization()
        {
            if (m_PropertyList == null)
            {
                m_PropertyList = new List<PMaterialProperty>();
            }
            m_PropertyList.Clear();
            if (PropertyDataList != null)
            {
                foreach (var property in PropertyDataList)
                {
                    m_PropertyList.Add(property.Deserialization());
                }
            }
        }

        public virtual void UpdatePMatProperties() { }
        #endregion

        #region ----Axiom of Foundation(Regularity)----
        //显然，我们不允许PMaterial把自己作为其(RTHandle的)属性，这样会造成死循环
        //因此，更一般的，我们不允许这样的"无穷下降链"存在
        //此外，即使是有限的，我们也必须要设置深度阈值，以防过多的渲染需求
        protected bool ExistInfiniteDescendingSequence(string node, string target, out int depth)
        {
            //我们用PMaterial的路径来判断
            if (node == target && !string.IsNullOrEmpty(node))
            {
                depth = 0;
                return true;
            }
            if (m_PropertyList == null)
            {
                depth = 1;
                return false;
            }
            //
            bool isInfinite = false;
            depth = 1;
            for (int i = 0; i < PropertyDataList.Count; i++)//不用m_PropertyList，用PropertyDataList，只有类型是RTHandle的时候再序列化对应的PMaterial
            {
                var prop = PropertyDataList[i];
                if (prop.Type == PMaterialPropertyType.RTHandle)
                {
                    isInfinite |= ExistInfiniteDescendingSequence(prop.PropertyPathOrValue, target, out int childDepth);
                    depth = Mathf.Max(depth, childDepth + 1);
                }
            }
            return isInfinite;
        }

        //由于我们在PMatPipeline里约定的树的高度就只有2，因此可以更简单一点
        public void CheckChildPMaterial(PMaterial mat, out bool isSelfInfLoop, out bool isChildHasChildren)
        {
            if (mat == this)
            {
                isSelfInfLoop = true;
                isChildHasChildren = true;
                return;
            }
            isSelfInfLoop = false;
            isChildHasChildren = false;
            if (mat.PropertyList == null)
            {
                return;
            }
            for (int i = 0; i < mat.PropertyList.Count; i++)
            {
                var prop = mat.PropertyList[i];
                if (prop.Type == PMaterialPropertyType.RTHandle)
                {
                    isChildHasChildren = true;
                    return;
                }
            }
        }

        #endregion

        #region ----Unity----
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void Reset() { }
        #endregion

        public virtual void ForceSave()
        {
            PropertiesSerialization();
            //
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            //
            //Debug.Log("保存成功!");
        }
    }
#endif
}