#!/bin/bash
# 必须先切到脚本所在目录：相对路径（luban.conf、GEN_CLIENT、WORKSPACE）都相对 MiniTemplate
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

WORKSPACE=../..
GEN_CLIENT=../Luban/Luban.dll

dotnet "$GEN_CLIENT" \
    -t client \
    -c cs-bin \
    -d bin \
    --conf ./luban.conf \
    -x "outputDataDir=$WORKSPACE/Assets/Unity/Resources/Luban" \
    -x "outputCodeDir=$WORKSPACE/Assets/Demo/Luban/DataTable" \
    -x "tableImporter.valueTypeNameFormat=Table{0}"

if [ -t 0 ]; then
    read -rp "Press Enter to continue..." _
fi
