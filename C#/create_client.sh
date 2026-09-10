#!/bin/bash
cd /run/media/glenn/shared-files/Projects/LIFE_AI_APPS/game-master/C#

# Create client project
dotnet new classlib -n GameMaster.Client -f net8.0
rm GameMaster.Client/Class1.cs

# Create tests project
dotnet new xunit -n GameMaster.Client.Tests -f net8.0

# Add to solution
dotnet sln GameMaster.sln add GameMaster.Client/GameMaster.Client.csproj
dotnet sln GameMaster.sln add GameMaster.Client.Tests/GameMaster.Client.Tests.csproj

# Add packages
cd GameMaster.Client
dotnet add package Microsoft.Extensions.Http
dotnet add package Microsoft.Extensions.Http.Polly
dotnet add package System.Text.Json

cd ../GameMaster.Client.Tests
dotnet add reference ../GameMaster.Client/GameMaster.Client.csproj
dotnet add package Moq
dotnet add package RichardSzalay.MockHttp
