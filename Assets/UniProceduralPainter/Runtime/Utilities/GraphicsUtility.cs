using System.Collections;
using System.Collections.Generic;
using UniProceduralPainter.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniProceduralPainter
{
    public static class GraphicsUtility
    {
        #region ----RTHandle----
#if UNITY_EDITOR
        private static bool CompareRTHandleDimension(PMatOutputDimension handleDim, TextureDimension dim)
        {
            if (handleDim == PMatOutputDimension.Tex2D && dim == TextureDimension.Tex2D)
            {
                return false;
            }
            else if (handleDim == PMatOutputDimension.Cube && dim == TextureDimension.Cube)
            {
                return false;
            }
            return true;
        }

        public static void AllocateRTHandle(ref RTHandle handle, PMaterialOutputSetting setting, bool enableRandomWrite = false)
        {
            int width = setting.TextureSize.x;
            int height = setting.TextureSize.y;
            if (handle == null || handle.Width != width || handle.Height != height || handle.Format != setting.OutputFormat || handle.EnableRandomWrite != enableRandomWrite || CompareRTHandleDimension(setting.Dimension, handle.Dimension))
            {
                handle?.Release();
                switch (setting.Dimension)
                {
                    case PMatOutputDimension.Tex2D:
                        handle = new RTHandle(width, height, 0, setting.OutputFormat, 0, enableRandomWrite);
                        break;
                    case PMatOutputDimension.Cube:
                        handle = new RTHandle(width, setting.OutputFormat, enableRandomWrite);
                        break;
                }

            }
        }
#endif
        #endregion

        #region ----Compute Buffer----
        public static void AllocateComputeBuffer(ref ComputeBuffer cb, int count, int stride, ComputeBufferType cbt = ComputeBufferType.Structured, ComputeBufferMode cbm = ComputeBufferMode.Immutable)
        {
            if (cb == null || cb.count != count || cb.stride != stride)
            {
                cb?.Release();
                cb = new ComputeBuffer(count, stride, cbt, cbm);
            }
        }
        #endregion
    }
}