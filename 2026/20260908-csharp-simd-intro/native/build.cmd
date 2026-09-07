@echo off
setlocal

rem Base64 C benchmark. Each ISA kernel is its own translation unit because
rem MSVC applies /arch:* per file -- exactly why lw.PPOCR.C splits its kernels
rem into sse2_*.c / avx2_*.c / neon_*.c / lsx_*.c.

if "%VCVARS%"=="" set VCVARS=C:\Program Files\Microsoft Visual Studio\18\Enterprise\VC\Auxiliary\Build\vcvars64.bat

if not exist "%VCVARS%" (
    echo ERROR: vcvars64.bat not found at "%VCVARS%".
    echo Set VCVARS to your Visual Studio location and retry.
    exit /b 1
)

if not defined VSCMD_ARG_TGT_ARCH call "%VCVARS%" >nul || exit /b 1

cd /d "%~dp0"
if not exist out mkdir out

set COMMON=/nologo /O2 /W4 /WX /fp:strict /GS- /MT /I.

rem Scalar gets /arch:AVX2 too, so the auto-vectorizer has every advantage.
cl %COMMON% /arch:AVX2 /c b64_scalar.c /Foout\b64_scalar.obj || exit /b 1

rem SSSE3 intrinsics need no /arch flag on x64 (SSE2 is baseline).
cl %COMMON% /c b64_ssse3.c /Foout\b64_ssse3.obj || exit /b 1

cl %COMMON% /arch:AVX2 /c b64_avx2.c /Foout\b64_avx2.obj || exit /b 1

cl %COMMON% /c main.c /Foout\main.obj || exit /b 1

link /nologo /OUT:out\b64bench.exe out\b64_scalar.obj out\b64_ssse3.obj out\b64_avx2.obj out\main.obj || exit /b 1

echo Built out\b64bench.exe
cl 2>&1 | findstr /C:"Version"
