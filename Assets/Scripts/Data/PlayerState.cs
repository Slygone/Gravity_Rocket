using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages all persistent player state: scores, unlocks, equipped items, credits, shields.
/// Uses PlayerPrefs for persistence across sessions.
/// </summary>
public class PlayerState
{
    private static PlayerState _instance;
    public static PlayerState Instance
    {
        get
        {
            if (_instance == null) _instance = new PlayerState();
            return _instance;
        }
    }

    // Level scores: key = "universe-sector-level" (1-based), value = stars (0-3)
    public Dictionary<string, int> LevelScores { get; private set; } = new Dictionary<string, int>();

    // Progression
    public bool IsPremium { get; set; } = false;
    public List<string> ActiveShips { get; private set; } = new List<string> { "shipA" };
    public string ActiveTrail { get; set; } = "trailDefault";
    public int CurrentUniverse { get; set; } = 1;
    public List<string> ClaimedRewards { get; private set; } = new List<string>();
    public int Credits { get; set; } = 0;

    // Session state
    public int GlobalShields { get; set; } = 5;

    // --- DERIVED STATE ---
    public int TotalStars
    {
        get
        {
            int sum = 0;
            foreach (var kv in LevelScores) sum += kv.Value;
            return sum;
        }
    }

    public int AccountLevel => TotalStars / 10;

    public List<string> UnlockedShips
    {
        get
        {
            List<string> ships = new List<string> { "shipA" };
            foreach (var tier in GameData.BATTLE_PASS_TIERS)
            {
                if (AccountLevel >= tier.level && tier.free.type == "ship" && !string.IsNullOrEmpty(tier.free.id))
                    if (!ships.Contains(tier.free.id)) ships.Add(tier.free.id);
            }
            return ships;
        }
    }

    public List<string> UnlockedTrails
    {
        get
        {
            List<string> trails = new List<string> { "trailDefault" };
            foreach (var tier in GameData.BATTLE_PASS_TIERS)
            {
                if (AccountLevel >= tier.level && tier.free.type == "trail" && !string.IsNullOrEmpty(tier.free.id))
                    if (!trails.Contains(tier.free.id)) trails.Add(tier.free.id);
                if (AccountLevel >= tier.level && IsPremium && tier.premium.type == "trail" && !string.IsNullOrEmpty(tier.premium.id))
                    if (!trails.Contains(tier.premium.id)) trails.Add(tier.premium.id);
            }
            return trails;
        }
    }

    public bool HasShieldBuff
    {
        get
        {
            foreach (var tier in GameData.BATTLE_PASS_TIERS)
            {
                if (tier.premium.id == "buffShield")
                    return ClaimedRewards.Contains(tier.level + "-premium");
            }
            return false;
        }
    }

    public int MaxShields => HasShieldBuff ? 6 : 5;

    public bool HasUnclaimedRewards
    {
        get
        {
            foreach (var tier in GameData.BATTLE_PASS_TIERS)
            {
                if (AccountLevel >= tier.level)
                {
                    bool freeUnclaimed = !ClaimedRewards.Contains(tier.level + "-free");
                    bool premiumUnclaimed = IsPremium && !ClaimedRewards.Contains(tier.level + "-premium");
                    if (freeUnclaimed || premiumUnclaimed) return true;
                }
            }
            return false;
        }
    }

    public bool Universe1Cleared
    {
        get
        {
            for (int s = 1; s <= 10; s++)
                for (int l = 1; l <= 5; l++)
                    if (!LevelScores.ContainsKey($"1-{s}-{l}") || LevelScores[$"1-{s}-{l}"] <= 0)
                        return false;
            return true;
        }
    }

    public bool CanEnterUniverse2 => Universe1Cleared && TotalStars >= 100;

    // --- ACTIONS ---
    public static string LevelKey(int universe, int sector, int level) => $"{universe}-{sector}-{level}";

    public void SetLevelScore(int universe, int sector, int level, int stars)
    {
        string key = LevelKey(universe, sector, level);
        int current = LevelScores.ContainsKey(key) ? LevelScores[key] : 0;
        if (stars > current) LevelScores[key] = stars;
    }

    public int GetLevelScore(int universe, int sector, int level)
    {
        string key = LevelKey(universe, sector, level);
        return LevelScores.ContainsKey(key) ? LevelScores[key] : 0;
    }

    public int GetSectorStars(int universe, int sector)
    {
        int total = 0;
        for (int l = 1; l <= 5; l++) total += GetLevelScore(universe, sector, l);
        return total;
    }

    public int GetSectorLevelsCleared(int universe, int sector)
    {
        int count = 0;
        for (int l = 1; l <= 5; l++)
            if (GetLevelScore(universe, sector, l) > 0) count++;
        return count;
    }

    public bool IsSectorUnlocked(int universe, int sector)
    {
        if (universe == 2 && !CanEnterUniverse2) return false;
        if (sector == 1) return true;
        return GetSectorLevelsCleared(universe, sector - 1) == 5;
    }

    public bool IsSectorBeaten(int universe, int sector)
    {
        return GetSectorLevelsCleared(universe, sector) == 5;
    }

    public void ClaimReward(int tierLevel, bool isPremiumReward)
    {
        string rewardId = tierLevel + "-" + (isPremiumReward ? "premium" : "free");
        if (ClaimedRewards.Contains(rewardId)) return;

        GameData.BattlePassTier tier = null;
        foreach (var t in GameData.BATTLE_PASS_TIERS)
            if (t.level == tierLevel) { tier = t; break; }
        if (tier == null) return;

        var rewardData = isPremiumReward ? tier.premium : tier.free;
        if (rewardData.credits > 0) Credits += rewardData.credits;

        ClaimedRewards.Add(rewardId);
    }

    public void ToggleShipEquip(string id)
    {
        if (ActiveShips.Contains(id))
        {
            if (ActiveShips.Count > 1) ActiveShips.Remove(id);
        }
        else
        {
            ActiveShips.Add(id);
        }
    }

    // --- PERSISTENCE ---
    public void Save()
    {
        // Scores
        List<string> scoreEntries = new List<string>();
        foreach (var kv in LevelScores) scoreEntries.Add(kv.Key + "=" + kv.Value);
        PlayerPrefs.SetString("LevelScores", string.Join("|", scoreEntries));

        // Ships/Trails
        PlayerPrefs.SetString("ActiveShips", string.Join("|", ActiveShips));
        PlayerPrefs.SetString("ActiveTrail", ActiveTrail);

        // Progression
        PlayerPrefs.SetInt("IsPremium", IsPremium ? 1 : 0);
        PlayerPrefs.SetInt("Credits", Credits);
        PlayerPrefs.SetInt("CurrentUniverse", CurrentUniverse);
        PlayerPrefs.SetString("ClaimedRewards", string.Join("|", ClaimedRewards));
        PlayerPrefs.SetInt("GlobalShields", GlobalShields);

        PlayerPrefs.Save();
    }

    public void Load()
    {
        // Scores
        LevelScores.Clear();
        string scoresStr = PlayerPrefs.GetString("LevelScores", "");
        if (!string.IsNullOrEmpty(scoresStr))
        {
            foreach (var entry in scoresStr.Split('|'))
            {
                var parts = entry.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out int val))
                    LevelScores[parts[0]] = val;
            }
        }

        // Ships
        string shipsStr = PlayerPrefs.GetString("ActiveShips", "shipA");
        ActiveShips = new List<string>(shipsStr.Split('|'));
        if (ActiveShips.Count == 0) ActiveShips.Add("shipA");

        ActiveTrail = PlayerPrefs.GetString("ActiveTrail", "trailDefault");
        IsPremium = PlayerPrefs.GetInt("IsPremium", 0) == 1;
        Credits = PlayerPrefs.GetInt("Credits", 0);
        CurrentUniverse = PlayerPrefs.GetInt("CurrentUniverse", 1);
        GlobalShields = PlayerPrefs.GetInt("GlobalShields", 5);

        ClaimedRewards.Clear();
        string rewardsStr = PlayerPrefs.GetString("ClaimedRewards", "");
        if (!string.IsNullOrEmpty(rewardsStr))
            ClaimedRewards = new List<string>(rewardsStr.Split('|'));
    }
}
