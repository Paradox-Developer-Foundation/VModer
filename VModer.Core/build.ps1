$serverOutputDir = "..\VModer.Extensions\client\server"

dotnet publish -r win-x64 -o .\bin\publish\win-x64 ./VModer.Core.csproj
dotnet publish -r linux-x64 -o .\bin\publish\linux-x64 ./VModer.Core.csproj
dotnet publish -r osx-x64 -o .\bin\publish\osx-x64 ./VModer.Core.csproj
Remove-Item -Path $serverOutputDir -Recurse -Force
Copy-Item -Path .\bin\publish -Destination $serverOutputDir -Recurse