using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public enum PMaterialPropertyType
    {
        Int,
        Float,
        Vector,
        Color,
        //Matrix,//GUI未提供对应的接口, 就不允许传入了
        Texture,//TODO: 利用ShaderUtil.GetTexDim()区分2D, 3D, Cubemap
        RTHandle,
        ComputeBuffer//允许通过TextAsset作为数据源, 当然, 需要提供stride
    }

    public class PMaterialProperty : IDisposable
    {
        private string m_Name;

        private string m_Description;

        private object m_Value;

        private PMaterialPropertyType m_Type;

        #region ----Property----
        public string Name => m_Name;
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }
        public PMaterialPropertyType Type => m_Type;

        public PMaterialPropertyType TextureType
        {
            set
            {
                if (value == PMaterialPropertyType.Texture || value == PMaterialPropertyType.RTHandle)
                {
                    m_Type = value;
                }
            }
        }

        public int ValueInt
        {
            get
            {
                if (m_Type == PMaterialPropertyType.Int)
                {
                    if (m_Value == null)
                    {
                        return 0;
                    }
                    return (int)m_Value;
                }
                return 0;
            }
            set
            {
                if (m_Type == PMaterialPropertyType.Int)
                {
                    m_Value = value;
                }
            }
        }

        public float ValueFloat
        {
            get
            {
                if (m_Type == PMaterialPropertyType.Float)
                {
                    if (m_Value == null)
                    {
                        return 0;
                    }
                    return (float)m_Value;
                }
                return 0f;
            }
            set
            {
                if (m_Type == PMaterialPropertyType.Float)
                {
                    m_Value = value;
                }
            }
        }

        public Vector4 ValueVector
        {
            get
            {
                if (m_Type == PMaterialPropertyType.Vector)
                {
                    if (m_Value == null)
                    {
                        return Vector4.zero;
                    }
                    return (Vector4)m_Value;
                }
                return Vector4.zero;
            }
            set
            {
                if (m_Type == PMaterialPropertyType.Vector)
                {
                    m_Value = value;
                }
            }
        }

        public Color ValueColor
        {
            get
            {
                if (m_Type == PMaterialPropertyType.Color)
                {
                    if (m_Value == null)
                    {
                        return Color.white;
                    }
                    return (Color)m_Value;
                }
                return Color.white;
            }
            set
            {
                if (m_Type == PMaterialPropertyType.Color)
                {
                    m_Value = value;
                }
            }
        }

        public Texture ValueTexture
        {
            get
            {
                if (m_Type == PMaterialPropertyType.Texture)
                {
                    if (m_Value == null)
                    {
                        return Texture2D.whiteTexture;
                    }
                    try
                    {
                        return (Texture)m_Value;
                    }
                    catch
                    {
                        return Texture2D.whiteTexture;
                    }
                }
                else if (m_Type == PMaterialPropertyType.RTHandle)
                {
                    if (m_Value == null)
                    {
                        return Texture2D.whiteTexture;
                    }
                    try
                    {
                        return ((PMaterial)m_Value).OutputTexture;
                    }
                    catch
                    {
                        return Texture2D.whiteTexture;
                    }
                }
                return Texture2D.whiteTexture;
            }
            set
            {
                if (m_Type == PMaterialPropertyType.Texture)
                {
                    m_Value = value;
                }
            }
        }

        public PMaterial ValuePMaterial
        {
            get
            {
                if (m_Value == null || m_Type != PMaterialPropertyType.RTHandle)
                {
                    return null;
                }
                try
                {
                    return (PMaterial)m_Value;
                }
                catch
                {
                    return null;
                }
            }
        }

        public PMaterial TempChildPMat { get; set; }

        public void SetRTHandleValue(PMaterial pm)
        {
            if (m_Type == PMaterialPropertyType.RTHandle)
            {
                m_Value = pm;
            }
        }

        public bool IsBufferSourceBinary { get; set; }
        public TextAsset TempBufferDataAsset { get; set; }
        private int m_ComputeBufferStride;
        private ComputeBuffer m_CptBuffer;

        public int ComputeBufferStride
        {
            get
            {
                int comp = 1 - m_ComputeBufferStride;
                int noneZeroStride = 1 - (comp & (comp >> 31));//max(1, m_ComputeBufferStride)
                int stride = ((noneZeroStride + 3) / 4) * 4;//必须为4的倍数
                return stride;
            }
            set { m_ComputeBufferStride = value; }
        }

        public void SetComputeBuffer(TextAsset textAsset, bool isBinary = false)
        {
            if (textAsset == null)
            {
                m_Value = null;
                return;
            }
            if (m_Type == PMaterialPropertyType.ComputeBuffer && (textAsset != (TextAsset)m_Value))
            {
                m_Value = textAsset;
                IsBufferSourceBinary = isBinary;
                //
                if (isBinary)
                {
                    //如果是二进制的, 需要先验证textAsset是有效的: 要求实际读出来的数据的字节数能被ComputeBufferStride整除
                    if (textAsset.dataSize % ComputeBufferStride != 0)
                    {
                        isBinary = false;
                        Debug.LogError(textAsset.name + "的文件字节数不能被设置的结构体的Stride: " + ComputeBufferStride + "整除! 已视为文本格式读取!");
                    }
                }
                //有效再更新m_CptBuffer
                uint[] bufferData;//最终所有数据都解释为uint, 有其他类型的通过对应的asXXX()方法转换
                if (isBinary)
                {
                    var rawData = textAsset.bytes;
                    bufferData = new uint[rawData.Length / 4];
                    CopyUtility.CopyByteArrayToUintArray(ref rawData, ref bufferData);
                }
                else
                {
                    var str = textAsset.text;
                    string[] splits = str.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    bufferData = new uint[splits.Length];
                    for (int i = 0; i < splits.Length; i++)
                    {
                        if (!float.TryParse(splits[i], out float v))
                        {
                            v = 0f;
                        }
                        bufferData[i] = TypeConvertUtility.FloatToUint(v);
                    }
                }
                //
                if ((bufferData.Length * 4) % ComputeBufferStride != 0)
                {
                    Debug.LogError("解析得到的数据的长度无法被Stride整除!");
                    return;
                }
                GraphicsUtility.AllocateComputeBuffer(ref m_CptBuffer, bufferData.Length, ComputeBufferStride);
                m_CptBuffer.SetData(bufferData);
            }
        }

        public ComputeBuffer GetComputeBuffer()
        {
            return m_CptBuffer;
        }
        #endregion

        #region ----Serialize----
        public PMaterialPropertyData ToSerializedData()
        {
            PMaterialPropertyData data = new PMaterialPropertyData();
            data.PropertyName = m_Name;
            data.PropertyDescription = m_Description;
            data.Type = m_Type;
            switch (m_Type)
            {
                case PMaterialPropertyType.Int:
                    data.PropertyPathOrValue = ValueInt.ToString();
                    break;
                case PMaterialPropertyType.Float:
                    data.PropertyPathOrValue = ValueFloat.ToString();
                    break;
                case PMaterialPropertyType.Vector:
                    var vec = ValueVector;
                    data.PropertyPathOrValue = $"{vec.x}, {vec.y}, {vec.z}, {vec.w}";
                    break;
                case PMaterialPropertyType.Color:
                    var col = ValueColor;
                    data.PropertyPathOrValue = $"{col.r}, {col.g}, {col.b}, {col.a}";
                    break;
                case PMaterialPropertyType.Texture:
                    if (m_Value == null)
                    {
                        data.PropertyPathOrValue = string.Empty;
                    }
                    else
                    {
                        try
                        {
                            var tex = (Texture)m_Value;
                            data.PropertyPathOrValue = AssetDatabase.GetAssetPath(tex);
                        }
                        catch
                        {
                            data.PropertyPathOrValue = string.Empty;
                        }
                    }
                    break;
                case PMaterialPropertyType.RTHandle:
                    if (m_Value == null)
                    {
                        data.PropertyPathOrValue = string.Empty;
                    }
                    else
                    {
                        try
                        {
                            var pmat = (PMaterial)m_Value;
                            data.PropertyPathOrValue = AssetDatabase.GetAssetPath(pmat);
                        }
                        catch
                        {
                            data.PropertyPathOrValue = string.Empty;
                        }
                    }
                    break;
                case PMaterialPropertyType.ComputeBuffer:
                    if (m_Value == null)
                    {
                        data.PropertyPathOrValue = string.Empty;
                    }
                    else
                    {
                        var textAsset = (TextAsset)m_Value;
                        string textAssetPath = AssetDatabase.GetAssetPath(textAsset);
                        data.PropertyPathOrValue = $"{ComputeBufferStride}, {(IsBufferSourceBinary ? 1 : 0)}, {textAssetPath}";
                    }
                    break;
            }
            return data;
        }
        #endregion

        #region ----Constructor----
        public PMaterialProperty(string name, string description, PMaterialPropertyType propertyType)
        {
            m_Name = name;
            m_Description = description;
            m_Type = propertyType;
        }
        #endregion

        public void Dispose()
        {
            m_CptBuffer?.Release();
        }
    }
#endif
}