using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // Core state, nested data structures, and lifecycle entry points.
    private static int s_CurrentStage = 1;
    private static bool s_ShowLobbyOnLoad = true;

    private const int Cols = 17;
    private const int Rows = 11;
    private const int MaxTowerLevel = 3;
    private const int PlantKindCount = 50;
    private const int EnemyKindCount = 50;
    private const int PlantKindsPerPage = 5;
    private const int LineupPlantsPerPage = 10;
    private const int RequiredLineupCount = 7;
    private const int BossWave1 = 10;
    private const int BossWave2 = 15;
    private const int VictoryWave = 15;
    private const float CellSize = 56f;
    private const float TowerAttackAnimDuration = 0.18f;
    private const float EnemyHitFlashDuration = 0.12f;
    private const float HitFxDuration = 0.18f;
    private const float ProjectileMaxLife = 2.2f;
    private const string PeaSpriteResourcePath = "Towers/Pea";
    private const string IceSpriteResourcePath = "Towers/Ice";

    private enum TowerType
    {
        Pea,
        Ice,
        Fire,
        Cannon,
        Poison
    }

    private struct TowerProfile
    {
        public int BuildCost;
        public int UpgradeBaseCost;
        public float Damage;
        public float Range;
        public float FireInterval;
        public float SlowTime;

        public TowerProfile(int buildCost, int upgradeBaseCost, float damage, float range, float fireInterval, float slowTime)
        {
            BuildCost = buildCost;
            UpgradeBaseCost = upgradeBaseCost;
            Damage = damage;
            Range = range;
            FireInterval = fireInterval;
            SlowTime = slowTime;
        }
    }

    private struct TowerRuntime
    {
        public float Damage;
        public float Range;
        public float FireInterval;
        public float SlowTime;

        public TowerRuntime(float damage, float range, float fireInterval, float slowTime)
        {
            Damage = damage;
            Range = range;
            FireInterval = fireInterval;
            SlowTime = slowTime;
        }
    }

    private struct WaveProfile
    {
        public int Count;
        public float Hp;
        public float Speed;
        public float SpawnGap;
        public int Reward;

        public WaveProfile(int count, float hp, float speed, float spawnGap, int reward)
        {
            Count = count;
            Hp = hp;
            Speed = speed;
            SpawnGap = spawnGap;
            Reward = reward;
        }
    }

    private sealed class CellData
    {
        public Vector2 Position;
        public bool IsPath;
        public Image View;
        public TowerData Tower;
        public ObstacleData Obstacle;
    }

    private sealed class TowerData
    {
        public TowerType Type;
        public int KindId;
        public int Level;
        public float Hp;
        public RectTransform View;
        public TowerModelData Model;
        public Text Label;
        public float Cooldown;
        public float IdlePhase;
        public float AttackAnimTimer;
    }

    private sealed class ObstacleData
    {
        public RectTransform View;
        public int ClearCost;
        public int Bonus;
    }

    private sealed class TowerModelData
    {
        public RectTransform Root;
        public RectTransform Shadow;
        public RectTransform Base;
        public RectTransform HeadPivot;
        public RectTransform Head;
        public RectTransform Barrel;
        public RectTransform Muzzle;
        public RectTransform Flash;
        public Image ShadowImage;
        public Image BaseImage;
        public Image HeadImage;
        public Image BarrelImage;
        public Image FlashImage;
        public float BaseBarrelLength;
    }

    private sealed class EnemyData
    {
        public RectTransform View;
        public EnemyModelData Model;
        public int KindId;
        public int BossTypeId;
        public float Hp;
        public float MaxHp;
        public float BaseSpeed;
        public float Speed;
        public int Reward;
        public int NextNode;
        public float SlowTimer;
        public float HitFlashTimer;
        public bool IsBoss;
        public bool IsFinalBoss;
        public bool EnrageTriggered;
        public bool HealTriggered;
        public bool SummonTriggered;
        public bool CanAttackTower;
        public float TowerAttackDamage;
        public float TowerAttackRange;
        public float TowerAttackInterval;
        public float TowerAttackCooldown;
    }

    private sealed class EnemyModelData
    {
        public RectTransform Root;
        public RectTransform Shadow;
        public RectTransform Body;
        public RectTransform Core;
        public RectTransform Crown;
        public RectTransform HealthBar;
        public Image ShadowImage;
        public Image BodyImage;
        public Image CoreImage;
        public Image CrownImage;
        public Image HpBackImage;
        public Image HpFillImage;
        public Color BodyBaseColor;
        public Color CoreBaseColor;
        public Color CrownBaseColor;
        public float HealthFillMaxWidth;
    }

    private sealed class ProjectileData
    {
        public RectTransform View;
        public TowerType Type;
        public int KindId;
        public EnemyData Target;
        public float Damage;
        public float SlowTime;
        public float Speed;
        public float Life;
    }

    private sealed class ImpactFxData
    {
        public RectTransform View;
        public Image Image;
        public float Timer;
        public float Duration;
        public float StartSize;
        public float EndSize;
        public Color BaseColor;
    }

    private sealed class PlantKindData
    {
        public int KindId;
        public string DisplayName;
        public TowerType Family;
        public int BuildCost;
        public int UpgradeBaseCost;
        public float DamageMul;
        public float RangeBonus;
        public float FireIntervalMul;
        public float SlowTimeBonus;
        public string TraitName;
        public float CritChance;
        public float CritDamageMul;
        public float BossDamageMul;
        public float SplashRadius;
        public float SplashDamageMul;
        public float ExecuteHpRatio;
        public float ArmorBreakRatio;
    }

    private sealed class EnemyKindData
    {
        public int KindId;
        public string DisplayName;
        public float HpMul;
        public float SpeedMul;
        public float RewardMul;
        public float SizeMul;
        public Color BodyColor;
    }

    private sealed class BossTypeData
    {
        public int BossTypeId;
        public string Name;
        public Color BodyColor;
    }

    private readonly List<Vector2Int> _path = new List<Vector2Int>
    {
        new Vector2Int(0, 8),
        new Vector2Int(1, 8),
        new Vector2Int(2, 8),
        new Vector2Int(3, 8),
        new Vector2Int(4, 8),
        new Vector2Int(5, 8),
        new Vector2Int(6, 8),
        new Vector2Int(7, 8),
        new Vector2Int(8, 8),
        new Vector2Int(9, 8),
        new Vector2Int(10, 8),
        new Vector2Int(11, 8),
        new Vector2Int(12, 8),
        new Vector2Int(13, 8),
        new Vector2Int(14, 8),
        new Vector2Int(15, 8),
        new Vector2Int(16, 8),
        new Vector2Int(16, 7),
        new Vector2Int(16, 6),
        new Vector2Int(15, 6),
        new Vector2Int(14, 6),
        new Vector2Int(13, 6),
        new Vector2Int(12, 6),
        new Vector2Int(11, 6),
        new Vector2Int(10, 6),
        new Vector2Int(9, 6),
        new Vector2Int(8, 6),
        new Vector2Int(7, 6),
        new Vector2Int(6, 6),
        new Vector2Int(5, 6),
        new Vector2Int(4, 6),
        new Vector2Int(3, 6),
        new Vector2Int(2, 6),
        new Vector2Int(1, 6),
        new Vector2Int(0, 6),
        new Vector2Int(0, 5),
        new Vector2Int(0, 4),
        new Vector2Int(1, 4),
        new Vector2Int(2, 4),
        new Vector2Int(3, 4),
        new Vector2Int(4, 4),
        new Vector2Int(5, 4),
        new Vector2Int(6, 4),
        new Vector2Int(7, 4),
        new Vector2Int(8, 4),
        new Vector2Int(8, 5)
    };

    private readonly List<Vector2Int> _obstacleLayout = new List<Vector2Int>
    {
        new Vector2Int(1, 1),
        new Vector2Int(2, 2),
        new Vector2Int(3, 9),
        new Vector2Int(4, 2),
        new Vector2Int(5, 9),
        new Vector2Int(6, 2),
        new Vector2Int(7, 9),
        new Vector2Int(9, 2),
        new Vector2Int(10, 9),
        new Vector2Int(11, 2),
        new Vector2Int(12, 9),
        new Vector2Int(13, 2),
        new Vector2Int(14, 9),
        new Vector2Int(15, 2)
    };

    // Lightweight level config: first waves are explicit, later waves auto-scale.
    private readonly List<WaveProfile> _levelConfig = new List<WaveProfile>
    {
        new WaveProfile(6, 60f, 78f, 0.75f, 11),
        new WaveProfile(8, 78f, 84f, 0.70f, 12),
        new WaveProfile(10, 96f, 88f, 0.66f, 13),
        new WaveProfile(12, 118f, 92f, 0.62f, 14),
        new WaveProfile(14, 142f, 96f, 0.58f, 15)
    };

    private readonly List<Vector2> _pathPoints = new List<Vector2>();
    private readonly List<TowerData> _towers = new List<TowerData>();
    private readonly List<EnemyData> _enemies = new List<EnemyData>();
    private readonly List<ProjectileData> _projectiles = new List<ProjectileData>();
    private readonly List<ImpactFxData> _impactFx = new List<ImpactFxData>();
    private readonly List<PlantKindData> _plantKinds = new List<PlantKindData>();
    private readonly List<EnemyKindData> _enemyKinds = new List<EnemyKindData>();
    private readonly List<BossTypeData> _bossTypes = new List<BossTypeData>();
    private readonly List<int> _battlePlantKindIds = new List<int>();
    private readonly List<int> _lineupPickPlantKindIds = new List<int>();
    private readonly List<int> _matchEnemyKindPool = new List<int>();

    private readonly Color _pathColor = new Color(0.84f, 0.67f, 0.48f, 1f);
    private readonly Color _grassColor = new Color(0.45f, 0.46f, 0.22f, 1f);
    private readonly Color _blockedCellColor = new Color(0.30f, 0.33f, 0.18f, 1f);
    private readonly Color _builtCellColor = new Color(0.35f, 0.42f, 0.24f, 1f);
    private readonly Color _peaColor = new Color(0.12f, 0.72f, 0.24f, 1f);
    private readonly Color _iceColor = new Color(0.22f, 0.68f, 0.88f, 1f);
    private readonly Color _enemyColor = new Color(0.88f, 0.23f, 0.23f, 1f);
    private readonly Color _bossColor = new Color(0.62f, 0.18f, 0.68f, 1f);
    private readonly Color _bossFinalColor = new Color(0.86f, 0.16f, 0.52f, 1f);

    [SerializeField] private Sprite _peaTowerSprite;
    [SerializeField] private Sprite _iceTowerSprite;

    private CellData[,] _cells;
    private RectTransform _board;
    private Font _font;

    private Text _goldText;
    private Text _waveText;
    private Text _lifeText;
    private Text _scoreText;
    private Text _tipText;

    private Button _peaButton;
    private Button _iceButton;
    private Button _fireButton;
    private Button _cannonButton;
    private Button _poisonButton;
    private Button _plantPrevButton;
    private Button _plantNextButton;
    private Button _pauseButton;
    private Button _x1Button;
    private Button _x2Button;
    private Button _lineupPrevPageButton;
    private Button _lineupNextPageButton;
    private Button _lineupConfirmButton;

    private RectTransform _lineupPanel;
    private RectTransform _battlePopupPanel;
    private Text _lineupSelectedText;
    private Text _lineupTipText;
    private Text _lineupDetailText;
    private Text _battlePopupTitleText;
    private Text _battlePopupContentText;
    private readonly List<Button> _lineupPlantButtons = new List<Button>();

    private Button _battlePopupPrimaryButton;
    private Button _battlePopupSecondaryButton;
    private UnityEngine.Events.UnityAction _battlePopupPrimaryAction;
    private UnityEngine.Events.UnityAction _battlePopupSecondaryAction;

    private TowerType _selectedTower = TowerType.Pea;
    private int _selectedPlantKindId = 1;
    private int _plantPage;
    private int _lineupPage;
    private int _lineupPreviewKindId;
    private int _stageBossTypeId = 1;
    private bool _matchReady;
    private WaveProfile _activeWave;

    private int _gold = 150;
    private int _life = 12;
    private int _wave;
    private int _score;
    private int _stage = 1;
    private int _pendingSpawn;
    private bool _bossSpawnPending;

    private float _spawnTimer;
    private float _nextWaveTimer;
    private float _tipTimer;
    private float _speed = 1f;

    private bool _paused;
    private bool _gameOver;
    private bool _battlePopupVisible;
    private bool _battlePopupPausedTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (s_ShowLobbyOnLoad) return;
        if (FindObjectOfType<CarrotDefenseUGUI>() != null) return;
        new GameObject(nameof(CarrotDefenseUGUI)).AddComponent<CarrotDefenseUGUI>();
    }

    public static bool ShouldShowLobbyOnLoad()
    {
        return s_ShowLobbyOnLoad;
    }

    public static void EnterLobbyOnNextLoad()
    {
        s_ShowLobbyOnLoad = true;
        s_CurrentStage = 1;
    }

    public static void StartBattleOnNextLoad(int stage = 1)
    {
        GameMetaData.EnsureInit();
        s_ShowLobbyOnLoad = false;
        s_CurrentStage = Mathf.Clamp(stage, 1, GameMetaData.MaxStage);
    }

    private void Start()
    {
        // 初始化关卡状态，并构建整套战斗 UI/地图。
        SystemSettingsData.ApplySavedSettings();
        GameMetaData.EnsureInit();
        Time.timeScale = 1f;
        _stage = Mathf.Clamp(s_CurrentStage, 1, GameMetaData.MaxStage);
        _gold = 150 + (_stage - 1) * 35;
        LoadTowerSpritesIfNeeded();
        EnsurePlantKinds();
        EnsureEnemyKinds();
        EnsureBossTypes();
        EnsureBattlePlantDefaults();
        _life = 12;
        _score = 0;
        EnsureEventSystem();
        BuildUi();
        BuildGrid();
        BuildCarrot();
        BuildObstacles();

        _matchReady = false;
        SelectPlantKindById(_selectedPlantKindId);
        SetSpeed(1f);
        RefreshHud();
        ShowPlantLineupPanel();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // 游戏主循环：先处理波次和战斗，再更新提示文案。
        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(2f);

        if (_gameOver)
        {
            UpdateTip(Time.unscaledDeltaTime);
            return;
        }

        if (!_matchReady)
        {
            UpdateTip(Time.unscaledDeltaTime);
            return;
        }

        float dt = Time.deltaTime;
        UpdateWave(dt);
        UpdateEnemies(dt);
        UpdateTowers(dt);
        UpdateProjectiles(dt);
        UpdateImpactEffects(dt);
        UpdateTip(Time.unscaledDeltaTime);
    }

}
