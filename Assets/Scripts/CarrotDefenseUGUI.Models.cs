using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // Enemy/tower model assembly and style animation helpers.
    private Color GetTowerColor(TowerType type, int level)
    {
        Color baseColor;
        switch (type)
        {
            case TowerType.Pea: baseColor = _peaColor; break;
            case TowerType.Ice: baseColor = _iceColor; break;
            case TowerType.Fire: baseColor = new Color(0.92f, 0.42f, 0.2f, 1f); break;
            case TowerType.Cannon: baseColor = new Color(0.78f, 0.64f, 0.25f, 1f); break;
            case TowerType.Poison: baseColor = new Color(0.52f, 0.82f, 0.24f, 1f); break;
            default: baseColor = _peaColor; break;
        }
        float t = Mathf.Clamp01((level - 1) * 0.14f);
        return Color.Lerp(baseColor, Color.white, t);
    }

    private TowerModelData BuildTowerModel(RectTransform parent)
    {
        // 用 UGUI 组合一个“塔模型”：底座、炮台头、炮管、炮口闪光。
        var modelRoot = CreateRect("ModelRoot", parent);
        modelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        modelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        modelRoot.pivot = new Vector2(0.5f, 0.5f);
        modelRoot.sizeDelta = Vector2.one * (CellSize - 12f);

        var shadowRt = CreateRect("Shadow", modelRoot);
        shadowRt.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRt.pivot = new Vector2(0.5f, 0.5f);
        shadowRt.anchoredPosition = new Vector2(0f, -18f);
        Image shadowImage = AddImage(shadowRt.gameObject, new Color(0f, 0f, 0f, 0.3f));
        shadowImage.raycastTarget = false;

        var baseRt = CreateRect("Base", modelRoot);
        baseRt.anchorMin = new Vector2(0.5f, 0.5f);
        baseRt.anchorMax = new Vector2(0.5f, 0.5f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        baseRt.anchoredPosition = new Vector2(0f, -12f);
        Image baseImage = AddImage(baseRt.gameObject, Color.white);
        baseImage.raycastTarget = false;

        var headPivot = CreateRect("HeadPivot", modelRoot);
        headPivot.anchorMin = new Vector2(0.5f, 0.5f);
        headPivot.anchorMax = new Vector2(0.5f, 0.5f);
        headPivot.pivot = new Vector2(0.5f, 0.5f);
        headPivot.anchoredPosition = new Vector2(0f, -2f);

        var headRt = CreateRect("Head", headPivot);
        headRt.anchorMin = new Vector2(0.5f, 0.5f);
        headRt.anchorMax = new Vector2(0.5f, 0.5f);
        headRt.pivot = new Vector2(0.5f, 0.5f);
        headRt.anchoredPosition = Vector2.zero;
        Image headImage = AddImage(headRt.gameObject, Color.white);
        headImage.raycastTarget = false;

        var barrelRt = CreateRect("Barrel", headPivot);
        barrelRt.anchorMin = new Vector2(0.5f, 0.5f);
        barrelRt.anchorMax = new Vector2(0.5f, 0.5f);
        barrelRt.pivot = new Vector2(0f, 0.5f);
        barrelRt.anchoredPosition = new Vector2(4f, 0f);
        Image barrelImage = AddImage(barrelRt.gameObject, Color.white);
        barrelImage.raycastTarget = false;

        var muzzleRt = CreateRect("Muzzle", barrelRt);
        muzzleRt.anchorMin = new Vector2(1f, 0.5f);
        muzzleRt.anchorMax = new Vector2(1f, 0.5f);
        muzzleRt.pivot = new Vector2(0.5f, 0.5f);
        muzzleRt.anchoredPosition = Vector2.zero;

        var flashRt = CreateRect("Flash", muzzleRt);
        flashRt.anchorMin = new Vector2(0.5f, 0.5f);
        flashRt.anchorMax = new Vector2(0.5f, 0.5f);
        flashRt.pivot = new Vector2(0.5f, 0.5f);
        flashRt.anchoredPosition = Vector2.zero;
        Image flashImage = AddImage(flashRt.gameObject, new Color(1f, 1f, 1f, 0f));
        flashImage.raycastTarget = false;

        return new TowerModelData
        {
            Root = modelRoot,
            Shadow = shadowRt,
            Base = baseRt,
            HeadPivot = headPivot,
            Head = headRt,
            Barrel = barrelRt,
            Muzzle = muzzleRt,
            Flash = flashRt,
            ShadowImage = shadowImage,
            BaseImage = baseImage,
            HeadImage = headImage,
            BarrelImage = barrelImage,
            FlashImage = flashImage,
            BaseBarrelLength = 24f
        };
    }

    private void ApplyTowerModelStyle(TowerModelData model, TowerType type, int level)
    {
        if (model == null) return;

        float lv = Mathf.Clamp(level, 1, MaxTowerLevel);
        float scale = 1f + (lv - 1f) * 0.12f;

        model.Shadow.sizeDelta = new Vector2(34f, 11f) * scale;
        model.Shadow.anchoredPosition = new Vector2(0f, -18f);
        model.Base.sizeDelta = new Vector2(31f, 17f) * scale;
        model.Base.anchoredPosition = new Vector2(0f, -11f);
        model.Head.sizeDelta = new Vector2(24f, 20f) * scale;

        float baseBarrelLength;
        float barrelWidth;
        float muzzleSize;
        float flashSize;
        Color headColor;
        Color barrelColor;
        Color flashColor;

        switch (type)
        {
            case TowerType.Pea:
                baseBarrelLength = 20f; barrelWidth = 6.6f; muzzleSize = 8f; flashSize = 11f;
                headColor = new Color(0.31f, 0.67f, 0.27f, 1f);
                barrelColor = new Color(0.24f, 0.57f, 0.21f, 1f);
                flashColor = new Color(1f, 0.96f, 0.52f, 0f);
                break;
            case TowerType.Ice:
                baseBarrelLength = 18f; barrelWidth = 7.4f; muzzleSize = 9f; flashSize = 13f;
                headColor = new Color(0.33f, 0.72f, 0.94f, 1f);
                barrelColor = new Color(0.24f, 0.58f, 0.82f, 1f);
                flashColor = new Color(0.82f, 0.97f, 1f, 0f);
                break;
            case TowerType.Fire:
                baseBarrelLength = 19f; barrelWidth = 7.2f; muzzleSize = 9f; flashSize = 14f;
                headColor = new Color(0.9f, 0.45f, 0.23f, 1f);
                barrelColor = new Color(0.82f, 0.36f, 0.17f, 1f);
                flashColor = new Color(1f, 0.78f, 0.32f, 0f);
                break;
            case TowerType.Cannon:
                baseBarrelLength = 22f; barrelWidth = 8.6f; muzzleSize = 10f; flashSize = 15f;
                headColor = new Color(0.74f, 0.62f, 0.26f, 1f);
                barrelColor = new Color(0.58f, 0.52f, 0.26f, 1f);
                flashColor = new Color(1f, 0.9f, 0.58f, 0f);
                break;
            case TowerType.Poison:
                baseBarrelLength = 19f; barrelWidth = 6.9f; muzzleSize = 9f; flashSize = 12f;
                headColor = new Color(0.52f, 0.79f, 0.28f, 1f);
                barrelColor = new Color(0.44f, 0.68f, 0.21f, 1f);
                flashColor = new Color(0.86f, 1f, 0.6f, 0f);
                break;
            default:
                baseBarrelLength = 20f; barrelWidth = 7f; muzzleSize = 9f; flashSize = 12f;
                headColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                barrelColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                flashColor = new Color(1f, 1f, 1f, 0f);
                break;
        }

        model.BaseBarrelLength = baseBarrelLength + (lv - 1f) * 4.2f;
        model.Barrel.sizeDelta = new Vector2(model.BaseBarrelLength, barrelWidth + (lv - 1f) * 0.9f);
        model.Barrel.anchoredPosition = new Vector2(3.8f, 0f);
        model.Muzzle.sizeDelta = Vector2.one * muzzleSize;
        model.Flash.sizeDelta = Vector2.one * flashSize;

        Color ringColor = new Color(0.34f, 0.31f, 0.26f, 1f);

        model.BaseImage.color = Color.Lerp(ringColor, Color.white, (lv - 1f) * 0.18f);
        model.HeadImage.color = Color.Lerp(headColor, Color.white, (lv - 1f) * 0.16f);
        model.BarrelImage.color = Color.Lerp(barrelColor, Color.white, (lv - 1f) * 0.10f);
        model.FlashImage.color = flashColor;
    }

    private void UpdateTowerModelAnimation(TowerData tower, EnemyData target)
    {
        // 待机时轻微摆动，攻击时后坐并闪光，同时朝目标方向旋转。
        if (tower == null || tower.Model == null) return;
        TowerModelData model = tower.Model;

        float time = Time.time;
        float idleWave = Mathf.Sin(time * 2.2f + tower.IdlePhase);
        float idleBob = idleWave * 1.2f;
        float idleScale = 1f + Mathf.Sin(time * 1.8f + tower.IdlePhase) * 0.018f;
        float idleSway = Mathf.Sin(time * 1.3f + tower.IdlePhase) * 9f;

        float attack01 = 1f - Mathf.Clamp01(tower.AttackAnimTimer / TowerAttackAnimDuration);
        float recoilBase;
        switch (tower.Type)
        {
            case TowerType.Cannon: recoilBase = 6.5f; break;
            case TowerType.Fire: recoilBase = 5.4f; break;
            case TowerType.Ice: recoilBase = 4.9f; break;
            case TowerType.Poison: recoilBase = 4.6f; break;
            default: recoilBase = 5.8f; break;
        }
        float recoil = Mathf.Sin(attack01 * Mathf.PI) * recoilBase * (1f + (tower.Level - 1) * 0.08f);

        float aimAngle = idleSway;
        if (target != null && target.View != null)
        {
            Vector2 to = target.View.anchoredPosition - tower.View.anchoredPosition;
            if (to.sqrMagnitude > 0.01f)
            {
                aimAngle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            }
        }

        model.Root.localScale = Vector3.one * idleScale;
        model.HeadPivot.anchoredPosition = new Vector2(0f, -2f + idleBob);
        model.HeadPivot.localEulerAngles = new Vector3(0f, 0f, aimAngle);
        model.Barrel.anchoredPosition = new Vector2(3.8f - recoil, 0f);

        if (model.FlashImage != null)
        {
            float flash = Mathf.Clamp01(1f - attack01 * 5f);
            Color c = model.FlashImage.color;
            model.FlashImage.color = new Color(c.r, c.g, c.b, flash * 0.9f);
            float flashSize;
            switch (tower.Type)
            {
                case TowerType.Cannon: flashSize = 15f; break;
                case TowerType.Fire: flashSize = 14f; break;
                case TowerType.Ice: flashSize = 13f; break;
                case TowerType.Poison: flashSize = 12f; break;
                default: flashSize = 11f; break;
            }
            model.Flash.sizeDelta = Vector2.one * (flashSize + flash * 7.5f);
        }
    }

    private Vector2 GetTowerMuzzlePosition(TowerData tower)
    {
        if (tower == null || tower.Model == null || tower.Model.Muzzle == null || _board == null)
        {
            return tower != null ? tower.View.anchoredPosition : Vector2.zero;
        }

        Vector3 world = tower.Model.Muzzle.TransformPoint(Vector3.zero);
        return _board.InverseTransformPoint(world);
    }

    private void LoadTowerSpritesIfNeeded()
    {
        if (_peaTowerSprite == null)
        {
            _peaTowerSprite = LoadTowerSprite(PeaSpriteResourcePath);
        }

        if (_iceTowerSprite == null)
        {
            _iceTowerSprite = LoadTowerSprite(IceSpriteResourcePath);
        }
    }

    private static Sprite LoadTowerSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null) return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private Sprite GetTowerSprite(TowerType type)
    {
        switch (type)
        {
            case TowerType.Pea: return _peaTowerSprite;
            case TowerType.Ice: return _iceTowerSprite;
            default: return null;
        }
    }

    private void ApplyTowerVisual(Image towerImage, TowerType type, int level)
    {
        Sprite towerSprite = GetTowerSprite(type);
        if (towerSprite != null)
        {
            towerImage.sprite = towerSprite;
            towerImage.preserveAspect = true;
            towerImage.color = Color.white;
            return;
        }

        towerImage.sprite = null;
        towerImage.preserveAspect = false;
        towerImage.color = GetTowerColor(type, level);
    }

}
