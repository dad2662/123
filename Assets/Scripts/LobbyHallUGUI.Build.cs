using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class LobbyHallUGUI : MonoBehaviour
{
    private void BuildUi()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasObj = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3200;

        _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasObj.GetComponent<RectTransform>();
        AddImage(root.gameObject, new Color(0.08f, 0.11f, 0.14f, 0.95f));

        BuildTopBar(root);
        BuildMainPanel(root);
        BuildLevelPanel(root);
        BuildFriendPanel(root);
        BuildShopPanel(root);
        BuildAtlasPanel(root);
        BuildDailyPanel(root);
        BuildRechargePanel(root);
        BuildSettingsPanel(root);

        _mainTipText = CreateBottomTip(root, "MainTip", new Color(0.9f, 0.95f, 0.95f, 1f));
    }

    private void BuildTopBar(RectTransform root)
    {
        var top = CreateRect("TopBar", root);
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(0f, 92f);
        top.anchoredPosition = Vector2.zero;
        AddImage(top.gameObject, new Color(0.04f, 0.06f, 0.08f, 0.85f));

        var title = CreateText("Title", top, "主城大厅", 42, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        title.rectTransform.pivot = new Vector2(0f, 0.5f);
        title.rectTransform.anchoredPosition = new Vector2(26f, 0f);
        title.rectTransform.sizeDelta = new Vector2(260f, 60f);
        title.color = new Color(0.94f, 0.97f, 1f, 1f);

        _coinText = CreateText("CoinText", top, "金币: 0", 32, TextAnchor.MiddleRight);
        _coinText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        _coinText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        _coinText.rectTransform.pivot = new Vector2(1f, 0.5f);
        _coinText.rectTransform.anchoredPosition = new Vector2(-220f, 0f);
        _coinText.rectTransform.sizeDelta = new Vector2(360f, 54f);
        _coinText.color = new Color(1f, 0.93f, 0.6f, 1f);

        CreateButton("SettingsButton", top, "设置", new Vector2(140f, 54f), new Vector2(-206f, 0f), new Color(0.32f, 0.46f, 0.7f, 1f), ShowSettingsPanel);
        CreateButton("RechargeButton", top, "充值", new Vector2(140f, 54f), new Vector2(-54f, 0f), new Color(0.78f, 0.42f, 0.2f, 1f), ShowRechargePanel);
    }

    private void BuildMainPanel(RectTransform root)
    {
        _mainPanel = CreatePanel(root, "MainPanel", new Vector2(780f, 610f));
        var subtitle = CreateText("MainSubTitle", _mainPanel, "请选择功能", 30, TextAnchor.UpperCenter);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -92f);

        CreateButton("BattleButton", _mainPanel, "闯关", new Vector2(340f, 76f), new Vector2(0f, 170f), new Color(0.18f, 0.62f, 0.28f, 1f), ShowLevelPanel);
        CreateButton("FriendButton", _mainPanel, "好友", new Vector2(340f, 76f), new Vector2(0f, 74f), new Color(0.2f, 0.46f, 0.76f, 1f), ShowFriendPanel);
        CreateButton("ShopButton", _mainPanel, "商城", new Vector2(340f, 76f), new Vector2(0f, -22f), new Color(0.72f, 0.46f, 0.16f, 1f), ShowShopPanel);
        CreateButton("AtlasButton", _mainPanel, "图鉴", new Vector2(340f, 76f), new Vector2(0f, -118f), new Color(0.42f, 0.38f, 0.72f, 1f), ShowAtlasPanel);
        CreateButton("DailyButton", _mainPanel, "任务", new Vector2(340f, 76f), new Vector2(0f, -214f), new Color(0.36f, 0.56f, 0.84f, 1f), ShowDailyPanel);
    }

    private void BuildLevelPanel(RectTransform root)
    {
        _levelPanel = CreatePanel(root, "LevelPanel", new Vector2(920f, 570f));
        var title = CreateText("LevelTitle", _levelPanel, "关卡选择（共5关）", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -42f);

        _stageButtons.Clear();
        for (int stage = 1; stage <= GameMetaData.MaxStage; stage++)
        {
            int row = (stage - 1) / 3;
            int col = (stage - 1) % 3;
            float x = -260f + col * 260f;
            float y = 120f - row * 150f;
            int capturedStage = stage;
            string label = "第" + stage + "关\nHP x" + GameMetaData.GetHpScaleForStage(stage).ToString("0.00");
            Button btn = CreateButton("Stage_" + stage, _levelPanel, label, new Vector2(230f, 112f), new Vector2(x, y), new Color(0.24f, 0.56f, 0.31f, 1f), () => EnterStage(capturedStage));
            _stageButtons.Add(btn);
        }

        CreateButton("LevelBack", _levelPanel, "返回", new Vector2(180f, 64f), new Vector2(0f, -220f), new Color(0.36f, 0.4f, 0.46f, 1f), ShowMainPanel);
    }

    private void BuildFriendPanel(RectTransform root)
    {
        _friendPanel = CreatePanel(root, "FriendPanel", new Vector2(900f, 560f));
        var title = CreateText("FriendTitle", _friendPanel, "好友列表", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -42f);

        string[] names = { "小绿", "冰法师", "火焰王", "炮手K", "毒藤酱" };
        for (int i = 0; i < names.Length; i++)
        {
            float y = 130f - i * 74f;
            var row = CreateRect("FriendRow_" + i, _friendPanel);
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(700f, 62f);
            row.anchoredPosition = new Vector2(0f, y);
            AddImage(row.gameObject, new Color(0.2f, 0.25f, 0.3f, 1f));

            var nameText = CreateText("Name", row, names[i], 26, TextAnchor.MiddleLeft);
            nameText.rectTransform.offsetMin = new Vector2(20f, 0f);
            nameText.color = new Color(0.92f, 0.95f, 1f, 1f);

            var stateText = CreateText("State", row, i % 2 == 0 ? "在线" : "离线", 22, TextAnchor.MiddleRight);
            stateText.rectTransform.offsetMax = new Vector2(-20f, 0f);
            stateText.color = i % 2 == 0 ? new Color(0.7f, 1f, 0.76f, 1f) : new Color(0.72f, 0.74f, 0.78f, 1f);
        }

        _friendGiftButton = CreateButton("FriendGiftBtn", _friendPanel, "领取好友赠礼 +" + FriendGiftCoins + " 金币", new Vector2(360f, 64f), new Vector2(0f, -190f), new Color(0.24f, 0.62f, 0.34f, 1f), ClaimFriendGift);
        _friendTipText = CreateText("FriendTip", _friendPanel, string.Empty, 24, TextAnchor.MiddleCenter);
        _friendTipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _friendTipText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _friendTipText.rectTransform.pivot = new Vector2(0.5f, 0f);
        _friendTipText.rectTransform.anchoredPosition = new Vector2(0f, 52f);
        _friendTipText.rectTransform.sizeDelta = new Vector2(760f, 42f);
        _friendTipText.color = new Color(0.9f, 0.95f, 1f, 1f);

        CreateButton("FriendBack", _friendPanel, "返回", new Vector2(180f, 64f), new Vector2(0f, -240f), new Color(0.36f, 0.4f, 0.46f, 1f), ShowMainPanel);
    }

    private void BuildShopPanel(RectTransform root)
    {
        _shopPanel = CreatePanel(root, "ShopPanel", new Vector2(980f, 620f));
        var title = CreateText("ShopTitle", _shopPanel, "商城 - 植物购买", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -40f);

        _shopCards.Clear();
        _allShopPlantIds = GameMetaData.GetAllPlantIds();
        _shopPage = 0;

        for (int i = 0; i < ShopCardsPerPage; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = -300f + col * 300f;
            float y = 120f - row * 218f;
            BuildPlantCard(_shopPanel, new Vector2(x, y));
        }

        _shopPrevPageButton = CreateButton("ShopPrevPage", _shopPanel, "上一页", new Vector2(160f, 58f), new Vector2(-210f, -230f), new Color(0.35f, 0.46f, 0.64f, 1f), PrevShopPage);
        _shopNextPageButton = CreateButton("ShopNextPage", _shopPanel, "下一页", new Vector2(160f, 58f), new Vector2(-20f, -230f), new Color(0.35f, 0.46f, 0.64f, 1f), NextShopPage);
        _shopPageText = CreateText("ShopPageText", _shopPanel, "1 / 1", 24, TextAnchor.MiddleCenter);
        _shopPageText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _shopPageText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _shopPageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _shopPageText.rectTransform.sizeDelta = new Vector2(140f, 40f);
        _shopPageText.rectTransform.anchoredPosition = new Vector2(-115f, -170f);
        _shopPageText.color = new Color(0.88f, 0.94f, 1f, 1f);

        _shopTipText = CreateText("ShopTip", _shopPanel, string.Empty, 24, TextAnchor.MiddleCenter);
        _shopTipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _shopTipText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _shopTipText.rectTransform.pivot = new Vector2(0.5f, 0f);
        _shopTipText.rectTransform.anchoredPosition = new Vector2(0f, 64f);
        _shopTipText.rectTransform.sizeDelta = new Vector2(860f, 40f);
        _shopTipText.color = new Color(0.9f, 0.95f, 1f, 1f);

        CreateButton("ShopBack", _shopPanel, "返回", new Vector2(180f, 64f), new Vector2(330f, -230f), new Color(0.36f, 0.4f, 0.46f, 1f), ShowMainPanel);
    }

    private void BuildPlantCard(Transform parent, Vector2 anchoredPos)
    {
        var card = CreateRect("PlantCard", parent);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(270f, 190f);
        card.anchoredPosition = anchoredPos;
        AddImage(card.gameObject, new Color(0.2f, 0.25f, 0.31f, 1f));

        var t = CreateText("Title", card, "植物", 25, TextAnchor.UpperCenter);
        t.rectTransform.anchoredPosition = new Vector2(0f, -22f);
        t.color = new Color(0.93f, 0.97f, 1f, 1f);

        var d = CreateText("Desc", card, string.Empty, 20, TextAnchor.MiddleCenter);
        d.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        d.rectTransform.sizeDelta = new Vector2(230f, 54f);
        d.color = new Color(0.85f, 0.91f, 0.96f, 0.95f);

        Button buy = CreateButton("Buy", card, "购买", new Vector2(190f, 52f), new Vector2(0f, -56f), new Color(0.24f, 0.6f, 0.32f, 1f), () => { });

        ShopCard shopCard = new ShopCard
        {
            Root = card,
            PlantId = string.Empty,
            TitleText = t,
            BuyButton = buy,
            DescText = d
        };

        buy.onClick.RemoveAllListeners();
        buy.onClick.AddListener(() => TryBuyPlant(shopCard.PlantId));
        _shopCards.Add(shopCard);
    }

    private void BuildDailyPanel(RectTransform root)
    {
        _dailyPanel = CreatePanel(root, "DailyPanel", new Vector2(860f, 540f));
        var title = CreateText("DailyTitle", _dailyPanel, "任务中心", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -44f);

        var info = CreateText("DailyInfo", _dailyPanel, "每日 00:00 刷新任务", 26, TextAnchor.MiddleCenter);
        info.rectTransform.anchoredPosition = new Vector2(0f, 170f);
        info.color = new Color(0.92f, 0.96f, 1f, 1f);

        BuildDailyTaskRow(
            _dailyPanel,
            "TaskLogin",
            "每日登录",
            "登录游戏 1 次",
            DailyTaskSystem.LoginRewardCoins,
            new Vector2(0f, 72f),
            ClaimDailyReward,
            out _dailyClaimButton,
            out _dailyLoginStateText);

        BuildDailyTaskRow(
            _dailyPanel,
            "TaskBattle",
            "每日完成一局",
            "通关任意关卡 1 次",
            DailyTaskSystem.BattleRewardCoins,
            new Vector2(0f, -66f),
            ClaimDailyBattleReward,
            out _dailyBattleClaimButton,
            out _dailyBattleStateText);

        _dailyTipText = CreateText("DailyTip", _dailyPanel, string.Empty, 24, TextAnchor.MiddleCenter);
        _dailyTipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _dailyTipText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _dailyTipText.rectTransform.pivot = new Vector2(0.5f, 0f);
        _dailyTipText.rectTransform.anchoredPosition = new Vector2(0f, 64f);
        _dailyTipText.rectTransform.sizeDelta = new Vector2(740f, 40f);
        _dailyTipText.color = new Color(0.9f, 0.96f, 1f, 1f);

        _dailyCloseButton = CreateButton("DailyClose", _dailyPanel, "关闭", new Vector2(180f, 64f), new Vector2(0f, -218f), new Color(0.36f, 0.4f, 0.46f, 1f), TryCloseDailyPanel);
    }

    private void BuildAtlasPanel(RectTransform root)
    {
        _atlasPanel = CreatePanel(root, "AtlasPanel", new Vector2(1240f, 730f));
        var title = CreateText("AtlasTitle", _atlasPanel, "图鉴中心", 40, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -38f);

        _atlasPlantTabButton = CreateButton("AtlasPlantTab", _atlasPanel, "植物图册", new Vector2(180f, 62f), new Vector2(-150f, 266f), new Color(0.26f, 0.58f, 0.36f, 1f), ShowPlantAtlas);
        _atlasMonsterTabButton = CreateButton("AtlasMonsterTab", _atlasPanel, "怪物图册", new Vector2(180f, 62f), new Vector2(50f, 266f), new Color(0.62f, 0.34f, 0.28f, 1f), ShowMonsterAtlas);

        _atlasCards.Clear();
        for (int i = 0; i < 8; i++)
        {
            int row = i / 4;
            int col = i % 4;
            float x = -430f + col * 286f;
            float y = 90f - row * 248f;

            var card = CreateRect("AtlasCard_" + i, _atlasPanel);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(252f, 224f);
            card.anchoredPosition = new Vector2(x, y);
            AddImage(card.gameObject, new Color(0.2f, 0.25f, 0.31f, 1f));

            var photoFrame = CreateRect("PhotoFrame", card);
            photoFrame.anchorMin = new Vector2(0.5f, 1f);
            photoFrame.anchorMax = new Vector2(0.5f, 1f);
            photoFrame.pivot = new Vector2(0.5f, 1f);
            photoFrame.sizeDelta = new Vector2(216f, 118f);
            photoFrame.anchoredPosition = new Vector2(0f, -12f);
            AddImage(photoFrame.gameObject, new Color(0.08f, 0.1f, 0.14f, 1f));

            var photoRect = CreateRect("Photo", photoFrame);
            photoRect.anchorMin = new Vector2(0.5f, 0.5f);
            photoRect.anchorMax = new Vector2(0.5f, 0.5f);
            photoRect.pivot = new Vector2(0.5f, 0.5f);
            photoRect.sizeDelta = new Vector2(206f, 108f);
            Image photoImage = AddImage(photoRect.gameObject, new Color(0.35f, 0.35f, 0.35f, 1f));
            photoImage.preserveAspect = true;

            Text cardTitle = CreateText("Title", card, "--", 24, TextAnchor.MiddleCenter);
            cardTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cardTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cardTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cardTitle.rectTransform.sizeDelta = new Vector2(230f, 34f);
            cardTitle.rectTransform.anchoredPosition = new Vector2(0f, -28f);
            cardTitle.color = new Color(0.94f, 0.97f, 1f, 1f);

            Text desc = CreateText("Desc", card, string.Empty, 18, TextAnchor.UpperLeft);
            desc.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            desc.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            desc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            desc.rectTransform.sizeDelta = new Vector2(226f, 66f);
            desc.rectTransform.anchoredPosition = new Vector2(0f, -82f);
            desc.color = new Color(0.82f, 0.9f, 0.96f, 0.96f);

            _atlasCards.Add(new AtlasCard
            {
                Root = card,
                PhotoImage = photoImage,
                TitleText = cardTitle,
                DescText = desc
            });
        }

        _atlasPrevPageButton = CreateButton("AtlasPrevPage", _atlasPanel, "上一页", new Vector2(160f, 58f), new Vector2(-210f, -298f), new Color(0.36f, 0.48f, 0.66f, 1f), PrevAtlasPage);
        _atlasNextPageButton = CreateButton("AtlasNextPage", _atlasPanel, "下一页", new Vector2(160f, 58f), new Vector2(-20f, -298f), new Color(0.36f, 0.48f, 0.66f, 1f), NextAtlasPage);

        _atlasPageText = CreateText("AtlasPageText", _atlasPanel, "1 / 1", 24, TextAnchor.MiddleCenter);
        _atlasPageText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _atlasPageText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _atlasPageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _atlasPageText.rectTransform.sizeDelta = new Vector2(140f, 40f);
        _atlasPageText.rectTransform.anchoredPosition = new Vector2(-115f, -238f);
        _atlasPageText.color = new Color(0.88f, 0.94f, 1f, 1f);

        _atlasTipText = CreateText("AtlasTip", _atlasPanel, "图册照片为系统生成示意图", 24, TextAnchor.MiddleLeft);
        _atlasTipText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _atlasTipText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _atlasTipText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _atlasTipText.rectTransform.sizeDelta = new Vector2(640f, 40f);
        _atlasTipText.rectTransform.anchoredPosition = new Vector2(204f, -238f);
        _atlasTipText.color = new Color(0.9f, 0.95f, 1f, 1f);

        CreateButton("AtlasBack", _atlasPanel, "返回", new Vector2(180f, 64f), new Vector2(520f, -298f), new Color(0.36f, 0.4f, 0.46f, 1f), ShowMainPanel);
    }

    private void BuildDailyTaskRow(
        RectTransform parent,
        string rowName,
        string taskTitle,
        string taskDesc,
        int rewardCoins,
        Vector2 anchoredPos,
        UnityEngine.Events.UnityAction onClaimClick,
        out Button claimButton,
        out Text stateText)
    {
        var row = CreateRect(rowName, parent);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(760f, 114f);
        row.anchoredPosition = anchoredPos;
        AddImage(row.gameObject, new Color(0.2f, 0.26f, 0.32f, 1f));

        var title = CreateText("TaskTitle", row, taskTitle, 30, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        title.rectTransform.pivot = new Vector2(0f, 0.5f);
        title.rectTransform.anchoredPosition = new Vector2(22f, 26f);
        title.rectTransform.sizeDelta = new Vector2(340f, 34f);
        title.color = new Color(0.95f, 0.98f, 1f, 1f);

        var desc = CreateText("TaskDesc", row, taskDesc, 22, TextAnchor.MiddleLeft);
        desc.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        desc.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        desc.rectTransform.pivot = new Vector2(0f, 0.5f);
        desc.rectTransform.anchoredPosition = new Vector2(22f, -2f);
        desc.rectTransform.sizeDelta = new Vector2(360f, 30f);
        desc.color = new Color(0.84f, 0.9f, 0.96f, 1f);

        stateText = CreateText("TaskState", row, "状态：未完成", 22, TextAnchor.MiddleLeft);
        stateText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        stateText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        stateText.rectTransform.pivot = new Vector2(0f, 0.5f);
        stateText.rectTransform.anchoredPosition = new Vector2(22f, -30f);
        stateText.rectTransform.sizeDelta = new Vector2(360f, 30f);
        stateText.color = new Color(0.8f, 0.85f, 0.9f, 1f);

        claimButton = CreateButton(
            "ClaimButton",
            row,
            "领取 +" + rewardCoins + " 金币",
            new Vector2(260f, 62f),
            new Vector2(222f, 0f),
            new Color(0.27f, 0.62f, 0.33f, 1f),
            onClaimClick);
    }

    private void BuildRechargePanel(RectTransform root)
    {
        _rechargePanel = CreatePanel(root, "RechargePanel", new Vector2(820f, 520f));
        var title = CreateText("RechargeTitle", _rechargePanel, "充值中心", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -44f);

        CreateButton("Pack1", _rechargePanel, "￥6 +100 金币", new Vector2(300f, 78f), new Vector2(0f, 96f), new Color(0.7f, 0.42f, 0.18f, 1f), () => DoRecharge(100));
        CreateButton("Pack2", _rechargePanel, "￥30 +600 金币", new Vector2(300f, 78f), new Vector2(0f, -6f), new Color(0.74f, 0.46f, 0.2f, 1f), () => DoRecharge(600));
        CreateButton("Pack3", _rechargePanel, "￥68 +1500 金币", new Vector2(300f, 78f), new Vector2(0f, -108f), new Color(0.78f, 0.5f, 0.24f, 1f), () => DoRecharge(1500));

        _rechargeTipText = CreateText("RechargeTip", _rechargePanel, string.Empty, 24, TextAnchor.MiddleCenter);
        _rechargeTipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _rechargeTipText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _rechargeTipText.rectTransform.pivot = new Vector2(0.5f, 0f);
        _rechargeTipText.rectTransform.anchoredPosition = new Vector2(0f, 56f);
        _rechargeTipText.rectTransform.sizeDelta = new Vector2(680f, 40f);
        _rechargeTipText.color = new Color(1f, 0.95f, 0.78f, 1f);

        CreateButton("RechargeBack", _rechargePanel, "返回", new Vector2(180f, 64f), new Vector2(0f, -206f), new Color(0.36f, 0.4f, 0.46f, 1f), ShowMainPanel);
    }

    private void BuildSettingsPanel(RectTransform root)
    {
        _settingsPanel = CreatePanel(root, "SettingsPanel", new Vector2(920f, 660f));
        var title = CreateText("SettingsTitle", _settingsPanel, "系统设置", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, -42f);

        var soundRow = CreateRect("SoundRow", _settingsPanel);
        soundRow.anchorMin = new Vector2(0.5f, 0.5f);
        soundRow.anchorMax = new Vector2(0.5f, 0.5f);
        soundRow.pivot = new Vector2(0.5f, 0.5f);
        soundRow.sizeDelta = new Vector2(780f, 98f);
        soundRow.anchoredPosition = new Vector2(0f, 170f);
        AddImage(soundRow.gameObject, new Color(0.2f, 0.26f, 0.32f, 1f));

        var soundLabel = CreateText("SoundLabel", soundRow, "声音", 30, TextAnchor.MiddleLeft);
        soundLabel.rectTransform.offsetMin = new Vector2(24f, 0f);
        soundLabel.rectTransform.offsetMax = new Vector2(-620f, 0f);
        _soundStateText = CreateText("SoundState", soundRow, string.Empty, 24, TextAnchor.MiddleLeft);
        _soundStateText.rectTransform.offsetMin = new Vector2(128f, 0f);
        _soundStateText.rectTransform.offsetMax = new Vector2(-460f, 0f);

        _soundOnButton = CreateButton("SoundOn", soundRow, "打开", new Vector2(130f, 56f), new Vector2(222f, 0f), new Color(0.24f, 0.62f, 0.34f, 1f), EnableSound);
        _soundOffButton = CreateButton("SoundOff", soundRow, "关闭", new Vector2(130f, 56f), new Vector2(366f, 0f), new Color(0.68f, 0.32f, 0.28f, 1f), DisableSound);

        var fullscreenRow = CreateRect("FullscreenRow", _settingsPanel);
        fullscreenRow.anchorMin = new Vector2(0.5f, 0.5f);
        fullscreenRow.anchorMax = new Vector2(0.5f, 0.5f);
        fullscreenRow.pivot = new Vector2(0.5f, 0.5f);
        fullscreenRow.sizeDelta = new Vector2(780f, 98f);
        fullscreenRow.anchoredPosition = new Vector2(0f, 42f);
        AddImage(fullscreenRow.gameObject, new Color(0.2f, 0.26f, 0.32f, 1f));

        var fullscreenLabel = CreateText("FullscreenLabel", fullscreenRow, "显示模式", 30, TextAnchor.MiddleLeft);
        fullscreenLabel.rectTransform.offsetMin = new Vector2(24f, 0f);
        fullscreenLabel.rectTransform.offsetMax = new Vector2(-620f, 0f);
        _fullscreenStateText = CreateText("FullscreenState", fullscreenRow, string.Empty, 24, TextAnchor.MiddleLeft);
        _fullscreenStateText.rectTransform.offsetMin = new Vector2(170f, 0f);
        _fullscreenStateText.rectTransform.offsetMax = new Vector2(-450f, 0f);

        _fullscreenOnButton = CreateButton("FullscreenOn", fullscreenRow, "全屏", new Vector2(130f, 56f), new Vector2(222f, 0f), new Color(0.26f, 0.52f, 0.72f, 1f), EnableFullscreen);
        _fullscreenOffButton = CreateButton("FullscreenOff", fullscreenRow, "窗口", new Vector2(130f, 56f), new Vector2(366f, 0f), new Color(0.44f, 0.5f, 0.68f, 1f), DisableFullscreen);

        var qualityRow = CreateRect("QualityRow", _settingsPanel);
        qualityRow.anchorMin = new Vector2(0.5f, 0.5f);
        qualityRow.anchorMax = new Vector2(0.5f, 0.5f);
        qualityRow.pivot = new Vector2(0.5f, 0.5f);
        qualityRow.sizeDelta = new Vector2(780f, 98f);
        qualityRow.anchoredPosition = new Vector2(0f, -86f);
        AddImage(qualityRow.gameObject, new Color(0.2f, 0.26f, 0.32f, 1f));

        var qualityLabel = CreateText("QualityLabel", qualityRow, "清晰度", 30, TextAnchor.MiddleLeft);
        qualityLabel.rectTransform.offsetMin = new Vector2(24f, 0f);
        qualityLabel.rectTransform.offsetMax = new Vector2(-620f, 0f);
        _qualityStateText = CreateText("QualityState", qualityRow, string.Empty, 24, TextAnchor.MiddleLeft);
        _qualityStateText.rectTransform.offsetMin = new Vector2(170f, 0f);
        _qualityStateText.rectTransform.offsetMax = new Vector2(-450f, 0f);

        _qualityPrevButton = CreateButton("QualityPrev", qualityRow, "上一档", new Vector2(130f, 56f), new Vector2(222f, 0f), new Color(0.26f, 0.52f, 0.72f, 1f), PrevQualityLevel);
        _qualityNextButton = CreateButton("QualityNext", qualityRow, "下一档", new Vector2(130f, 56f), new Vector2(366f, 0f), new Color(0.26f, 0.52f, 0.72f, 1f), NextQualityLevel);

        var resolutionRow = CreateRect("ResolutionRow", _settingsPanel);
        resolutionRow.anchorMin = new Vector2(0.5f, 0.5f);
        resolutionRow.anchorMax = new Vector2(0.5f, 0.5f);
        resolutionRow.pivot = new Vector2(0.5f, 0.5f);
        resolutionRow.sizeDelta = new Vector2(780f, 98f);
        resolutionRow.anchoredPosition = new Vector2(0f, -214f);
        AddImage(resolutionRow.gameObject, new Color(0.2f, 0.26f, 0.32f, 1f));

        var resolutionLabel = CreateText("ResolutionLabel", resolutionRow, "分辨率", 30, TextAnchor.MiddleLeft);
        resolutionLabel.rectTransform.offsetMin = new Vector2(24f, 0f);
        resolutionLabel.rectTransform.offsetMax = new Vector2(-620f, 0f);
        _resolutionStateText = CreateText("ResolutionState", resolutionRow, string.Empty, 24, TextAnchor.MiddleLeft);
        _resolutionStateText.rectTransform.offsetMin = new Vector2(170f, 0f);
        _resolutionStateText.rectTransform.offsetMax = new Vector2(-450f, 0f);

        _resolutionPrevButton = CreateButton("ResolutionPrev", resolutionRow, "上一个", new Vector2(130f, 56f), new Vector2(222f, 0f), new Color(0.44f, 0.5f, 0.68f, 1f), PrevResolution);
        _resolutionNextButton = CreateButton("ResolutionNext", resolutionRow, "下一个", new Vector2(130f, 56f), new Vector2(366f, 0f), new Color(0.44f, 0.5f, 0.68f, 1f), NextResolution);

        _settingsTipText = CreateText("SettingsTip", _settingsPanel, string.Empty, 24, TextAnchor.MiddleCenter);
        _settingsTipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _settingsTipText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _settingsTipText.rectTransform.pivot = new Vector2(0.5f, 0f);
        _settingsTipText.rectTransform.anchoredPosition = new Vector2(0f, 62f);
        _settingsTipText.rectTransform.sizeDelta = new Vector2(780f, 40f);
        _settingsTipText.color = new Color(0.9f, 0.95f, 1f, 1f);

        CreateButton("SettingsBack", _settingsPanel, "返回", new Vector2(180f, 64f), new Vector2(0f, -270f), new Color(0.36f, 0.4f, 0.46f, 1f), ShowMainPanel);
    }

    private RectTransform CreatePanel(RectTransform root, string name, Vector2 size)
    {
        var panel = CreateRect(name, root);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = size;
        panel.anchoredPosition = new Vector2(0f, -10f);
        AddImage(panel.gameObject, new Color(0.14f, 0.19f, 0.24f, 0.96f));
        return panel;
    }

    private Text CreateBottomTip(RectTransform root, string name, Color color)
    {
        Text tip = CreateText(name, root, string.Empty, 26, TextAnchor.MiddleCenter);
        tip.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        tip.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        tip.rectTransform.pivot = new Vector2(0.5f, 0f);
        tip.rectTransform.anchoredPosition = new Vector2(0f, 24f);
        tip.rectTransform.sizeDelta = new Vector2(860f, 44f);
        tip.color = color;
        return tip;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var rt = CreateRect(name, parent);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var image = AddImage(rt.gameObject, color);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var text = CreateText("Text", rt, label, 28, TextAnchor.MiddleCenter);
        text.color = Color.white;
        return button;
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

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
