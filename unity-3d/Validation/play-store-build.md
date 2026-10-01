# Signed Play Store bundle

Build entry point: `RealmRelease.AndroidBundle` (Unity 6000.6.0f1, Android Build Support installed). Close the interactive editor before a batch build. Output: `Builds/Android/LostRealms3D.aab` and public native symbols. This is a release IL2CPP ARM64 build, package `com.manus.lostrealms3d`, minimum API 26, target API 36.

The build requires these process environment variables:

| Name | Value |
| --- | --- |
| ANDROID_KEYSTORE_PATH | Absolute path to the existing upload keystore |
| ANDROID_KEY_ALIAS | Upload key alias |
| ANDROID_KEYSTORE_PASSWORD | Keystore password |
| ANDROID_KEY_PASSWORD | Alias/private-key password |
| ANDROID_VERSION_CODE | Positive integer greater than every previous Play upload |
| ANDROID_VERSION_NAME | Player-facing version, for example 0.1.0 |

Never commit the keystore or passwords. Keep a secure backup of the upload key and its credentials. Existing Play applications must use their registered upload key. A new key requires the Play upload-key reset process if the app already has one.

## GitHub secrets

Local signed builds do not require GitHub secrets. The existing `android-debug.yml` workflow builds APKs; it does not call this signed-AAB entry point. Existing repository secrets are `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.

If a signed-AAB workflow is added, it should accept `ANDROID_KEYSTORE_BASE64` (base64 of the binary keystore), `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASSWORD`, and `ANDROID_KEY_PASSWORD` as repository/environment secrets. It must decode the key to a temporary file and pass that file's path as `ANDROID_KEYSTORE_PATH`. Version code/name should be explicit workflow inputs, not secrets. Do not reuse the debug APK workflow as proof of a signed AAB. No Play service-account secret is needed for manual upload in Play Console.

Before upload, verify the AAB signature and certificate against the intended upload key, confirm package and version code, and test the generated release through Play internal testing. A successful local build does not imply Play Console acceptance.
