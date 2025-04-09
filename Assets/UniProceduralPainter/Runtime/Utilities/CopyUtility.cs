//用span的CopyTo()实现拷贝"加速"的方法
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UniProceduralPainter
{
    public static class CopyUtility
    {
        #region ----Arrays----
        /// <summary>
        /// 从一个byte的array中拷贝至一个uint的array
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyByteArrayToUintArray(ref byte[] src, ref uint[] dest)
        {
            Span<byte> srcSpan = src.AsSpan();
            Span<uint> destSpan = dest.AsSpan();
            Span<byte> destByteSpan = MemoryMarshal.AsBytes(destSpan);
            //
            srcSpan.CopyTo(destByteSpan);
        }

        /// <summary>
        /// 从一个byte的array中拷贝至一个uint的array
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="startIndex"></param>
        /// <param name="Length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyByteArrayToUintArray(ref byte[] src, ref uint[] dest, int startIndex, int Length)
        {
            Span<byte> srcSpan = src.AsSpan();
            Span<uint> destSpan = dest.AsSpan();
            Span<byte> destByteSpan = MemoryMarshal.AsBytes(destSpan);
            //
            srcSpan.Slice(startIndex, Length).CopyTo(destByteSpan);
        }

        /// <summary>
        /// 从一个uint的array中拷贝至一个byte的array
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyUintArrayToByteArray(ref uint[] src, ref byte[] dest)
        {
            Span<uint> srcSpan = src.AsSpan();
            Span<byte> srcByteSpan = MemoryMarshal.AsBytes(srcSpan);
            Span<byte> destSpan = dest.AsSpan();
            //
            srcByteSpan.CopyTo(destSpan);
        }
        #endregion
    }
}