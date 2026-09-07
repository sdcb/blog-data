# Runs every (implementation, size) pair in its own process.
#
# Why: running all implementations inside one process lets the slow scalar
# kernel heat the CPU and depress boost clocks for whatever runs next, which
# inflated the vector numbers by ~30%. Each measurement now gets a fresh
# process, C# and C are interleaved so neither systematically follows the
# scalar kernel, and the whole matrix runs 3 times.

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$cs = Join-Path $root 'Base64Bench\bin\Release\net10.0\Base64Bench.exe'
$c = Join-Path $root 'native\out\b64bench.exe'

foreach ($exe in @($cs, $c)) {
    if (-not (Test-Path $exe)) { throw "missing $exe -- build first" }
}

$sizes = @(64, 4096, 49152, 1048576)

# Interleaved so C# and C alternate.
$plan = @(
    @{ Exe = $cs; Impl = 'cs_scalar' }
    @{ Exe = $c;  Impl = 'c_scalar_auto' }
    @{ Exe = $cs; Impl = 'cs_vector128' }
    @{ Exe = $c;  Impl = 'c_ssse3_manual' }
    @{ Exe = $cs; Impl = 'cs_vector256' }
    @{ Exe = $c;  Impl = 'c_avx2_manual' }
    @{ Exe = $cs; Impl = 'cs_bcl' }
)

# 5 passes, not 3: at 1 MB roughly one process in three still lands on a slow
# stretch (background load or thermal), and a 5-pass median absorbs that.
$Passes = 5
$rows = @{}

for ($pass = 1; $pass -le $Passes; $pass++) {
    Write-Host "--- pass $pass/$Passes ---"
    foreach ($size in $sizes) {
        foreach ($entry in $plan) {
            $line = & $entry.Exe bench1 $entry.Impl $size
            if ($LASTEXITCODE -ne 0) { throw "failed: $($entry.Impl) $size" }

            $parts = $line.Split(',')
            $key = "$($parts[0])|$($parts[1])"
            if (-not $rows.ContainsKey($key)) {
                $rows[$key] = [ordered]@{
                    impl = $parts[0]; bytes = [int]$parts[1]
                    chunk = [int]$parts[2]; medians = @()
                }
            }
            # Chunk size is probed per process (how often the clock is read), so
            # it can differ between passes; keep the most recent for the record.
            $rows[$key].chunk = [int]$parts[2]
            $rows[$key].medians += [double]$parts[3]

            Write-Host ("  {0,-16} {1,8}  {2,12} ns" -f $parts[0], $parts[1], $parts[3])

            # Brief settle so one measurement's heat does not bias the next.
            Start-Sleep -Milliseconds 400
        }
    }
}

# Median across passes.
$out = foreach ($key in $rows.Keys) {
    $r = $rows[$key]
    $sorted = $r.medians | Sort-Object
    $median = $sorted[[int]([math]::Floor($sorted.Count / 2))]
    [pscustomobject]@{
        impl = $r.impl
        bytes = $r.bytes
        chunk = $r.chunk
        median_ns = [math]::Round($median, 2)
        all_passes = ($r.medians | ForEach-Object { [math]::Round($_, 2) }) -join ' '
        mb_per_s = [math]::Round($r.bytes / ($median / 1e9) / 1MB, 0)
    }
}

$out = $out | Sort-Object bytes, impl
$dest = Join-Path $root 'results\combined-bench.csv'
$out | Export-Csv -Path $dest -NoTypeInformation -Encoding utf8

Write-Host ''
Write-Host "wrote $dest"
$out | Format-Table -AutoSize
