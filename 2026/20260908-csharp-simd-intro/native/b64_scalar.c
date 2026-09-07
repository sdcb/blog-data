#include "b64.h"

const uint8_t b64_map[64] = {
    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
    'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
    'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'};

/* Straight 3-in / 4-out table lookup. This is what you write first, and what
 * you hand the auto-vectorizer when you have not written any intrinsics. */
size_t b64_encode_scalar_from(const uint8_t *source, size_t source_length, uint8_t *destination,
                              size_t i, size_t o)
{
    while (i + 3 <= source_length)
    {
        uint32_t t = ((uint32_t)source[i] << 16) | ((uint32_t)source[i + 1] << 8) | source[i + 2];
        destination[o] = b64_map[t >> 18];
        destination[o + 1] = b64_map[(t >> 12) & 0x3F];
        destination[o + 2] = b64_map[(t >> 6) & 0x3F];
        destination[o + 3] = b64_map[t & 0x3F];
        i += 3;
        o += 4;
    }

    size_t remaining = source_length - i;

    if (remaining == 1)
    {
        uint32_t t = (uint32_t)source[i] << 16;
        destination[o] = b64_map[t >> 18];
        destination[o + 1] = b64_map[(t >> 12) & 0x3F];
        destination[o + 2] = '=';
        destination[o + 3] = '=';
        o += 4;
    }
    else if (remaining == 2)
    {
        uint32_t t = ((uint32_t)source[i] << 16) | ((uint32_t)source[i + 1] << 8);
        destination[o] = b64_map[t >> 18];
        destination[o + 1] = b64_map[(t >> 12) & 0x3F];
        destination[o + 2] = b64_map[(t >> 6) & 0x3F];
        destination[o + 3] = '=';
        o += 4;
    }

    return o;
}

size_t b64_encode_scalar(const uint8_t *source, size_t source_length, uint8_t *destination)
{
    return b64_encode_scalar_from(source, source_length, destination, 0, 0);
}
