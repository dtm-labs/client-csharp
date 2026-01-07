#!/bin/bash

nugetpkgs=~/temp/nugetpkgs
mkdir -p $nugetpkgs && rm -rf $nugetpkgs/*

dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/Dtmcli/Dtmcli.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/strong-name/Dtmcli.StrongName/Dtmcli.StrongName.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/Dtmgrpc/Dtmgrpc.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/strong-name/Dtmgrpc.StrongName/Dtmgrpc.StrongName.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/DtmCommon/DtmCommon.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/strong-name/DtmCommon.StrongName/DtmCommon.StrongName.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/DtmSERedisBarrier/DtmSERedisBarrier.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/DtmMongoBarrier/DtmMongoBarrier.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/DtmDapr/DtmDapr.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/Dtmworkflow/Dtmworkflow.csproj
dotnet build --configuration Release --source https://api.nuget.org/v3/index.json src/strong-name/Dtmworkflow.StrongName/Dtmworkflow.StrongName.csproj


ver=alpha`date +%Y%m%d%H%M%S`

dotnet pack src/Dtmcli/Dtmcli.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/strong-name/Dtmcli.StrongName/Dtmcli.StrongName.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/Dtmgrpc/Dtmgrpc.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/strong-name/Dtmgrpc.StrongName/Dtmgrpc.StrongName.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/DtmCommon/DtmCommon.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/strong-name/DtmCommon.StrongName/DtmCommon.StrongName.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/DtmSERedisBarrier/DtmSERedisBarrier.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/DtmMongoBarrier/DtmMongoBarrier.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/DtmDapr/DtmDapr.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/Dtmworkflow/Dtmworkflow.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build
dotnet pack src/strong-name/Dtmworkflow.StrongName/Dtmworkflow.StrongName.csproj --version-suffix $ver -o $nugetpkgs -c Release --no-build

for file in $nugetpkgs/*.nupkg
do
    dotnet nuget push $file -k <token> --skip-duplicate -s <nuget feed>
done
