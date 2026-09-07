using System.Buffers.Text;
using BenchmarkDotNet.Attributes;

namespace Base64Bench;

/// <summary>
/// BenchmarkDotNet cross-check on the hand-rolled timer in <see cref="Program"/>.
/// If the two disagree on ordering or ratios, the hand-rolled numbers are wrong.
/// </summary>
[MemoryDiagnoser]
public class Base64Benchmarks
{
    private byte[] _source = [];
    private byte[] _destination = [];

    [Params(64, 4096, 49152, 1048576)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        _source = Program.MakeData(N);
        _destination = new byte[Base64Kernels.EncodedLength(N)];
    }

    [Benchmark]
    public int Scalar() => Base64Kernels.EncodeScalar(_source, _destination);

    [Benchmark]
    public int Vector128() => Base64Kernels.EncodeVector128(_source, _destination);

    [Benchmark]
    public int Vector256() => Base64Kernels.EncodeVector256(_source, _destination);

    [Benchmark(Baseline = true)]
    public int Bcl()
    {
        Base64.EncodeToUtf8(_source, _destination, out _, out int written);
        return written;
    }
}
