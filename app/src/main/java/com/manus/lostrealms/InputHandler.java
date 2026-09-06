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
        float offsetX = (game.getWidth() - GameView.VIRTUAL_WIDTH * scale) * 0.5f;
        float offsetY = (game.getHeight() - GameView.VIRTUAL_HEIGHT * scale) * 0.5f;
        int action = event.getActionMasked();

        if (action == MotionEvent.ACTION_DOWN || action == MotionEvent.ACTION_POINTER_DOWN) {
            int actionIndex = event.getActionIndex();
            int pointerId = event.getPointerId(actionIndex);
            float x = (event.getX(actionIndex) - offsetX) / scale;
            float y = (event.getY(actionIndex) - offsetY) / scale;

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
        if (action == MotionEvent.ACTION_MOVE) {
            boolean changed = false;
            int pointerCount = event.getPointerCount();
            for (int i = 0; i < pointerCount; i++) {
                int pId = event.getPointerId(i);
                float px = (event.getX(i) - offsetX) / scale;
                float py = (event.getY(i) - offsetY) / scale;
                int newAction = game.hitAction(px, py);
                Integer oldActionObj = heldActions.get(pId);
                int oldAction = oldActionObj != null ? oldActionObj : 0;

                if (newAction != oldAction) {
                    if (oldAction == 4 && newAction != 4) {
                        game.cancelAttackHold();
                    }
                    if (newAction == 4 && oldAction != 4) {
                        game.pressAttack();
                    } else if ((newAction == 1 || newAction == 2 || newAction == 3) && newAction != oldAction) {
                        game.handleAction(newAction);
                    }
                    heldActions.put(pId, newAction);
                    changed = true;
                }
            }
            if (changed) {
                game.refreshHeld();
            }
            return true;
        }
        if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP) {
            int actionIndex = event.getActionIndex();
            int pointerId = event.getPointerId(actionIndex);
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
