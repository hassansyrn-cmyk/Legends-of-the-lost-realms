package com.manus.lostrealms;

import android.view.MotionEvent;
import java.util.HashMap;
import java.util.Map;

/**
 * Owns touch-pointer state and converts screen touches to the game's existing
 * virtual-coordinate actions. Gameplay decisions remain in GameView.
 */
final class InputHandler {
    private final HashMap<Integer, Integer> heldActions = new HashMap<>();

    boolean onTouchEvent(GameView game, MotionEvent event) {
        float scale = Math.min(
                game.getWidth() / GameView.VIRTUAL_WIDTH,
                game.getHeight() / GameView.VIRTUAL_HEIGHT);
        float x = (event.getX() - (game.getWidth() - GameView.VIRTUAL_WIDTH * scale) * 0.5f) / scale;
        float y = (event.getY() - (game.getHeight() - GameView.VIRTUAL_HEIGHT * scale) * 0.5f) / scale;
        int pointerId = event.getPointerId(event.getActionIndex());
        int action = event.getActionMasked();

        if (action == MotionEvent.ACTION_DOWN || action == MotionEvent.ACTION_POINTER_DOWN) {
            if (game.isStoryOverlayVisible()) {
                clear();
                game.refreshHeld();
                game.dismissStoryOverlay();
                return true;
            }
            int gameAction = game.hitAction(x, y);
            heldActions.put(pointerId, gameAction);
            if (gameAction == 4) game.pressAttack();
            else game.handleAction(gameAction);
            game.refreshHeld();
            return true;
        }
        if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP) {
            Integer releasedAction = heldActions.remove(pointerId);
            if (releasedAction != null && releasedAction == 4) game.releaseAttack();
            game.refreshHeld();
            return true;
        }
        if (action == MotionEvent.ACTION_CANCEL) {
            clear();
            game.cancelAttackHold();
            game.refreshHeld();
            return true;
        }
        return true;
    }

    boolean isHeld(int action) {
        for (Map.Entry<Integer, Integer> entry : heldActions.entrySet()) {
            if (entry.getValue() == action) {
                return true;
            }
        }
        return false;
    }

    void clear() {
        heldActions.clear();
    }
}
