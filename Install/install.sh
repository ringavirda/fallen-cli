#!/usr/bin/env bash

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
fcli_path="$SCRIPT_DIR/../FCli"

if ! command -v dotnet &>/dev/null; then
    echo "Dotnet is not installed on this machine."
    echo "Aborting"
    exit
fi

dotnet pack "$fcli_path"
echo "FCli packed and ready."

if dotnet tool list --global | grep fcli >/dev/null; then
    dotnet tool uninstall --global fcli
fi

dotnet tool install --global --add-source "$fcli_path" FCli
