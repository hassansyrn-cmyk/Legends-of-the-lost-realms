$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$checks = 0
function Require-Text([string]$file, [string]$text) {
    $source = Get-Content -LiteralPath (Join-Path $repoRoot $file) -Raw
    if (-not $source.Contains($text)) { throw "Missing safeguard in ${file}: $text" }
    $script:checks++
}
function Forbid-Text([string]$file, [string]$text) {
    $source = Get-Content -LiteralPath (Join-Path $repoRoot $file) -Raw
    if ($source.Contains($text)) { throw "Regression in ${file}: $text" }
    $script:checks++
}
$hero = 'unity-3d/Assets/Scripts/Hero.cs'
foreach ($text in @('const float MaxMoveSpeed=4.8f','GroundResponse=18f','AirResponse=8f','StopResponse=22f','Vector3.MoveTowards(velocity,desiredVelocity,response*dt)','"double_jump"','vertical=8.4f','jumpGrace=.14f','jumpPressed?.13f','velocity=wish*9.5f','vertical=Mathf.Max(vertical,-20f)','CounterReady')) { Require-Text $hero $text }
foreach ($text in @('Visual.Restart("double_jump")','wish*6.1f','speedRatio*.82f','SetSpeed(3.1f)')) { Forbid-Text $hero $text }
$heroSource = Get-Content (Join-Path $repoRoot $hero) -Raw
if ([regex]::Matches($heroSource,'Controller\.Move\(').Count -ne 1) { throw 'Hero must have exactly one Controller.Move call' }; $checks++
Forbid-Text 'unity-3d/Assets/Scripts/RealmWorld.cs' 'g.Player.Controller.Move('
foreach ($text in @('Weapons/Aster_Axe','Weapons/Aster_LongSword','Weapons/Aster_CurvedSword','public sealed class WeaponDrop','public sealed class EquippedWeapon','model=created?created.transform:null;')) { Require-Text 'unity-3d/Assets/Scripts/WeaponSystem.cs' $text }
Require-Text $hero 'float damage=(charged?3.5f:combo==3?2.5f:1.5f)*weapon.Damage;'
Require-Text $hero 'float reach=(charged?3.4f:2.7f)*weapon.Reach;'
foreach ($asset in Get-ChildItem (Join-Path $repoRoot 'unity-3d/Assets/Resources/Weapons') -Filter *.fbx) {
    if ($asset.Length -ge 2000000) { throw "Weapon exceeds mobile budget: $($asset.Name)" }; $checks++
}
Require-Text 'unity-3d/Assets/Editor/AsterPhase1.cs' 'AnimationCurve.Constant(0f,clip.length,0f)'
Require-Text 'unity-3d/Assets/Editor/AsterPhase1.cs' 'baselineRootY*scale'
Require-Text 'unity-3d/Packages/manifest.json' 'com.unity.modules.particlesystem'
Require-Text 'unity-3d/Assets/Scripts/RealmPostFx.cs' 'Resources.Load<Shader>("Shaders/RealmPost")'
Require-Text 'unity-3d/Assets/Scripts/RealmGame.cs' 'Time.fixedDeltaTime=1f/60f'
Forbid-Text 'unity-3d/Assets/Scripts/Vfx.cs' 'vel.y=c;vel.z=c;'
Write-Output "QUALITY_SOURCE_GUARDS_PASSED $checks"
