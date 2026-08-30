#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

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
bash tools/test_pure_java_controllers.sh

if [[ -x "$ROOT/gradlew" ]] && [[ -f "$ROOT/gradle/wrapper/gradle-wrapper.jar" ]]; then
    GRADLE="$ROOT/gradlew"
elif command -v gradle >/dev/null 2>&1; then
    GRADLE="$(command -v gradle)"
else
    echo "Gradle is required. Install Gradle or restore gradle/wrapper/gradle-wrapper.jar." >&2
    exit 1
fi

"$GRADLE" :app:testDebugUnitTest :app:assembleDebug :app:lintDebug

echo "Project verification suite: OK"
