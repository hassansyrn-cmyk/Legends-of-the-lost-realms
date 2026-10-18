# Developer Mode

Available from the main menu, Sanctuary and pause menu. Enter password `3258` using the number pad or a keyboard, then select ACTIVATE TEST MODE.

- Unlocks all 48 weapons and all 7 outfits, including Aether Sovereign, for the current session.
- Does not change normal chest probabilities or unlock chapters/upgrades.
- Uses a separate copy of progress and suppresses saves while active. Turning it off restores the original collection, selected outfit and weapon. Restarting the app also returns to the normal saved collection.
- New journeys and chapter reloads keep the test collection unlocked while Developer Mode is active.
- Invalid passwords show an error. Cancel and Escape dismiss the password prompt.

Validation: Unity 6000.6.0f1, Direct3D 11, with -menuVisualProbe to protect the player's save. Wrong-password rejection, successful activation, complete collections, idempotent activation/disable, deep-copy isolation and exact normal-save restoration passed. The in-game probe also verified restoring equipped gear after testing the legendary outfit and last weapon, and captured six screens without text-overflow reports. All six existing regression scripts pass. The existing Unity Search startup exception and imported gate missing-script warnings remain unrelated to these checks. Android device testing and a new release bundle were not performed.
