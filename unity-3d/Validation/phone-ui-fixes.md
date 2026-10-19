# Phone UI and new realm performance fixes — 2026-10-19

The bundled Arabic fonts now map bidi overrides to empty, zero-advance glyphs.
This preserves the previously verified reading order and numeric signs while
preventing Android from drawing missing-glyph markers for the overrides.
Calligraphic Amiri Bold headings accompany the compact Arabic UI body font.
Both modified fonts have distinct family names and retain their SIL OFL notices.

Main menu counters and Atlas earned stars use existing icon textures. The attack
caption is الهجوم and its hint reads اضغط مطولا للهجوم المشحون on two lines inside
the circular artwork, including its pressed size. The spell hint occupies the
same safe interior band. Developer Mode fits inside the enlarged pause panel.

Stormveil and Astral scenery materials enable GPU instancing without changing
mesh geometry, textures, normal maps, lighting, particles, or quality settings.
Storm current lines and arms refresh when a choice, round, or parent pose changes.
Astral focus emission updates when its state changes; restored focuses keep
their jewel restoration effect. Route-owned material instances are released.

Validation: Unity 6000.6.0f1 D3D11 rendered localization probe passed, including
numeric/placeholder integrity, font controls, developer compatibility, source
save isolation, and screenshots in ArabicLocalization. All six repository
verification scripts passed. Relay direction and parent-motion checks passed.
Instancing comparisons for chapters 16 and 21 each had zero pixels differing by
more than 3/255 across 640×360 images. See realm-performance.txt and the before
report for the CPU microbenchmark and repeated-instance counts. The Mono
allocation counter proved unavailable; its earlier zero readings are not an
allocation measurement. Editor timings are not phone FPS measurements.

Android phone typography and frame-rate confirmation remain device checks.
Build entry point: LostRealms.QualityBuild.AndroidDebug; development APK output:
Builds/Android/LostRealms3D-debug.apk. Existing release signing is unchanged.
