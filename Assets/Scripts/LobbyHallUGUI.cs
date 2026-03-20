using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class LobbyHallUGUI : MonoBehaviour
{
    private sealed class ShopCard
    {
        public RectTransform Root;
        public string PlantId;
        public Text TitleText;
        public Button BuyButton;
        public Text DescText;
    }

    private sealed class AtlasCard
    {
        public RectTransform Root;
        public Image PhotoImage;
        public Text TitleText;
        public Text DescText;
    }

    private const string KeyFriendGiftDate = "lobby_friend_gift_date_v1";
    private const int FriendGiftCoins = 90;
    private const int ShopCardsPerPage = 6;
    private const int EnemyAtlasCount = 50;
    private const int BossAtlasCount = 5;

    private static readonly string[] s_EnemyAtlasPrefix =
    {
        "裂喉", "腐爪", "赤眸", "霜牙", "铁背",
        "影袭", "瘴骨", "雷吻", "渊鳞", "噬火"
    };

    private static readonly string[] s_EnemyAtlasSuffix =
    {
        "行者", "猎犬", "狂徒", "术士", "卫士"
    };

    private static readonly string[] s_BossAtlasNames =
    {
        "裂蹄战王", "腐沼母后", "钢甲暴君", "雷鸣先驱", "深渊主宰"
    };

    private Font _font;
    private CanvasGroup _canvasGroup;

    private RectTransform _mainPanel;
    private RectTransform _levelPanel;
    private RectTransform _friendPanel;
    private RectTransform _shopPanel;
    private RectTransform _dailyPanel;
    private RectTransform _rechargePanel;
    private RectTransform _settingsPanel;
    private RectTransform _atlasPanel;

    private Text _coinText;
    private Text _mainTipText;
    private Text _friendTipText;
    private Text _shopTipText;
    private Text _dailyTipText;
    private Text _rechargeTipText;
    private Text _settingsTipText;
    private Text _dailyLoginStateText;
    private Text _dailyBattleStateText;
    private Text _soundStateText;
    private Text _fullscreenStateText;
    private Text _qualityStateText;
    private Text _resolutionStateText;
    private Text _shopPageText;
    private Text _atlasPageText;
    private Text _atlasTipText;

    private Button _dailyClaimButton;
    private Button _dailyBattleClaimButton;
    private Button _dailyCloseButton;
    private Button _friendGiftButton;
    private Button _soundOnButton;
    private Button _soundOffButton;
    private Button _fullscreenOnButton;
    private Button _fullscreenOffButton;
    private Button _qualityPrevButton;
    private Button _qualityNextButton;
    private Button _resolutionPrevButton;
    private Button _resolutionNextButton;
    private Button _shopPrevPageButton;
    private Button _shopNextPageButton;
    private Button _atlasPlantTabButton;
    private Button _atlasMonsterTabButton;
    private Button _atlasPrevPageButton;
    private Button _atlasNextPageButton;

    private Resolution[] _resolutionOptions = new Resolution[0];
    private int _currentResolutionIndex = -1;

    private readonly List<Button> _stageButtons = new List<Button>();
    private readonly List<ShopCard> _shopCards = new List<ShopCard>();
    private readonly List<AtlasCard> _atlasCards = new List<AtlasCard>();
    private readonly Dictionary<string, Sprite> _atlasPhotoCache = new Dictionary<string, Sprite>();
    private string[] _allShopPlantIds = new string[0];
    private int _shopPage;
    private int _atlasPage;
    private bool _atlasShowPlant = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (!CarrotDefenseUGUI.ShouldShowLobbyOnLoad()) return;
        if (FindObjectOfType<LobbyHallUGUI>() != null) return;
        new GameObject(nameof(LobbyHallUGUI)).AddComponent<LobbyHallUGUI>();
    }

    private void Start()
    {
        SystemSettingsData.ApplySavedSettings();
        GameMetaData.EnsureInit();
        DailyTaskSystem.MarkLoginCompleted();
        EnsureEventSystem();
        BuildUi();
        InitSettingsRuntime();
        RefreshAllViews();
        ShowPanel(_mainPanel);

        if (DailyTaskSystem.HasAnyClaimableRewards())
        {
            ShowPanel(_dailyPanel);
        }
    }

    private static string TodayKey()
    {
        return System.DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static bool CanClaimDaily()
    {
        return DailyTaskSystem.CanClaimLoginReward();
    }

    private static bool CanClaimDailyBattle()
    {
        return DailyTaskSystem.CanClaimBattleReward();
    }

    private static bool CanClaimFriendGift()
    {
        return PlayerPrefs.GetString(KeyFriendGiftDate, string.Empty) != TodayKey();
    }

    private void OnDestroy()
    {
        foreach (var pair in _atlasPhotoCache)
        {
            Sprite sprite = pair.Value;
            if (sprite == null) continue;
            Texture2D tex = sprite.texture;
            Destroy(sprite);
            if (tex != null) Destroy(tex);
        }
        _atlasPhotoCache.Clear();
    }
}
