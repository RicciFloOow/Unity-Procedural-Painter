using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UniProceduralPainter
{
    public static class TypeConvertUtility
    {
        #region ----Union Like Struct----
        [StructLayout(LayoutKind.Explicit)]
        internal struct FloatUintUnion
        {
            [FieldOffset(0)]
            public uint UintValue;
            [FieldOffset(0)]
            public float FloatValue;
        }
        #endregion

        #region ----Convertor----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FloatToUint(float v)
        {
            FloatUintUnion u;
            u.UintValue = 0;
            u.FloatValue = v;
            return u.UintValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UintToFloat(uint v)
        {
            FloatUintUnion u;
            u.FloatValue = 0;
            u.UintValue = v;
            return u.FloatValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T2 UniversalConvertTypeToType<T1, T2>(ref T1 input, int startIndex, int length) where T1 : struct where T2 : struct
        {
            Span<T1> t1Span = MemoryMarshal.CreateSpan(ref input, 1);
            Span<byte> t1ByteSpan = MemoryMarshal.AsBytes(t1Span);
            //
            T2 output = new();
            Span<T2> t2Span = MemoryMarshal.CreateSpan(ref output, 1);
            Span<byte> t2ByteSpan = MemoryMarshal.AsBytes(t2Span);
            t1ByteSpan.Slice(startIndex, length).CopyTo(t2ByteSpan);
            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UniversalConvertTypeToType<T1, T2>(ref T1 input, ref T2 output, int startIndex, int length) where T1 : struct where T2 : struct
        {
            Span<T1> t1Span = MemoryMarshal.CreateSpan(ref input, 1);
            Span<byte> t1ByteSpan = MemoryMarshal.AsBytes(t1Span);
            //
            Span<T2> t2Span = MemoryMarshal.CreateSpan(ref output, 1);
            Span<byte> t2ByteSpan = MemoryMarshal.AsBytes(t2Span);
            t1ByteSpan.Slice(startIndex, length).CopyTo(t2ByteSpan);
        }
        #endregion
    }
}