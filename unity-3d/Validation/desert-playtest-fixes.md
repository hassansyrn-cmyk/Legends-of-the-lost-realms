# Desert playtest fixes — September 29, 2026

- Clear props using combined visual and collider bounds around checkpoints, respawn positions, bounce pads and the final gate approach. Applied during chapter generation.
- Move chapter 6 arena spikes to the supported next-island position (0, 0.87, 42), retaining trap reservation. Remove the reported House_01 on that island.
- Turret arrows retain vertical aim and compensate flight gravity. A swept hit against Aster's actual collider now damages him, including legs and upper body; existing damage immunity remains active.

Validation:
- Standalone runtime C# compilation passed (existing warnings only).
- 72 quality source guards and 95 PowerShell CI-equivalent checks passed.
- DesertLayoutProbe regenerated chapters 5 and 6 successfully.
- Unity play-mode DesertPlaytestChecks: DESERT_FOCUSED_TESTS_PASSED 921, zero runtime errors. Checks checkpoint/bounce-pad clearance, relocated spike support and house removal, chapter 5 forward gate approach without jumping, actual arrows at three body heights, and intervening wall protection.
- Tests isolate arrow flight above the level; actual telegraphed three-arrow volleys and visual presentation still benefit from user playtesting.

Restart Play mode/reload the chapter to rebuild placement. No commits or pushes were made.
