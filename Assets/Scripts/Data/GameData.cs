using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Static data definitions matching the original HTML5 game code.
/// All game constants, ship definitions, trail definitions, battle pass tiers,
/// sector mechanics, and level generation live here.
/// </summary>
public static class GameData
{
    // --- GAME CONSTANTS ---
    public const float GAME_WIDTH = 400f;
    public const float GAME_HEIGHT = 800f;
    public const float LAUNCH_SPEED = 9f;
    public const float ROCKET_RADIUS = 8f;
    public const float GOAL_HEIGHT = 30f;
    public const float ABERRATION_MULT = 12f;
    public const float BOUNDS_MARGIN = 20f;
    public const int MAX_TRAIL = 100;
    public const int UNIVERSES = 2;
    public const int SECTORS_PER_UNIVERSE = 10;
    public const int LEVELS_PER_SECTOR = 5;

    public static float GoalWidth => GAME_WIDTH / 2.5f; // 160

    // --- SHIPS ---
    [System.Serializable]
    public class ShipDef
    {
        public string id;
        public string name;
        public string perk;
        public Color color;
        public Color hex;
    }

    public static readonly Dictionary<string, ShipDef> SHIPS = new Dictionary<string, ShipDef>
    {
        { "shipA", new ShipDef { id = "shipA", name = "Starter Cruiser", perk = "Standard issue.", color = Color.white, hex = Color.white } },
        { "shipB", new ShipDef { id = "shipB", name = "Anti-Grav Skiff", perk = "Reduced gravity effect.", color = new Color(0.75f, 0.52f, 0.99f), hex = new Color(0.75f, 0.52f, 0.99f) } },
        { "shipC", new ShipDef { id = "shipC", name = "Nebula Piercer", perk = "Immune to toxic nebulae.", color = new Color(0.29f, 0.87f, 0.50f), hex = new Color(0.29f, 0.87f, 0.50f) } }
    };

    // --- TRAILS ---
    [System.Serializable]
    public class TrailDef
    {
        public string id;
        public string name;
        public string perk;
        public Color color;
        public Color[] hex; // 3 channel colors
        public bool isChroma;
    }

    public static readonly Dictionary<string, TrailDef> TRAILS = new Dictionary<string, TrailDef>
    {
        { "trailDefault", new TrailDef { id = "trailDefault", name = "Default Exhaust", perk = "Standard engine output.", color = new Color(0.47f, 0.53f, 0.60f),
            hex = new Color[] { Color.red, Color.green, Color.blue }, isChroma = true } },
        { "trailBasic", new TrailDef { id = "trailBasic", name = "Basic Trail", perk = "A simple ion exhaust.", color = new Color(0.6f, 0.6f, 0.6f),
            hex = new Color[] { new Color(0.67f, 0.67f, 0.67f), Color.white, new Color(0.53f, 0.53f, 0.53f) }, isChroma = false } },
        { "trailNeon", new TrailDef { id = "trailNeon", name = "Neon Trail", perk = "Bright and flashy.", color = new Color(1f, 0.44f, 0.78f),
            hex = new Color[] { Color.magenta, new Color(1f, 0.53f, 1f), new Color(0.67f, 0f, 0.67f) }, isChroma = false } },
        { "trailPlasma", new TrailDef { id = "trailPlasma", name = "Plasma Trail", perk = "Superheated plasma.", color = new Color(0.38f, 0.73f, 1f),
            hex = new Color[] { Color.cyan, new Color(0.53f, 1f, 1f), new Color(0f, 0.53f, 0.53f) }, isChroma = false } },
        { "trailStardust", new TrailDef { id = "trailStardust", name = "Stardust Trail", perk = "Leaves a trail of stars.", color = new Color(0.98f, 0.84f, 0.37f),
            hex = new Color[] { Color.yellow, new Color(1f, 0.67f, 0f), new Color(0.67f, 0.67f, 0f) }, isChroma = false } },
        { "trailComet", new TrailDef { id = "trailComet", name = "Comet Trail", perk = "Icy comet tail.", color = new Color(0.33f, 0.83f, 1f),
            hex = new Color[] { new Color(0.88f, 1f, 1f), Color.cyan, new Color(0f, 0.67f, 0.67f) }, isChroma = false } },
        { "trailSupernova", new TrailDef { id = "trailSupernova", name = "Supernova", perk = "Explosive energy.", color = new Color(1f, 0.49f, 0.13f),
            hex = new Color[] { new Color(1f, 0.27f, 0f), new Color(1f, 0.55f, 0f), Color.red }, isChroma = false } }
    };

    // --- BATTLE PASS ---
    [System.Serializable]
    public class BattlePassReward
    {
        public string type;  // "ship", "trail", "skin", "powerup", "buff", "perk"
        public string name;
        public string id;    // optional, for unlockable items
        public int credits;  // optional bonus credits
    }

    [System.Serializable]
    public class BattlePassTier
    {
        public int level;
        public int starsReq;
        public BattlePassReward free;
        public BattlePassReward premium;
    }

    public static readonly BattlePassTier[] BATTLE_PASS_TIERS = new BattlePassTier[]
    {
        new BattlePassTier { level = 1, starsReq = 10,
            free = new BattlePassReward { type = "ship", name = "Ship B (Anti-Grav)", id = "shipB", credits = 10 },
            premium = new BattlePassReward { type = "skin", name = "Elite Skin" } },
        new BattlePassTier { level = 2, starsReq = 20,
            free = new BattlePassReward { type = "ship", name = "Ship C (Nebula)", id = "shipC" },
            premium = new BattlePassReward { type = "powerup", name = "Mega Power Bundle", credits = 20 } },
        new BattlePassTier { level = 3, starsReq = 30,
            free = new BattlePassReward { type = "powerup", name = "Small Powerup", credits = 10 },
            premium = new BattlePassReward { type = "buff", name = "Shield Capacity 6/6", id = "buffShield" } },
        new BattlePassTier { level = 4, starsReq = 40,
            free = new BattlePassReward { type = "skin", name = "Rookie Skin" },
            premium = new BattlePassReward { type = "trail", name = "Neon Trail", id = "trailNeon", credits = 20 } },
        new BattlePassTier { level = 5, starsReq = 50,
            free = new BattlePassReward { type = "skin", name = "Veteran Skin", credits = 10 },
            premium = new BattlePassReward { type = "perk", name = "Free Replay Token / Sector" } },
        new BattlePassTier { level = 6, starsReq = 60,
            free = new BattlePassReward { type = "trail", name = "Plasma Trail", id = "trailPlasma" },
            premium = new BattlePassReward { type = "skin", name = "Galactic Skin", credits = 20 } },
        new BattlePassTier { level = 7, starsReq = 70,
            free = new BattlePassReward { type = "powerup", name = "Medium Powerup", credits = 10 },
            premium = new BattlePassReward { type = "powerup", name = "Ultra Power Bundle" } },
        new BattlePassTier { level = 8, starsReq = 80,
            free = new BattlePassReward { type = "skin", name = "Ace Skin" },
            premium = new BattlePassReward { type = "trail", name = "Stardust Trail", id = "trailStardust", credits = 20 } },
        new BattlePassTier { level = 9, starsReq = 90,
            free = new BattlePassReward { type = "powerup", name = "Large Powerup", credits = 10 },
            premium = new BattlePassReward { type = "skin", name = "Cosmic Skin" } },
        new BattlePassTier { level = 10, starsReq = 100,
            free = new BattlePassReward { type = "trail", name = "Basic Trail", id = "trailBasic" },
            premium = new BattlePassReward { type = "skin", name = "Legendary Skin", credits = 20 } },
        new BattlePassTier { level = 11, starsReq = 110,
            free = new BattlePassReward { type = "trail", name = "Comet Trail", id = "trailComet", credits = 10 },
            premium = new BattlePassReward { type = "powerup", name = "Infinite Power Bundle" } },
        new BattlePassTier { level = 12, starsReq = 120,
            free = new BattlePassReward { type = "skin", name = "Master Skin" },
            premium = new BattlePassReward { type = "trail", name = "Supernova Trail", id = "trailSupernova", credits = 20 } }
    };

    // --- SECTOR MECHANICS ---
    [System.Serializable]
    public class SectorMechanic
    {
        public string id;
        public string name;
        public string reqShip;   // null if no ship required
        public string warning;
        public Color color;
    }

    public static readonly Dictionary<string, SectorMechanic> SECTOR_MECHANICS = new Dictionary<string, SectorMechanic>
    {
        { "normal", new SectorMechanic { id = "normal", name = "Normal Space", reqShip = null, warning = "Standard physics apply.", color = Color.white } },
        { "highGrav", new SectorMechanic { id = "highGrav", name = "High Gravity", reqShip = "shipB", warning = "High gravity detected. Ship B (Anti-Grav) recommended.", color = new Color(0.75f, 0.52f, 0.99f) } },
        { "nebula", new SectorMechanic { id = "nebula", name = "Toxic Nebula", reqShip = "shipC", warning = "Lethal gases. Ship C (Nebula Piercer) REQUIRED.", color = new Color(0.29f, 0.87f, 0.50f) } }
    };

    public static string[] GetSectorMechanics(int sectorIndex)
    {
        if (sectorIndex == 2) return new[] { "highGrav" };
        if (sectorIndex == 3) return new[] { "nebula" };
        if (sectorIndex == 4) return new[] { "highGrav", "nebula" };
        if (sectorIndex >= 8) return new[] { "nebula" };
        if (sectorIndex >= 5) return new[] { "highGrav" };
        return new[] { "normal" };
    }

    // --- SHOP ---
    [System.Serializable]
    public class CreditPack
    {
        public int amount;
        public string price;
    }

    public static readonly CreditPack[] CREDIT_PACKS = new CreditPack[]
    {
        new CreditPack { amount = 100, price = "4.99$" },
        new CreditPack { amount = 300, price = "9.99$" },
        new CreditPack { amount = 900, price = "14.99$" }
    };

    [System.Serializable]
    public class ShopItem
    {
        public string name;
        public int qty;
        public int cost;
        public int originalCost; // 0 if no discount
        public string desc;
    }

    public static readonly ShopItem[] SHOP_ITEMS = new ShopItem[]
    {
        new ShopItem { name = "Magnet", qty = 10, cost = 200, originalCost = 0, desc = "Pulls rocket toward center." },
        new ShopItem { name = "Gate Extender", qty = 10, cost = 200, originalCost = 0, desc = "Entire gate counts as 3-stars." },
        new ShopItem { name = "Placeholder", qty = 1, cost = 50, originalCost = 0, desc = "Standard system utility." },
        new ShopItem { name = "Placeholder", qty = 5, cost = 80, originalCost = 100, desc = "System utility bundle." },
        new ShopItem { name = "Placeholder", qty = 10, cost = 110, originalCost = 150, desc = "Bulk system utility." }
    };

    // --- LEVEL GENERATION ---
    [System.Serializable]
    public class PlanetData
    {
        public float x, y, radius, mass;
    }

    [System.Serializable]
    public class LevelData
    {
        public PlanetData[] planets;
        public float startX, startY;
        public float goalX, goalY;
    }

    /// <summary>
    /// Generate all levels matching the original HTML5 algorithm exactly.
    /// Returns 50 levels (2 universes × 25 levels each).
    /// </summary>
    public static LevelData[] GenerateAllLevels()
    {
        List<LevelData> generated = new List<LevelData>();

        for (int u = 0; u < 2; u++)
        {
            for (int i = 0; i < 25; i++)
            {
                float diff = (float)i / 25f + u * 0.3f;
                int type = i % 5;
                List<PlanetData> pList = new List<PlanetData>();

                float startX = GAME_WIDTH / 2f + Mathf.Sin((i + u * 10) * 1.3f) * 100f;
                float goalX = (GAME_WIDTH / 2f - GoalWidth / 2f) + Mathf.Cos((i + u * 10) * 1.7f) * 80f;

                // Tutorial levels (universe 1, first 3 levels)
                if (u == 0 && i < 3)
                {
                    startX = GAME_WIDTH / 2f;
                    goalX = GAME_WIDTH / 2f - GoalWidth / 2f;
                }

                if (type == 0)
                {
                    pList.Add(new PlanetData { x = 80, y = 400, radius = 30 + diff * 15, mass = 100 + diff * 100 });
                    pList.Add(new PlanetData { x = 320, y = 400, radius = 30 + diff * 15, mass = 100 + diff * 100 });
                    if (diff > 0.5f) pList.Add(new PlanetData { x = 200 + u * 20, y = 200, radius = 20, mass = 80 + u * 20 });
                }
                else if (type == 1)
                {
                    float blockX = GAME_WIDTH / 2f + Mathf.Sin(i) * 50f;
                    pList.Add(new PlanetData { x = blockX, y = 450, radius = 40 + diff * 20, mass = 150 + diff * 150 });
                    if (diff > 0.3f) pList.Add(new PlanetData { x = GAME_WIDTH - blockX, y = 250, radius = 25 + u * 5, mass = 100 + u * 50 });
                }
                else if (type == 2)
                {
                    pList.Add(new PlanetData { x = 120, y = 550, radius = 30, mass = 120 + diff * 80 });
                    pList.Add(new PlanetData { x = 280, y = 350, radius = 30, mass = 120 + diff * 80 });
                    if (diff > 0.4f) pList.Add(new PlanetData { x = 120, y = 150, radius = 30, mass = 100 + u * 40 });
                }
                else if (type == 3)
                {
                    float px = GAME_WIDTH / 2f + (i % 2 == 0 ? 60f : -60f);
                    pList.Add(new PlanetData { x = px, y = 400, radius = 50 + diff * 20, mass = 300 + diff * 200 + u * 100 });
                }
                else if (type == 4)
                {
                    int count = Mathf.FloorToInt(3 + diff * 4) + u;
                    for (int j = 0; j < count; j++)
                    {
                        pList.Add(new PlanetData
                        {
                            x = 80 + ((i * j * 31) % 240),
                            y = 150 + ((i * j * 47) % 450),
                            radius = 15 + (j % 2) * 5,
                            mass = 50 + diff * 40 + u * 10
                        });
                    }
                }

                generated.Add(new LevelData
                {
                    planets = pList.ToArray(),
                    startX = startX,
                    startY = GAME_HEIGHT - 60f,
                    goalX = goalX,
                    goalY = 40f
                });
            }
        }

        return generated.ToArray();
    }

    /// <summary>
    /// Converts universe (1-based), sector (1-based), level (1-based) to a global level index (0-based).
    /// </summary>
    public static int ToGlobalIndex(int universe, int sector, int level)
    {
        return (universe - 1) * 25 + (sector - 1) * 5 + (level - 1);
    }

    /// <summary>
    /// Gets the gravity constant for a given ship.
    /// Ship B (Anti-Grav) has reduced gravity.
    /// Values scaled for Unity physics (original HTML5 uses 9/15, we scale up).
    /// </summary>
    public static float GetGravityForShip(string shipId)
    {
        // Original: shipB=9, others=15. We multiply by ~3.3 for Unity feel.
        if (shipId == "shipB") return 30f;
        return 50f;
    }
}
