using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class LobbyHallUGUI : MonoBehaviour
{
    private void InitSettingsRuntime()
    {
        _resolutionOptions = Screen.resolutions;
        if (_resolutionOptions == null || _resolutionOptions.Length == 0)
        {
            _resolutionOptions = new[]
            {
                new Resolution { width = Screen.width, height = Screen.height }
            };
        }

        int savedWidth;
        int savedHeight;
        if (SystemSettingsData.TryGetSavedResolution(out savedWidth, out savedHeight))
        {
            _currentResolutionIndex = FindResolutionIndex(savedWidth, savedHeight);
            if (_currentResolutionIndex < 0) _currentResolutionIndex = FindResolutionIndex(Screen.width, Screen.height);
        }
        else
        {
            _currentResolutionIndex = FindResolutionIndex(Screen.width, Screen.height);
        }

        if (_currentResolutionIndex < 0) _currentResolutionIndex = 0;
    }

    private int FindResolutionIndex(int width, int height)
    {
        if (_resolutionOptions == null || _resolutionOptions.Length == 0) return -1;
        for (int i = 0; i < _resolutionOptions.Length; i++)
        {
            if (_resolutionOptions[i].width == width && _resolutionOptions[i].height == height) return i;
        }

        return -1;
    }

    private void EnableSound()
    {
        SystemSettingsData.SetSoundEnabled(true);
        RefreshSettingsView();
        SetSettingsTip("声音已打开", new Color(0.78f, 1f, 0.82f, 1f));
    }

    private void DisableSound()
    {
        SystemSettingsData.SetSoundEnabled(false);
        RefreshSettingsView();
        SetSettingsTip("声音已关闭", new Color(1f, 0.86f, 0.72f, 1f));
    }

    private void EnableFullscreen()
    {
        SystemSettingsData.SetFullscreen(true);
        RefreshSettingsView();
        SetSettingsTip("已切换为全屏模式", new Color(0.82f, 0.95f, 1f, 1f));
    }

    private void DisableFullscreen()
    {
        SystemSettingsData.SetFullscreen(false);
        RefreshSettingsView();
        SetSettingsTip("已切换为窗口模式", new Color(0.82f, 0.95f, 1f, 1f));
    }

    private void PrevQualityLevel()
    {
        int count = QualitySettings.names.Length;
        if (count <= 0) return;
        int current = SystemSettingsData.GetQualityLevel();
        int next = (current - 1 + count) % count;
        SystemSettingsData.SetQualityLevel(next);
        RefreshSettingsView();
        SetSettingsTip("清晰度切换为：" + QualitySettings.names[next], new Color(0.82f, 0.95f, 1f, 1f));
    }

    private void NextQualityLevel()
    {
        int count = QualitySettings.names.Length;
        if (count <= 0) return;
        int current = SystemSettingsData.GetQualityLevel();
        int next = (current + 1) % count;
        SystemSettingsData.SetQualityLevel(next);
        RefreshSettingsView();
        SetSettingsTip("清晰度切换为：" + QualitySettings.names[next], new Color(0.82f, 0.95f, 1f, 1f));
    }

    private void PrevResolution()
    {
        if (_resolutionOptions == null || _resolutionOptions.Length == 0) return;
        _currentResolutionIndex = (_currentResolutionIndex - 1 + _resolutionOptions.Length) % _resolutionOptions.Length;
        ApplyCurrentResolution();
    }

    private void NextResolution()
    {
        if (_resolutionOptions == null || _resolutionOptions.Length == 0) return;
        _currentResolutionIndex = (_currentResolutionIndex + 1) % _resolutionOptions.Length;
        ApplyCurrentResolution();
    }

    private void ApplyCurrentResolution()
    {
        if (_resolutionOptions == null || _resolutionOptions.Length == 0) return;
        Resolution r = _resolutionOptions[_currentResolutionIndex];
        SystemSettingsData.SetResolution(r.width, r.height);
        RefreshSettingsView();
        SetSettingsTip("分辨率切换为：" + r.width + " x " + r.height, new Color(0.82f, 0.95f, 1f, 1f));
    }

    private void RefreshSettingsView()
    {
        if (_soundStateText != null)
        {
            bool soundOn = SystemSettingsData.IsSoundEnabled();
            _soundStateText.text = soundOn ? "当前：开启" : "当前：关闭";
            _soundStateText.color = soundOn ? new Color(0.76f, 1f, 0.8f, 1f) : new Color(1f, 0.84f, 0.74f, 1f);

            if (_soundOnButton != null) _soundOnButton.interactable = !soundOn;
            if (_soundOffButton != null) _soundOffButton.interactable = soundOn;
        }

        if (_qualityStateText != null)
        {
            int quality = SystemSettingsData.GetQualityLevel();
            string[] names = QualitySettings.names;
            _qualityStateText.text = names != null && quality >= 0 && quality < names.Length
                ? ("当前：" + names[quality])
                : "当前：未知";
            _qualityStateText.color = new Color(0.86f, 0.92f, 1f, 1f);
        }

        if (_fullscreenStateText != null)
        {
            bool fullscreen = SystemSettingsData.IsFullscreen();
            _fullscreenStateText.text = fullscreen ? "当前：全屏" : "当前：窗口";
            _fullscreenStateText.color = new Color(0.86f, 0.92f, 1f, 1f);

            if (_fullscreenOnButton != null) _fullscreenOnButton.interactable = !fullscreen;
            if (_fullscreenOffButton != null) _fullscreenOffButton.interactable = fullscreen;
        }

        if (_resolutionStateText != null)
        {
            int idx = FindResolutionIndex(Screen.width, Screen.height);
            if (idx >= 0) _currentResolutionIndex = idx;
            if (_currentResolutionIndex < 0) _currentResolutionIndex = 0;

            Resolution r = _resolutionOptions[_currentResolutionIndex];
            _resolutionStateText.text = "当前：" + r.width + " x " + r.height;
            _resolutionStateText.color = new Color(0.86f, 0.92f, 1f, 1f);
        }
    }

    private static float AtlasHash01(int a, int b, int c)
    {
        int n = a * 92837111 ^ b * 689287499 ^ c * 283923481;
        n ^= n << 13;
        n ^= n >> 17;
        n ^= n << 5;
        return (n & 0x7fffffff) / 2147483647f;
    }

    private Sprite GetOrCreateAtlasPhotoSprite(bool monster, int index)
    {
        string key = (monster ? "m_" : "p_") + index;
        Sprite cached;
        if (_atlasPhotoCache.TryGetValue(key, out cached) && cached != null)
        {
            return cached;
        }

        const int width = 144;
        const int height = 96;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float hueBase = monster
            ? Mathf.Repeat(0.01f * index + 0.57f, 1f)
            : Mathf.Repeat(0.017f * index + 0.12f, 1f);
        Color top = Color.HSVToRGB(hueBase, monster ? 0.58f : 0.46f, 0.88f);
        Color bottom = Color.HSVToRGB(Mathf.Repeat(hueBase + 0.08f, 1f), monster ? 0.74f : 0.62f, 0.52f);
        Vector2 center = new Vector2(width * 0.52f, height * 0.5f);
        float radius = monster ? 31f : 28f;

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                Color col = Color.Lerp(bottom, top, v);
                float noise = AtlasHash01(x + index * 17, y + index * 29, monster ? 1009 : 709);
                col = Color.Lerp(col, Color.white, noise * 0.12f);

                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius)
                {
                    float bodyT = 1f - dist / radius;
                    Color body = monster
                        ? Color.HSVToRGB(Mathf.Repeat(hueBase + 0.22f, 1f), 0.72f, 0.92f)
                        : Color.HSVToRGB(Mathf.Repeat(hueBase + 0.14f, 1f), 0.52f, 0.96f);
                    col = Color.Lerp(col, body, bodyT * 0.9f);
                }

                if (monster)
                {
                    if (y > 18 && y < 24 && Mathf.Abs(x - width * 0.42f) < 7f) col = Color.white;
                    if (y > 18 && y < 24 && Mathf.Abs(x - width * 0.60f) < 7f) col = Color.white;
                }
                else
                {
                    if (y > 60 && Mathf.Abs(x - width * 0.52f) < 2f) col = Color.Lerp(col, new Color(0.24f, 0.38f, 0.2f, 1f), 0.8f);
                    if (y > 52 && y < 60 && Mathf.Abs(x - width * 0.46f) < 5f) col = Color.Lerp(col, new Color(0.25f, 0.5f, 0.24f, 1f), 0.85f);
                    if (y > 52 && y < 60 && Mathf.Abs(x - width * 0.58f) < 5f) col = Color.Lerp(col, new Color(0.25f, 0.5f, 0.24f, 1f), 0.85f);
                }

                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        _atlasPhotoCache[key] = sprite;
        return sprite;
    }

    private void ShowPlantAtlas()
    {
        _atlasShowPlant = true;
        _atlasPage = 0;
        RefreshAllViews();
    }

    private void ShowMonsterAtlas()
    {
        _atlasShowPlant = false;
        _atlasPage = 0;
        RefreshAllViews();
    }

    private void PrevAtlasPage()
    {
        int totalCount = _atlasShowPlant ? GameMetaData.GetAllPlantIds().Length : (EnemyAtlasCount + BossAtlasCount);
        int perPage = Mathf.Max(1, _atlasCards.Count);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)perPage));
        if (totalPages <= 1) return;

        _atlasPage = (_atlasPage - 1 + totalPages) % totalPages;
        RefreshAllViews();
    }

    private void NextAtlasPage()
    {
        int totalCount = _atlasShowPlant ? GameMetaData.GetAllPlantIds().Length : (EnemyAtlasCount + BossAtlasCount);
        int perPage = Mathf.Max(1, _atlasCards.Count);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)perPage));
        if (totalPages <= 1) return;

        _atlasPage = (_atlasPage + 1) % totalPages;
        RefreshAllViews();
    }

    private static string GetAtlasEnemyName(int enemyIndex)
    {
        int idx = Mathf.Clamp(enemyIndex - 1, 0, EnemyAtlasCount - 1);
        int prefixIdx = idx % s_EnemyAtlasPrefix.Length;
        int suffixIdx = idx / s_EnemyAtlasPrefix.Length;
        suffixIdx = Mathf.Clamp(suffixIdx, 0, s_EnemyAtlasSuffix.Length - 1);
        return s_EnemyAtlasPrefix[prefixIdx] + s_EnemyAtlasSuffix[suffixIdx];
    }

    private static string GetAtlasBossName(int bossIndex)
    {
        int idx = Mathf.Clamp(bossIndex - 1, 0, s_BossAtlasNames.Length - 1);
        return s_BossAtlasNames[idx];
    }

    private void RefreshAtlasView()
    {
        if (_atlasCards.Count == 0) return;
        string[] plantIds = GameMetaData.GetAllPlantIds();
        int totalCount = _atlasShowPlant ? plantIds.Length : (EnemyAtlasCount + BossAtlasCount);
        int perPage = Mathf.Max(1, _atlasCards.Count);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)perPage));
        _atlasPage = Mathf.Clamp(_atlasPage, 0, totalPages - 1);

        for (int i = 0; i < _atlasCards.Count; i++)
        {
            AtlasCard card = _atlasCards[i];
            if (card == null || card.Root == null) continue;

            int idx = _atlasPage * perPage + i;
            if (idx >= totalCount)
            {
                card.Root.gameObject.SetActive(false);
                continue;
            }

            card.Root.gameObject.SetActive(true);
            if (_atlasShowPlant)
            {
                string plantId = plantIds[idx];
                bool owned = GameMetaData.IsPlantOwned(plantId);
                if (card.TitleText != null) card.TitleText.text = GameMetaData.GetPlantDisplayName(plantId);
                if (card.DescText != null) card.DescText.text = GameMetaData.GetPlantDesc(plantId) + "\n状态：" + (owned ? "已拥有" : "未拥有");
                if (card.PhotoImage != null) card.PhotoImage.sprite = GetOrCreateAtlasPhotoSprite(false, idx + 1);
            }
            else
            {
                bool isBoss = idx >= EnemyAtlasCount;
                int number = isBoss ? (idx - EnemyAtlasCount + 1) : (idx + 1);
                string name = isBoss ? GetAtlasBossName(number) : GetAtlasEnemyName(number);
                string desc = isBoss
                    ? "关卡首领，拥有特殊技能与强化阶段"
                    : "普通怪物，速度/血量/奖励各不相同";
                if (card.TitleText != null) card.TitleText.text = name;
                if (card.DescText != null) card.DescText.text = desc;
                if (card.PhotoImage != null) card.PhotoImage.sprite = GetOrCreateAtlasPhotoSprite(true, idx + 1);
            }
        }

        if (_atlasPageText != null) _atlasPageText.text = (_atlasPage + 1) + " / " + totalPages;
        if (_atlasPrevPageButton != null) _atlasPrevPageButton.interactable = totalPages > 1;
        if (_atlasNextPageButton != null) _atlasNextPageButton.interactable = totalPages > 1;

        if (_atlasPlantTabButton != null) _atlasPlantTabButton.interactable = !_atlasShowPlant;
        if (_atlasMonsterTabButton != null) _atlasMonsterTabButton.interactable = _atlasShowPlant;

        if (_atlasTipText != null)
        {
            _atlasTipText.text = _atlasShowPlant
                ? "植物图册：显示已购买状态与特性简介（含照片）"
                : "怪物图册：显示普通怪与首领档案（含照片）";
        }
    }

    private void PrevShopPage()
    {
        if (_allShopPlantIds == null || _allShopPlantIds.Length == 0) return;
        int totalPages = Mathf.CeilToInt(_allShopPlantIds.Length / (float)ShopCardsPerPage);
        if (totalPages <= 1) return;

        _shopPage = (_shopPage - 1 + totalPages) % totalPages;
        RefreshAllViews();
    }

    private void NextShopPage()
    {
        if (_allShopPlantIds == null || _allShopPlantIds.Length == 0) return;
        int totalPages = Mathf.CeilToInt(_allShopPlantIds.Length / (float)ShopCardsPerPage);
        if (totalPages <= 1) return;

        _shopPage = (_shopPage + 1) % totalPages;
        RefreshAllViews();
    }

    private void RefreshShopCards()
    {
        if (_allShopPlantIds == null) _allShopPlantIds = GameMetaData.GetAllPlantIds();
        int total = _allShopPlantIds.Length;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(total / (float)ShopCardsPerPage));
        _shopPage = Mathf.Clamp(_shopPage, 0, totalPages - 1);

        for (int i = 0; i < _shopCards.Count; i++)
        {
            ShopCard card = _shopCards[i];
            if (card == null || card.Root == null) continue;

            int idx = _shopPage * ShopCardsPerPage + i;
            if (idx < 0 || idx >= total)
            {
                card.PlantId = string.Empty;
                card.Root.gameObject.SetActive(false);
                continue;
            }

            string plantId = _allShopPlantIds[idx];
            card.PlantId = plantId;
            card.Root.gameObject.SetActive(true);

            bool owned = GameMetaData.IsPlantOwned(plantId);
            int price = GameMetaData.GetPlantPrice(plantId);

            if (card.TitleText != null)
            {
                card.TitleText.text = GameMetaData.GetPlantDisplayName(plantId);
            }

            if (card.DescText != null)
            {
                card.DescText.text = GameMetaData.GetPlantDesc(plantId);
            }

            if (card.BuyButton != null)
            {
                card.BuyButton.interactable = !owned;
                Text label = card.BuyButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = owned ? "已拥有" : (price <= 0 ? "免费领取" : "购买 " + price + " 金币");
                }
            }
        }

        if (_shopPrevPageButton != null) _shopPrevPageButton.interactable = totalPages > 1;
        if (_shopNextPageButton != null) _shopNextPageButton.interactable = totalPages > 1;
        if (_shopPageText != null) _shopPageText.text = (_shopPage + 1) + " / " + totalPages;
    }

    private void TryBuyPlant(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return;
        int price = GameMetaData.GetPlantPrice(plantId);
        string name = GameMetaData.GetPlantDisplayName(plantId);

        if (GameMetaData.IsPlantOwned(plantId))
        {
            SetShopTip(name + " 已拥有", new Color(0.82f, 0.95f, 1f, 1f));
            return;
        }

        if (!GameMetaData.SpendCoins(price))
        {
            SetShopTip("金币不足，无法购买", new Color(1f, 0.72f, 0.72f, 1f));
            return;
        }

        GameMetaData.SetPlantOwned(plantId, true);
        RefreshAllViews();
        SetShopTip("购买成功：" + name + "，已可在战斗中使用", new Color(0.76f, 1f, 0.8f, 1f));
    }

    private void EnterStage(int stage)
    {
        int unlocked = GameMetaData.GetUnlockedStage();
        if (stage > unlocked)
        {
            SetMainTip("第" + stage + "关尚未解锁", new Color(1f, 0.74f, 0.74f, 1f));
            return;
        }

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        SetMainTip("正在进入第" + stage + "关...", new Color(0.72f, 1f, 0.78f, 1f));
        CarrotDefenseUGUI.StartBattleOnNextLoad(stage);
        StartCoroutine(EnterBattleNow());
    }

    private IEnumerator EnterBattleNow()
    {
        yield return new WaitForSecondsRealtime(0.08f);
        if (FindObjectOfType<CarrotDefenseUGUI>() == null)
        {
            new GameObject(nameof(CarrotDefenseUGUI)).AddComponent<CarrotDefenseUGUI>();
        }

        Destroy(gameObject);
    }

    private void ClaimDailyReward()
    {
        int reward = DailyTaskSystem.ClaimLoginReward();
        if (reward <= 0)
        {
            SetDailyTip(
                DailyTaskSystem.IsLoginRewardClaimed() ? "今日登录任务奖励已领取" : "登录任务尚未完成",
                new Color(1f, 0.86f, 0.72f, 1f));
            RefreshAllViews();
            return;
        }

        RefreshAllViews();
        SetDailyTip("领取成功，获得 +" + reward + " 金币", new Color(0.78f, 1f, 0.82f, 1f));
    }

    private void ClaimDailyBattleReward()
    {
        int reward = DailyTaskSystem.ClaimBattleReward();
        if (reward <= 0)
        {
            SetDailyTip(
                DailyTaskSystem.IsBattleRewardClaimed() ? "今日完成一局任务奖励已领取" : "请先完成任意一局再来领取",
                new Color(1f, 0.86f, 0.72f, 1f));
            RefreshAllViews();
            return;
        }

        RefreshAllViews();
        SetDailyTip("领取成功，获得 +" + reward + " 金币", new Color(0.78f, 1f, 0.82f, 1f));
    }

    private void ClaimFriendGift()
    {
        if (!CanClaimFriendGift())
        {
            SetFriendTip("今日好友赠礼已领取", new Color(1f, 0.86f, 0.74f, 1f));
            return;
        }

        GameMetaData.AddCoins(FriendGiftCoins);
        PlayerPrefs.SetString(KeyFriendGiftDate, TodayKey());
        PlayerPrefs.Save();

        RefreshAllViews();
        SetFriendTip("已领取好友赠礼 +" + FriendGiftCoins + " 金币", new Color(0.8f, 1f, 0.84f, 1f));
    }

    private void DoRecharge(int coinAmount)
    {
        GameMetaData.AddCoins(coinAmount);
        RefreshAllViews();

        if (_rechargeTipText == null) return;
        _rechargeTipText.text = "充值成功，获得 +" + coinAmount + " 金币";
        _rechargeTipText.color = new Color(1f, 0.96f, 0.8f, 1f);
    }

    private void TryCloseDailyPanel()
    {
        if (DailyTaskSystem.HasAnyClaimableRewards())
        {
            SetDailyTip("每日奖励未领取，不能取消。", new Color(1f, 0.86f, 0.72f, 1f));
            return;
        }

        ShowMainPanel();
    }

    private void RefreshAllViews()
    {
        int coins = GameMetaData.GetCoins();
        if (_coinText != null) _coinText.text = "金币: " + coins;

        int unlocked = GameMetaData.GetUnlockedStage();
        for (int i = 0; i < _stageButtons.Count; i++)
        {
            int stage = i + 1;
            bool canPlay = stage <= unlocked;
            Button button = _stageButtons[i];
            button.interactable = canPlay;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                string line = "第" + stage + "关\nHP x" + GameMetaData.GetHpScaleForStage(stage).ToString("0.00");
                label.text = canPlay ? line : (line + "\n(未解锁)");
            }
        }

        RefreshShopCards();
        RefreshAtlasView();

        if (_dailyClaimButton != null)
        {
            _dailyClaimButton.interactable = CanClaimDaily();
        }

        if (_dailyBattleClaimButton != null)
        {
            _dailyBattleClaimButton.interactable = CanClaimDailyBattle();
        }

        if (_dailyCloseButton != null)
        {
            _dailyCloseButton.interactable = !DailyTaskSystem.HasAnyClaimableRewards();
        }

        RefreshDailyTaskStates();

        if (_friendGiftButton != null)
        {
            _friendGiftButton.interactable = CanClaimFriendGift();
        }

        RefreshSettingsView();
    }

    private void RefreshDailyTaskStates()
    {
        if (_dailyLoginStateText != null)
        {
            if (DailyTaskSystem.IsLoginRewardClaimed())
            {
                _dailyLoginStateText.text = "状态：已领取";
                _dailyLoginStateText.color = new Color(0.75f, 1f, 0.8f, 1f);
            }
            else if (DailyTaskSystem.CanClaimLoginReward())
            {
                _dailyLoginStateText.text = "状态：可领取";
                _dailyLoginStateText.color = new Color(1f, 0.94f, 0.74f, 1f);
            }
            else
            {
                _dailyLoginStateText.text = "状态：未完成";
                _dailyLoginStateText.color = new Color(0.8f, 0.85f, 0.9f, 1f);
            }
        }

        if (_dailyBattleStateText == null) return;
        if (DailyTaskSystem.IsBattleRewardClaimed())
        {
            _dailyBattleStateText.text = "状态：已领取";
            _dailyBattleStateText.color = new Color(0.75f, 1f, 0.8f, 1f);
        }
        else if (DailyTaskSystem.CanClaimBattleReward())
        {
            _dailyBattleStateText.text = "状态：可领取";
            _dailyBattleStateText.color = new Color(1f, 0.94f, 0.74f, 1f);
        }
        else
        {
            _dailyBattleStateText.text = "状态：未完成";
            _dailyBattleStateText.color = new Color(0.8f, 0.85f, 0.9f, 1f);
        }
    }

    private void ShowMainPanel()
    {
        ShowPanel(_mainPanel);
    }

    private void ShowLevelPanel()
    {
        RefreshAllViews();
        ShowPanel(_levelPanel);
    }

    private void ShowFriendPanel()
    {
        RefreshAllViews();
        ShowPanel(_friendPanel);
    }

    private void ShowShopPanel()
    {
        RefreshAllViews();
        ShowPanel(_shopPanel);
    }

    private void ShowAtlasPanel()
    {
        RefreshAllViews();
        ShowPanel(_atlasPanel);
    }

    private void ShowDailyPanel()
    {
        RefreshAllViews();
        ShowPanel(_dailyPanel);
    }

    private void ShowRechargePanel()
    {
        RefreshAllViews();
        ShowPanel(_rechargePanel);
    }

    private void ShowSettingsPanel()
    {
        RefreshAllViews();
        ShowPanel(_settingsPanel);
    }

    private void ShowPanel(RectTransform panel)
    {
        if (_mainPanel != null) _mainPanel.gameObject.SetActive(panel == _mainPanel);
        if (_levelPanel != null) _levelPanel.gameObject.SetActive(panel == _levelPanel);
        if (_friendPanel != null) _friendPanel.gameObject.SetActive(panel == _friendPanel);
        if (_shopPanel != null) _shopPanel.gameObject.SetActive(panel == _shopPanel);
        if (_atlasPanel != null) _atlasPanel.gameObject.SetActive(panel == _atlasPanel);
        if (_dailyPanel != null) _dailyPanel.gameObject.SetActive(panel == _dailyPanel);
        if (_rechargePanel != null) _rechargePanel.gameObject.SetActive(panel == _rechargePanel);
        if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(panel == _settingsPanel);
    }

    private void SetMainTip(string value, Color color)
    {
        if (_mainTipText == null) return;
        _mainTipText.text = value;
        _mainTipText.color = color;
    }

    private void SetFriendTip(string value, Color color)
    {
        if (_friendTipText == null) return;
        _friendTipText.text = value;
        _friendTipText.color = color;
    }

    private void SetShopTip(string value, Color color)
    {
        if (_shopTipText == null) return;
        _shopTipText.text = value;
        _shopTipText.color = color;
    }

    private void SetDailyTip(string value, Color color)
    {
        if (_dailyTipText == null) return;
        _dailyTipText.text = value;
        _dailyTipText.color = color;
    }

    private void SetSettingsTip(string value, Color color)
    {
        if (_settingsTipText == null) return;
        _settingsTipText.text = value;
        _settingsTipText.color = color;
    }
}
