#!/usr/bin/env bash
# Runs the .NET CLI inside the official SDK container so no SDK is needed on the host.
# Usage: ./dotnet.sh <any dotnet args>     e.g.  ./dotnet.sh build   ./dotnet.sh test
set -euo pipefail

IMAGE="mcr.microsoft.com/dotnet/sdk:8.0"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NUGET_DIR="${HOME}/.nuget/packages"          # shared cache on the host -> fast restores
CLI_HOME="${HOME}/.cache/dotnet-sdk-docker"  # persists tool cache / first-run state between runs
mkdir -p "${NUGET_DIR}" "${CLI_HOME}"

# Only allocate a TTY when we have one (Claude Code / CI have none).
TTY_FLAGS=()
[ -t 0 ] && TTY_FLAGS=(-it)

# Only publish the port for commands that actually serve HTTP,
# so `build`/`test` can run while `run` is active in another terminal.
PORT_FLAGS=()
case "${1:-}" in
  run|watch) PORT_FLAGS=(-p 5000:5000 -e ASPNETCORE_URLS=http://+:5000) ;;
esac

exec docker run --rm "${TTY_FLAGS[@]}" "${PORT_FLAGS[@]}" \
  --user "$(id -u):$(id -g)" \
  -v "${ROOT}":/src -w /src \
  -v "${NUGET_DIR}":/nuget \
  -v "${CLI_HOME}":/home/dotnet \
  -e NUGET_PACKAGES=/nuget \
  -e HOME=/home/dotnet \
  -e DOTNET_CLI_HOME=/home/dotnet \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOTNET_NOLOGO=1 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  "${IMAGE}" dotnet "$@"
