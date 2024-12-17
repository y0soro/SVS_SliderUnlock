#!/usr/bin/env bash
set -e

DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)

cd $DIR

dotnet build --configuration Release -p:ContinuousIntegrationBuild=true -p:PublishTrimmed=false

name="SVS_SliderUnlock"
version=$(sed -n 's/.*<Version>\(.*\)<.*/\1/p' ${name}.csproj | tr -d '\n')

tmp=$(mktemp -d)
cd $tmp

install -D "${DIR}/bin/Release/"*/${name}.dll -t ./BepInEx/plugins

zipFile="$DIR/bin/${name}-v${version}.zip"

rm -f $zipFile
zip -r $zipFile .
