#!/bin/sh
# Generates an RSA 2048 dev keypair for JWT signing (bolt 041).
# Output lands in secrets/ (gitignored). Never commit these files.
set -eu

mkdir -p secrets
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out secrets/dev-jwt-private.pem
openssl rsa -in secrets/dev-jwt-private.pem -pubout -out secrets/dev-jwt-public.pem

echo "Wrote secrets/dev-jwt-private.pem and secrets/dev-jwt-public.pem (gitignored)."
echo
echo "Register the private key with the API (choose one):"
echo "  # user-secrets (recommended):"
echo "  dotnet user-secrets set \"JwtSettings:PrivateKeyPem\" \"\$(cat secrets/dev-jwt-private.pem)\" --project src/PhotoPrint.API"
echo
echo "  # or appsettings.Development.Local.json (gitignored):"
echo "  { \"JwtSettings\": { \"PrivateKeyPem\": \"<PEM with \\n line breaks>\" } }"
