using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public class PMatPipeline
    {
        #region ----Singleton----
        private static PMatPipeline instance;
        public static PMatPipeline Instance => instance ??= new PMatPipeline();
        #endregion

        #region ----Tree----
        //由于水平有限，我们这里允许的树的高度就为2
        //假设我们限定了树的高度为8，树的宽度的上限为16(即使是sampler的话也能有16个)，那么如果我们需要修改根节点的某一个RTHandle的属性时，瞬间的开销可能会非常非常恐怖，在没有好办法限制之前我们还是限制允许的高度为2比较简单(对绝大多数情况应该是足够的了)
        private PMaterial m_RootPMat;
        private List<PMaterial> m_ChildrenPMats;

        public void BindingRenderingMaterial(PMaterial mat)
        {
            m_ChildrenPMats ??= new List<PMaterial>();
            m_ChildrenPMats.Clear();
            //
            m_RootPMat = mat;
            m_RootPMat.PropertiesDeserialization();
            m_RootPMat.OnEnterPMaterialEditor();
            //
            for (int i = 0; i < m_RootPMat.PropertyList.Count; i++)
            {
                var prop = m_RootPMat.PropertyList[i];
                if (prop.ValuePMaterial != null)
                {
                    m_ChildrenPMats.Add(prop.ValuePMaterial);
                    prop.ValuePMaterial.PropertiesDeserialization();
                    prop.ValuePMaterial.OnEnterPMaterialEditor();
                }
            }
        }

        public void UpdateBindingRenderingMaterial()
        {
            //更新m_RootPMat下绑定的PMat
            //懒人办法，全检查一遍
            for (int i = 0; i < m_RootPMat.PropertyList.Count; i++)
            {
                var prop = m_RootPMat.PropertyList[i];
                var pMat = prop.ValuePMaterial;
                if (pMat != null)
                {
                    bool contains = m_ChildrenPMats.Contains(pMat);
                    if (contains && prop.Type == PMaterialPropertyType.Texture)
                    {
                        m_ChildrenPMats.Remove(pMat);
                        pMat.PropertiesSerialization();
                        pMat.OnExitPMaterialEditor();
                    }
                    else if (!contains && prop.Type == PMaterialPropertyType.RTHandle)
                    {
                        m_ChildrenPMats.Add(pMat);
                        pMat.PropertiesDeserialization();
                        pMat.OnEnterPMaterialEditor();
                    }
                }
            }
        }

        public void UnbindingRenderingMaterial()
        {
            m_RootPMat?.PropertiesSerialization();
            m_RootPMat?.OnExitPMaterialEditor();
            if (m_ChildrenPMats != null)
            {
                for (int i = 0; i < m_ChildrenPMats.Count; i++)
                {
                    m_ChildrenPMats[i].PropertiesSerialization();
                    m_ChildrenPMats[i].OnExitPMaterialEditor();
                }
            }
            //
            m_RootPMat = null;
            m_ChildrenPMats?.Clear();
        }

        public void OnPipelineRendering()
        {
            //先子节点再父节点
            for (int i = 0; i < m_ChildrenPMats.Count; i++)
            {
                m_ChildrenPMats[i].OnRenderingProceduralMaterial();
            }
            //
            m_RootPMat.OnRenderingProceduralMaterial();
        }
        #endregion
    }
#endif
}