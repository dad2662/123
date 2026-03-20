using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // Wave flow, enemy/tower update loop, projectiles, and hit effects.
    private void UpdateWave(float dt)
    {
        // 波次推进：处理刷怪队列，敌人清空后开启下一波。
        if (_pendingSpawn > 0)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                if (_bossSpawnPending)
                {
                    if (!SpawnEnemy(true)) return;
                    _bossSpawnPending = false;
                    _spawnTimer = Mathf.Max(0.45f, _activeWave.SpawnGap);
                }
                else
                {
                    if (!SpawnEnemy(false)) return;
                    _pendingSpawn--;
                    _spawnTimer = _activeWave.SpawnGap;
                }
            }
            return;
        }

        if (_enemies.Count > 0) return;
        if (_wave >= VictoryWave)
        {
            GoToNextStage();
            return;
        }

        _nextWaveTimer -= dt;
        if (_nextWaveTimer <= 0f) BeginWave();
    }

    private void BeginWave()
    {
        _wave++;
        _activeWave = GetWaveProfile(_wave);
        _pendingSpawn = _activeWave.Count;
        _bossSpawnPending = IsBossWave(_wave);
        _spawnTimer = 0.15f;
        _nextWaveTimer = 2.2f;
        if (_bossSpawnPending)
        {
            PickStageBossType();
            BossTypeData bossType = GetBossTypeById(_stageBossTypeId);
            string bossName = bossType != null ? bossType.Name : "首领";
            SetTip("第 " + _stage + " 关 · 第 " + _wave + " 波 [" + bossName + "] 来袭", 1.2f);
        }
        else
        {
            SetTip("第 " + _stage + " 关 · 第 " + _wave + " 波来袭", 1.2f);
        }
        RefreshHud();
    }

    private WaveProfile GetWaveProfile(int wave)
    {
        WaveProfile baseProfile;
        int idx = wave - 1;
        if (idx >= 0 && idx < _levelConfig.Count)
        {
            baseProfile = _levelConfig[idx];
        }
        else
        {
            WaveProfile last = _levelConfig[_levelConfig.Count - 1];
            int extra = wave - _levelConfig.Count;
            baseProfile = new WaveProfile(
                count: last.Count + extra * 2,
                hp: last.Hp + extra * 18f,
                speed: last.Speed + extra * 4f,
                spawnGap: Mathf.Max(0.28f, last.SpawnGap - extra * 0.02f),
                reward: last.Reward + extra);
        }

        float stageScale = GameMetaData.GetHpScaleForStage(_stage);
        return new WaveProfile(
            count: baseProfile.Count + (_stage - 1),
            hp: baseProfile.Hp * stageScale,
            speed: baseProfile.Speed + (_stage - 1) * 2.5f,
            spawnGap: Mathf.Max(0.22f, baseProfile.SpawnGap - (_stage - 1) * 0.01f),
            reward: baseProfile.Reward + (_stage - 1) * 2);
    }

    private bool SpawnEnemy(bool spawnBoss)
    {
        if (!EnsurePathPoints()) return false;

        bool finalBoss = spawnBoss && _wave >= BossWave2;
        int enemyKindId = 0;
        EnemyKindData enemyKind = null;
        if (!spawnBoss)
        {
            EnsureEnemyKinds();
            enemyKindId = PickMatchEnemyKindId();
            enemyKind = GetEnemyKindById(enemyKindId);
        }
        else
        {
            PickStageBossType();
        }

        float enemyHp = spawnBoss ? GetBossHp(_wave) * (1f + (_stage - 1) * 0.28f) : _activeWave.Hp;
        float enemySpeed = spawnBoss ? GetBossSpeed(_wave) + (_stage - 1) * 2.6f : _activeWave.Speed;
        int enemyReward = spawnBoss ? GetBossReward(_wave) + (_stage - 1) * 30 : _activeWave.Reward;
        if (!spawnBoss && enemyKind != null)
        {
            enemyHp *= enemyKind.HpMul;
            enemySpeed *= enemyKind.SpeedMul;
            enemyReward = Mathf.Max(1, Mathf.RoundToInt(enemyReward * enemyKind.RewardMul));
        }

        CreateEnemyEntity(
            spawnBoss,
            finalBoss,
            enemyKindId,
            spawnBoss ? _stageBossTypeId : 0,
            enemyHp,
            enemySpeed,
            enemyReward,
            _pathPoints[0],
            1);

        if (spawnBoss)
        {
            BossTypeData bossType = GetBossTypeById(_stageBossTypeId);
            string bossName = bossType != null ? bossType.Name : "首领";
            SetTip(_wave >= BossWave2
                ? "第 " + _stage + " 关终章首领 [" + bossName + "] 现身！"
                : "第 " + _stage + " 关首领 [" + bossName + "] 来袭！", 1.4f);
        }
        return true;
    }

    private EnemyData CreateEnemyEntity(bool isBoss, bool isFinalBoss, int kindId, int bossTypeId, float hp, float speed, int reward, Vector2 spawnPosition, int nextNode)
    {
        EnemyKindData kind = !isBoss ? GetEnemyKindById(kindId) : null;
        BossTypeData bossType = isBoss ? GetBossTypeById(bossTypeId) : null;
        float sizeMul = kind != null ? kind.SizeMul : 1f;
        var enemyRt = CreateRect("Enemy", _board);
        enemyRt.anchorMin = new Vector2(0.5f, 0.5f);
        enemyRt.anchorMax = new Vector2(0.5f, 0.5f);
        enemyRt.pivot = new Vector2(0.5f, 0.5f);
        enemyRt.sizeDelta = Vector2.one * (isBoss ? CellSize - 4f : (CellSize - 22f) * sizeMul);
        enemyRt.anchoredPosition = spawnPosition;
        AddImage(enemyRt.gameObject, new Color(0f, 0f, 0f, 0f)).raycastTarget = false;

        EnemyModelData model = BuildEnemyModel(enemyRt, isBoss, isFinalBoss);
        if (!isBoss && kind != null)
        {
            Color core = Color.Lerp(kind.BodyColor, Color.white, 0.18f);
            model.BodyBaseColor = kind.BodyColor;
            model.CoreBaseColor = core;
            if (model.BodyImage != null) model.BodyImage.color = kind.BodyColor;
            if (model.CoreImage != null) model.CoreImage.color = core;
        }
        else if (isBoss && bossType != null)
        {
            Color body = isFinalBoss
                ? Color.Lerp(bossType.BodyColor, _bossFinalColor, 0.35f)
                : bossType.BodyColor;
            Color core = Color.Lerp(body, Color.white, 0.25f);
            Color crown = Color.Lerp(body, new Color(1f, 0.95f, 0.62f, 1f), 0.5f);

            model.BodyBaseColor = body;
            model.CoreBaseColor = core;
            model.CrownBaseColor = crown;

            if (model.BodyImage != null) model.BodyImage.color = body;
            if (model.CoreImage != null) model.CoreImage.color = core;
            if (model.CrownImage != null) model.CrownImage.color = crown;
        }

        if (isBoss)
        {
            string bossName = bossType != null ? bossType.Name : "首领";
            string label = isFinalBoss ? bossName + "·终章" : bossName;
            var mark = CreateText("BossMark", enemyRt, label, 18, TextAnchor.MiddleCenter);
            mark.color = new Color(1f, 0.96f, 0.7f, 0.96f);
            mark.rectTransform.anchoredPosition = new Vector2(0f, -2f);
        }
        else if (kind != null)
        {
            var mark = CreateText("EnemyKind", enemyRt, kind.DisplayName, 12, TextAnchor.MiddleCenter);
            mark.color = new Color(1f, 1f, 1f, 0.78f);
            mark.rectTransform.anchoredPosition = new Vector2(0f, -2f);
        }

        var enemyData = new EnemyData
        {
            View = enemyRt,
            Model = model,
            KindId = kindId,
            BossTypeId = bossTypeId,
            Hp = hp,
            MaxHp = hp,
            BaseSpeed = speed,
            Speed = speed,
            Reward = reward,
            NextNode = Mathf.Clamp(nextNode, 1, _pathPoints.Count - 1),
            SlowTimer = 0f,
            HitFlashTimer = 0f,
            IsBoss = isBoss,
            IsFinalBoss = isFinalBoss,
            EnrageTriggered = false,
            HealTriggered = false,
            SummonTriggered = false,
            CanAttackTower = isFinalBoss || bossTypeId == 5,
            TowerAttackDamage = isFinalBoss ? 85f : (bossTypeId == 5 ? 45f : 0f),
            TowerAttackRange = isFinalBoss ? 172f : (bossTypeId == 5 ? 160f : 0f),
            TowerAttackInterval = isFinalBoss ? 1.3f : (bossTypeId == 5 ? 1.65f : 0f),
            TowerAttackCooldown = 0.6f
        };
        _enemies.Add(enemyData);
        RefreshEnemyHealthBar(enemyData);
        return enemyData;
    }

    private void UpdateBossSkills(EnemyData boss)
    {
        // 5 类首领技能：每关固定一种首领，不同类型触发不同机制。
        if (boss == null || !boss.IsBoss) return;
        if (boss.MaxHp <= 0f) return;

        float hpRatio = boss.Hp / boss.MaxHp;
        switch (Mathf.Clamp(boss.BossTypeId, 1, 5))
        {
            case 1:
                if (!boss.EnrageTriggered && hpRatio <= 0.70f)
                {
                    boss.EnrageTriggered = true;
                    boss.Speed = boss.BaseSpeed * 1.42f;
                    SetTip("裂蹄战王狂暴，移动速度提升！", 1.05f);
                }

                if (!boss.SummonTriggered && hpRatio <= 0.38f)
                {
                    boss.SummonTriggered = true;
                    SummonBossMinions(boss, 4, 0.62f, 1.08f);
                    SetTip("裂蹄战王召唤了冲锋随从！", 1.05f);
                }
                break;

            case 2:
                if (!boss.EnrageTriggered && hpRatio <= 0.74f)
                {
                    boss.EnrageTriggered = true;
                    for (int i = 0; i < _towers.Count; i++)
                    {
                        _towers[i].Cooldown = Mathf.Max(_towers[i].Cooldown, 0.65f);
                    }
                    SetTip("腐沼母后释放腐蚀瘴气，植物攻速被压制！", 1.1f);
                }

                if (!boss.HealTriggered && hpRatio <= 0.50f)
                {
                    boss.HealTriggered = true;
                    boss.Hp = Mathf.Min(boss.MaxHp, boss.Hp + boss.MaxHp * 0.36f);
                    RefreshEnemyHealthBar(boss);
                    SetTip("腐沼母后吞噬腐液，恢复了大量生命！", 1.05f);
                }

                if (!boss.SummonTriggered && hpRatio <= 0.28f)
                {
                    boss.SummonTriggered = true;
                    SummonBossMinions(boss, 3, 0.70f, 0.98f);
                    SetTip("腐沼母后召唤了沼泽护卫！", 1.05f);
                }
                break;

            case 3:
                if (!boss.EnrageTriggered && hpRatio <= 0.78f)
                {
                    boss.EnrageTriggered = true;
                    float shield = boss.MaxHp * 0.24f;
                    boss.MaxHp += shield;
                    boss.Hp += shield;
                    RefreshEnemyHealthBar(boss);
                    SetTip("钢甲暴君展开重甲护盾，耐久大幅提升！", 1.1f);
                }

                if (!boss.HealTriggered && hpRatio <= 0.46f)
                {
                    boss.HealTriggered = true;
                    TowerData tower = FindTowerTarget(boss.View.anchoredPosition, 176f);
                    if (tower != null)
                    {
                        RemoveTower(tower);
                        SetTip("钢甲暴君震碎了一座植物！", 1f);
                    }
                }

                if (!boss.SummonTriggered && hpRatio <= 0.24f)
                {
                    boss.SummonTriggered = true;
                    boss.Speed = boss.BaseSpeed * 1.3f;
                    SetTip("钢甲暴君冲锋突进！", 1f);
                }
                break;

            case 4:
                if (!boss.EnrageTriggered && hpRatio <= 0.72f)
                {
                    boss.EnrageTriggered = true;
                    SummonBossMinions(boss, 4, 0.56f, 1.34f);
                    SetTip("雷鸣先驱放出高速雷兽！", 1.08f);
                }

                if (!boss.HealTriggered && hpRatio <= 0.44f)
                {
                    boss.HealTriggered = true;
                    boss.Speed = boss.BaseSpeed * 1.58f;
                    SetTip("雷鸣先驱进入闪电形态！", 1.08f);
                }

                if (!boss.SummonTriggered && hpRatio <= 0.22f)
                {
                    boss.SummonTriggered = true;
                    SummonBossMinions(boss, 5, 0.62f, 1.20f);
                    SetTip("雷鸣先驱再度召唤了雷兽群！", 1.08f);
                }
                break;

            case 5:
                if (!boss.EnrageTriggered && hpRatio <= 0.80f)
                {
                    boss.EnrageTriggered = true;
                    boss.CanAttackTower = true;
                    boss.TowerAttackDamage = Mathf.Max(boss.TowerAttackDamage, boss.IsFinalBoss ? 95f : 52f);
                    boss.TowerAttackRange = Mathf.Max(boss.TowerAttackRange, 180f);
                    boss.TowerAttackInterval = Mathf.Min(boss.TowerAttackInterval > 0f ? boss.TowerAttackInterval : 1.7f, 1.45f);
                    SetTip("深渊主宰觉醒，开始主动摧毁植物！", 1.1f);
                }

                if (!boss.HealTriggered && hpRatio <= 0.55f)
                {
                    boss.HealTriggered = true;
                    boss.Hp = Mathf.Min(boss.MaxHp, boss.Hp + boss.MaxHp * 0.40f);
                    RefreshEnemyHealthBar(boss);
                    SetTip("深渊主宰吞噬阴影，回复生命！", 1.1f);
                }

                if (!boss.SummonTriggered && hpRatio <= 0.30f)
                {
                    boss.SummonTriggered = true;
                    SummonBossMinions(boss, 6, 0.68f, 1.16f);
                    SetTip("深渊主宰打开裂隙，召唤了大量怪物！", 1.1f);
                }
                break;
        }
    }

    private void SummonBossMinions(EnemyData boss, int count, float hpMul, float speedMul)
    {
        if (boss == null || boss.View == null) return;
        if (!EnsurePathPoints()) return;

        int closestNode = GetClosestPathNodeIndex(boss.View.anchoredPosition);
        int nextNode = Mathf.Clamp(closestNode + 1, 1, _pathPoints.Count - 1);

        float minionHp = Mathf.Max(60f, _activeWave.Hp * hpMul);
        float minionSpeed = _activeWave.Speed * speedMul + (_stage - 1) * 1.8f;
        int minionReward = Mathf.Max(6, _activeWave.Reward / 2);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = new Vector2(Random.Range(-18f, 18f), Random.Range(-12f, 12f));
            Vector2 spawnPos = boss.View.anchoredPosition + offset;
            int kindId = PickMatchEnemyKindId();
            CreateEnemyEntity(false, false, kindId, 0, minionHp, minionSpeed, minionReward, spawnPos, nextNode);
        }
    }

    private int GetClosestPathNodeIndex(Vector2 position)
    {
        if (!EnsurePathPoints() || _pathPoints.Count == 0) return 0;

        int bestIndex = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _pathPoints.Count; i++)
        {
            float dist = (_pathPoints[i] - position).sqrMagnitude;
            if (dist >= bestDist) continue;
            bestDist = dist;
            bestIndex = i;
        }

        return bestIndex;
    }

    private void UpdateEnemies(float dt)
    {
        // 敌人沿路径移动；首领按类型触发技能并可执行拆塔行为。
        if (!EnsurePathPoints()) return;

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            EnemyData enemy = _enemies[i];
            enemy.SlowTimer = Mathf.Max(0f, enemy.SlowTimer - dt);
            UpdateEnemyHitFlash(enemy, dt);
            if (enemy.IsBoss) UpdateBossSkills(enemy);
            if (enemy.CanAttackTower) UpdateBossTowerAttack(enemy, dt);

            float speedScale = enemy.SlowTimer > 0f ? 0.55f : 1f;
            float moveRemain = enemy.Speed * speedScale * dt;
            Vector2 position = enemy.View.anchoredPosition;
            bool leaked = false;

            while (moveRemain > 0f && enemy.NextNode < _pathPoints.Count)
            {
                Vector2 target = _pathPoints[enemy.NextNode];
                Vector2 delta = target - position;
                float dist = delta.magnitude;

                if (dist < 0.001f)
                {
                    enemy.NextNode++;
                    continue;
                }

                if (moveRemain >= dist)
                {
                    position = target;
                    moveRemain -= dist;
                    enemy.NextNode++;
                    if (enemy.NextNode >= _pathPoints.Count)
                    {
                        leaked = true;
                        break;
                    }
                }
                else
                {
                    position += delta / dist * moveRemain;
                    moveRemain = 0f;
                }
            }

            if (leaked)
            {
                Destroy(enemy.View.gameObject);
                _enemies.RemoveAt(i);
                _life--;
                _score = Mathf.Max(0, _score - 30);
                RefreshHud();
                if (_life <= 0) RestartStage();
                continue;
            }

            enemy.View.anchoredPosition = position;
        }
    }

    private void UpdateTowers(float dt)
    {
        // 炮塔瞄准并开火，同时更新待机/攻击动画。
        for (int i = 0; i < _towers.Count; i++)
        {
            TowerData tower = _towers[i];
            tower.Cooldown -= dt;

            TowerRuntime runtime = GetTowerRuntime(tower);
            EnemyData target = FindTarget(tower.View.anchoredPosition, runtime.Range);
            bool fired = false;
            if (target != null && tower.Cooldown <= 0f)
            {
                tower.Cooldown = runtime.FireInterval;
                tower.AttackAnimTimer = TowerAttackAnimDuration;
                UpdateTowerModelAnimation(tower, target);
                FireProjectile(tower, target, runtime);
                fired = true;
            }

            if (tower.AttackAnimTimer > 0f)
            {
                tower.AttackAnimTimer = Mathf.Max(0f, tower.AttackAnimTimer - dt);
            }

            if (!fired) UpdateTowerModelAnimation(tower, target);
        }
    }
}
