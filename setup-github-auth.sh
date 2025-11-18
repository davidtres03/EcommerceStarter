#!/bin/bash

# Get GitHub token from Windows Credential Manager and authenticate gh CLI

set -e

echo "[•] Attempting to retrieve GitHub token from Credential Manager..."

# Use PowerShell to get token from Credential Manager
TOKEN=$(powershell.exe -NoProfile -Command "@(Get-StoredCredential -Target 'github_token' -AsPlainText).token" 2>/dev/null || echo "")

if [ -z "$TOKEN" ]; then
    # Try alternate method
    TOKEN=$(powershell.exe -NoProfile -Command "
    \$cred = Get-StoredCredential -Target 'github_token' 2>\$null
    if (\$cred) { \$cred.GetNetworkCredential().Password }
    " 2>/dev/null || echo "")
fi

if [ -z "$TOKEN" ]; then
    echo "[✗] Could not retrieve GitHub token from Credential Manager"
    echo ""
    echo "Options:"
    echo "  1. Create new token at: https://github.com/settings/tokens"
    echo "  2. Run: gh auth login"
    exit 1
fi

echo "[✓] Token retrieved (${#TOKEN} characters)"
echo ""
echo "[•] Configuring gh CLI with token..."

# Logout existing bad token
gh auth logout -h github.com -u davidtres03 2>/dev/null || true

# Authenticate with token via stdin
echo "$TOKEN" | gh auth login -h github.com --with-token

echo "[✓] GitHub authentication successful"
gh auth status
