package com.manus.lostrealms;

import android.graphics.Canvas;

/** Renders non-live interface screens through the scene's existing visual rules. */
final class UIRenderer {
    void render(GameView game, Canvas canvas) {
        switch (game.screenForRenderer()) {
            case GameView.MENU:
                game.drawMenu(canvas);
                break;
            case GameView.MAP:
                game.drawMap(canvas);
                break;
            case GameView.UPGRADES:
                game.drawUpgrades(canvas);
                break;
            case GameView.SETTINGS:
                game.drawSettings(canvas);
                break;
            case GameView.DEV_TOOLS:
                game.drawDevTools(canvas);
                break;
            default:
                throw new IllegalArgumentException("Unsupported UI screen");
        }
    }
}
