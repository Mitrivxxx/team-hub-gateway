#!/bin/sh
set -e

/generate-certs.sh /https

if ! openssl x509 -in /https/localhost.pem -noout -issuer 2>/dev/null | grep -qi mkcert; then
  echo "Gateway: self-signed TLS certs. For trusted HTTPS run on the host:"
  echo "  ./scripts/setup-certs.sh"
fi

exec dotnet team-hub-gateway.dll
