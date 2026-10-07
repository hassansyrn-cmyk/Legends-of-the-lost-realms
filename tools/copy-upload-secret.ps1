param([Parameter(Mandatory=$true)][ValidateSet('Password','KeystoreBase64')][string]$Value)
$ErrorActionPreference='Stop'
$signing=Join-Path $PSScriptRoot '../unity-3d/Builds/Signing'
if($Value -eq 'Password') {
    $credential=Import-Clixml -LiteralPath (Join-Path $signing 'upload-credentials.xml')
    Set-Clipboard -Value $credential.GetNetworkCredential().Password
    Write-Output 'Upload password copied. Save it in your password manager, then clear your clipboard.'
} else {
    $bytes=[IO.File]::ReadAllBytes((Join-Path $signing 'lost-realms-upload.keystore'))
    Set-Clipboard -Value ([Convert]::ToBase64String($bytes))
    Write-Output 'Keystore copied as base64. Paste only into the intended private GitHub Actions secret, then clear your clipboard.'
}
