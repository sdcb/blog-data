# Base64 SIMD 实验：复现步骤

配套文章：`../index.md`

对照 C# 与 C 各三种 Base64 编码实现（标量 / 128 位 / 256 位），
外加 BCL 的 `Base64.EncodeToUtf8` 作为"微软调过的天花板"。

## 环境

- Windows 11，AMD Ryzen 7 5800X（Zen 3，8C/16T，**无 AVX-512**）
- .NET 10（`net10.0`）
- MSVC 19.51 x64（Visual Studio 18），经 `vcvars64.bat`

## 目录

| 路径 | 说明 |
| --- | --- |
| `Base64Bench/Base64Kernels.cs` | C# 三种内核（标量 / `Vector128`+SSSE3 / `Vector256`+AVX2） |
| `Base64Bench/Program.cs` | 正确性校验、手写计时器、ISA 降级探针 |
| `Base64Bench/Base64Benchmarks.cs` | BenchmarkDotNet 交叉验证 |
| `native/b64_scalar.c` | C 标量（交给编译器自动向量化） |
| `native/b64_ssse3.c` | C 手写 SSSE3 |
| `native/b64_avx2.c` | C 手写 AVX2 |
| `native/main.c` | C 侧校验 + 计时器 |
| `native/build.cmd` | 逐文件设置 `/arch:` 的编译脚本 |
| `run-bench.ps1` | 主驱动：每个测量点独立进程 |

C 侧刻意**每个指令集一个 `.c` 文件**：MSVC 的 `/arch:` 是按翻译单元生效的，
这正是 `lw.PPOCR.C` 把内核拆成 `sse2_*.c` / `avx2_*.c` / `neon_*.c` / `lsx_*.c` 的原因。

## 构建

```powershell
# C#
cd Base64Bench
dotnet build -c Release

# C（若 VS 装在别处，先 $env:VCVARS="...\vcvars64.bat"）
cd ..\native
.\build.cmd
```

## 正确性（先跑这个）

两侧都必须**逐字节**等于 `Convert.ToBase64String`，不是"近似"。
覆盖长度 0~300（穷举所有尾部情形与向量→标量交接）外加 1023/1024/1025/4096/49152/100000。

```powershell
.\Base64Bench\bin\Release\net10.0\Base64Bench.exe verify
.\native\out\b64bench.exe verify
```

跨语言一致性用 FNV-1a 哈希比对（两侧用同一个 LCG 生成输入，无需共享数据文件）：

```powershell
.\Base64Bench\bin\Release\net10.0\Base64Bench.exe hash
.\native\out\b64bench.exe hash
```

7 个实现在 4 种尺寸下哈希应全部相同。

## 性能

```powershell
pwsh -NoProfile -File .\run-bench.ps1
```

输出 `results/combined-bench.csv`。

这套计时器被两个真实的方法学 bug 教育过，两个都是 BenchmarkDotNet 先发现的
（BDN 每个 benchmark 用独立进程、且按时长而非次数预热，所以它两个都不中招）：

**1. 进程内互相污染。** 把所有实现放在一个进程里连续跑，慢的标量内核会把 CPU 烤热、
压低后续内核的 boost 频率——实测让向量内核虚高约 20~30%（4KB 下 `cs_vector256`
208ns → 170ns）。所以 `bench1` 模式一次只测一个 (实现, 尺寸) 组合，
C#/C 交替执行（避免某一方系统性地紧跟标量内核），整个矩阵跑 3 遍取中位数。

**2. 按"调用次数"预热。** 最初 1MB 档只预热 500 次调用 ≈ 20ms，
短于分层编译的提升延迟，导致计时循环有一部分跑在**未提升的 R2R 代码**上
（框架代码如 `Base64.EncodeToUtf8` 是按基线指令集预编译的）。
后果是 1MB 档的 BCL 被低估 5 倍（192µs vs 实际 ~37µs）。
现在预热以**秒**为单位（0.6s）。

**3. 一次性标定迭代次数。** 修掉 2 之后仍有残留：标定只探测一次，
一个调度抖动就会把 per-call 估高、从而永久锁定一个过小的迭代数
（4KB 档 `cs_bcl` 只跑了 33985 次 ≈ 6ms，而非目标的 250ms），
结果被噪声主导。现在改成两条：per-call 取 **5 次探测的最小值**；
每轮**按墙钟时间**跑满 0.25s（分块读表，边跑边数），
所以坏探测再也不能截断一轮。CSV 里第三列是 **chunk（读表间隔）**，
不是迭代次数——它每次跑会不同，属预期。

两侧用完全相同的方法学：同样的预热时长、同样的探测逻辑、同样的每轮时长、
同样取 5 轮中位数，整个矩阵再跑 5 遍取中位数
（1MB 档约每三个进程仍有一个会撞上慢时段，5 遍中位数能吸收掉）。

## ISA 降级（同一个编译产物，零重编译）

```powershell
$exe = ".\Base64Bench\bin\Release\net10.0\Base64Bench.exe"
& $exe isa
$env:DOTNET_EnableAVX2=0;        & $exe isa; Remove-Item Env:\DOTNET_EnableAVX2
$env:DOTNET_EnableHWIntrinsic=0; & $exe isa; Remove-Item Env:\DOTNET_EnableHWIntrinsic
```

注：`DOTNET_EnableSSSE3=0` 在 .NET 10 上无效（`Ssse3.IsSupported` 仍为 True），
所以只有上面三档可用。

## 关于 BCL 那一行的已知分歧（重要）

我的计时器和 BDN 在**我手写的三个内核**上一致（`Vector256` @48KB：1954 vs 1967 ns，差 0.7%），
但在 `Base64.EncodeToUtf8` 上系统性分歧：

| @48KB | 我的 harness | BDN |
| --- | ---: | ---: |
| `Vector256`（手写） | 1954 ns | 1967 ns |
| `Bcl` | **1664 ns** | **2546 ns** |

已排查清楚，两边都不是 bug：

- 不是"前面的慢内核烤热 CPU"——单独只跑 `Bcl` 的 BDN 结果不变（2546 ns）
- `DOTNET_TieredPGO=0` 时我的 harness 测出 2445 ns，**接近 BDN**
  → 分歧来自动态 PGO 对调用点的去虚化/内联特化程度不同

顺带挖出一个值得知道的事实（`isa` 那组数据的机制）：

| 配置 | `Bcl` @48KB | 解释 |
| --- | ---: | --- |
| 默认 | 1670 ns | R2R 起步，tier-1 重编译后拿到 AVX2 |
| `TieredCompilation=0` | **4406 ns** | R2R 代码按**基线指令集**编译，且永不重编译 → 卡在慢版本 |
| `TieredCompilation=0` + `ReadyToRun=0` | 1666 ns | 绕过 R2R，直接全优化 JIT → 拿到 AVX2 |

**结论**：主对比表（C# vs C）只用我的 harness，因为两侧方法学完全相同、
且 BDN 已交叉验证过我手写内核的数字。BCL 那一行只当"同一量级的参照"，
**不声称"我比 BCL 快"或"BCL 比我快"**——这个先后顺序会随测量方式翻转，不能拿来当结论。

## BenchmarkDotNet 交叉验证

```powershell
.\Base64Bench\bin\Release\net10.0\Base64Bench.exe bdn --job short --filter *
```

BDN 每个 benchmark 用独立进程，正是它先暴露了单进程计时被污染的问题。
