@echo off

set BUILD_CONFIG=%1
set BUILD_TARGET_DIR=%2
set BUILD_PLATFORM=%3

cd %~dp0
xcopy "..\Shared" "%2\Shared\" /E /Y
xcopy "D:\IonMVC\IonMVC\Classes\Ion\*.*" "..\Src\Ion\" /E /Y

echo ######## SharpFont fix ########
copy "..\..\packages\SharpFont.Dependencies.2.6\bin\msvc12\%BUILD_PLATFORM%\freetype6.dll" "..\bin\%BUILD_CONFIG%\freetype6.dll"