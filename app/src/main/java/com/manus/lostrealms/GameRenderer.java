package com.manus.lostrealms;

import android.graphics.Canvas;

/** Routes the active screen to the appropriate renderer. */
final class GameRenderer {
    private final UIRenderer uiRenderer = new UIRenderer();

    void render(GameView game, Canvas canvas) {
        int screen = game.screenForRenderer();
        switch (screen) {
            case GameView.SPLASH:
                game.drawSplash(canvas);
                break;
            case GameView.MENU:
            case GameView.MAP:
            case GameView.UPGRADES:
            case GameView.SETTINGS:
            case GameView.DEV_TOOLS:
                uiRenderer.render(game, canvas);
                break;
            default:
                game.drawLevel(canvas);
                break;
        }
    }
}
