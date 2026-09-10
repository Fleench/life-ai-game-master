#!/bin/bash
set -e
cd "/run/media/glenn/shared-files/Projects/LIFE_AI_APPS/game-master/C#"
dotnet add GameMaster.Core package Microsoft.Extensions.Logging.Abstractions
dotnet add GameMaster.Data package Microsoft.Extensions.Logging
dotnet add GameMaster.Data package BCrypt.Net-Next
dotnet add GameMaster.Desktop package Serilog.AspNetCore
dotnet add GameMaster.Desktop package Serilog.Sinks.File
dotnet add GameMaster.Api package System.ComponentModel.Annotations
