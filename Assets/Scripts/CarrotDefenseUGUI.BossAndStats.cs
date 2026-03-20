using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // Targeting, kills, boss return flow, combat stats, and enemy visuals.
    private EnemyData FindTarget(Vector2 from, float range)
    {
        EnemyData best = null;
        float rangeSqr = range * range;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyData enemy = _enemies[i];
            float dist = (enemy.View.anchoredPosition - from).sqrMagnitude;
            if (dist > rangeSqr || dist >= bestDist) continue;
            best = enemy;
            bestDist = dist;
        }

        return best;
    }

    private void KillEnemy(EnemyData enemy)
    {
        // 统一处理击杀收益；击杀终极首领时回到大厅。
        int index = _enemies.IndexOf(enemy);
        if (index < 0) return;

        bool killedFinalBoss = enemy.IsFinalBoss;
        Destroy(enemy.View.gameObject);
        _enemies.RemoveAt(index);
        _gold += enemy.Reward;
        _score += enemy.IsBoss ? enemy.Reward * 8 : enemy.Reward * 3;
        RefreshHud();

        if (killedFinalBoss)
        {
            ReturnToLobbyAfterFinalBoss();
        }
    }

    private void ReturnToLobbyAfterFinalBoss()
    {
        // 最终 BOSS 击杀后结算奖励，并弹出返回大厅按钮。
        if (_gameOver) return;
        int reward = GameMetaData.GetStageClearReward(_stage);
        GameMetaData.OnStageCleared(_stage);
        _gameOver = true;
        _paused = false;
        Time.timeScale = 1f;
        RefreshSpeedButtons();
        SetTip("终极首领已被击败", 1.3f);
        ShowBattlePopup(
            "游戏结束",
            "终极首领已被击败！\n获得 " + reward + " 金币奖励。",
            "返回大厅",
            ReturnToLobbyNow);
    }

    private void UpdateBossTowerAttack(EnemyData boss, float dt)
    {
        boss.TowerAttackCooldown -= dt;
        if (boss.TowerAttackCooldown > 0f) return;

        TowerData target = FindTowerTarget(boss.View.anchoredPosition, boss.TowerAttackRange);
        if (target == null) return;

        boss.TowerAttackCooldown = boss.TowerAttackInterval;
        target.Hp -= boss.TowerAttackDamage;
        if (target.Hp > 0f) return;

        RemoveTower(target);
        SetTip("你的炮塔被首领摧毁了！", 0.9f);
    }

    private TowerData FindTowerTarget(Vector2 from, float range)
    {
        TowerData best = null;
        float rangeSqr = range * range;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _towers.Count; i++)
        {
            TowerData tower = _towers[i];
            float dist = (tower.View.anchoredPosition - from).sqrMagnitude;
            if (dist > rangeSqr || dist >= bestDist) continue;
            best = tower;
            bestDist = dist;
        }

        return best;
    }

    private void RemoveTower(TowerData tower)
    {
        _towers.Remove(tower);
        Destroy(tower.View.gameObject);

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                CellData cell = _cells[x, y];
                if (cell.Tower != tower) continue;
                cell.Tower = null;
                cell.View.color = _grassColor;
                return;
            }
        }
    }

    private TowerProfile GetTowerProfile(TowerType type)
    {
        switch (type)
        {
            case TowerType.Pea:
                return new TowerProfile(buildCost: 50, upgradeBaseCost: 38, damage: 22f, range: 150f, fireInterval: 0.58f, slowTime: 0f);
            case TowerType.Ice:
                return new TowerProfile(buildCost: 70, upgradeBaseCost: 45, damage: 12f, range: 138f, fireInterval: 0.86f, slowTime: 1.15f);
            case TowerType.Fire:
                return new TowerProfile(buildCost: 95, upgradeBaseCost: 58, damage: 18f, range: 146f, fireInterval: 0.43f, slowTime: 0f);
            case TowerType.Cannon:
                return new TowerProfile(buildCost: 125, upgradeBaseCost: 72, damage: 46f, range: 168f, fireInterval: 1.02f, slowTime: 0f);
            case TowerType.Poison:
                return new TowerProfile(buildCost: 105, upgradeBaseCost: 64, damage: 16f, range: 156f, fireInterval: 0.52f, slowTime: 0.45f);
            default:
                return new TowerProfile(buildCost: 50, upgradeBaseCost: 38, damage: 22f, range: 150f, fireInterval: 0.58f, slowTime: 0f);
        }
    }

    private TowerRuntime GetTowerRuntime(TowerData tower)
    {
        TowerProfile profile = GetTowerProfile(tower.Type);
        PlantKindData kind = GetPlantKindById(tower.KindId);
        int lvOffset = tower.Level - 1;

        float kindDamageMul = kind != null ? kind.DamageMul : 1f;
        float kindRangeBonus = kind != null ? kind.RangeBonus : 0f;
        float kindFireMul = kind != null ? kind.FireIntervalMul : 1f;
        float kindSlowBonus = kind != null ? kind.SlowTimeBonus : 0f;

        float damage = profile.Damage * kindDamageMul * (1f + lvOffset * 0.55f);
        float range = profile.Range + kindRangeBonus + lvOffset * 16f;
        float fireInterval = Mathf.Max(0.2f, profile.FireInterval * kindFireMul * (1f - lvOffset * 0.12f));
        float slowTime = profile.SlowTime + kindSlowBonus + lvOffset * 0.25f;

        return new TowerRuntime(damage, range, fireInterval, slowTime);
    }

    private int GetUpgradeCost(TowerData tower)
    {
        PlantKindData kind = GetPlantKindById(tower.KindId);
        int baseCost = kind != null ? kind.UpgradeBaseCost : GetTowerProfile(tower.Type).UpgradeBaseCost;
        return baseCost + (tower.Level - 1) * 26;
    }

    private static float GetTowerMaxHp(int level)
    {
        return 120f + (level - 1) * 60f;
    }

    private static bool IsBossWave(int wave)
    {
        return wave == BossWave1 || wave == BossWave2;
    }

    private static float GetBossHp(int wave)
    {
        if (wave >= BossWave2) return 5800f;
        if (wave >= BossWave1) return 3200f;
        return 0f;
    }

    private static float GetBossSpeed(int wave)
    {
        return wave >= BossWave2 ? 74f : 68f;
    }

    private static int GetBossReward(int wave)
    {
        return wave >= BossWave2 ? 280 : 160;
    }

    private EnemyModelData BuildEnemyModel(RectTransform parent, bool isBoss, bool isFinalBoss)
    {
        // 用 UGUI 组合敌人模型，并在顶部挂载血条。
        var modelRoot = CreateRect("EnemyModel", parent);
        modelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        modelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        modelRoot.pivot = new Vector2(0.5f, 0.5f);
        modelRoot.sizeDelta = parent.sizeDelta;

        float bodySize = Mathf.Min(parent.sizeDelta.x, parent.sizeDelta.y) * (isBoss ? 0.86f : 0.74f);
        Color bodyColor = isBoss ? (isFinalBoss ? _bossFinalColor : _bossColor) : _enemyColor;
        Color coreColor = Color.Lerp(bodyColor, Color.white, isBoss ? 0.24f : 0.18f);

        var shadowRt = CreateRect("Shadow", modelRoot);
        shadowRt.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRt.pivot = new Vector2(0.5f, 0.5f);
        shadowRt.anchoredPosition = new Vector2(0f, -bodySize * 0.34f);
        shadowRt.sizeDelta = new Vector2(bodySize * 0.78f, bodySize * 0.24f);
        Image shadowImage = AddImage(shadowRt.gameObject, new Color(0f, 0f, 0f, 0.34f));
        shadowImage.raycastTarget = false;

        var bodyRt = CreateRect("Body", modelRoot);
        bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRt.pivot = new Vector2(0.5f, 0.5f);
        bodyRt.anchoredPosition = Vector2.zero;
        bodyRt.sizeDelta = Vector2.one * bodySize;
        Image bodyImage = AddImage(bodyRt.gameObject, bodyColor);
        bodyImage.raycastTarget = false;

        var coreRt = CreateRect("Core", bodyRt);
        coreRt.anchorMin = new Vector2(0.5f, 0.5f);
        coreRt.anchorMax = new Vector2(0.5f, 0.5f);
        coreRt.pivot = new Vector2(0.5f, 0.5f);
        coreRt.anchoredPosition = new Vector2(0f, bodySize * 0.03f);
        coreRt.sizeDelta = Vector2.one * (bodySize * (isBoss ? 0.58f : 0.52f));
        Image coreImage = AddImage(coreRt.gameObject, coreColor);
        coreImage.raycastTarget = false;

        RectTransform crownRt = null;
        Image crownImage = null;
        Color crownColor = new Color(1f, 0.94f, 0.55f, 1f);
        if (isBoss)
        {
            crownRt = CreateRect("Crown", modelRoot);
            crownRt.anchorMin = new Vector2(0.5f, 0.5f);
            crownRt.anchorMax = new Vector2(0.5f, 0.5f);
            crownRt.pivot = new Vector2(0.5f, 0.5f);
            crownRt.anchoredPosition = new Vector2(0f, bodySize * 0.45f);
            crownRt.sizeDelta = new Vector2(bodySize * 0.64f, bodySize * 0.17f);
            crownImage = AddImage(crownRt.gameObject, crownColor);
            crownImage.raycastTarget = false;
        }

        var hpBarRt = CreateRect("HpBar", modelRoot);
        hpBarRt.anchorMin = new Vector2(0.5f, 0.5f);
        hpBarRt.anchorMax = new Vector2(0.5f, 0.5f);
        hpBarRt.pivot = new Vector2(0.5f, 0.5f);
        hpBarRt.anchoredPosition = new Vector2(0f, bodySize * 0.70f);

        float hpBarWidth = isBoss ? bodySize * 0.98f : bodySize * 0.9f;
        float hpBarHeight = isBoss ? 8f : 7f;
        var hpBackRt = CreateRect("Back", hpBarRt);
        hpBackRt.anchorMin = new Vector2(0.5f, 0.5f);
        hpBackRt.anchorMax = new Vector2(0.5f, 0.5f);
        hpBackRt.pivot = new Vector2(0.5f, 0.5f);
        hpBackRt.sizeDelta = new Vector2(hpBarWidth, hpBarHeight);
        Image hpBackImage = AddImage(hpBackRt.gameObject, new Color(0f, 0f, 0f, 0.5f));
        hpBackImage.raycastTarget = false;

        var hpFillRt = CreateRect("Fill", hpBackRt);
        hpFillRt.anchorMin = new Vector2(0f, 0.5f);
        hpFillRt.anchorMax = new Vector2(0f, 0.5f);
        hpFillRt.pivot = new Vector2(0f, 0.5f);
        hpFillRt.anchoredPosition = new Vector2(1f, 0f);
        float hpFillMaxWidth = hpBarWidth - 2f;
        hpFillRt.sizeDelta = new Vector2(hpFillMaxWidth, hpBarHeight - 2f);
        Image hpFillImage = AddImage(hpFillRt.gameObject, new Color(0.44f, 0.96f, 0.38f, 0.98f));
        hpFillImage.raycastTarget = false;

        return new EnemyModelData
        {
            Root = modelRoot,
            Shadow = shadowRt,
            Body = bodyRt,
            Core = coreRt,
            Crown = crownRt,
            HealthBar = hpBarRt,
            ShadowImage = shadowImage,
            BodyImage = bodyImage,
            CoreImage = coreImage,
            CrownImage = crownImage,
            HpBackImage = hpBackImage,
            HpFillImage = hpFillImage,
            BodyBaseColor = bodyColor,
            CoreBaseColor = coreColor,
            CrownBaseColor = crownColor,
            HealthFillMaxWidth = hpFillMaxWidth
        };
    }

    private static void RefreshEnemyHealthBar(EnemyData enemy)
    {
        // 按当前血量百分比更新血条宽度和颜色。
        if (enemy == null || enemy.Model == null || enemy.Model.HpFillImage == null) return;

        float ratio = enemy.MaxHp > 0f ? Mathf.Clamp01(enemy.Hp / enemy.MaxHp) : 0f;
        RectTransform fillRt = enemy.Model.HpFillImage.rectTransform;
        fillRt.sizeDelta = new Vector2(enemy.Model.HealthFillMaxWidth * ratio, fillRt.sizeDelta.y);

        Color low = new Color(0.95f, 0.2f, 0.2f, 0.98f);
        Color high = enemy.IsBoss
            ? new Color(1f, 0.86f, 0.28f, 0.98f)
            : new Color(0.44f, 0.96f, 0.38f, 0.98f);
        enemy.Model.HpFillImage.color = Color.Lerp(low, high, ratio);
    }

}
