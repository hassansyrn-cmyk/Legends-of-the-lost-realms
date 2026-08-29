#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GRADLE="/home/ubuntu/android-tools/gradle-8.10.2/bin/gradle"

cd "$ROOT"
python3 tools/test_level_data.py
python3 tools/simulate_level_bounds.py
python3 tools/verify_save_contract.py
python3 tools/verify_dev_tools.py
python3 tools/test_enemy_sprite_atlas.py
python3 tools/check_boss_rig_parts.py
python3 tools/test_extra_sprite_atlases.py
python3 tools/test_action_fx_atlas.py
python3 tools/test_boss_hud_layout.py
python3 tools/test_layout_background_fix.py
python3 tools/test_uploaded_asset_integration.py
"$GRADLE" :app:testDebugUnitTest :app:assembleDebug :app:lintDebug

echo "Project verification suite: OK"
