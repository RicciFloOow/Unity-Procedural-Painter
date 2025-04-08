using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public enum ShaderState
    {
        None,
        Valid,
        Error,
        NameMismatch
    }

    public class PixelPMaterial : PMaterial
    {
        public override bool IsMaterialValid
        {
            get
            {
                return State == ShaderState.Valid;
            }
        }

        #region ----Shader----
        [HideInInspector]
        public Shader Shader;

        public ShaderState State { get; private set; }

        private ShaderState CheckShaderValidation()
        {
            if (Shader == null)
            {
                return ShaderState.None;
            }
            if (ShaderUtil.ShaderHasError(Shader))
            {
                return ShaderState.Error;
            }
            string shaderAssetPath = AssetDatabase.GetAssetPath(Shader);
            string shaderFileName = Path.GetFileName(shaderAssetPath);
            if (shaderFileName != name + "_PS.shader")
            {
                //这个的主要目的是为了防止用户替换目标的Shader, 因为一旦替换了一些序列化下来的数据就难处理了
                //unity的material就存在一些关于序列化下来的数据的问题
                return ShaderState.NameMismatch;
            }
            return ShaderState.Valid;
        }

        private List<string> GetShaderComputeBuffersNameDesc()
        {
            List<string> props = new List<string>();
            string shaderPath = AssetDatabase.GetAssetPath(Shader);
            string shaderCode = File.ReadAllText(shaderPath);
            //
            var contentMatch = Regex.Match(
                shaderCode,
                @"/[*]PROPBEGIN(.*?)PROPEND[*]/",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );
            if (!contentMatch.Success)
            {
                return props;
            }
            string advProps = contentMatch.Groups[1].Value;
            string[] bufferAssignments = advProps.Split(
                "Buffer:",
                StringSplitOptions.RemoveEmptyEntries
            );
            //
            foreach (var ba in bufferAssignments)
            {
                var baMatch = Regex.Match(
                    ba,
                    @"^\s*\s+Name=(.*?);\s+Desc=(.*?);\s+Stride=(.*?);\s*$",
                    RegexOptions.IgnoreCase
                );
                if (baMatch.Success)
                {
                    props.Add(baMatch.Groups[1].Value.Trim());// Name
                    props.Add(baMatch.Groups[2].Value.Trim());// Description
                    props.Add(baMatch.Groups[3].Value.Trim());// Stride
                }
            }
            return props;
        }
        #endregion

        #region ----Unity Material----
        private Material m_uniMaterial;

        private void SetupUnityMaterial()
        {
            m_uniMaterial = new Material(Shader);
        }

        private void ReleaseUnityMaterial()
        {
            if (m_uniMaterial != null)
            {
                DestroyImmediate(m_uniMaterial);
            }
        }
        #endregion

        #region ----Custom Editor----
        public override void OnEnterPMaterialEditor()
        {
            SetupOutputHandle();
            SetupUnityMaterial();
        }

        public override void OnExitPMaterialEditor()
        {
            ReleaseOutputHandle();
            ReleaseUnityMaterial();
        }
        #endregion

        #region ----Properties----
        private class MatPropDesc
        {
            public string Desc;
            public PMaterialPropertyType Type;
            public MatPropDesc(string desc, ShaderUtil.ShaderPropertyType propertyType)
            {
                Desc = desc;
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        Type = PMaterialPropertyType.Color;
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        Type = PMaterialPropertyType.Vector;
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        Type = PMaterialPropertyType.Float;
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        Type = PMaterialPropertyType.Texture;
                        break;
                    case ShaderUtil.ShaderPropertyType.Int:
                        Type = PMaterialPropertyType.Int;
                        break;
                }
            }
        }

        public override void UpdatePMatProperties()
        {
            State = CheckShaderValidation();
            if (State == ShaderState.Valid)
            {
                Dictionary<string, MatPropDesc> shaderPropNameDescDict = new Dictionary<string, MatPropDesc>();
                int propCount = ShaderUtil.GetPropertyCount(Shader);
                for (int i = 0; i < propCount; i++)
                {
                    //虽然方法名看起来好像是获得shader的属性, 不过更准确的说法应该是: 获得的是material的属性
                    string propName = ShaderUtil.GetPropertyName(Shader, i);
                    string propDesc = ShaderUtil.GetPropertyDescription(Shader, i);
                    ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(Shader, i);
                    if (!shaderPropNameDescDict.ContainsKey(propName))//理论上可以不需要，有重名的会编译报错
                    {
                        shaderPropNameDescDict.Add(propName, new MatPropDesc(propDesc, propType));
                    }
                }
                //像ComputeBuffer我们是无法直接从unity提供的接口中取到的
                //一种办法是自行写一个轻量级的编译器, 在编译过程中去记录全部用到的Compute Buffer,
                //PS. 为什么需要编译呢?因为可能存在Compute Buffer是在include文件(.cginc, .hlsl)中声明的
                //显然这过程太浪费了(除非修改unity编译shader的方法), 
                //因此我们选择通过类似AmplifyShaderEditor这些自定义的编辑器记录连连看信息的方法, 让用户在shader下方插入满足一定语法规则的注释, 然后分析即可
                var bufferList = GetShaderComputeBuffersNameDesc();
                int bufferCount = bufferList.Count / 3;
                //先移除当前结果中没有的属性
                m_PropertyList ??= new List<PMaterialProperty>();
                for (int i = m_PropertyList.Count - 1; i >= 0; i--)
                {
                    var prop = m_PropertyList[i];
                    if (prop.Type == PMaterialPropertyType.ComputeBuffer)
                    {
                        if (!bufferList.Exists(x => x == prop.Name))
                        {
                            m_PropertyList.RemoveAt(i);
                        }
                        else
                        {
                            //TODO:与Exists整合一起
                            int index = bufferList.IndexOf(prop.Name);
                            m_PropertyList[i].Description = bufferList[index + 1];
                        }
                    }
                    else
                    {
                        if (!shaderPropNameDescDict.ContainsKey(prop.Name))
                        {
                            m_PropertyList.RemoveAt(i);
                        }
                        else
                        {
                            m_PropertyList[i].Description = shaderPropNameDescDict[prop.Name].Desc;
                            //理论上存在修改属性类型的情况, 我这里就不改了, 需要的话修改名字以实现
                        }
                    }
                }
                //再向m_PropertyList添加新的属性
                foreach (var nd in shaderPropNameDescDict)
                {
                    if (!m_PropertyList.Exists(x => x.Name == nd.Key))
                    {
                        //添加新的属性
                        m_PropertyList.Add(new PMaterialProperty(nd.Key, nd.Value.Desc, nd.Value.Type));
                    }
                }
                for (int i = 0; i < bufferCount; i++)
                {
                    if (!m_PropertyList.Exists(x => x.Name == bufferList[i * 3]))
                    {
                        var newProp = new PMaterialProperty(bufferList[i * 3], bufferList[i * 3 + 1], PMaterialPropertyType.ComputeBuffer);
                        newProp.ComputeBufferStride = int.Parse(bufferList[i * 3 + 2]);
                        m_PropertyList.Add(newProp);
                    }
                }
            }
        }
        #endregion

        #region ----RTHandle----
        public override void SetupOutputHandle()
        {
            var size = OutputSetting.TextureSize;
            size = Vector2Int.Max(size, Vector2Int.one);
            OutputSetting.TextureSize = size;//防止异常尺寸
            GraphicsUtility.AllocateRTHandle(ref m_outputHandle, OutputSetting);
        }

        private void SetUniMatProps(ref MaterialPropertyBlock block, PMaterialProperty prop)
        {
            switch (prop.Type)
            {
                case PMaterialPropertyType.Int:
                    block.SetInt(prop.Name, prop.ValueInt);
                    break;
                case PMaterialPropertyType.Float:
                    block.SetFloat(prop.Name, prop.ValueFloat);
                    break;
                case PMaterialPropertyType.Vector:
                    block.SetVector(prop.Name, prop.ValueVector);
                    break;
                case PMaterialPropertyType.Color:
                    block.SetColor(prop.Name, prop.ValueColor);
                    break;
                case PMaterialPropertyType.Texture:
                case PMaterialPropertyType.RTHandle:
                    block.SetTexture(prop.Name, prop.ValueTexture);
                    break;
                case PMaterialPropertyType.ComputeBuffer:
                    var b = prop.GetComputeBuffer();
                    if (b != null)
                    {
                        block.SetBuffer(prop.Name, b);
                    }
                    break;
            }
        }

        private void OnRenderingProceduralMaterial2D()
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.SetRenderTarget(m_outputHandle);
            cmd.ClearRenderTarget(false, true, Color.clear);
            MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();
            for (int i = 0; i < m_PropertyList.Count; i++)
            {
                SetUniMatProps(ref matPropertyBlock, m_PropertyList[i]);
            }
            cmd.DrawProcedural(Matrix4x4.identity, m_uniMaterial, -1, MeshTopology.Triangles, 3, 1, matPropertyBlock);
            Graphics.ExecuteCommandBuffer(cmd);
        }

        public override void OnRenderingProceduralMaterial()
        {
            //TODO: cubemap
            OnRenderingProceduralMaterial2D();
        }
        #endregion
    }
#endif
}