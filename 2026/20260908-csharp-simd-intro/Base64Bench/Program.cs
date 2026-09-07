using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Running;

namespace Base64Bench;

public static class Program
{
    /// <summary>Sizes shared with the C harness (bytes).</summary>
    internal static readonly int[] Sizes = [64, 4096, 49152, 1048576];

    internal const int Reps = 5;

    /// <summary>
    /// Warmup and per-rep budgets are expressed in SECONDS, not call counts.
    /// A call-count warmup is what broke the first version of this harness: at
    /// 1 MB, 500 calls is about 20 ms, which is shorter than the tiered
    /// compilation promotion delay, so the timed loop partly measured
    /// unpromoted R2R code built for a baseline ISA. That understated
    /// Base64.EncodeToUtf8 at 1 MB by more than 3x.
    /// </summary>
    private const double WarmupSeconds = 0.6;

    private const double TargetRepSeconds = 0.25;

    public static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "verify";

        return mode switch
        {
            "verify" => Verify(),
            "bench1" => Bench1(args),
            "isa" => Isa(),
            "hash" => Hash(),
            "bdn" => Bdn(args),
            _ => Usage(mode),
        };
    }

    private static int Usage(string mode)
    {
        Console.Error.WriteLine($"unknown mode '{mode}'; expected verify | bench1 | isa | hash | bdn");
        return 2;
    }

    /// <summary>
    /// Deterministic LCG (Numerical Recipes constants) so the C harness can
    /// generate byte-identical input without shipping a data file.
    /// </summary>
    internal static byte[] MakeData(int length)
    {
        byte[] data = new byte[length];
        uint state = 12345u;
        for (int i = 0; i < length; i++)
        {
            state = state * 1664525u + 1013904223u;
            data[i] = (byte)(state >> 24);
        }
        return data;
    }

    private delegate int EncodeFn(ReadOnlySpan<byte> source, Span<byte> destination);

    private static readonly (string Name, EncodeFn Fn)[] Impls =
    [
        ("cs_scalar", Base64Kernels.EncodeScalar),
        ("cs_vector128", Base64Kernels.EncodeVector128),
        ("cs_vector256", Base64Kernels.EncodeVector256),
        ("cs_bcl", static (s, d) =>
        {
            Base64.EncodeToUtf8(s, d, out _, out int written);
            return written;
        }),
    ];

    private static double Seconds(long from) =>
        (Stopwatch.GetTimestamp() - from) / (double)Stopwatch.Frequency;

    /// <summary>Runs until the wall-clock warmup budget is spent.</summary>
    private static void Warmup(EncodeFn fn, byte[] src, byte[] dst)
    {
        long start = Stopwatch.GetTimestamp();
        long calls = 0;
        do
        {
            for (int i = 0; i < 64; i++) fn(src, dst);
            calls += 64;
        }
        while (Seconds(start) < WarmupSeconds || calls < 1000);
    }

    /// <summary>
    /// Estimates per-call cost as the MINIMUM over several short probes. A single
    /// probe is not enough: one scheduling hiccup inflates the estimate, and an
    /// earlier version of this harness then locked in a far-too-small iteration
    /// count (33,985 instead of ~1.3M at 4 KB), making each rep ~6 ms instead of
    /// 250 ms and leaving the result dominated by noise.
    /// </summary>
    private static double ProbePerCallSeconds(EncodeFn fn, byte[] src, byte[] dst)
    {
        double best = double.MaxValue;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            int probe = 16;
            while (true)
            {
                long start = Stopwatch.GetTimestamp();
                for (int i = 0; i < probe; i++) fn(src, dst);
                double elapsed = Seconds(start);

                // Need a window well above timer granularity to be meaningful.
                if (elapsed >= 0.002)
                {
                    best = Math.Min(best, elapsed / probe);
                    break;
                }

                if (probe >= 1 << 22) { best = Math.Min(best, elapsed / probe); break; }
                probe *= 4;
            }
        }

        return best;
    }

    /// <summary>
    /// Times one rep by running fixed-size chunks until the time budget is spent,
    /// returning ns/op. Because the loop is driven by elapsed time rather than a
    /// precomputed iteration count, a bad probe can no longer truncate the rep.
    /// </summary>
    private static double MeasureRepNs(EncodeFn fn, byte[] src, byte[] dst, int chunk)
    {
        long start = Stopwatch.GetTimestamp();
        long calls = 0;
        double elapsed;

        do
        {
            for (int i = 0; i < chunk; i++) fn(src, dst);
            calls += chunk;
            elapsed = Seconds(start);
        }
        while (elapsed < TargetRepSeconds);

        return elapsed * 1e9 / calls;
    }

    // ------------------------------------------------------------- verify

    private static int Verify()
    {
        int failures = 0;

        // Every length 0..300 exercises all tail cases and every kernel's
        // vector-to-scalar handoff, plus a few larger sizes.
        List<int> lengths = [];
        for (int n = 0; n <= 300; n++) lengths.Add(n);
        lengths.AddRange([1023, 1024, 1025, 4096, 49152, 100_000]);

        foreach (int n in lengths)
        {
            byte[] src = MakeData(n);
            string expected = Convert.ToBase64String(src);

            foreach ((string name, EncodeFn fn) in Impls)
                failures += Check(name, n, expected, src, fn);
        }

        Console.WriteLine(failures == 0
            ? $"OK  all kernels bit-exact vs Convert.ToBase64String across {lengths.Count} lengths"
            : $"FAIL  {failures} mismatch(es)");

        return failures == 0 ? 0 : 1;
    }

    private static int Check(string name, int n, string expected, byte[] src, EncodeFn fn)
    {
        byte[] dst = new byte[Base64Kernels.EncodedLength(n) + 64];
        dst.AsSpan().Fill(0xCC);

        int written = fn(src, dst);
        string actual = System.Text.Encoding.ASCII.GetString(dst, 0, written);

        if (written != expected.Length)
        {
            Console.WriteLine($"FAIL {name} n={n}: wrote {written}, expected {expected.Length}");
            return 1;
        }

        if (actual != expected)
        {
            int at = 0;
            while (at < actual.Length && actual[at] == expected[at]) at++;
            Console.WriteLine($"FAIL {name} n={n}: first diff at {at} " +
                $"(got '{actual[at]}', want '{expected[at]}')");
            return 1;
        }

        // Guard against writing past the reported length.
        for (int i = written; i < dst.Length; i++)
        {
            if (dst[i] != 0xCC)
            {
                Console.WriteLine($"FAIL {name} n={n}: overwrote destination at +{i - written}");
                return 1;
            }
        }

        return 0;
    }

    // --------------------------------------------------------------- hash

    /// <summary>
    /// FNV-1a over the encoded output, matching the C harness's `hash` mode.
    /// Equal hashes across the two languages prove byte-identical Base64.
    /// </summary>
    private static int Hash()
    {
        Console.WriteLine("impl,bytes,fnv1a");

        foreach (int n in Sizes)
        {
            byte[] src = MakeData(n);
            byte[] dst = new byte[Base64Kernels.EncodedLength(n) + 64];

            foreach ((string name, EncodeFn fn) in Impls)
            {
                int written = fn(src, dst);
                ulong hash = 14695981039346656037UL;
                for (int i = 0; i < written; i++)
                {
                    hash ^= dst[i];
                    hash *= 1099511628211UL;
                }
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{name},{n},{hash:x16}"));
            }
        }

        return 0;
    }

    // ------------------------------------------------------------- bench1

    /// <summary>
    /// Benchmarks exactly one implementation at one size, so the caller can run
    /// each in a fresh process. Running all impls in one process lets the slow
    /// scalar kernel heat the CPU and depress boost clocks for whatever follows,
    /// which inflated the vector numbers by 20-30% in earlier runs.
    /// </summary>
    private static int Bench1(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: bench1 <impl> <bytes>");
            return 2;
        }

        string want = args[1];
        int n = int.Parse(args[2], CultureInfo.InvariantCulture);
        if (n < 0)
        {
            Console.Error.WriteLine("size must be non-negative");
            return 2;
        }

        foreach ((string name, EncodeFn fn) in Impls)
        {
            if (name != want) continue;

            byte[] src = MakeData(n);
            byte[] dst = new byte[Base64Kernels.EncodedLength(n)];

            Warmup(fn, src, dst);

            // Chunk size only controls how often the clock is read, not how long
            // the rep runs; each rep still runs until TargetRepSeconds is spent.
            double perCall = ProbePerCallSeconds(fn, src, dst);
            int chunk = (int)Math.Clamp(0.005 / Math.Max(perCall, 1e-12), 1, 1 << 20);

            double[] samples = new double[Reps];
            for (int rep = 0; rep < Reps; rep++)
                samples[rep] = MeasureRepNs(fn, src, dst, chunk);

            Array.Sort(samples);
            double median = samples[Reps / 2];
            double mbps = n / (median / 1e9) / (1024 * 1024);

            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{name},{n},{chunk},{median:F2},{samples[0]:F2},{mbps:F0}"));
            return 0;
        }

        Console.Error.WriteLine($"unknown impl '{want}'");
        return 2;
    }

    // ---------------------------------------------------------------- isa

    /// <summary>
    /// Reports the effective ISA plus BCL throughput, for the
    /// DOTNET_Enable* downgrade table. One compiled artifact, no recompile.
    /// </summary>
    private static int Isa()
    {
        const int N = 49152;

        byte[] src = MakeData(N);
        byte[] dst = new byte[Base64Kernels.EncodedLength(N)];

        EncodeFn bcl = static (s, d) =>
        {
            Base64.EncodeToUtf8(s, d, out _, out int written);
            return written;
        };

        Warmup(bcl, src, dst);

        double perCall = ProbePerCallSeconds(bcl, src, dst);
        int chunk = (int)Math.Clamp(0.005 / Math.Max(perCall, 1e-12), 1, 1 << 20);

        double best = double.MaxValue;
        for (int rep = 0; rep < Reps; rep++)
            best = Math.Min(best, MeasureRepNs(bcl, src, dst, chunk));

        string isa =
            Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported ? "avx512vbmi"
            : Avx2.IsSupported ? "avx2"
            : Ssse3.IsSupported ? "ssse3"
            : Vector128.IsHardwareAccelerated ? "vector128"
            : "scalar";

        double mbps = N / (best / 1e9) / (1024 * 1024);

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"isa={isa} v512hw={Vector512.IsHardwareAccelerated} vbmi={Avx512Vbmi.IsSupported} " +
            $"avx2={Avx2.IsSupported} ssse3={Ssse3.IsSupported} v128hw={Vector128.IsHardwareAccelerated} " +
            $"| {best / 1000:F1} us | {mbps:F0} MB/s"));

        return 0;
    }

    // ---------------------------------------------------------------- bdn

    private static int Bdn(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args[1..]);
        return 0;
    }
}
