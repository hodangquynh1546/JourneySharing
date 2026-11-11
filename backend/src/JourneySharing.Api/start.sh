#!/bin/sh
set -eu

export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}"

if [ "$#" -gt 0 ]; then
  exec dotnet JourneySharing.Api.dll "$@"
fi

if [ "${MIGRATE_ON_START:-false}" = "true" ]; then
  dotnet JourneySharing.Api.dll --migrate
fi

exec dotnet JourneySharing.Api.dll
