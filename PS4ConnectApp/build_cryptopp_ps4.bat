@echo off
SETLOCAL EnableDelayedExpansion

REM Use your exact environment variable
set OO_PS4_TOOLCHAIN=H:\PS4Toolchain

REM Verify toolchain exists
if not exist "%OO_PS4_TOOLCHAIN%" (
    echo ERROR: OpenOrbis toolchain not found at %OO_PS4_TOOLCHAIN%
    echo Please check your OO_PS4_TOOLCHAIN environment variable.
    exit /b 1
)

echo Building Crypto++ for PS4 using OpenOrbis...
echo Toolchain: %OO_PS4_TOOLCHAIN%

REM Create output directory matching your pattern
set intdir=ps4_build
if not exist %intdir% mkdir %intdir%

REM Clear any existing object files
del /q %intdir%\*.o 2>nul

REM Set compiler flags EXACTLY matching your build.bat pattern
set COMMON_FLAGS=--target=x86_64-pc-freebsd12-elf -fPIC -funwind-tables -DCRYPTOPP_DISABLE_ASM -D__PS4__ -DORBIS -O2
set INCLUDES=-I"%OO_PS4_TOOLCHAIN%\include" -I"%OO_PS4_TOOLCHAIN%\include\c++\v1"

echo Compiling Crypto++ source files...

REM Compile ALL .cpp files (Crypto++ has hundreds!)
for %%f in (*.cpp) do (
    echo   %%~nf.cpp...
    clang++ %COMMON_FLAGS% %INCLUDES% -c -o %intdir%\%%~nf.o %%~nf.cpp
    if errorlevel 1 (
        echo Failed to compile %%~nf.cpp
        exit /b 1
    )
)

REM Also compile any .c files (Crypto++ has a few)
for %%f in (*.c) do (
    echo   %%~nf.c...
    clang %COMMON_FLAGS% %INCLUDES% -c -o %intdir%\%%~nf.o %%~nf.c
    if errorlevel 1 (
        echo Failed to compile %%~nf.c
        exit /b 1
    )
)

REM Create static library using your LLVM ar
echo Creating static library...
cd %intdir%

REM Use LLVM ar from your toolchain
"%OO_PS4_TOOLCHAIN%\LLVM\bin\llvm-ar" rcs libcryptopp_ps4.a *.o
if errorlevel 1 (
    echo Failed to create library
    cd ..
    exit /b 1
)

cd ..

echo.
echo ========================================
echo SUCCESS! Library created at:
echo %CD%\%intdir%\libcryptopp_ps4.a
echo ========================================
echo.
echo To use in your PS4 project:
echo 1. Add to your libraries: -l:"cryptopp_ps4" -L"%CD%\%intdir%"
echo 2. Add to your includes: -I"%CD%"
echo.