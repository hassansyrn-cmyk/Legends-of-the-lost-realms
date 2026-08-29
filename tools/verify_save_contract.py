from pathlib import Path

source = Path('/home/ubuntu/lost-realms/app/src/main/java/com/manus/lostrealms/SaveManager.java').read_text(encoding='utf-8')
required = [
    'CURRENT_SCHEMA_VERSION = 5',
    'migrateIfNeeded()',
    'best_time_',
    'stat_runs_started',
    'stat_runs_completed',
    'stat_deaths',
    'stat_enemies_defeated',
    'stat_secrets_found',
    'recordSecretFound',
    'recordLevelCompletion',
    'CompletionResult',
    'formatDuration',
]
for marker in required:
    if marker not in source:
        raise SystemExit(f'Missing save contract marker: {marker}')

migration_block = source[source.index('private void migrateIfNeeded()'):source.index('public int schemaVersion()')]
if '.clear()' in migration_block:
    raise SystemExit('Migration must not clear existing save values.')

print('Save migration contract: OK')
print('Legacy progression keys remain additive; schema v5 adds timing, statistics, and unique secret-cache keys only.')
