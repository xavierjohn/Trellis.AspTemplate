@echo off
dotnet build template\TrellisAspTemplate.slnx -c Release
IF ERRORLEVEL 1 ( EXIT /B %ERRORLEVEL% )
dotnet test --solution template\TrellisAspTemplate.slnx -c Release --no-build
IF ERRORLEVEL 1 ( EXIT /B %ERRORLEVEL% )
dotnet pack templatepack.csproj -c Release -o nupkg -p:PublicRelease=true
