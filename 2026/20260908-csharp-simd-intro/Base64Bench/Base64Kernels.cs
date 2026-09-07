using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Base64Bench;

/// <summary>
/// Three hand-written Base64 encoders (scalar / Vector128 / Vector256) sharing one
/// alphabet and one scalar tail. The vectorized algorithm is ported from
/// dotnet/runtime's Base64EncoderHelper for the standard Base64 alphabet.
/// </summary>
public static class Base64Kernels
{
    /// <summary>Standard Base64 alphabet (RFC 4648 §4).</summary>
    private static ReadOnlySpan<byte> Map =>
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;

    public static int EncodedLength(int sourceLength) => (sourceLength + 2) / 3 * 4;

    // ---------------------------------------------------------------- scalar

    /// <summary>
    /// Straight 3-bytes-in / 4-chars-out table lookup. This is the shape a C
    /// programmer would write first, and the shape an auto-vectorizer sees.
    /// </summary>
    public static int EncodeScalar(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        int consumed = EncodeScalarCore(source, destination, 0, 0);
        return consumed;
    }

    private static int EncodeScalarCore(ReadOnlySpan<byte> source, Span<byte> destination, int i, int o)
    {
        ref byte map = ref MemoryMarshal.GetReference(Map);
        int end = source.Length - 2;

        while (i < end)
        {
            uint t = (uint)((source[i] << 16) | (source[i + 1] << 8) | source[i + 2]);
            destination[o] = Unsafe.Add(ref map, (int)(t >> 18));
            destination[o + 1] = Unsafe.Add(ref map, (int)((t >> 12) & 0x3F));
            destination[o + 2] = Unsafe.Add(ref map, (int)((t >> 6) & 0x3F));
            destination[o + 3] = Unsafe.Add(ref map, (int)(t & 0x3F));
            i += 3;
            o += 4;
        }

        return o + EncodeTail(source, destination, i, o);
    }

    /// <summary>Encodes the trailing 1 or 2 bytes with '=' padding.</summary>
    private static int EncodeTail(ReadOnlySpan<byte> source, Span<byte> destination, int i, int o)
    {
        ref byte map = ref MemoryMarshal.GetReference(Map);
        int remaining = source.Length - i;

        if (remaining == 1)
        {
            uint t = (uint)source[i] << 16;
            destination[o] = Unsafe.Add(ref map, (int)(t >> 18));
            destination[o + 1] = Unsafe.Add(ref map, (int)((t >> 12) & 0x3F));
            destination[o + 2] = (byte)'=';
            destination[o + 3] = (byte)'=';
            return 4;
        }

        if (remaining == 2)
        {
            uint t = (uint)((source[i] << 16) | (source[i + 1] << 8));
            destination[o] = Unsafe.Add(ref map, (int)(t >> 18));
            destination[o + 1] = Unsafe.Add(ref map, (int)((t >> 12) & 0x3F));
            destination[o + 2] = Unsafe.Add(ref map, (int)((t >> 6) & 0x3F));
            destination[o + 3] = (byte)'=';
            return 4;
        }

        return 0;
    }

    // ----------------------------------------------------------- shared LUT

    // Absolute offset added to each 6-bit value, indexed by range:
    //   [0..25] -> 'A'..'Z'  +65      [26..51] -> 'a'..'z'  +71
    //   [52..61] -> '0'..'9'  -4      [62] -> '+'  -19      [63] -> '/'  -16
    private static readonly Vector128<sbyte> Lut128 = Vector128.Create(
        (sbyte)65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);

    // Gathers the 3-byte groups into 4-byte lanes: for output quad n the lane
    // holds bytes (b1 b2 a b1) so the two masks below can pull four 6-bit fields.
    private const int Shuffle0 = 0x01020001;
    private const int Shuffle1 = 0x04050304;
    private const int Shuffle2 = 0x07080607;
    private const int Shuffle3 = 0x0A0B090A;

    // ------------------------------------------------------------ Vector128

    /// <summary>
    /// SSSE3 path: consumes 12 bytes and emits 16 characters per iteration.
    /// Reads 16 bytes per step, so it stops 16 bytes short of the end.
    /// </summary>
    public static int EncodeVector128(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!Ssse3.IsSupported)
            return EncodeScalar(source, destination);

        Vector128<byte> shuffleVec = Vector128.Create(Shuffle0, Shuffle1, Shuffle2, Shuffle3).AsByte();
        Vector128<byte> lut = Lut128.AsByte();
        Vector128<byte> maskAC = Vector128.Create(0x0fc0fc00).AsByte();
        Vector128<byte> maskBB = Vector128.Create(0x003f03f0).AsByte();
        Vector128<ushort> shiftAC = Vector128.Create(0x04000040).AsUInt16();
        Vector128<short> shiftBB = Vector128.Create(0x01000010).AsInt16();
        Vector128<byte> const51 = Vector128.Create((byte)51);
        Vector128<sbyte> const25 = Vector128.Create((sbyte)25);

        ref byte src = ref MemoryMarshal.GetReference(source);
        ref byte dst = ref MemoryMarshal.GetReference(destination);

        int i = 0, o = 0;

        while (i + 16 <= source.Length)
        {
            Vector128<byte> str = Vector128.LoadUnsafe(ref Unsafe.Add(ref src, i));

            str = Ssse3.Shuffle(str, shuffleVec);

            Vector128<byte> t0 = str & maskAC;
            Vector128<byte> t2 = str & maskBB;
            Vector128<ushort> t1 = Sse2.MultiplyHigh(t0.AsUInt16(), shiftAC);
            Vector128<short> t3 = t2.AsInt16() * shiftBB;
            str = t1.AsByte() | t3.AsByte();

            // Map each 6-bit value to its character by adding a range-dependent offset.
            Vector128<byte> indices = Sse2.SubtractSaturate(str, const51);
            Vector128<sbyte> mask = Vector128.GreaterThan(str.AsSByte(), const25);
            Vector128<sbyte> tmp = indices.AsSByte() - mask;
            str += Ssse3.Shuffle(lut, tmp.AsByte());

            str.StoreUnsafe(ref Unsafe.Add(ref dst, o));

            i += 12;
            o += 16;
        }

        return EncodeScalarCore(source, destination, i, o);
    }

    // ------------------------------------------------------------ Vector256

    /// <summary>
    /// AVX2 path: consumes 24 bytes and emits 32 characters per iteration.
    /// Builds the 256-bit vector from two overlapping 128-bit loads (offsets i and
    /// i+12) so the per-lane shuffle pattern is identical to the SSSE3 one; the BCL
    /// instead does one 256-bit load plus a PermuteVar8x32 to shift by 4 bytes.
    /// Reads 28 bytes per step.
    /// </summary>
    public static int EncodeVector256(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!Avx2.IsSupported)
            return EncodeVector128(source, destination);

        Vector256<byte> shuffleVec = Vector256.Create(
            Shuffle0, Shuffle1, Shuffle2, Shuffle3,
            Shuffle0, Shuffle1, Shuffle2, Shuffle3).AsByte();
        Vector256<byte> lut = Vector256.Create(Lut128, Lut128).AsByte();
        Vector256<byte> maskAC = Vector256.Create(0x0fc0fc00).AsByte();
        Vector256<byte> maskBB = Vector256.Create(0x003f03f0).AsByte();
        Vector256<ushort> shiftAC = Vector256.Create(0x04000040).AsUInt16();
        Vector256<short> shiftBB = Vector256.Create(0x01000010).AsInt16();
        Vector256<byte> const51 = Vector256.Create((byte)51);
        Vector256<sbyte> const25 = Vector256.Create((sbyte)25);

        ref byte src = ref MemoryMarshal.GetReference(source);
        ref byte dst = ref MemoryMarshal.GetReference(destination);

        int i = 0, o = 0;

        while (i + 28 <= source.Length)
        {
            Vector128<byte> lo = Vector128.LoadUnsafe(ref Unsafe.Add(ref src, i));
            Vector128<byte> hi = Vector128.LoadUnsafe(ref Unsafe.Add(ref src, i + 12));
            Vector256<byte> str = Vector256.Create(lo, hi);

            str = Avx2.Shuffle(str, shuffleVec);

            Vector256<byte> t0 = str & maskAC;
            Vector256<byte> t2 = str & maskBB;
            Vector256<ushort> t1 = Avx2.MultiplyHigh(t0.AsUInt16(), shiftAC);
            Vector256<short> t3 = t2.AsInt16() * shiftBB;
            str = t1.AsByte() | t3.AsByte();

            Vector256<byte> indices = Avx2.SubtractSaturate(str, const51);
            Vector256<sbyte> mask = Vector256.GreaterThan(str.AsSByte(), const25);
            Vector256<sbyte> tmp = indices.AsSByte() - mask;
            str += Avx2.Shuffle(lut, tmp.AsByte());

            str.StoreUnsafe(ref Unsafe.Add(ref dst, o));

            i += 24;
            o += 32;
        }

        // Finish with the 128-bit kernel, then the scalar tail.
        if (Ssse3.IsSupported)
        {
            Vector128<byte> shuffleVec128 = Vector128.Create(Shuffle0, Shuffle1, Shuffle2, Shuffle3).AsByte();
            Vector128<byte> lut128 = Lut128.AsByte();
            Vector128<byte> maskAC128 = Vector128.Create(0x0fc0fc00).AsByte();
            Vector128<byte> maskBB128 = Vector128.Create(0x003f03f0).AsByte();
            Vector128<ushort> shiftAC128 = Vector128.Create(0x04000040).AsUInt16();
            Vector128<short> shiftBB128 = Vector128.Create(0x01000010).AsInt16();
            Vector128<byte> c51 = Vector128.Create((byte)51);
            Vector128<sbyte> c25 = Vector128.Create((sbyte)25);

            while (i + 16 <= source.Length)
            {
                Vector128<byte> str = Vector128.LoadUnsafe(ref Unsafe.Add(ref src, i));
                str = Ssse3.Shuffle(str, shuffleVec128);
                Vector128<byte> t0 = str & maskAC128;
                Vector128<byte> t2 = str & maskBB128;
                Vector128<ushort> t1 = Sse2.MultiplyHigh(t0.AsUInt16(), shiftAC128);
                Vector128<short> t3 = t2.AsInt16() * shiftBB128;
                str = t1.AsByte() | t3.AsByte();
                Vector128<byte> indices = Sse2.SubtractSaturate(str, c51);
                Vector128<sbyte> mask = Vector128.GreaterThan(str.AsSByte(), c25);
                Vector128<sbyte> tmp = indices.AsSByte() - mask;
                str += Ssse3.Shuffle(lut128, tmp.AsByte());
                str.StoreUnsafe(ref Unsafe.Add(ref dst, o));
                i += 12;
                o += 16;
            }
        }

        return EncodeScalarCore(source, destination, i, o);
    }
}
