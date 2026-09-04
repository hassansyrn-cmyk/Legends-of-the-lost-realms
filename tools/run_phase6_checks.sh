#!/usr/bin/env bash
set -euo pipefail

trap 'status=$?; echo "::error title=Project verification failed::Command ${BASH_COMMAND} failed at line ${LINENO} with status ${status}"; exit "$status"' ERR

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
python3 tools/test_sprite_sheet_contract.py
python3 tools/test_sprite_render_contract.py
bash tools/test_pure_java_controllers.sh
bash tools/test_scene_rules.sh

if command -v gradle >/dev/null 2>&1; then
    GRADLE="$(command -v gradle)"
elif [[ -x "$ROOT/gradlew" ]] && [[ -f "$ROOT/gradle/wrapper/gradle-wrapper.jar" ]]; then
    GRADLE="$ROOT/gradlew"
else
    echo "Gradle is required. Install Gradle or restore gradle/wrapper/gradle-wrapper.jar." >&2
    exit 1
fi

if ! GRADLE_OUTPUT="$("$GRADLE" :app:testDebugUnitTest :app:assembleDebug :app:lintDebug 2>&1)"; then
    printf '%s\n' "$GRADLE_OUTPUT"
    FAILURE="$(printf '%s\n' "$GRADLE_OUTPUT" | grep -m1 -E 'error:|FAILURE:|What went wrong' || true)"
    echo "::error title=Android build failed::${FAILURE:-Gradle verification exited unsuccessfully}"
    exit 1
fi
printf '%s\n' "$GRADLE_OUTPUT"

echo "Project verification suite: OK"
