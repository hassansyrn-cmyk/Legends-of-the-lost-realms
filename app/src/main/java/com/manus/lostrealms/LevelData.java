package com.manus.lostrealms;

import android.graphics.Color;

public class LevelData {
    public final int id, world, sky, ground, accent;
    public final String worldName, title, mechanic, bossName;
    public final boolean boss;

    private LevelData(int id, int world, int sky, int ground, int accent, String worldName, String title, String mechanic, String bossName) {
        this.id = id; this.world = world; this.sky = sky; this.ground = ground; this.accent = accent;
        this.worldName = worldName; this.title = title; this.mechanic = mechanic; this.bossName = bossName;
        this.boss = bossName != null;
    }

    public static LevelData get(int id) {
        switch (id) {
            case 1: return new LevelData(1, 1, Color.rgb(32,113,112), Color.rgb(32,83,56), Color.rgb(103,234,164), "VERDANT KINGDOM", "Mosslight Trail", "Movement and safe jumps", null);
            case 2: return new LevelData(2, 1, Color.rgb(28,94,104), Color.rgb(36,73,52), Color.rgb(117,241,202), "VERDANT KINGDOM", "Whispering Falls", "Combat and short hazards", null);
            case 3: return new LevelData(3, 1, Color.rgb(43,108,83), Color.rgb(31,66,53), Color.rgb(235,205,98), "VERDANT KINGDOM", "Rootbound Ruins", "Dash, walls, and combat", null);
            case 4: return new LevelData(4, 1, Color.rgb(21,82,72), Color.rgb(24,60,46), Color.rgb(150,255,188), "VERDANT KINGDOM", "The Elder Grove", "Guardian patterns", "Thornwold, Corrupted Guardian");
            case 5: return new LevelData(5, 2, Color.rgb(201,89,43), Color.rgb(112,56,34), Color.rgb(255,209,91), "THE BURNING DUNES", "Sunscorched Pass", "Brittle routes and height", null);
            case 6: return new LevelData(6, 2, Color.rgb(181,67,37), Color.rgb(105,49,32), Color.rgb(89,225,225), "THE BURNING DUNES", "Temple of Keys", "Vertical ruins and timing", null);
            case 7: return new LevelData(7, 2, Color.rgb(155,57,42), Color.rgb(92,46,37), Color.rgb(255,195,66), "THE BURNING DUNES", "Sandstone Colossus", "Marked ground and dodges", "Akaros, Stone Warden");
            case 8: return new LevelData(8, 3, Color.rgb(76,133,199), Color.rgb(52,76,135), Color.rgb(151,244,255), "FROZEN PEAKS", "Frostwind Climb", "Ice traction and wind", null);
            case 9: return new LevelData(9, 3, Color.rgb(58,109,178), Color.rgb(43,70,130), Color.rgb(232,249,255), "FROZEN PEAKS", "Crystal Hollow", "Crystal climbing and combat", null);
            default: return new LevelData(10, 3, Color.rgb(39,85,154), Color.rgb(38,62,117), Color.rgb(149,231,255), "FROZEN PEAKS", "Crown of Winter", "Frost lanes and wind", "Vyrn, the Icebound Maw");
        }
    }
}
