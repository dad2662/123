using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // Naming, path math, controls, stage flow, and reusable UI helpers.
    private static string TowerCode(TowerType type)
    {
        switch (type)
        {
            case TowerType.Pea: return "豌";
            case TowerType.Ice: return "冰";
            case TowerType.Fire: return "焰";
            case TowerType.Cannon: return "炮";
            case TowerType.Poison: return "毒";
            default: return "?";
        }
    }

    private static string TowerName(TowerType type)
    {
        switch (type)
        {
            case TowerType.Pea: return "豌豆塔";
            case TowerType.Ice: return "冰冻塔";
            case TowerType.Fire: return "火焰塔";
            case TowerType.Cannon: return "重炮塔";
            case TowerType.Poison: return "毒藤塔";
            default: return "未知炮塔";
        }
    }

    private void AddGroundDecor(RectTransform cellRt, int x, int y, bool isPath)
    {
        if (isPath)
        {
            if (Hash01(x, y, 61) > 0.8f) return;

            int count = Hash01(x, y, 67) > 0.55f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                float px = (Hash01(x, y, 71 + i) - 0.5f) * (CellSize - 20f);
                float py = (Hash01(x, y, 81 + i) - 0.5f) * (CellSize - 20f);
                float size = 6f + Hash01(x, y, 91 + i) * 10f;

                var pebble = CreateRect("Pebble_" + i, cellRt);
                pebble.anchorMin = new Vector2(0.5f, 0.5f);
                pebble.anchorMax = new Vector2(0.5f, 0.5f);
                pebble.pivot = new Vector2(0.5f, 0.5f);
                pebble.sizeDelta = new Vector2(size, size * 0.58f);
                pebble.anchoredPosition = new Vector2(px, py);
                AddImage(pebble.gameObject, new Color(0.56f, 0.40f, 0.28f, 0.95f));
            }
            return;
        }

        if (Hash01(x, y, 31) < 0.38f)
        {
            int tuftCount = Hash01(x, y, 37) > 0.6f ? 2 : 1;
            for (int i = 0; i < tuftCount; i++)
            {
                float px = (Hash01(x, y, 41 + i) - 0.5f) * (CellSize - 14f);
                float py = (Hash01(x, y, 51 + i) - 0.5f) * (CellSize - 14f);
                float w = 8f + Hash01(x, y, 57 + i) * 7f;
                float h = 14f + Hash01(x, y, 63 + i) * 12f;

                var grass = CreateRect("Grass_" + i, cellRt);
                grass.anchorMin = new Vector2(0.5f, 0.5f);
                grass.anchorMax = new Vector2(0.5f, 0.5f);
                grass.pivot = new Vector2(0.5f, 0.1f);
                grass.sizeDelta = new Vector2(w, h);
                grass.anchoredPosition = new Vector2(px, py);
                AddImage(grass.gameObject, new Color(0.20f, 0.26f, 0.12f, 0.85f));
            }
        }

        if (Hash01(x, y, 101) < 0.16f)
        {
            var rock = CreateRect("Rock", cellRt);
            rock.anchorMin = new Vector2(0.5f, 0.5f);
            rock.anchorMax = new Vector2(0.5f, 0.5f);
            rock.pivot = new Vector2(0.5f, 0.5f);
            rock.sizeDelta = new Vector2(10f + Hash01(x, y, 107) * 10f, 8f + Hash01(x, y, 111) * 8f);
            rock.anchoredPosition = new Vector2((Hash01(x, y, 109) - 0.5f) * (CellSize - 14f), (Hash01(x, y, 113) - 0.5f) * (CellSize - 14f));
            AddImage(rock.gameObject, new Color(0.66f, 0.66f, 0.68f, 0.9f));
        }
    }

    private static float Hash01(int x, int y, int salt)
    {
        int n = x * 92837111 ^ y * 689287499 ^ salt * 283923481;
        n ^= n << 13;
        n ^= n >> 17;
        n ^= n << 5;
        return (n & 0x7fffffff) / 2147483647f;
    }

    private static Color Tint(Color color, float mul)
    {
        return new Color(
            Mathf.Clamp01(color.r * mul),
            Mathf.Clamp01(color.g * mul),
            Mathf.Clamp01(color.b * mul),
            color.a);
    }

    private bool EnsurePathPoints()
    {
        // 将网格路径节点缓存为世界坐标，减少每帧重复计算。
        if (_pathPoints.Count > 0) return true;

        _pathPoints.Clear();
        for (int i = 0; i < _path.Count; i++)
        {
            Vector2Int node = _path[i];
            _pathPoints.Add(GetCellPosition(node.x, node.y));
        }

        if (_pathPoints.Count == 0)
        {
            Debug.LogError("路径为空，请检查路径配置。");
            return false;
        }

        return true;
    }

    private static Vector2 GetCellPosition(int x, int y)
    {
        float left = -((Cols - 1) * CellSize) * 0.5f;
        float bottom = -((Rows - 1) * CellSize) * 0.5f;
        return new Vector2(left + x * CellSize, bottom + y * CellSize);
    }

    private static Text GetButtonLabel(Button button)
    {
        if (button == null) return null;
        return button.GetComponentInChildren<Text>();
    }

    private static string TowerFamilyName(TowerType type)
    {
        switch (type)
        {
            case TowerType.Pea: return "豌豆";
            case TowerType.Ice: return "冰冻";
            case TowerType.Fire: return "火焰";
            case TowerType.Cannon: return "重炮";
            case TowerType.Poison: return "毒藤";
            default: return "未知";
        }
    }

    private static Color GetFamilySelectColor(TowerType type)
    {
        switch (type)
        {
            case TowerType.Pea: return new Color(0.22f, 0.8f, 0.28f, 1f);
            case TowerType.Ice: return new Color(0.2f, 0.74f, 0.95f, 1f);
            case TowerType.Fire: return new Color(0.9f, 0.42f, 0.2f, 1f);
            case TowerType.Cannon: return new Color(0.75f, 0.62f, 0.24f, 1f);
            case TowerType.Poison: return new Color(0.52f, 0.78f, 0.2f, 1f);
            default: return new Color(0.42f, 0.42f, 0.42f, 1f);
        }
    }

    private static string GetPlantTraitBaseName(int kindId)
    {
        string[] prefix =
        {
            "锋刃", "霜环", "烈脉", "雷铠", "暗潮",
            "星辉", "裂地", "苍穹", "幻影", "终焉"
        };
        string[] suffix = { "之息", "印记", "律动", "核心", "契约" };

        int idx = Mathf.Clamp(kindId - 1, 0, PlantKindCount - 1);
        return prefix[idx % prefix.Length] + suffix[idx / prefix.Length];
    }

    private static string BuildPlantTraitSummary(PlantKindData kind)
    {
        if (kind == null) return string.Empty;

        int crit = Mathf.RoundToInt(kind.CritChance * 100f);
        int boss = Mathf.RoundToInt((kind.BossDamageMul - 1f) * 100f);
        int splash = Mathf.RoundToInt(kind.SplashDamageMul * 100f);
        int exec = Mathf.RoundToInt(kind.ExecuteHpRatio * 100f);
        int armor = Mathf.RoundToInt(kind.ArmorBreakRatio * 100f);

        return kind.TraitName
            + " | 暴击" + crit + "%"
            + " / 首领增伤" + boss + "%"
            + " / 溅射" + splash + "%"
            + " / 斩杀" + exec + "%"
            + " / 破甲" + armor + "%";
    }

    private static string BuildPlantDetailDescription(PlantKindData kind)
    {
        if (kind == null) return string.Empty;

        int crit = Mathf.RoundToInt(kind.CritChance * 100f);
        int critMul = Mathf.RoundToInt(kind.CritDamageMul * 100f);
        int boss = Mathf.RoundToInt((kind.BossDamageMul - 1f) * 100f);
        int splashRadius = Mathf.RoundToInt(kind.SplashRadius);
        int splash = Mathf.RoundToInt(kind.SplashDamageMul * 100f);
        int exec = Mathf.RoundToInt(kind.ExecuteHpRatio * 100f);
        int armor = Mathf.RoundToInt(kind.ArmorBreakRatio * 100f);

        string feature = "暴击" + crit + "%(x" + (critMul / 100f).ToString("0.00")
            + ")  首领增伤+" + boss + "%";
        if (splash > 0) feature += "  溅射" + splashRadius + "(" + splash + "%)";
        if (exec > 0) feature += "  斩杀" + exec + "%";
        if (armor > 0) feature += "  破甲" + armor + "%";

        return "【" + kind.DisplayName + "】 特性：" + kind.TraitName + "\n"
            + "造价" + kind.BuildCost + "  升级基础" + kind.UpgradeBaseCost
            + "  伤害x" + kind.DamageMul.ToString("0.00")
            + "  射程+" + kind.RangeBonus.ToString("0")
            + "  攻速x" + kind.FireIntervalMul.ToString("0.00")
            + "  控制+" + kind.SlowTimeBonus.ToString("0.00")
            + "  |  " + feature;
    }

    private static readonly string[] s_UniquePlantNames =
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

    private static readonly string[] s_EnemyNamePrefix =
    {
        "裂喉", "腐爪", "赤眸", "霜牙", "铁背",
        "影袭", "瘴骨", "雷吻", "渊鳞", "噬火"
    };

    private static readonly string[] s_EnemyNameSuffix =
    {
        "行者", "猎犬", "狂徒", "术士", "卫士"
    };

    private static string GetUniquePlantDisplayName(int kindId, TowerType family)
    {
        int idx = kindId - 1;
        if (idx >= 0 && idx < s_UniquePlantNames.Length)
        {
            return s_UniquePlantNames[idx];
        }

        return TowerFamilyName(family) + "·" + kindId.ToString("00");
    }

    private static string GetUniqueEnemyDisplayName(int kindId)
    {
        int idx = Mathf.Clamp(kindId - 1, 0, EnemyKindCount - 1);
        int prefixIdx = idx % s_EnemyNamePrefix.Length;
        int suffixIdx = idx / s_EnemyNamePrefix.Length;
        suffixIdx = Mathf.Clamp(suffixIdx, 0, s_EnemyNameSuffix.Length - 1);
        return s_EnemyNamePrefix[prefixIdx] + s_EnemyNameSuffix[suffixIdx];
    }

    private void EnsurePlantKinds()
    {
        if (_plantKinds.Count == PlantKindCount) return;

        _plantKinds.Clear();
        for (int i = 1; i <= PlantKindCount; i++)
        {
            TowerType family = (TowerType)((i - 1) % 5);
            TowerProfile familyProfile = GetTowerProfile(family);
            int tier = (i - 1) / 5;

            float buildNoise = 0.88f + Hash01(i, tier, 211) * 0.38f;
            float upgradeNoise = 0.9f + Hash01(i, tier, 223) * 0.35f;
            float damageMul = 0.82f + Hash01(i, tier, 227) * 0.56f + i * 0.0015f;
            float rangeBonus = -14f + Hash01(i, tier, 229) * 38f + (i % 9) * 0.45f;
            float fireMul = 0.8f + Hash01(i, tier, 233) * 0.45f + (i % 7) * 0.008f;
            float slowBonus = (family == TowerType.Ice || family == TowerType.Poison)
                ? Hash01(i, tier, 239) * 0.75f
                : 0f;

            float critChance = Mathf.Clamp(0.03f + Hash01(i, tier, 241) * 0.17f + i * 0.0008f, 0.03f, 0.33f);
            float critDamageMul = 1.18f + Hash01(i, tier, 251) * 0.92f + tier * 0.02f;
            float bossDamageMul = 1.05f + Hash01(i, tier, 257) * 0.45f + (i % 6) * 0.01f;
            float splashRadius = (family == TowerType.Cannon || family == TowerType.Fire || i % 3 == 0)
                ? 46f + Hash01(i, tier, 263) * 68f + (i % 5) * 2.8f
                : 0f;
            float splashDamageMul = splashRadius > 0f
                ? 0.16f + Hash01(i, tier, 269) * 0.36f
                : 0f;
            float executeRatio = (i % 5 == 0 || family == TowerType.Poison)
                ? 0.07f + Hash01(i, tier, 271) * 0.19f
                : 0f;
            float armorBreakRatio = (i % 4 == 1 || family == TowerType.Poison || family == TowerType.Cannon)
                ? 0.008f + Hash01(i, tier, 277) * 0.052f
                : 0f;

            int buildCost = Mathf.Max(20, Mathf.RoundToInt((familyProfile.BuildCost + tier * 9f) * buildNoise));
            int upgradeBaseCost = Mathf.Max(16, Mathf.RoundToInt((familyProfile.UpgradeBaseCost + tier * 6f) * upgradeNoise));

            _plantKinds.Add(new PlantKindData
            {
                KindId = i,
                DisplayName = GetUniquePlantDisplayName(i, family),
                Family = family,
                BuildCost = buildCost,
                UpgradeBaseCost = upgradeBaseCost,
                DamageMul = damageMul,
                RangeBonus = rangeBonus,
                FireIntervalMul = fireMul,
                SlowTimeBonus = slowBonus,
                TraitName = GetPlantTraitBaseName(i),
                CritChance = critChance,
                CritDamageMul = critDamageMul,
                BossDamageMul = bossDamageMul,
                SplashRadius = splashRadius,
                SplashDamageMul = splashDamageMul,
                ExecuteHpRatio = executeRatio,
                ArmorBreakRatio = armorBreakRatio
            });
        }
    }

    private void EnsureEnemyKinds()
    {
        if (_enemyKinds.Count == EnemyKindCount) return;

        _enemyKinds.Clear();
        for (int i = 1; i <= EnemyKindCount; i++)
        {
            float hpMul = 0.76f + Hash01(i, 31, 401) * 0.9f;
            float speedMul = 0.74f + Hash01(i, 47, 409) * 0.85f;
            float rewardMul = 0.72f + Hash01(i, 59, 419) * 0.82f;
            float sizeMul = 0.86f + Hash01(i, 61, 431) * 0.42f;
            float hue = Hash01(i, 71, 443);
            Color bodyColor = Color.HSVToRGB(hue, 0.74f, 0.94f);

            _enemyKinds.Add(new EnemyKindData
            {
                KindId = i,
                DisplayName = GetUniqueEnemyDisplayName(i),
                HpMul = hpMul,
                SpeedMul = speedMul,
                RewardMul = rewardMul,
                SizeMul = sizeMul,
                BodyColor = bodyColor
            });
        }
    }

    private void EnsureBossTypes()
    {
        if (_bossTypes.Count == 5) return;

        _bossTypes.Clear();
        _bossTypes.Add(new BossTypeData
        {
            BossTypeId = 1,
            Name = "裂蹄战王",
            BodyColor = new Color(0.86f, 0.34f, 0.24f, 1f)
        });
        _bossTypes.Add(new BossTypeData
        {
            BossTypeId = 2,
            Name = "腐沼母后",
            BodyColor = new Color(0.42f, 0.72f, 0.24f, 1f)
        });
        _bossTypes.Add(new BossTypeData
        {
            BossTypeId = 3,
            Name = "钢甲暴君",
            BodyColor = new Color(0.56f, 0.62f, 0.78f, 1f)
        });
        _bossTypes.Add(new BossTypeData
        {
            BossTypeId = 4,
            Name = "雷鸣先驱",
            BodyColor = new Color(0.26f, 0.72f, 0.92f, 1f)
        });
        _bossTypes.Add(new BossTypeData
        {
            BossTypeId = 5,
            Name = "深渊主宰",
            BodyColor = new Color(0.66f, 0.34f, 0.86f, 1f)
        });
    }

    private BossTypeData GetBossTypeById(int bossTypeId)
    {
        EnsureBossTypes();
        int idx = bossTypeId - 1;
        if (idx < 0 || idx >= _bossTypes.Count) return null;
        return _bossTypes[idx];
    }

    private void EnsureBattlePlantDefaults()
    {
        EnsurePlantKinds();
        GameMetaData.EnsureInit();

        List<int> ownedKindIds = new List<int>();
        CollectOwnedPlantKindIds(ownedKindIds);

        bool battleValid = _battlePlantKindIds.Count == RequiredLineupCount;
        if (battleValid)
        {
            for (int i = 0; i < _battlePlantKindIds.Count; i++)
            {
                if (ownedKindIds.Contains(_battlePlantKindIds[i])) continue;
                battleValid = false;
                break;
            }
        }

        if (!battleValid)
        {
            _battlePlantKindIds.Clear();
            for (int i = 0; i < RequiredLineupCount; i++)
            {
                _battlePlantKindIds.Add(ownedKindIds[i % ownedKindIds.Count]);
            }
        }

        bool lineupValid = _lineupPickPlantKindIds.Count == RequiredLineupCount;
        if (lineupValid)
        {
            for (int i = 0; i < _lineupPickPlantKindIds.Count; i++)
            {
                if (ownedKindIds.Contains(_lineupPickPlantKindIds[i])) continue;
                lineupValid = false;
                break;
            }
        }

        if (!lineupValid)
        {
            _lineupPickPlantKindIds.Clear();
            _lineupPickPlantKindIds.AddRange(_battlePlantKindIds);
        }

        _selectedPlantKindId = _battlePlantKindIds[0];
        _lineupPreviewKindId = _selectedPlantKindId;
        _plantPage = 0;
        _lineupPage = 0;
    }

    private void CollectOwnedPlantKindIds(List<int> outKindIds)
    {
        if (outKindIds == null) return;
        outKindIds.Clear();
        EnsurePlantKinds();
        GameMetaData.EnsureInit();

        for (int kindId = 1; kindId <= PlantKindCount; kindId++)
        {
            string plantId = GameMetaData.GetPlantIdByIndex(kindId);
            if (!GameMetaData.IsPlantOwned(plantId)) continue;
            outKindIds.Add(kindId);
        }

        if (outKindIds.Count == 0)
        {
            outKindIds.Add(1);
        }
    }

    private PlantKindData GetPlantKindById(int kindId)
    {
        EnsurePlantKinds();
        int idx = kindId - 1;
        if (idx < 0 || idx >= _plantKinds.Count) return null;
        return _plantKinds[idx];
    }

    private EnemyKindData GetEnemyKindById(int kindId)
    {
        EnsureEnemyKinds();
        int idx = kindId - 1;
        if (idx < 0 || idx >= _enemyKinds.Count) return null;
        return _enemyKinds[idx];
    }

    private PlantKindData GetSelectedPlantKind()
    {
        return GetPlantKindById(_selectedPlantKindId);
    }

    private string GetPlantKindName(int kindId)
    {
        PlantKindData kind = GetPlantKindById(kindId);
        return kind != null ? kind.DisplayName : "未知植物";
    }

    private string GetPlantKindCode(int kindId)
    {
        return "P" + Mathf.Clamp(kindId, 1, PlantKindCount).ToString("00");
    }

    private void RefreshTowerPlantButtons()
    {
        EnsureBattlePlantDefaults();
        int totalCount = _battlePlantKindIds.Count;
        int totalPages = Mathf.CeilToInt(totalCount / (float)PlantKindsPerPage);
        _plantPage = Mathf.Clamp(_plantPage, 0, Mathf.Max(0, totalPages - 1));

        Button[] slots = { _peaButton, _iceButton, _fireButton, _cannonButton, _poisonButton };
        for (int i = 0; i < slots.Length; i++)
        {
            int index = _plantPage * PlantKindsPerPage + i;
            RefreshPlantSlotButton(slots[i], index);
        }

        if (_plantPrevButton != null) _plantPrevButton.interactable = totalPages > 1;
        if (_plantNextButton != null) _plantNextButton.interactable = totalPages > 1;

        RefreshTowerSelectionColors();
    }

    private void RefreshPlantSlotButton(Button button, int index)
    {
        if (button == null) return;
        Text t = GetButtonLabel(button);
        if (index < 0 || index >= _battlePlantKindIds.Count)
        {
            button.interactable = false;
            if (t != null) t.text = "--";
            return;
        }

        PlantKindData kind = GetPlantKindById(_battlePlantKindIds[index]);
        if (kind == null)
        {
            button.interactable = false;
            if (t != null) t.text = "--";
            return;
        }

        button.interactable = true;
        if (t != null) t.text = kind.DisplayName;
    }

    private void SelectPlantSlot(int slotIndex)
    {
        int index = _plantPage * PlantKindsPerPage + slotIndex;
        if (index < 0 || index >= _battlePlantKindIds.Count) return;
        SelectPlantKindById(_battlePlantKindIds[index]);
    }

    private void PrevPlantPage()
    {
        int totalPages = Mathf.CeilToInt(_battlePlantKindIds.Count / (float)PlantKindsPerPage);
        if (totalPages <= 1) return;
        _plantPage = (_plantPage - 1 + totalPages) % totalPages;
        RefreshTowerPlantButtons();
    }

    private void NextPlantPage()
    {
        int totalPages = Mathf.CeilToInt(_battlePlantKindIds.Count / (float)PlantKindsPerPage);
        if (totalPages <= 1) return;
        _plantPage = (_plantPage + 1) % totalPages;
        RefreshTowerPlantButtons();
    }

    private void SelectPlantKindById(int kindId)
    {
        PlantKindData kind = GetPlantKindById(kindId);
        if (kind == null) return;
        _selectedPlantKindId = kind.KindId;
        _selectedTower = kind.Family;
        int lineupIdx = _battlePlantKindIds.IndexOf(_selectedPlantKindId);
        _plantPage = lineupIdx >= 0 ? lineupIdx / PlantKindsPerPage : 0;
        RefreshTowerSelectionColors();
        SetTip("已选择 " + kind.DisplayName + "（建造 " + kind.BuildCost + "）  " + BuildPlantTraitSummary(kind), 1.2f);
    }

    private void RefreshTowerSelectionColors()
    {
        Button[] slots = { _peaButton, _iceButton, _fireButton, _cannonButton, _poisonButton };
        for (int i = 0; i < slots.Length; i++)
        {
            int index = _plantPage * PlantKindsPerPage + i;
            if (index < 0 || index >= _battlePlantKindIds.Count)
            {
                UpdateTowerButtonColor(slots[i], false, TowerType.Pea);
                continue;
            }

            PlantKindData kind = GetPlantKindById(_battlePlantKindIds[index]);
            if (kind == null)
            {
                UpdateTowerButtonColor(slots[i], false, TowerType.Pea);
                continue;
            }
            UpdateTowerButtonColor(slots[i], kind.KindId == _selectedPlantKindId, kind.Family);
        }
    }

    private void UpdateTowerButtonColor(Button button, bool selected, TowerType family)
    {
        if (button == null) return;
        Image image = button.targetGraphic as Image;
        if (image == null) return;
        image.color = button.interactable && selected
            ? GetFamilySelectColor(family)
            : (button.interactable ? new Color(0.42f, 0.42f, 0.42f, 1f) : new Color(0.22f, 0.22f, 0.22f, 1f));
    }

    private void ShowPlantLineupPanel()
    {
        if (_lineupPanel == null) return;

        EnsureBattlePlantDefaults();
        _matchReady = false;
        _paused = false;
        Time.timeScale = _speed;
        RefreshSpeedButtons();

        if (_lineupPickPlantKindIds.Count != RequiredLineupCount)
        {
            _lineupPickPlantKindIds.Clear();
            _lineupPickPlantKindIds.AddRange(_battlePlantKindIds);
        }

        _lineupPanel.gameObject.SetActive(true);
        _lineupPage = 0;
        if (_lineupPickPlantKindIds.Count > 0) _lineupPreviewKindId = _lineupPickPlantKindIds[0];
        RefreshLineupPanel();
        SetTip("请选择 7 个植物后开始对局", 1.4f);
    }

    private void HidePlantLineupPanel()
    {
        if (_lineupPanel == null) return;
        _lineupPanel.gameObject.SetActive(false);
    }

    private void RefreshLineupPanel()
    {
        EnsurePlantKinds();
        List<int> ownedKindIds = new List<int>();
        CollectOwnedPlantKindIds(ownedKindIds);
        int totalPages = Mathf.CeilToInt(ownedKindIds.Count / (float)LineupPlantsPerPage);
        _lineupPage = Mathf.Clamp(_lineupPage, 0, Mathf.Max(0, totalPages - 1));

        if (ownedKindIds.Count > 0 && !ownedKindIds.Contains(_lineupPreviewKindId))
        {
            _lineupPreviewKindId = _lineupPickPlantKindIds.Count > 0 ? _lineupPickPlantKindIds[0] : ownedKindIds[0];
        }

        for (int i = 0; i < _lineupPlantButtons.Count; i++)
        {
            Button button = _lineupPlantButtons[i];
            if (button == null) continue;

            int index = _lineupPage * LineupPlantsPerPage + i;
            Text label = GetButtonLabel(button);
            if (index < 0 || index >= ownedKindIds.Count)
            {
                button.interactable = false;
                if (label != null) label.text = "--";
                ApplyLineupButtonColor(button, false, false, TowerType.Pea);
                continue;
            }

            PlantKindData kind = GetPlantKindById(ownedKindIds[index]);
            if (kind == null)
            {
                button.interactable = false;
                if (label != null) label.text = "--";
                ApplyLineupButtonColor(button, false, false, TowerType.Pea);
                continue;
            }
            bool selected = _lineupPickPlantKindIds.Contains(kind.KindId);
            bool preview = kind.KindId == _lineupPreviewKindId;
            button.interactable = true;
            if (label != null) label.text = kind.DisplayName;
            ApplyLineupButtonColor(button, selected, preview, kind.Family);
        }

        if (_lineupPrevPageButton != null) _lineupPrevPageButton.interactable = totalPages > 1;
        if (_lineupNextPageButton != null) _lineupNextPageButton.interactable = totalPages > 1;

        if (_lineupSelectedText != null)
        {
            string chosen = string.Empty;
            for (int i = 0; i < _lineupPickPlantKindIds.Count; i++)
            {
                if (i > 0) chosen += " / ";
                chosen += GetPlantKindCode(_lineupPickPlantKindIds[i]);
            }

            if (string.IsNullOrEmpty(chosen)) chosen = "无";
            _lineupSelectedText.text = "已选(" + _lineupPickPlantKindIds.Count + "/" + RequiredLineupCount + "): " + chosen;
        }

        if (_lineupTipText != null)
        {
            _lineupTipText.text = _lineupPickPlantKindIds.Count == RequiredLineupCount
                ? "已选满 7 个植物（点新植物会自动替换最早选择）"
                : "从已拥有植物中选择；也可直接开始自动补齐";
        }

        if (_lineupDetailText != null)
        {
            PlantKindData detailKind = GetPlantKindById(_lineupPreviewKindId);
            _lineupDetailText.text = detailKind != null
                ? BuildPlantDetailDescription(detailKind)
                : "点击植物按钮可查看属性介绍";
        }

        if (_lineupConfirmButton != null)
        {
            bool exactReady = _lineupPickPlantKindIds.Count == RequiredLineupCount;
            _lineupConfirmButton.interactable = true;
            Image image = _lineupConfirmButton.targetGraphic as Image;
            if (image != null)
            {
                image.color = exactReady
                    ? new Color(0.24f, 0.68f, 0.26f, 1f)
                    : new Color(0.48f, 0.58f, 0.22f, 1f);
            }

            Text label = GetButtonLabel(_lineupConfirmButton);
            if (label != null)
            {
                label.text = exactReady ? "开始战斗" : "开始(自动补齐)";
            }
        }
    }

    private void ToggleLineupPlantSlot(int slotIndex)
    {
        List<int> ownedKindIds = new List<int>();
        CollectOwnedPlantKindIds(ownedKindIds);

        int index = _lineupPage * LineupPlantsPerPage + slotIndex;
        if (index < 0 || index >= ownedKindIds.Count) return;

        int kindId = ownedKindIds[index];
        _lineupPreviewKindId = kindId;
        int existingIndex = _lineupPickPlantKindIds.IndexOf(kindId);
        if (existingIndex >= 0)
        {
            _lineupPickPlantKindIds.RemoveAt(existingIndex);
            SetTip("已移除 " + GetPlantKindName(kindId), 0.7f);
        }
        else
        {
            if (_lineupPickPlantKindIds.Count >= RequiredLineupCount)
            {
                int removedKindId = _lineupPickPlantKindIds[0];
                _lineupPickPlantKindIds.RemoveAt(0);
                _lineupPickPlantKindIds.Add(kindId);
                SetTip("已替换 " + GetPlantKindName(removedKindId) + " -> " + GetPlantKindName(kindId), 1f);
            }
            else
            {
                _lineupPickPlantKindIds.Add(kindId);
                SetTip("已加入 " + GetPlantKindName(kindId), 0.7f);
            }
        }

        RefreshLineupPanel();
    }

    private void PrevLineupPage()
    {
        List<int> ownedKindIds = new List<int>();
        CollectOwnedPlantKindIds(ownedKindIds);
        int totalPages = Mathf.CeilToInt(ownedKindIds.Count / (float)LineupPlantsPerPage);
        if (totalPages <= 1) return;
        _lineupPage = (_lineupPage - 1 + totalPages) % totalPages;
        RefreshLineupPanel();
    }

    private void NextLineupPage()
    {
        List<int> ownedKindIds = new List<int>();
        CollectOwnedPlantKindIds(ownedKindIds);
        int totalPages = Mathf.CeilToInt(ownedKindIds.Count / (float)LineupPlantsPerPage);
        if (totalPages <= 1) return;
        _lineupPage = (_lineupPage + 1) % totalPages;
        RefreshLineupPanel();
    }

    private void NormalizeLineupPicksToRequired()
    {
        EnsurePlantKinds();
        List<int> ownedKindIds = new List<int>();
        CollectOwnedPlantKindIds(ownedKindIds);
        HashSet<int> ownedSet = new HashSet<int>(ownedKindIds);

        // 去重并移除非法 ID。
        HashSet<int> used = new HashSet<int>();
        for (int i = _lineupPickPlantKindIds.Count - 1; i >= 0; i--)
        {
            int kindId = _lineupPickPlantKindIds[i];
            if (!ownedSet.Contains(kindId) || !used.Add(kindId))
            {
                _lineupPickPlantKindIds.RemoveAt(i);
            }
        }

        if (_lineupPickPlantKindIds.Count > RequiredLineupCount)
        {
            _lineupPickPlantKindIds.RemoveRange(RequiredLineupCount, _lineupPickPlantKindIds.Count - RequiredLineupCount);
        }

        // 优先补上当前对局阵容，再补全体植物池。
        for (int i = 0; i < _battlePlantKindIds.Count && _lineupPickPlantKindIds.Count < RequiredLineupCount; i++)
        {
            int kindId = _battlePlantKindIds[i];
            if (ownedSet.Contains(kindId) && used.Add(kindId))
            {
                _lineupPickPlantKindIds.Add(kindId);
            }
        }

        for (int i = 0; i < ownedKindIds.Count && _lineupPickPlantKindIds.Count < RequiredLineupCount; i++)
        {
            int kindId = ownedKindIds[i];
            if (used.Add(kindId))
            {
                _lineupPickPlantKindIds.Add(kindId);
            }
        }
    }

    private void ConfirmLineupSelection()
    {
        int beforeCount = _lineupPickPlantKindIds.Count;
        NormalizeLineupPicksToRequired();
        bool autoFilled = beforeCount != _lineupPickPlantKindIds.Count;

        _battlePlantKindIds.Clear();
        _battlePlantKindIds.AddRange(_lineupPickPlantKindIds);
        _selectedPlantKindId = _battlePlantKindIds[0];
        _plantPage = 0;
        PickStageBossType();
        BuildMatchEnemyKindPool();

        _matchReady = false;
        HidePlantLineupPanel();
        RefreshTowerPlantButtons();
        SelectPlantKindById(_selectedPlantKindId);

        string battleTip = (autoFilled ? "已自动补齐 7 个植物。 " : "对局开始，")
            + "怪物池：" + GetMatchEnemyPoolSummary();
        ShowBattlePopup(
            "开始游戏",
            "阵容已锁定，准备开战！\n" + battleTip,
            "开始",
            () =>
            {
                _matchReady = true;
                _paused = false;
                SetTip(battleTip, 1.4f);
                BeginWave();
            });
    }

    private void ApplyLineupButtonColor(Button button, bool selected, bool preview, TowerType family)
    {
        if (button == null) return;
        Image image = button.targetGraphic as Image;
        if (image == null) return;

        if (selected)
        {
            image.color = GetFamilySelectColor(family);
            return;
        }

        Color baseColor = new Color(0.40f, 0.40f, 0.40f, 1f);
        image.color = preview ? Color.Lerp(baseColor, Color.white, 0.2f) : baseColor;
    }

    private void BuildMatchEnemyKindPool()
    {
        EnsureEnemyKinds();
        _matchEnemyKindPool.Clear();

        int targetCount = Random.Range(3, 5);
        HashSet<int> used = new HashSet<int>();
        int safety = 0;
        while (_matchEnemyKindPool.Count < targetCount && safety < EnemyKindCount * 6)
        {
            int kindId = Random.Range(1, EnemyKindCount + 1);
            if (used.Add(kindId))
            {
                _matchEnemyKindPool.Add(kindId);
            }
            safety++;
        }

        if (_matchEnemyKindPool.Count == 0)
        {
            _matchEnemyKindPool.Add(1);
            _matchEnemyKindPool.Add(2);
            _matchEnemyKindPool.Add(3);
        }
    }

    private int PickMatchEnemyKindId()
    {
        if (_matchEnemyKindPool.Count == 0)
        {
            BuildMatchEnemyKindPool();
        }

        int index = Random.Range(0, _matchEnemyKindPool.Count);
        return _matchEnemyKindPool[index];
    }

    private void PickStageBossType()
    {
        EnsureBossTypes();
        _stageBossTypeId = ((_stage - 1) % _bossTypes.Count) + 1;
    }

    private string GetMatchEnemyPoolSummary()
    {
        if (_matchEnemyKindPool.Count == 0) return "未初始化";

        string text = string.Empty;
        for (int i = 0; i < _matchEnemyKindPool.Count; i++)
        {
            EnemyKindData kind = GetEnemyKindById(_matchEnemyKindPool[i]);
            if (i > 0) text += " / ";
            text += kind != null ? kind.DisplayName : ("怪物-" + _matchEnemyKindPool[i].ToString("00"));
        }

        return text;
    }

    private void TogglePause()
    {
        if (_gameOver || _battlePopupVisible) return;

        _paused = !_paused;
        Time.timeScale = _paused ? 0f : _speed;
        RefreshSpeedButtons();
        SetTip(_paused ? "已暂停" : "继续游戏", 0.6f);
    }

    private void SetSpeed(float speed)
    {
        if (_battlePopupVisible) return;

        _speed = speed;
        _paused = false;
        Time.timeScale = _speed;
        RefreshSpeedButtons();
        SetTip("当前速度: " + _speed.ToString("0") + " 倍", 0.6f);
    }

    private void RefreshSpeedButtons()
    {
        ((Image)_pauseButton.targetGraphic).color = _paused
            ? new Color(0.74f, 0.56f, 0.18f, 1f)
            : new Color(0.42f, 0.42f, 0.42f, 1f);

        ((Image)_x1Button.targetGraphic).color = !_paused && Mathf.Approximately(_speed, 1f)
            ? new Color(0.22f, 0.68f, 0.22f, 1f)
            : new Color(0.42f, 0.42f, 0.42f, 1f);

        ((Image)_x2Button.targetGraphic).color = !_paused && Mathf.Approximately(_speed, 2f)
            ? new Color(0.22f, 0.68f, 0.22f, 1f)
            : new Color(0.42f, 0.42f, 0.42f, 1f);
    }

    private void ShowGameOver()
    {
        RestartStage();
    }

    private void ShowVictory()
    {
        GoToNextStage();
    }

    private void RestartStage()
    {
        // 失败后弹出结算并返回大厅。
        if (_gameOver) return;
        _gameOver = true;
        _paused = false;
        Time.timeScale = 1f;
        RefreshSpeedButtons();
        SetTip("本关失败", 1.2f);
        ShowBattlePopup(
            "游戏结束",
            "防线失守，本局战斗结束。",
            "返回大厅",
            ReturnToLobbyNow);
    }

    private void GoToNextStage()
    {
        // 通关后结算奖励并返回大厅。
        if (_gameOver) return;
        int reward = GameMetaData.GetStageClearReward(_stage);
        GameMetaData.OnStageCleared(_stage);
        _gameOver = true;
        _paused = false;
        Time.timeScale = 1f;
        RefreshSpeedButtons();
        SetTip("第 " + _stage + " 关胜利", 1.4f);
        ShowBattlePopup(
            "游戏结束",
            "第 " + _stage + " 关通关！\n获得 " + reward + " 金币奖励。",
            "返回大厅",
            ReturnToLobbyNow);
    }

    private void ReturnToLobbyNow()
    {
        EnterLobbyOnNextLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ShowBattlePopup(
        string title,
        string content,
        string primaryLabel,
        UnityEngine.Events.UnityAction primaryAction,
        string secondaryLabel = null,
        UnityEngine.Events.UnityAction secondaryAction = null,
        bool pauseTime = true)
    {
        if (_battlePopupPanel == null) return;

        if (pauseTime && !_battlePopupPausedTime)
        {
            _battlePopupPausedTime = true;
            Time.timeScale = 0f;
        }

        _battlePopupVisible = true;
        _battlePopupPrimaryAction = primaryAction;
        _battlePopupSecondaryAction = secondaryAction;

        if (_battlePopupTitleText != null)
        {
            _battlePopupTitleText.text = string.IsNullOrEmpty(title) ? "提示" : title;
        }

        if (_battlePopupContentText != null)
        {
            _battlePopupContentText.text = string.IsNullOrEmpty(content) ? string.Empty : content;
        }

        if (_battlePopupPrimaryButton != null)
        {
            Text primaryText = GetButtonLabel(_battlePopupPrimaryButton);
            if (primaryText != null)
            {
                primaryText.text = string.IsNullOrEmpty(primaryLabel) ? "确定" : primaryLabel;
            }
        }

        bool showSecondary = _battlePopupSecondaryButton != null && !string.IsNullOrEmpty(secondaryLabel);
        if (_battlePopupSecondaryButton != null)
        {
            _battlePopupSecondaryButton.gameObject.SetActive(showSecondary);
            if (showSecondary)
            {
                Text secondaryText = GetButtonLabel(_battlePopupSecondaryButton);
                if (secondaryText != null) secondaryText.text = secondaryLabel;
            }
        }

        _battlePopupPanel.SetAsLastSibling();
        _battlePopupPanel.gameObject.SetActive(true);
    }

    private void HideBattlePopup()
    {
        if (_battlePopupPanel == null) return;

        _battlePopupPanel.gameObject.SetActive(false);
        _battlePopupVisible = false;
        _battlePopupPrimaryAction = null;
        _battlePopupSecondaryAction = null;

        if (_battlePopupPausedTime)
        {
            _battlePopupPausedTime = false;
            Time.timeScale = _paused ? 0f : _speed;
        }
    }

    private void OnBattlePopupPrimaryClicked()
    {
        UnityEngine.Events.UnityAction action = _battlePopupPrimaryAction;
        HideBattlePopup();
        if (action != null) action.Invoke();
    }

    private void OnBattlePopupSecondaryClicked()
    {
        UnityEngine.Events.UnityAction action = _battlePopupSecondaryAction;
        HideBattlePopup();
        if (action != null) action.Invoke();
    }

    private void RefreshHud()
    {
        _goldText.text = _gold.ToString();
        _waveText.text = _wave.ToString();
        _lifeText.text = _life.ToString();
        if (_scoreText != null) _scoreText.text = _score.ToString();
    }

    private void SetTip(string value, float duration)
    {
        _tipText.text = value;
        _tipTimer = duration;
    }

    private void UpdateTip(float dt)
    {
        if (_tipTimer <= 0f) return;
        _tipTimer -= dt;
        if (_tipTimer <= 0f) _tipText.text = string.Empty;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image AddImage(GameObject go, Color color)
    {
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = rect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateRightButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        var rt = CreateRect(name, parent);
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var image = AddImage(rt.gameObject, new Color(0.42f, 0.42f, 0.42f, 1f));
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreateText("Text", rt, label, 22, TextAnchor.MiddleCenter);
        return button;
    }

    private Button CreateCenterButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        var rt = CreateRect(name, parent);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var image = AddImage(rt.gameObject, new Color(0.42f, 0.42f, 0.42f, 1f));
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreateText("Text", rt, label, 24, TextAnchor.MiddleCenter);
        return button;
    }

    private static void StretchTop(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero;
    }
}
