using System;
using System.Globalization;
using UnityEngine;

// 全局元数据：金币、关卡解锁、植物拥有状态。
public static class GameMetaData
{
    public const int MaxStage = 5;
    public const int PlantCount = 50;

    // Legacy IDs kept for save migration compatibility.
    public const string PlantPea = "pea";
    public const string PlantIce = "ice";
    public const string PlantFire = "fire";
    public const string PlantCannon = "cannon";
    public const string PlantPoison = "poison";

    private const string KeyInit = "meta_init_v1";
    private const string KeyCoins = "meta_coins";
    private const string KeyUnlockedStage = "meta_unlocked_stage";
    private const string KeyPlantPrefix = "meta_plant_";

    private static readonly string[] s_PlantIds = BuildPlantIds();
    private static readonly string[] s_PlantNames =
    {
        "双叶射手", "荆棘投芽", "疾风豌豆", "森盾芽兵", "青藤连射",
        "暴雨喷苗", "翠脉追击", "林影突刺", "春雷怒芽", "裂地豆王",
        "霜针幼株", "冰棱法芽", "寒雾菇灵", "雪脉射核", "冻土晶花",
        "极光寒芽", "霜环守卫", "冰河藤矛", "白夜霜塔", "玄霜先知",
        "烈焰花炮", "炽羽爆芽", "赤曜喷株", "炎脉巡猎", "熔火祭司",
        "焚野守望", "火冠战藤", "烬风突袭", "炎环裁决", "天火领主",
        "重炮铁芽", "钢刺榴株", "崩山炮藤", "铁穹卫苗", "雷铆爆塔",
        "裂甲轰花", "震地重芯", "岩脊火炮", "霆锤炮卫", "苍穹巨炮",
        "毒藤术芽", "蚀骨花灵", "幽绿渗株", "沼影毒针", "瘴雾猎苗",
        "酸潮毒蕊", "黯藤侵蚀", "噬魂毒冠", "夜幕腐芽", "终焉毒皇"
    };

    private static readonly string[] s_FamilyNames = { "豌豆系", "冰霜系", "火焰系", "重炮系", "毒藤系" };
    private static readonly string[] s_FamilyDescs =
    {
        "远程连射，压制杂兵",
        "附带减速，稳定控场",
        "高频输出，清线迅速",
        "重击爆发，专打硬敌",
        "持续侵蚀，残局强势"
    };

    public static void EnsureInit()
    {
        if (PlayerPrefs.GetInt(KeyInit, 0) == 1)
        {
            EnsurePlantOwnershipSchema();
            return;
        }

        PlayerPrefs.SetInt(KeyInit, 1);
        PlayerPrefs.SetInt(KeyCoins, 800);
        PlayerPrefs.SetInt(KeyUnlockedStage, 1);

        // 默认免费拥有前 7 个植物，保证可组 7 植物阵容。
        for (int i = 0; i < s_PlantIds.Length; i++)
        {
            PlayerPrefs.SetInt(KeyPlantPrefix + s_PlantIds[i], i < 7 ? 1 : 0);
        }

        // Legacy keys are still written once so old UI logic remains backward-safe.
        PlayerPrefs.SetInt(KeyPlantPrefix + PlantPea, 1);
        PlayerPrefs.SetInt(KeyPlantPrefix + PlantIce, 0);
        PlayerPrefs.SetInt(KeyPlantPrefix + PlantFire, 0);
        PlayerPrefs.SetInt(KeyPlantPrefix + PlantCannon, 0);
        PlayerPrefs.SetInt(KeyPlantPrefix + PlantPoison, 0);
        PlayerPrefs.Save();
    }

    public static string[] GetAllPlantIds()
    {
        EnsureInit();
        string[] result = new string[s_PlantIds.Length];
        Array.Copy(s_PlantIds, result, s_PlantIds.Length);
        return result;
    }

    public static string GetPlantDisplayName(string plantId)
    {
        int index = ParsePlantIndex(plantId);
        if (index <= 0 || index > s_PlantNames.Length) return "未知植物";
        return s_PlantNames[index - 1];
    }

    public static string GetPlantDesc(string plantId)
    {
        int index = ParsePlantIndex(plantId);
        if (index <= 0) return string.Empty;

        int family = (index - 1) % 5;
        int tier = (index - 1) / 5 + 1;
        int crit = 5 + (index * 3) % 23;
        int boss = 8 + (index * 5) % 37;
        int exec = (index % 5 == 0 || family == 4) ? (8 + (index * 7) % 17) : 0;
        return s_FamilyNames[family]
            + " · 阶级 " + tier
            + " · 特性「" + GetPlantTraitName(index) + "」"
            + " · 暴击" + crit + "% 首领增伤" + boss + "%"
            + (exec > 0 ? (" 斩杀" + exec + "%") : string.Empty);
    }

    public static int GetPlantPrice(string plantId)
    {
        int index = ParsePlantIndex(plantId);
        if (index <= 0) return 9999;
        if (index == 1) return 0;

        int tier = (index - 1) / 5;
        int family = (index - 1) % 5;
        int[] familyBase = { 420, 620, 760, 980, 840 };
        return familyBase[family] + tier * 130 + family * 35;
    }

    public static string GetPlantIdByIndex(int plantIndex)
    {
        EnsureInit();
        int idx = Mathf.Clamp(plantIndex, 1, PlantCount) - 1;
        return s_PlantIds[idx];
    }

    private static string[] BuildPlantIds()
    {
        var ids = new string[PlantCount];
        for (int i = 0; i < PlantCount; i++)
        {
            ids[i] = "p" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
        }

        return ids;
    }

    private static int ParsePlantIndex(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return -1;
        if (plantId == PlantPea) return 1;
        if (plantId == PlantIce) return 2;
        if (plantId == PlantFire) return 3;
        if (plantId == PlantCannon) return 4;
        if (plantId == PlantPoison) return 5;

        if (plantId.Length == 3 && plantId[0] == 'p')
        {
            int value;
            if (int.TryParse(plantId.Substring(1, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                if (value >= 1 && value <= PlantCount) return value;
            }
        }

        return -1;
    }

    private static string GetPlantTraitName(int plantIndex)
    {
        string[] prefix =
        {
            "锋刃", "霜环", "烈脉", "雷铠", "暗潮",
            "星辉", "裂地", "苍穹", "幻影", "终焉"
        };
        string[] suffix = { "之息", "印记", "律动", "核心", "契约" };

        int idx = Mathf.Clamp(plantIndex - 1, 0, PlantCount - 1);
        return prefix[idx % prefix.Length] + suffix[idx / prefix.Length];
    }

    private static void EnsurePlantOwnershipSchema()
    {
        bool dirty = false;
        bool hasAny = false;
        for (int i = 0; i < s_PlantIds.Length; i++)
        {
            if (PlayerPrefs.HasKey(KeyPlantPrefix + s_PlantIds[i]))
            {
                hasAny = true;
                break;
            }
        }

        // Migrate old 5-plant ownership to the new 50-plant schema.
        if (!hasAny)
        {
            bool[] legacyOwned =
            {
                PlayerPrefs.GetInt(KeyPlantPrefix + PlantPea, 1) == 1,
                PlayerPrefs.GetInt(KeyPlantPrefix + PlantIce, 0) == 1,
                PlayerPrefs.GetInt(KeyPlantPrefix + PlantFire, 0) == 1,
                PlayerPrefs.GetInt(KeyPlantPrefix + PlantCannon, 0) == 1,
                PlayerPrefs.GetInt(KeyPlantPrefix + PlantPoison, 0) == 1
            };

            for (int i = 0; i < s_PlantIds.Length; i++)
            {
                bool owned = i < legacyOwned.Length ? legacyOwned[i] : false;
                PlayerPrefs.SetInt(KeyPlantPrefix + s_PlantIds[i], owned ? 1 : 0);
            }
            dirty = true;
        }

        // Safety: keep at least 7 free plants available to avoid lineup deadlock.
        for (int i = 0; i < 7 && i < s_PlantIds.Length; i++)
        {
            if (PlayerPrefs.GetInt(KeyPlantPrefix + s_PlantIds[i], 0) == 1) continue;
            PlayerPrefs.SetInt(KeyPlantPrefix + s_PlantIds[i], 1);
            dirty = true;
        }

        if (dirty) PlayerPrefs.Save();
    }

    public static int GetCoins()
    {
        EnsureInit();
        return Mathf.Max(0, PlayerPrefs.GetInt(KeyCoins, 0));
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        EnsureInit();
        PlayerPrefs.SetInt(KeyCoins, GetCoins() + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;

        int coins = GetCoins();
        if (coins < amount) return false;

        PlayerPrefs.SetInt(KeyCoins, coins - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool IsPlantOwned(string plantId)
    {
        EnsureInit();
        if (string.IsNullOrEmpty(plantId)) return false;
        return PlayerPrefs.GetInt(KeyPlantPrefix + plantId, 0) == 1;
    }

    public static void SetPlantOwned(string plantId, bool owned)
    {
        if (string.IsNullOrEmpty(plantId)) return;
        EnsureInit();
        PlayerPrefs.SetInt(KeyPlantPrefix + plantId, owned ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int GetUnlockedStage()
    {
        EnsureInit();
        return Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlockedStage, 1), 1, MaxStage);
    }

    public static void SetUnlockedStage(int stage)
    {
        EnsureInit();
        PlayerPrefs.SetInt(KeyUnlockedStage, Mathf.Clamp(stage, 1, MaxStage));
        PlayerPrefs.Save();
    }

    public static float GetHpScaleForStage(int stage)
    {
        // 每一关额外提高敌人血量倍率。
        int clampedStage = Mathf.Clamp(stage, 1, MaxStage);
        return 1f + (clampedStage - 1) * 0.35f;
    }

    public static int GetStageClearReward(int stage)
    {
        int clampedStage = Mathf.Clamp(stage, 1, MaxStage);
        return 120 + (clampedStage - 1) * 80;
    }

    public static void OnStageCleared(int stage)
    {
        EnsureInit();
        DailyTaskSystem.MarkBattleCompleted();

        int clampedStage = Mathf.Clamp(stage, 1, MaxStage);
        AddCoins(GetStageClearReward(clampedStage));

        if (clampedStage >= GetUnlockedStage() && clampedStage < MaxStage)
        {
            SetUnlockedStage(clampedStage + 1);
        }
    }
}

// 每日任务系统：按天重置任务状态，提供完成与领奖接口。
public static class DailyTaskSystem
{
    public const int LoginRewardCoins = 120;
    public const int BattleRewardCoins = 180;

    private const string KeyDay = "daily_task_day_v1";
    private const string KeyLoginCompleted = "daily_task_login_completed_v1";
    private const string KeyLoginClaimed = "daily_task_login_claimed_v1";
    private const string KeyBattleCompleted = "daily_task_battle_completed_v1";
    private const string KeyBattleClaimed = "daily_task_battle_claimed_v1";

    private static string TodayKey()
    {
        return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static bool GetFlag(string key)
    {
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private static void SetFlag(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    private static void EnsureTodayState()
    {
        string today = TodayKey();
        string cachedDay = PlayerPrefs.GetString(KeyDay, string.Empty);
        if (cachedDay == today) return;

        PlayerPrefs.SetString(KeyDay, today);
        SetFlag(KeyLoginCompleted, false);
        SetFlag(KeyLoginClaimed, false);
        SetFlag(KeyBattleCompleted, false);
        SetFlag(KeyBattleClaimed, false);
        PlayerPrefs.Save();
    }

    public static void MarkLoginCompleted()
    {
        EnsureTodayState();
        if (GetFlag(KeyLoginCompleted)) return;
        SetFlag(KeyLoginCompleted, true);
        PlayerPrefs.Save();
    }

    public static void MarkBattleCompleted()
    {
        EnsureTodayState();
        if (GetFlag(KeyBattleCompleted)) return;
        SetFlag(KeyBattleCompleted, true);
        PlayerPrefs.Save();
    }

    public static bool IsBattleCompleted()
    {
        EnsureTodayState();
        return GetFlag(KeyBattleCompleted);
    }

    public static bool IsLoginRewardClaimed()
    {
        EnsureTodayState();
        return GetFlag(KeyLoginClaimed);
    }

    public static bool IsBattleRewardClaimed()
    {
        EnsureTodayState();
        return GetFlag(KeyBattleClaimed);
    }

    public static bool CanClaimLoginReward()
    {
        EnsureTodayState();
        return GetFlag(KeyLoginCompleted) && !GetFlag(KeyLoginClaimed);
    }

    public static bool CanClaimBattleReward()
    {
        EnsureTodayState();
        return GetFlag(KeyBattleCompleted) && !GetFlag(KeyBattleClaimed);
    }

    public static int ClaimLoginReward()
    {
        if (!CanClaimLoginReward()) return 0;
        SetFlag(KeyLoginClaimed, true);
        GameMetaData.AddCoins(LoginRewardCoins);
        PlayerPrefs.Save();
        return LoginRewardCoins;
    }

    public static int ClaimBattleReward()
    {
        if (!CanClaimBattleReward()) return 0;
        SetFlag(KeyBattleClaimed, true);
        GameMetaData.AddCoins(BattleRewardCoins);
        PlayerPrefs.Save();
        return BattleRewardCoins;
    }

    public static bool HasAnyClaimableRewards()
    {
        return CanClaimLoginReward() || CanClaimBattleReward();
    }
}

// 系统设置：声音开关、全屏模式、画质和分辨率。
public static class SystemSettingsData
{
    private const string KeySoundEnabled = "sys_sound_enabled_v1";
    private const string KeyFullscreen = "sys_fullscreen_v1";
    private const string KeyQualityLevel = "sys_quality_level_v1";
    private const string KeyResolutionWidth = "sys_resolution_w_v1";
    private const string KeyResolutionHeight = "sys_resolution_h_v1";

    public static void ApplySavedSettings()
    {
        SetSoundEnabled(IsSoundEnabled());
        SetFullscreen(IsFullscreen());
        SetQualityLevel(GetQualityLevel());

        int width;
        int height;
        if (TryGetSavedResolution(out width, out height))
        {
            Screen.SetResolution(width, height, IsFullscreen());
        }
    }

    public static bool IsSoundEnabled()
    {
        return PlayerPrefs.GetInt(KeySoundEnabled, 1) == 1;
    }

    public static void SetSoundEnabled(bool enabled)
    {
        AudioListener.volume = enabled ? 1f : 0f;
        PlayerPrefs.SetInt(KeySoundEnabled, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int GetQualityLevel()
    {
        int current = QualitySettings.GetQualityLevel();
        int saved = PlayerPrefs.GetInt(KeyQualityLevel, current);
        return Mathf.Clamp(saved, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    public static void SetQualityLevel(int qualityLevel)
    {
        int level = Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        QualitySettings.SetQualityLevel(level, true);
        PlayerPrefs.SetInt(KeyQualityLevel, level);
        PlayerPrefs.Save();
    }

    public static bool IsFullscreen()
    {
        return PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
    }

    public static void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool TryGetSavedResolution(out int width, out int height)
    {
        width = PlayerPrefs.GetInt(KeyResolutionWidth, 0);
        height = PlayerPrefs.GetInt(KeyResolutionHeight, 0);
        return width > 0 && height > 0;
    }

    public static void SetResolution(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        Screen.SetResolution(width, height, IsFullscreen());
        PlayerPrefs.SetInt(KeyResolutionWidth, width);
        PlayerPrefs.SetInt(KeyResolutionHeight, height);
        PlayerPrefs.Save();
    }
}
