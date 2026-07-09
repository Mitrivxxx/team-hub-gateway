#!/bin/sh
set -e

/generate-certs.sh /https
exec dotnet team-hub-gateway.dll
