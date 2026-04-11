@echo off
git -C "%~dp0." clean -fxd
git -C "%~dp0ext\art" clean -fxd
set /p kix_version=<"%~dp0version.txt"
mkdir "%~dp0build\kix"
dotnet publish "%~dp0src\kix" -p Version="%kix_version%" -c Release -f net10.0 /p:PublishSingleFile=true --self-contained -p GenerateDocumentationFile=false -o "%~dp0build\kix"