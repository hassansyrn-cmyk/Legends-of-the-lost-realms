# Arabic Support for Unity

Upstream: https://github.com/Konash/arabic-support-unity
Source: Assets/ArabicSupport/Scripts/ArabicSupport.cs, retrieved 2026-10-19.
License: MIT; see LICENSE.txt and the source header. No local modifications.

GameLanguage wraps logical text before using ArabicFixer.Fix(line, true, false, false). Combining diacritics is disabled to avoid the upstream ReturnTashkeel position bug; individual marks are retained. Western digits are retained for consistent passwords, cooldowns and numerical gameplay values. The upstream library runs only on Unity's main thread.
