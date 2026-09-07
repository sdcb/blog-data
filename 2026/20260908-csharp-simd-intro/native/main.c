#include "b64.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

/* Sizes must match Base64Bench/Program.cs. */
static const size_t kSizes[] = {64, 4096, 49152, 1048576};
#define kSizeCount (sizeof(kSizes) / sizeof(kSizes[0]))
#define kReps 5

/* Budgets in SECONDS, mirroring the C# harness. Iteration counts are
 * calibrated at runtime rather than hard-coded, so both languages spend the
 * same wall-clock time per measurement regardless of how fast the kernel is. */
static const double kWarmupSeconds = 0.6;
static const double kTargetRepSeconds = 0.25;

/* Same LCG as Program.MakeData, so both harnesses encode identical input. */
static void make_data(uint8_t *destination, size_t length)
{
    uint32_t state = 12345u;
    for (size_t i = 0; i < length; i++)
    {
        state = state * 1664525u + 1013904223u;
        destination[i] = (uint8_t)(state >> 24);
    }
}

/* FNV-1a over the encoded output; compared against the C# harness to prove both
 * languages produce byte-identical Base64. */
static uint64_t fnv1a(const uint8_t *data, size_t length)
{
    uint64_t hash = 14695981039346656037ULL;
    for (size_t i = 0; i < length; i++)
    {
        hash ^= data[i];
        hash *= 1099511628211ULL;
    }
    return hash;
}

typedef size_t (*encode_fn)(const uint8_t *, size_t, uint8_t *);

typedef struct
{
    const char *name;
    encode_fn fn;
} impl_t;

static const impl_t kImpls[] = {
    {"c_scalar_auto", b64_encode_scalar},
    {"c_ssse3_manual", b64_encode_ssse3},
    {"c_avx2_manual", b64_encode_avx2},
};
#define kImplCount (sizeof(kImpls) / sizeof(kImpls[0]))

static double now_seconds(void)
{
    static LARGE_INTEGER frequency;
    if (frequency.QuadPart == 0)
        QueryPerformanceFrequency(&frequency);
    LARGE_INTEGER counter;
    QueryPerformanceCounter(&counter);
    return (double)counter.QuadPart / (double)frequency.QuadPart;
}

static int compare_double(const void *a, const void *b)
{
    double left = *(const double *)a, right = *(const double *)b;
    return (left > right) - (left < right);
}

/* Keeps the optimizer from discarding the encode calls. */
static volatile uint64_t g_sink;

static void warmup(encode_fn fn, const uint8_t *source, size_t n, uint8_t *destination)
{
    double start = now_seconds();
    long long calls = 0;
    do
    {
        for (int i = 0; i < 64; i++)
            g_sink += fn(source, n, destination);
        calls += 64;
    } while (now_seconds() - start < kWarmupSeconds || calls < 1000);
}

/* Estimates per-call cost as the MINIMUM over several short probes. A single
 * probe is not enough: one scheduling hiccup inflates the estimate, and an
 * earlier version of this harness then locked in a far-too-small iteration
 * count, making a rep a few ms instead of 250 ms and leaving the result
 * dominated by noise. */
static double probe_per_call_seconds(encode_fn fn, const uint8_t *source, size_t n,
                                    uint8_t *destination)
{
    double best = 1e300;

    for (int attempt = 0; attempt < 5; attempt++)
    {
        int probe = 16;
        for (;;)
        {
            double start = now_seconds();
            for (int i = 0; i < probe; i++)
                g_sink += fn(source, n, destination);
            double elapsed = now_seconds() - start;

            if (elapsed >= 0.002)
            {
                double per_call = elapsed / probe;
                if (per_call < best) best = per_call;
                break;
            }

            if (probe >= (1 << 22))
            {
                double per_call = elapsed / probe;
                if (per_call < best) best = per_call;
                break;
            }
            probe *= 4;
        }
    }

    return best;
}

/* Times one rep by running fixed-size chunks until the time budget is spent,
 * returning ns/op. Driven by elapsed time rather than a precomputed iteration
 * count, so a bad probe can no longer truncate the rep. */
static double measure_rep_ns(encode_fn fn, const uint8_t *source, size_t n,
                             uint8_t *destination, int chunk)
{
    double start = now_seconds();
    long long calls = 0;
    double elapsed;

    do
    {
        for (int i = 0; i < chunk; i++)
            g_sink += fn(source, n, destination);
        calls += chunk;
        elapsed = now_seconds() - start;
    } while (elapsed < kTargetRepSeconds);

    return elapsed * 1e9 / (double)calls;
}

static int run_verify(void)
{
    int failures = 0;

    size_t lengths[307];
    size_t count = 0;
    for (size_t n = 0; n <= 300; n++) lengths[count++] = n;
    lengths[count++] = 1023;
    lengths[count++] = 1024;
    lengths[count++] = 1025;
    lengths[count++] = 4096;
    lengths[count++] = 49152;
    lengths[count++] = 100000;

    uint8_t *source = (uint8_t *)malloc(100000);
    uint8_t *reference = (uint8_t *)malloc(b64_encoded_length(100000) + 64);
    uint8_t *actual = (uint8_t *)malloc(b64_encoded_length(100000) + 64);
    if (!source || !reference || !actual)
    {
        fprintf(stderr, "out of memory\n");
        free(source);
        free(reference);
        free(actual);
        return 2;
    }

    for (size_t li = 0; li < count; li++)
    {
        size_t n = lengths[li];
        make_data(source, n);
        size_t expected = b64_encode_scalar(source, n, reference);

        for (size_t ii = 1; ii < kImplCount; ii++)
        {
            memset(actual, 0xCC, b64_encoded_length(n) + 64);
            size_t written = kImpls[ii].fn(source, n, actual);

            if (written != expected)
            {
                printf("FAIL %s n=%zu: wrote %zu, expected %zu\n",
                       kImpls[ii].name, n, written, expected);
                failures++;
                continue;
            }
            if (memcmp(actual, reference, expected) != 0)
            {
                size_t at = 0;
                while (at < expected && actual[at] == reference[at]) at++;
                printf("FAIL %s n=%zu: first diff at %zu (got '%c', want '%c')\n",
                       kImpls[ii].name, n, at, actual[at], reference[at]);
                failures++;
            }
        }
    }

    if (failures == 0)
        printf("OK  ssse3/avx2 match scalar across %zu lengths\n", count);

    free(source);
    free(reference);
    free(actual);
    return failures == 0 ? 0 : 1;
}

/* Prints an FNV-1a hash per size so the C# harness can confirm equality. */
static int run_hash(void)
{
    printf("impl,bytes,fnv1a\n");
    for (size_t si = 0; si < kSizeCount; si++)
    {
        size_t n = kSizes[si];
        uint8_t *source = (uint8_t *)malloc(n ? n : 1);
        uint8_t *destination = (uint8_t *)malloc(b64_encoded_length(n) + 64);
        if (!source || !destination)
        {
            fprintf(stderr, "out of memory\n");
            free(source);
            free(destination);
            return 2;
        }
        make_data(source, n);

        for (size_t ii = 0; ii < kImplCount; ii++)
        {
            size_t written = kImpls[ii].fn(source, n, destination);
            printf("%s,%zu,%016llx\n", kImpls[ii].name, n,
                   (unsigned long long)fnv1a(destination, written));
        }

        free(source);
        free(destination);
    }
    return 0;
}

/* Benchmarks exactly one implementation at one size so the caller can run each
 * in a fresh process. Running all impls in one process lets the slow scalar
 * kernel heat the CPU and depress boost clocks for whatever runs next. */
static int run_bench_one(const char *want, const char *size_text)
{
    long long parsed = atoll(size_text);
    if (parsed < 0)
    {
        fprintf(stderr, "size must be non-negative\n");
        return 2;
    }
    size_t n = (size_t)parsed;

    for (size_t ii = 0; ii < kImplCount; ii++)
    {
        if (strcmp(kImpls[ii].name, want) != 0)
            continue;

        encode_fn fn = kImpls[ii].fn;
        uint8_t *source = (uint8_t *)malloc(n ? n : 1);
        uint8_t *destination = (uint8_t *)malloc(b64_encoded_length(n) + 64);
        if (!source || !destination)
        {
            fprintf(stderr, "out of memory\n");
            free(source);
            free(destination);
            return 2;
        }
        make_data(source, n);

        warmup(fn, source, n, destination);

        /* Chunk size only controls how often the clock is read, not how long the
         * rep runs; each rep still runs until kTargetRepSeconds is spent. */
        double per_call = probe_per_call_seconds(fn, source, n, destination);
        if (per_call < 1e-12) per_call = 1e-12;
        double chunk_estimate = 0.005 / per_call;
        if (chunk_estimate < 1.0) chunk_estimate = 1.0;
        if (chunk_estimate > (double)(1 << 20)) chunk_estimate = (double)(1 << 20);
        int chunk = (int)chunk_estimate;

        double samples[kReps];
        for (int rep = 0; rep < kReps; rep++)
            samples[rep] = measure_rep_ns(fn, source, n, destination, chunk);

        qsort(samples, kReps, sizeof(double), compare_double);
        double median = samples[kReps / 2];
        double mbps = (double)n / (median / 1e9) / (1024.0 * 1024.0);

        printf("%s,%zu,%d,%.2f,%.2f,%.0f\n",
               kImpls[ii].name, n, chunk, median, samples[0], mbps);

        free(source);
        free(destination);
        return 0;
    }

    fprintf(stderr, "unknown impl '%s'\n", want);
    return 2;
}

int main(int argc, char **argv)
{
    const char *mode = argc > 1 ? argv[1] : "verify";

    if (strcmp(mode, "verify") == 0) return run_verify();
    if (strcmp(mode, "hash") == 0) return run_hash();
    if (strcmp(mode, "bench1") == 0)
    {
        if (argc < 4)
        {
            fprintf(stderr, "usage: bench1 <impl> <bytes>\n");
            return 2;
        }
        return run_bench_one(argv[2], argv[3]);
    }

    fprintf(stderr, "unknown mode '%s'; expected verify | bench1 | hash\n", mode);
    return 2;
}
