# Generates an RSA 2048 dev keypair for JWT signing (bolt 041).
# Output lands in secrets/ (gitignored). Never commit these files.
$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path secrets | Out-Null
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out secrets/dev-jwt-private.pem
openssl rsa -in secrets/dev-jwt-private.pem -pubout -out secrets/dev-jwt-public.pem

Write-Host "Wrote secrets/dev-jwt-private.pem and secrets/dev-jwt-public.pem (gitignored)."
Write-Host ""
Write-Host "Register the private key with the API (recommended: user-secrets):"
Write-Host '  dotnet user-secrets set "JwtSettings:PrivateKeyPem" "$(Get-Content -Raw secrets/dev-jwt-private.pem)" --project src/PhotoPrint.API'
