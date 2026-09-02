#!/usr/bin/env bash
set -euo pipefail

# This project is an Android game with auxiliary preview workspaces; it has no
# runtime database migration step. Keep post-merge setup deterministic and
# non-interactive so it can run with stdin closed.
export CI="${CI:-true}"
pnpm install --frozen-lockfile --prefer-offline --reporter=append-only
