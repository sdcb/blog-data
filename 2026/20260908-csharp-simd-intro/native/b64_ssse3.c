#include "b64.h"

#include <tmmintrin.h> /* SSSE3 */

/* Consumes 12 bytes and emits 16 characters per iteration.
 * Reads 16 bytes per step, so it stops 16 bytes short of the end. */
size_t b64_encode_ssse3(const uint8_t *source, size_t source_length, uint8_t *destination)
{
    const __m128i shuffle_vec = _mm_setr_epi32(0x01020001, 0x04050304, 0x07080607, 0x0A0B090A);
    const __m128i lut = _mm_setr_epi8(65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);
    const __m128i mask_ac = _mm_set1_epi32(0x0fc0fc00);
    const __m128i mask_bb = _mm_set1_epi32(0x003f03f0);
    const __m128i shift_ac = _mm_set1_epi32(0x04000040);
    const __m128i shift_bb = _mm_set1_epi32(0x01000010);
    const __m128i const51 = _mm_set1_epi8(51);
    const __m128i const25 = _mm_set1_epi8(25);

    size_t i = 0, o = 0;

    while (i + 16 <= source_length)
    {
        __m128i str = _mm_loadu_si128((const __m128i *)(source + i));

        /* Gather each 3-byte group into a 4-byte lane. */
        str = _mm_shuffle_epi8(str, shuffle_vec);

        __m128i t0 = _mm_and_si128(str, mask_ac);
        __m128i t2 = _mm_and_si128(str, mask_bb);
        __m128i t1 = _mm_mulhi_epu16(t0, shift_ac);
        __m128i t3 = _mm_mullo_epi16(t2, shift_bb);
        str = _mm_or_si128(t1, t3);

        /* Map each 6-bit value to its character via a range-dependent offset. */
        __m128i indices = _mm_subs_epu8(str, const51);
        __m128i mask = _mm_cmpgt_epi8(str, const25);
        __m128i tmp = _mm_sub_epi8(indices, mask);
        str = _mm_add_epi8(str, _mm_shuffle_epi8(lut, tmp));

        _mm_storeu_si128((__m128i *)(destination + o), str);

        i += 12;
        o += 16;
    }

    return b64_encode_scalar_from(source, source_length, destination, i, o);
}
