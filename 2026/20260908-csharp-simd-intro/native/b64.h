#ifndef B64_H
#define B64_H

#include <stddef.h>
#include <stdint.h>

/* Standard Base64 (RFC 4648 section 4), '=' padded.
 *
 * One declaration per ISA. Each lives in its own .c file because MSVC applies
 * /arch:* per translation unit -- the same split lw.PPOCR.C makes across
 * sse2_*.c / avx2_*.c / neon_*.c / lsx_*.c. */

size_t b64_encode_scalar(const uint8_t *source, size_t source_length, uint8_t *destination);
size_t b64_encode_ssse3(const uint8_t *source, size_t source_length, uint8_t *destination);
size_t b64_encode_avx2(const uint8_t *source, size_t source_length, uint8_t *destination);

/* Shared scalar body, also used as the tail of both vector kernels. */
size_t b64_encode_scalar_from(const uint8_t *source, size_t source_length, uint8_t *destination,
                              size_t i, size_t o);

extern const uint8_t b64_map[64];

static inline size_t b64_encoded_length(size_t source_length)
{
    return (source_length + 2) / 3 * 4;
}

#endif /* B64_H */
