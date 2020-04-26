@echo off

cd %~dp0
rem xcopy "D:\MVC\IonMVC\IonMVC\Classes\Ion\*.*" "..\Src\Ion\" /E /Y

if "%1"=="Debug" exit
echo ######## Packing '/res' directory to 'ArchiveResource.cs' ########
packfolder.exe ../res ../ArchiveResource.cs -csharp -x "*IconBundler*;*sciter.dll"