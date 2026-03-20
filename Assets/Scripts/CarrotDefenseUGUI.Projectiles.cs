using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // Projectile creation, hit resolution, and transient impact effects.
    private void FireProjectile(TowerData tower, EnemyData target, TowerRuntime runtime)
    {
        var projectileRt = CreateRect("Projectile", _board);
        projectileRt.anchorMin = new Vector2(0.5f, 0.5f);
        projectileRt.anchorMax = new Vector2(0.5f, 0.5f);
        projectileRt.pivot = new Vector2(0.5f, 0.5f);
        projectileRt.anchoredPosition = GetTowerMuzzlePosition(tower);
        projectileRt.sizeDelta = Vector2.one * GetProjectileSize(tower.Type, tower.Level, tower.KindId);

        Color projectileColor = GetProjectileColor(tower.Type, tower.KindId);
        Image projectileImage = AddImage(projectileRt.gameObject, projectileColor);
        projectileImage.raycastTarget = false;

        _projectiles.Add(new ProjectileData
        {
            View = projectileRt,
            Type = tower.Type,
            KindId = tower.KindId,
            Target = target,
            Damage = runtime.Damage,
            SlowTime = runtime.SlowTime,
            Speed = GetProjectileSpeed(tower.Type, tower.Level, tower.KindId),
            Life = ProjectileMaxLife
        });
    }

    private static float GetKindFactor(int kindId, int salt, float min, float max)
    {
        return Mathf.Lerp(min, max, Hash01(kindId, salt, 541));
    }

    private static float GetProjectileSize(TowerType type, int level, int kindId)
    {
        int lv = Mathf.Max(1, level);
        float kindScale = GetKindFactor(kindId, 17, 0.88f, 1.16f);
        switch (type)
        {
            case TowerType.Pea: return (13f + lv * 1.6f) * kindScale;
            case TowerType.Ice: return (15f + lv * 1.7f) * kindScale;
            case TowerType.Fire: return (14f + lv * 1.9f) * kindScale;
            case TowerType.Cannon: return (18f + lv * 2.1f) * kindScale;
            case TowerType.Poison: return (13f + lv * 1.8f) * kindScale;
            default: return 14f * kindScale;
        }
    }

    private static float GetProjectileSpeed(TowerType type, int level, int kindId)
    {
        int lv = Mathf.Max(1, level) - 1;
        float kindScale = GetKindFactor(kindId, 29, 0.86f, 1.18f);
        switch (type)
        {
            case TowerType.Pea: return (560f + lv * 70f) * kindScale;
            case TowerType.Ice: return (500f + lv * 58f) * kindScale;
            case TowerType.Fire: return (610f + lv * 75f) * kindScale;
            case TowerType.Cannon: return (470f + lv * 45f) * kindScale;
            case TowerType.Poison: return (545f + lv * 62f) * kindScale;
            default: return 520f * kindScale;
        }
    }

    private static Color GetProjectileColor(TowerType type, int kindId)
    {
        Color baseColor;
        switch (type)
        {
            case TowerType.Pea: baseColor = new Color(0.34f, 0.92f, 0.26f, 1f); break;
            case TowerType.Ice: baseColor = new Color(0.38f, 0.86f, 1f, 0.96f); break;
            case TowerType.Fire: baseColor = new Color(1f, 0.54f, 0.22f, 0.97f); break;
            case TowerType.Cannon: baseColor = new Color(0.92f, 0.76f, 0.35f, 1f); break;
            case TowerType.Poison: baseColor = new Color(0.58f, 0.92f, 0.27f, 0.96f); break;
            default: baseColor = Color.white; break;
        }

        float lightMul = GetKindFactor(kindId, 43, 0.82f, 1.24f);
        float whiteMix = GetKindFactor(kindId, 51, 0f, 0.22f);
        Color color = Color.Lerp(baseColor, Color.white, whiteMix);
        color = Tint(color, lightMul);
        color.a = baseColor.a;
        return color;
    }

    private static float GetProjectileHitDistance(TowerType type, int kindId)
    {
        float kindOffset = GetKindFactor(kindId, 63, -1.6f, 2.1f);
        switch (type)
        {
            case TowerType.Cannon: return 14f + kindOffset;
            case TowerType.Ice: return 12f + kindOffset;
            default: return 10f + kindOffset;
        }
    }

    private void UpdateProjectiles(float dt)
    {
        // 子弹飞行到目标后才结算伤害，保证命中反馈更直观。
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileData projectile = _projectiles[i];
            if (projectile.View == null)
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            projectile.Life -= dt;
            if (projectile.Life <= 0f || projectile.Target == null || projectile.Target.View == null || !_enemies.Contains(projectile.Target))
            {
                Destroy(projectile.View.gameObject);
                _projectiles.RemoveAt(i);
                continue;
            }

            Vector2 currentPos = projectile.View.anchoredPosition;
            Vector2 targetPos = projectile.Target.View.anchoredPosition;
            Vector2 delta = targetPos - currentPos;
            float dist = delta.magnitude;
            float hitDist = GetProjectileHitDistance(projectile.Type, projectile.KindId);
            float step = projectile.Speed * dt;

            if (dist <= hitDist || dist <= step)
            {
                ApplyProjectileHit(projectile, projectile.Target, targetPos);
                Destroy(projectile.View.gameObject);
                _projectiles.RemoveAt(i);
                continue;
            }

            Vector2 dir = delta / dist;
            projectile.View.anchoredPosition = currentPos + dir * step;
            projectile.View.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
    }

    private void ApplyProjectileHit(ProjectileData projectile, EnemyData enemy, Vector2 hitPosition)
    {
        if (enemy == null || enemy.View == null) return;
        if (!_enemies.Contains(enemy)) return;

        PlantKindData kind = GetPlantKindById(projectile.KindId);
        float damage = projectile.Damage;
        if (kind != null)
        {
            if (enemy.IsBoss) damage *= kind.BossDamageMul;
            if (Random.value < kind.CritChance)
            {
                damage *= kind.CritDamageMul;
            }
        }

        enemy.Hp -= damage;
        if (kind != null && kind.ArmorBreakRatio > 0f)
        {
            enemy.Hp -= enemy.MaxHp * kind.ArmorBreakRatio;
        }

        if (projectile.SlowTime > 0f) enemy.SlowTimer = Mathf.Max(enemy.SlowTimer, projectile.SlowTime);
        enemy.HitFlashTimer = EnemyHitFlashDuration;
        RefreshEnemyHealthBar(enemy);

        if (kind != null && kind.SplashRadius > 0f && kind.SplashDamageMul > 0f)
        {
            ApplySplashDamage(enemy, damage * kind.SplashDamageMul, kind.SplashRadius);
        }

        if (kind != null && kind.ExecuteHpRatio > 0f && enemy.Hp > 0f && enemy.MaxHp > 0f)
        {
            if (enemy.Hp / enemy.MaxHp <= kind.ExecuteHpRatio)
            {
                enemy.Hp = 0f;
            }
        }

        SpawnHitEffect(hitPosition, projectile.Type, projectile.KindId);
        if (enemy.Hp <= 0f) KillEnemy(enemy);
    }

    private void ApplySplashDamage(EnemyData center, float damage, float radius)
    {
        if (center == null || center.View == null || damage <= 0f || radius <= 0f) return;

        float radiusSqr = radius * radius;
        List<EnemyData> targets = new List<EnemyData>();
        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyData enemy = _enemies[i];
            if (enemy == null || enemy.View == null || enemy == center) continue;
            if ((enemy.View.anchoredPosition - center.View.anchoredPosition).sqrMagnitude > radiusSqr) continue;
            targets.Add(enemy);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            EnemyData enemy = targets[i];
            if (!_enemies.Contains(enemy)) continue;

            enemy.Hp -= damage;
            enemy.HitFlashTimer = EnemyHitFlashDuration;
            RefreshEnemyHealthBar(enemy);
            if (enemy.Hp <= 0f) KillEnemy(enemy);
        }
    }

    private void SpawnHitEffect(Vector2 hitPosition, TowerType type, int kindId)
    {
        var fxRt = CreateRect("HitFx", _board);
        fxRt.anchorMin = new Vector2(0.5f, 0.5f);
        fxRt.anchorMax = new Vector2(0.5f, 0.5f);
        fxRt.pivot = new Vector2(0.5f, 0.5f);
        fxRt.anchoredPosition = hitPosition;

        float startSize;
        float endSize;
        Color fxColor;
        switch (type)
        {
            case TowerType.Pea:
                startSize = 12f; endSize = 44f; fxColor = new Color(0.62f, 1f, 0.46f, 0.95f); break;
            case TowerType.Ice:
                startSize = 15f; endSize = 52f; fxColor = new Color(0.62f, 0.92f, 1f, 0.98f); break;
            case TowerType.Fire:
                startSize = 14f; endSize = 58f; fxColor = new Color(1f, 0.72f, 0.36f, 0.98f); break;
            case TowerType.Cannon:
                startSize = 16f; endSize = 64f; fxColor = new Color(1f, 0.87f, 0.5f, 0.98f); break;
            case TowerType.Poison:
                startSize = 13f; endSize = 49f; fxColor = new Color(0.76f, 1f, 0.5f, 0.96f); break;
            default:
                startSize = 13f; endSize = 48f; fxColor = new Color(1f, 1f, 1f, 0.95f); break;
        }

        float kindScale = GetKindFactor(kindId, 79, 0.88f, 1.18f);
        float whiteMix = GetKindFactor(kindId, 89, 0f, 0.20f);
        startSize *= kindScale;
        endSize *= kindScale;
        fxColor = Color.Lerp(fxColor, Color.white, whiteMix);

        fxRt.sizeDelta = Vector2.one * startSize;
        Image fxImage = AddImage(fxRt.gameObject, fxColor);
        fxImage.raycastTarget = false;

        _impactFx.Add(new ImpactFxData
        {
            View = fxRt,
            Image = fxImage,
            Timer = HitFxDuration,
            Duration = HitFxDuration,
            StartSize = startSize,
            EndSize = endSize,
            BaseColor = fxColor
        });
    }

    private void UpdateImpactEffects(float dt)
    {
        for (int i = _impactFx.Count - 1; i >= 0; i--)
        {
            ImpactFxData fx = _impactFx[i];
            if (fx.View == null || fx.Image == null)
            {
                _impactFx.RemoveAt(i);
                continue;
            }

            fx.Timer -= dt;
            if (fx.Timer <= 0f)
            {
                Destroy(fx.View.gameObject);
                _impactFx.RemoveAt(i);
                continue;
            }

            float p = 1f - fx.Timer / fx.Duration;
            float size = Mathf.Lerp(fx.StartSize, fx.EndSize, p);
            float alpha = 1f - p;
            fx.View.sizeDelta = Vector2.one * size;
            fx.Image.color = new Color(fx.BaseColor.r, fx.BaseColor.g, fx.BaseColor.b, alpha * 0.9f);
        }
    }

    private static void UpdateEnemyHitFlash(EnemyData enemy, float dt)
    {
        EnemyModelData model = enemy.Model;
        if (model == null) return;

        if (model.BodyImage == null || model.CoreImage == null)
        {
            return;
        }

        if (enemy.HitFlashTimer <= 0f)
        {
            model.BodyImage.color = model.BodyBaseColor;
            model.CoreImage.color = model.CoreBaseColor;
            if (model.CrownImage != null) model.CrownImage.color = model.CrownBaseColor;
            return;
        }

        enemy.HitFlashTimer = Mathf.Max(0f, enemy.HitFlashTimer - dt);
        float t = enemy.HitFlashTimer / EnemyHitFlashDuration;
        model.BodyImage.color = Color.Lerp(model.BodyBaseColor, Color.white, t * 0.82f);
        model.CoreImage.color = Color.Lerp(model.CoreBaseColor, Color.white, t * 0.92f);
        if (model.CrownImage != null)
        {
            model.CrownImage.color = Color.Lerp(model.CrownBaseColor, Color.white, t * 0.75f);
        }
    }
}
