@echo off
dotnet publish Apollo -c Release --no-self-contained -r win-x64 -f net8.0 -o "./Apollo/bin/Publish/" -p:PublishReadyToRun=false -p:PublishSingleFile=true -p:DebugType=None -p:GenerateDocumentationFile=false -p:DebugSymbols=false
pause