@echo off
git -C "%~dp0." clean -fxd
git -C "%~dp0ext\art" clean -fxd
git -C "%~dp0ext\ConFormat" clean -fxd
set /p kix_version=<"%~dp0version.txt"
mkdir "%~dp0build\package"
dotnet pack "%~dp0src\kix" -p Version="%kix_version%" -p DebugSymbols=False -p DebugType=None -p GenerateDocumentationFile=false -o "%~dp0build\package"
