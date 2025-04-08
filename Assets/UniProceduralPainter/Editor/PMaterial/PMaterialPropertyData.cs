using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    [Serializable]
    public class PMaterialPropertyData
    {
        public string PropertyName;
        public string PropertyDescription;//compute buffer的得序列化下来
        public PMaterialPropertyType Type;
        public string PropertyPathOrValue;

        public PMaterialProperty Deserialization()
        {
            PMaterialProperty property = new PMaterialProperty(PropertyName, PropertyDescription, Type);
            switch (Type)
            {
                case PMaterialPropertyType.Int:
                    property.ValueInt = int.Parse(PropertyPathOrValue);
                    break;
                case PMaterialPropertyType.Float:
                    property.ValueFloat = float.Parse(PropertyPathOrValue);
                    break;
                case PMaterialPropertyType.Vector:
                    {
                        string[] splits = PropertyPathOrValue.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        if (splits.Length != 4)
                        {
                            property.ValueVector = Vector4.zero;
                            Debug.LogWarning(PropertyPathOrValue + "反序列化为Vector异常!");
                            break;
                        }
                        if (float.TryParse(splits[0].Trim(), out float x)
                            && float.TryParse(splits[1].Trim(), out float y)
                            && float.TryParse(splits[2].Trim(), out float z)
                            && float.TryParse(splits[3].Trim(), out float w))
                        {
                            property.ValueVector = new Vector4(x, y, z, w);
                        }
                        else
                        {
                            property.ValueVector = Vector4.zero;
                            Debug.LogWarning(PropertyPathOrValue + "反序列化为Vector异常!");
                        }
                    }
                    break;
                case PMaterialPropertyType.Color:
                    {
                        string[] splits = PropertyPathOrValue.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        if (splits.Length != 4)
                        {
                            property.ValueColor = Color.white;
                            Debug.LogWarning(PropertyPathOrValue + "反序列化为Color异常!");
                            break;
                        }
                        if (float.TryParse(splits[0].Trim(), out float r)
                            && float.TryParse(splits[1].Trim(), out float g)
                            && float.TryParse(splits[2].Trim(), out float b)
                            && float.TryParse(splits[3].Trim(), out float a))
                        {
                            property.ValueColor = new Color(r, g, b, a);
                        }
                        else
                        {
                            property.ValueColor = Color.white;
                            Debug.LogWarning(PropertyPathOrValue + "反序列化为Color异常!");
                        }
                    }
                    break;
                case PMaterialPropertyType.Texture:
                    {
                        if (string.IsNullOrEmpty(PropertyPathOrValue))
                        {
                            property.ValueTexture = Texture2D.whiteTexture;
                        }
                        else
                        {
                            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(PropertyPathOrValue);
                            if (tex == null)
                            {
                                property.ValueTexture = Texture2D.whiteTexture;
                                Debug.LogWarning("未能在" + PropertyPathOrValue + "处找到目标纹理!");
                                break;
                            }
                            property.ValueTexture = tex;
                        }
                    }
                    break;
                case PMaterialPropertyType.RTHandle:
                    {
                        if (string.IsNullOrEmpty(PropertyPathOrValue))
                        {
                            property.ValueTexture = Texture2D.whiteTexture;
                        }
                        else
                        {
                            PMaterial pm = AssetDatabase.LoadAssetAtPath<PMaterial>(PropertyPathOrValue);
                            if (pm == null)
                            {
                                property.ValueTexture = Texture2D.whiteTexture;
                                Debug.LogWarning("未能在" + PropertyPathOrValue + "处找到目标PMaterial!");
                                break;
                            }
                            property.SetRTHandleValue(pm);
                            property.TempChildPMat = pm;
                        }
                    }
                    break;
                case PMaterialPropertyType.ComputeBuffer:
                    {
                        if (string.IsNullOrEmpty(PropertyPathOrValue))
                        {
                            Debug.LogWarning($"Buffer Data: {PropertyDescription} 序列化的路径为空! 如果不需要请移除属性!");
                            break;
                        }
                        else
                        {
                            string[] splits = PropertyPathOrValue.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                            if (splits.Length != 3)
                            {
                                Debug.LogError(PropertyPathOrValue + "反序列化为ComputeBuffer异常!");
                                break;
                            }
                            int stride = int.Parse(splits[0]);
                            bool isBinary = bool.Parse(splits[1]);
                            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(PropertyPathOrValue);
                            if (ta == null)
                            {
                                Debug.LogError("未能在" + splits[2] + "处找到目标数据文件!");
                                break;
                            }
                            property.ComputeBufferStride = stride;
                            property.SetComputeBuffer(ta, isBinary);
                        }
                    }
                    break;
            }
            return property;
        }
    }
#endif
}