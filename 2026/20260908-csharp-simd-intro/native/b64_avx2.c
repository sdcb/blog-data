#include "b64.h"

#include <immintrin.h> /* AVX2 */

/* Consumes 24 bytes and emits 32 characters per iteration.
 * Built from two overlapping 128-bit loads (i and i+12) so the per-lane shuffle
 * pattern matches the SSSE3 kernel exactly. Reads 28 bytes per step. */
size_t b64_encode_avx2(const uint8_t *source, size_t source_length, uint8_t *destination)
{
    const __m256i shuffle_vec = _mm256_setr_epi32(
        0x01020001, 0x04050304, 0x07080607, 0x0A0B090A,
        0x01020001, 0x04050304, 0x07080607, 0x0A0B090A);
    const __m256i lut = _mm256_setr_epi8(
        65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0,
        65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);
    const __m256i mask_ac = _mm256_set1_epi32(0x0fc0fc00);
    const __m256i mask_bb = _mm256_set1_epi32(0x003f03f0);
    const __m256i shift_ac = _mm256_set1_epi32(0x04000040);
    const __m256i shift_bb = _mm256_set1_epi32(0x01000010);
    const __m256i const51 = _mm256_set1_epi8(51);
    const __m256i const25 = _mm256_set1_epi8(25);

    size_t i = 0, o = 0;

    while (i + 28 <= source_length)
    {
        __m128i lo = _mm_loadu_si128((const __m128i *)(source + i));
        __m128i hi = _mm_loadu_si128((const __m128i *)(source + i + 12));
        __m256i str = _mm256_set_m128i(hi, lo);

        str = _mm256_shuffle_epi8(str, shuffle_vec);

        __m256i t0 = _mm256_and_si256(str, mask_ac);
        __m256i t2 = _mm256_and_si256(str, mask_bb);
        __m256i t1 = _mm256_mulhi_epu16(t0, shift_ac);
        __m256i t3 = _mm256_mullo_epi16(t2, shift_bb);
        str = _mm256_or_si256(t1, t3);

        __m256i indices = _mm256_subs_epu8(str, const51);
        __m256i mask = _mm256_cmpgt_epi8(str, const25);
        __m256i tmp = _mm256_sub_epi8(indices, mask);
        str = _mm256_add_epi8(str, _mm256_shuffle_epi8(lut, tmp));

        _mm256_storeu_si256((__m256i *)(destination + o), str);

        i += 24;
        o += 32;
    }

    /* Finish with the 128-bit kernel, then the scalar tail. */
    {
        const __m128i shuffle128 = _mm_setr_epi32(0x01020001, 0x04050304, 0x07080607, 0x0A0B090A);
        const __m128i lut128 = _mm_setr_epi8(65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);
        const __m128i mac = _mm_set1_epi32(0x0fc0fc00);
        const __m128i mbb = _mm_set1_epi32(0x003f03f0);
        const __m128i sac = _mm_set1_epi32(0x04000040);
        const __m128i sbb = _mm_set1_epi32(0x01000010);
        const __m128i c51 = _mm_set1_epi8(51);
        const __m128i c25 = _mm_set1_epi8(25);

        while (i + 16 <= source_length)
        {
            __m128i str = _mm_loadu_si128((const __m128i *)(source + i));
            str = _mm_shuffle_epi8(str, shuffle128);
            __m128i t0 = _mm_and_si128(str, mac);
            __m128i t2 = _mm_and_si128(str, mbb);
            __m128i t1 = _mm_mulhi_epu16(t0, sac);
            __m128i t3 = _mm_mullo_epi16(t2, sbb);
            str = _mm_or_si128(t1, t3);
            __m128i indices = _mm_subs_epu8(str, c51);
            __m128i mask = _mm_cmpgt_epi8(str, c25);
            __m128i tmp = _mm_sub_epi8(indices, mask);
            str = _mm_add_epi8(str, _mm_shuffle_epi8(lut128, tmp));
            _mm_storeu_si128((__m128i *)(destination + o), str);
            i += 12;
            o += 16;
        }
    }

    return b64_encode_scalar_from(source, source_length, destination, i, o);
}
