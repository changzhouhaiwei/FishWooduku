#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

# 仓库根目录（相对本脚本：WoodukuProfile 的上一级）
REPO_ROOT="$(cd .. && pwd)"
LUBAN_DLL="${REPO_ROOT}/Tools/Luban/Luban.dll"
CONF_ROOT="$(pwd)"

if [[ ! -f "${LUBAN_DLL}" ]]; then
  echo "[ERROR] Luban not found: ${LUBAN_DLL}"
  echo "Please keep Tools/Luban under the repo root."
  exit 1
fi

echo "Luban: ${LUBAN_DLL}"
echo "Conf : ${CONF_ROOT}/luban.conf"
echo "Code : ${REPO_ROOT}/WoodukuClient/Assets/Scripts/GameLogic/Cfg"
echo "Data : ${REPO_ROOT}/WoodukuClient/Assets/GameRes/Config"
echo

dotnet "${LUBAN_DLL}" \
  -t client \
  -c cs-simple-json \
  -d json \
  --conf "${CONF_ROOT}/luban.conf" \
  -x outputCodeDir="${REPO_ROOT}/WoodukuClient/Assets/Scripts/GameLogic/Cfg" \
  -x outputDataDir="${REPO_ROOT}/WoodukuClient/Assets/GameRes/Config"

echo
echo "[OK] client config generated."
