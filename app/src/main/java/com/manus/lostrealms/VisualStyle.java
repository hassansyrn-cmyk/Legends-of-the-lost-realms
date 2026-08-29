package com.manus.lostrealms;

/** Cohesive world palette for UI readability, routes, collectibles, and terrain materials. */
final class VisualStyle {
    static final class Theme {
        final int uiPanel;
        final int uiStroke;
        final int controlFill;
        final int safeRoute;
        final int riskRoute;
        final int secretRoute;
        final int stoneMaterial;
        final int sandMaterial;
        final int iceMaterial;
        final int enemyWarning;

        Theme(int uiPanel, int uiStroke, int controlFill, int safeRoute, int riskRoute,
                int secretRoute, int stoneMaterial, int sandMaterial, int iceMaterial,
                int enemyWarning) {
            this.uiPanel = uiPanel;
            this.uiStroke = uiStroke;
            this.controlFill = controlFill;
            this.safeRoute = safeRoute;
            this.riskRoute = riskRoute;
            this.secretRoute = secretRoute;
            this.stoneMaterial = stoneMaterial;
            this.sandMaterial = sandMaterial;
            this.iceMaterial = iceMaterial;
            this.enemyWarning = enemyWarning;
        }
    }

    private static final Theme VERDANT = new Theme(
            0xE90A211C, 0xC79AF2D3, 0x8E103A32, 0xFF72E8B2, 0xFFE8CB61,
            0xFFD49BFF, 0xFF4F8066, 0xFFB07949, 0xFF6BB6CD, 0xFFF4C56A);
    private static final Theme DUNES = new Theme(
            0xE92A1714, 0xC7FFD28A, 0x8E4A241A, 0xFFFFD15E, 0xFFFF9A54,
            0xFFD6A2FF, 0xFF665340, 0xFFC9894D, 0xFF5CBAC3, 0xFFFFB35B);
    private static final Theme FROST = new Theme(
            0xE90D1B34, 0xC795E9FF, 0x8E13294C, 0xFF99F2FF, 0xFFA8C6FF,
            0xFFD7B7FF, 0xFF53677B, 0xFF9F805B, 0xFF72BDE9, 0xFFC8F2FF);

    private VisualStyle() {
    }

    static Theme forWorld(int world) {
        return world == 2 ? DUNES : world == 3 ? FROST : VERDANT;
    }

    static boolean isBright(int color) {
        int red = (color >> 16) & 0xFF;
        int green = (color >> 8) & 0xFF;
        int blue = color & 0xFF;
        return red + green + blue > 380;
    }
}
