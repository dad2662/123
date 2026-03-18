using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CarrotDefenseUGUI : MonoBehaviour
{
    private const int Cols = 12;
    private const int Rows = 7;
    private const int MaxTowerLevel = 3;
    private const float CellSize = 62f;

    private enum TowerType
    {
        Pea,
        Ice
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
        public int Level;
        public RectTransform View;
        public Text Label;
        public float Cooldown;
    }

    private sealed class ObstacleData
    {
        public RectTransform View;
        public int ClearCost;
        public int Bonus;
    }

    private sealed class EnemyData
    {
        public RectTransform View;
        public float Hp;
        public float Speed;
        public int Reward;
        public int NextNode;
        public float SlowTimer;
    }

    private readonly List<Vector2Int> _path = new List<Vector2Int>
    {
        new Vector2Int(0, 3),
        new Vector2Int(1, 3),
        new Vector2Int(2, 3),
        new Vector2Int(2, 2),
        new Vector2Int(2, 1),
        new Vector2Int(3, 1),
        new Vector2Int(4, 1),
        new Vector2Int(5, 1),
        new Vector2Int(5, 2),
        new Vector2Int(5, 3),
        new Vector2Int(6, 3),
        new Vector2Int(7, 3),
        new Vector2Int(8, 3),
        new Vector2Int(8, 4),
        new Vector2Int(8, 5),
        new Vector2Int(9, 5),
        new Vector2Int(10, 5),
        new Vector2Int(11, 5)
    };

    private readonly List<Vector2Int> _obstacleLayout = new List<Vector2Int>
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 5),
        new Vector2Int(3, 3),
        new Vector2Int(4, 4),
        new Vector2Int(6, 1),
        new Vector2Int(6, 5),
        new Vector2Int(7, 4),
        new Vector2Int(9, 2),
        new Vector2Int(10, 3)
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

    private readonly Color _pathColor = new Color(0.62f, 0.44f, 0.24f, 1f);
    private readonly Color _grassColor = new Color(0.22f, 0.47f, 0.22f, 1f);
    private readonly Color _blockedCellColor = new Color(0.17f, 0.30f, 0.17f, 1f);
    private readonly Color _builtCellColor = new Color(0.15f, 0.34f, 0.19f, 1f);
    private readonly Color _peaColor = new Color(0.12f, 0.72f, 0.24f, 1f);
    private readonly Color _iceColor = new Color(0.22f, 0.68f, 0.88f, 1f);
    private readonly Color _enemyColor = new Color(0.88f, 0.23f, 0.23f, 1f);

    private CellData[,] _cells;
    private RectTransform _board;
    private Font _font;

    private Text _goldText;
    private Text _waveText;
    private Text _lifeText;
    private Text _tipText;

    private Button _peaButton;
    private Button _iceButton;
    private Button _pauseButton;
    private Button _x1Button;
    private Button _x2Button;

    private TowerType _selectedTower = TowerType.Pea;
    private WaveProfile _activeWave;

    private int _gold = 150;
    private int _life = 12;
    private int _wave;
    private int _pendingSpawn;

    private float _spawnTimer;
    private float _nextWaveTimer;
    private float _tipTimer;
    private float _speed = 1f;

    private bool _paused;
    private bool _gameOver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindObjectOfType<CarrotDefenseUGUI>() != null) return;
        new GameObject(nameof(CarrotDefenseUGUI)).AddComponent<CarrotDefenseUGUI>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        BuildUi();
        BuildGrid();
        BuildCarrot();
        BuildObstacles();

        SelectTower(TowerType.Pea);
        SetSpeed(1f);
        BeginWave();
        RefreshHud();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(2f);

        if (_gameOver)
        {
            UpdateTip(Time.unscaledDeltaTime);
            return;
        }

        float dt = Time.deltaTime;
        UpdateWave(dt);
        UpdateEnemies(dt);
        UpdateTowers(dt);
        UpdateTip(Time.unscaledDeltaTime);
    }

    private void BuildUi()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasObj = new GameObject("TDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvas.GetComponent<RectTransform>();
        AddImage(root.gameObject, new Color(0.09f, 0.12f, 0.1f, 1f));

        var hud = CreateRect("HUD", root);
        StretchTop(hud, 94f);
        AddImage(hud.gameObject, new Color(0.13f, 0.17f, 0.14f, 0.96f));

        _goldText = CreateText("GoldText", hud, "Gold: 0", 28, TextAnchor.MiddleLeft);
        _goldText.rectTransform.anchorMin = new Vector2(0f, 0f);
        _goldText.rectTransform.anchorMax = new Vector2(0f, 1f);
        _goldText.rectTransform.pivot = new Vector2(0f, 0.5f);
        _goldText.rectTransform.anchoredPosition = new Vector2(24f, 0f);
        _goldText.rectTransform.sizeDelta = new Vector2(250f, 0f);

        _waveText = CreateText("WaveText", hud, "Wave: 0", 28, TextAnchor.MiddleLeft);
        _waveText.rectTransform.anchorMin = new Vector2(0f, 0f);
        _waveText.rectTransform.anchorMax = new Vector2(0f, 1f);
        _waveText.rectTransform.pivot = new Vector2(0f, 0.5f);
        _waveText.rectTransform.anchoredPosition = new Vector2(280f, 0f);
        _waveText.rectTransform.sizeDelta = new Vector2(220f, 0f);

        _lifeText = CreateText("LifeText", hud, "Carrot: 0", 28, TextAnchor.MiddleLeft);
        _lifeText.rectTransform.anchorMin = new Vector2(0f, 0f);
        _lifeText.rectTransform.anchorMax = new Vector2(0f, 1f);
        _lifeText.rectTransform.pivot = new Vector2(0f, 0.5f);
        _lifeText.rectTransform.anchoredPosition = new Vector2(512f, 0f);
        _lifeText.rectTransform.sizeDelta = new Vector2(260f, 0f);

        _tipText = CreateText("TipText", hud, string.Empty, 24, TextAnchor.MiddleCenter);
        _tipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _tipText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        _tipText.rectTransform.sizeDelta = new Vector2(560f, 0f);
        _tipText.color = new Color(1f, 0.95f, 0.72f, 1f);

        _peaButton = CreateRightButton("PeaButton", hud, "Pea (50)", new Vector2(170f, 56f), new Vector2(-430f, 0f), () => SelectTower(TowerType.Pea));
        _iceButton = CreateRightButton("IceButton", hud, "Ice (70)", new Vector2(170f, 56f), new Vector2(-248f, 0f), () => SelectTower(TowerType.Ice));

        _pauseButton = CreateRightButton("PauseButton", hud, "Pause", new Vector2(110f, 56f), new Vector2(-126f, 0f), TogglePause);
        _x1Button = CreateRightButton("X1Button", hud, "1x", new Vector2(52f, 56f), new Vector2(-66f, 0f), () => SetSpeed(1f));
        _x2Button = CreateRightButton("X2Button", hud, "2x", new Vector2(52f, 56f), new Vector2(0f, 0f), () => SetSpeed(2f));

        var hint = CreateText("Hint", root, "Click empty: build | Click tower: upgrade | Click block: clear", 22, TextAnchor.LowerCenter);
        hint.rectTransform.anchorMin = new Vector2(0f, 0f);
        hint.rectTransform.anchorMax = new Vector2(1f, 0f);
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        hint.rectTransform.sizeDelta = new Vector2(0f, 32f);
        hint.color = new Color(0.88f, 0.95f, 0.88f, 0.9f);

        _board = CreateRect("Board", root);
        _board.anchorMin = new Vector2(0.5f, 0.5f);
        _board.anchorMax = new Vector2(0.5f, 0.5f);
        _board.pivot = new Vector2(0.5f, 0.5f);
        _board.sizeDelta = new Vector2(Cols * CellSize + 12f, Rows * CellSize + 12f);
        _board.anchoredPosition = new Vector2(0f, -16f);
        AddImage(_board.gameObject, new Color(0.14f, 0.22f, 0.14f, 0.98f));
    }

    private void BuildGrid()
    {
        _cells = new CellData[Cols, Rows];
        var pathSet = new HashSet<Vector2Int>(_path);
        _pathPoints.Clear();

        float left = -((Cols - 1) * CellSize) * 0.5f;
        float bottom = -((Rows - 1) * CellSize) * 0.5f;

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                Vector2 position = new Vector2(left + x * CellSize, bottom + y * CellSize);
                bool isPath = pathSet.Contains(new Vector2Int(x, y));

                var cellRt = CreateRect("Cell_" + x + "_" + y, _board);
                cellRt.anchorMin = new Vector2(0.5f, 0.5f);
                cellRt.anchorMax = new Vector2(0.5f, 0.5f);
                cellRt.pivot = new Vector2(0.5f, 0.5f);
                cellRt.sizeDelta = Vector2.one * (CellSize - 4f);
                cellRt.anchoredPosition = position;

                var image = AddImage(cellRt.gameObject, isPath ? _pathColor : _grassColor);
                image.raycastTarget = !isPath;

                var cell = new CellData
                {
                    Position = position,
                    IsPath = isPath,
                    View = image
                };
                _cells[x, y] = cell;

                if (!isPath)
                {
                    var button = cellRt.gameObject.AddComponent<Button>();
                    button.targetGraphic = image;
                    button.transition = Selectable.Transition.ColorTint;
                    CellData captured = cell;
                    button.onClick.AddListener(() => OnCellClicked(captured));
                }
            }
        }

        for (int i = 0; i < _path.Count; i++)
        {
            Vector2Int node = _path[i];
            _pathPoints.Add(_cells[node.x, node.y].Position);
        }
    }

    private void BuildCarrot()
    {
        if (!EnsurePathPoints()) return;

        Vector2 goalPos = _pathPoints[_pathPoints.Count - 1];
        var carrot = CreateRect("Carrot", _board);
        carrot.anchorMin = new Vector2(0.5f, 0.5f);
        carrot.anchorMax = new Vector2(0.5f, 0.5f);
        carrot.pivot = new Vector2(0.5f, 0.5f);
        carrot.sizeDelta = Vector2.one * (CellSize - 12f);
        carrot.anchoredPosition = goalPos;
        AddImage(carrot.gameObject, new Color(0.98f, 0.58f, 0.18f, 1f));

        var text = CreateText("Mark", carrot, "C", 30, TextAnchor.MiddleCenter);
        text.color = new Color(0.2f, 0.12f, 0.06f, 1f);
    }

    private void BuildObstacles()
    {
        for (int i = 0; i < _obstacleLayout.Count; i++)
        {
            Vector2Int pos = _obstacleLayout[i];
            if (pos.x < 0 || pos.x >= Cols || pos.y < 0 || pos.y >= Rows) continue;

            CellData cell = _cells[pos.x, pos.y];
            if (cell.IsPath || cell.Tower != null) continue;

            int clearCost = 28 + (i % 3) * 6;
            int bonus = 8 + (i % 4) * 2;

            var obstacleRt = CreateRect("Obstacle", _board);
            obstacleRt.anchorMin = new Vector2(0.5f, 0.5f);
            obstacleRt.anchorMax = new Vector2(0.5f, 0.5f);
            obstacleRt.pivot = new Vector2(0.5f, 0.5f);
            obstacleRt.sizeDelta = Vector2.one * (CellSize - 22f);
            obstacleRt.anchoredPosition = cell.Position;

            AddImage(obstacleRt.gameObject, new Color(0.46f, 0.31f, 0.2f, 1f));
            var label = CreateText("Label", obstacleRt, "B", 26, TextAnchor.MiddleCenter);
            label.color = new Color(0.16f, 0.08f, 0.03f, 0.9f);

            cell.Obstacle = new ObstacleData
            {
                View = obstacleRt,
                ClearCost = clearCost,
                Bonus = bonus
            };
            cell.View.color = _blockedCellColor;
        }
    }

    private void OnCellClicked(CellData cell)
    {
        if (_gameOver || cell.IsPath) return;

        if (cell.Obstacle != null)
        {
            TryClearObstacle(cell);
            return;
        }

        if (cell.Tower != null)
        {
            TryUpgradeTower(cell.Tower);
            return;
        }

        TryBuildTower(cell);
    }

    private void TryClearObstacle(CellData cell)
    {
        ObstacleData obstacle = cell.Obstacle;
        if (obstacle == null) return;

        if (_gold < obstacle.ClearCost)
        {
            SetTip("Not enough gold to clear", 0.9f);
            return;
        }

        _gold -= obstacle.ClearCost;
        _gold += obstacle.Bonus;

        Destroy(obstacle.View.gameObject);
        cell.Obstacle = null;
        cell.View.color = _grassColor;

        SetTip("Cleared block, got +" + obstacle.Bonus, 1.1f);
        RefreshHud();
    }

    private void TryBuildTower(CellData cell)
    {
        TowerProfile profile = GetTowerProfile(_selectedTower);
        if (_gold < profile.BuildCost)
        {
            SetTip("Not enough gold to build", 0.9f);
            return;
        }

        _gold -= profile.BuildCost;

        var towerRt = CreateRect("Tower", _board);
        towerRt.anchorMin = new Vector2(0.5f, 0.5f);
        towerRt.anchorMax = new Vector2(0.5f, 0.5f);
        towerRt.pivot = new Vector2(0.5f, 0.5f);
        towerRt.sizeDelta = Vector2.one * (CellSize - 16f);
        towerRt.anchoredPosition = cell.Position;
        AddImage(towerRt.gameObject, GetTowerColor(_selectedTower, 1));

        var label = CreateText("Label", towerRt, TowerCode(_selectedTower) + "1", 22, TextAnchor.MiddleCenter);
        label.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var tower = new TowerData
        {
            Type = _selectedTower,
            Level = 1,
            View = towerRt,
            Label = label,
            Cooldown = 0.06f
        };

        cell.Tower = tower;
        cell.View.color = _builtCellColor;
        _towers.Add(tower);

        SetTip("Built " + TowerCode(_selectedTower) + " tower", 0.8f);
        RefreshHud();
    }

    private void TryUpgradeTower(TowerData tower)
    {
        if (tower.Level >= MaxTowerLevel)
        {
            SetTip("Tower is max level", 0.8f);
            return;
        }

        int upgradeCost = GetUpgradeCost(tower);
        if (_gold < upgradeCost)
        {
            SetTip("Not enough gold to upgrade", 0.9f);
            return;
        }

        _gold -= upgradeCost;
        tower.Level++;
        tower.Cooldown = Mathf.Min(tower.Cooldown, 0.05f);

        tower.Label.text = TowerCode(tower.Type) + tower.Level;
        Image towerImage = tower.View.GetComponent<Image>();
        towerImage.color = GetTowerColor(tower.Type, tower.Level);

        SetTip(TowerCode(tower.Type) + " tower Lv." + tower.Level, 0.9f);
        RefreshHud();
    }

    private void UpdateWave(float dt)
    {
        if (_pendingSpawn > 0)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                if (!SpawnEnemy()) return;
                _pendingSpawn--;
                _spawnTimer = _activeWave.SpawnGap;
            }
            return;
        }

        if (_enemies.Count > 0) return;

        _nextWaveTimer -= dt;
        if (_nextWaveTimer <= 0f) BeginWave();
    }

    private void BeginWave()
    {
        _wave++;
        _activeWave = GetWaveProfile(_wave);
        _pendingSpawn = _activeWave.Count;
        _spawnTimer = 0.15f;
        _nextWaveTimer = 2.2f;
        SetTip("Wave " + _wave + " incoming", 1.2f);
        RefreshHud();
    }

    private WaveProfile GetWaveProfile(int wave)
    {
        int idx = wave - 1;
        if (idx >= 0 && idx < _levelConfig.Count) return _levelConfig[idx];

        WaveProfile last = _levelConfig[_levelConfig.Count - 1];
        int extra = wave - _levelConfig.Count;
        return new WaveProfile(
            count: last.Count + extra * 2,
            hp: last.Hp + extra * 18f,
            speed: last.Speed + extra * 4f,
            spawnGap: Mathf.Max(0.28f, last.SpawnGap - extra * 0.02f),
            reward: last.Reward + extra);
    }

    private bool SpawnEnemy()
    {
        if (!EnsurePathPoints()) return false;

        var enemyRt = CreateRect("Enemy", _board);
        enemyRt.anchorMin = new Vector2(0.5f, 0.5f);
        enemyRt.anchorMax = new Vector2(0.5f, 0.5f);
        enemyRt.pivot = new Vector2(0.5f, 0.5f);
        enemyRt.sizeDelta = Vector2.one * (CellSize - 22f);
        enemyRt.anchoredPosition = _pathPoints[0];
        AddImage(enemyRt.gameObject, _enemyColor);

        _enemies.Add(new EnemyData
        {
            View = enemyRt,
            Hp = _activeWave.Hp,
            Speed = _activeWave.Speed,
            Reward = _activeWave.Reward,
            NextNode = 1,
            SlowTimer = 0f
        });
        return true;
    }

    private void UpdateEnemies(float dt)
    {
        if (!EnsurePathPoints()) return;

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            EnemyData enemy = _enemies[i];
            enemy.SlowTimer = Mathf.Max(0f, enemy.SlowTimer - dt);

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
                RefreshHud();
                if (_life <= 0) ShowGameOver();
                continue;
            }

            enemy.View.anchoredPosition = position;
        }
    }

    private void UpdateTowers(float dt)
    {
        for (int i = 0; i < _towers.Count; i++)
        {
            TowerData tower = _towers[i];
            tower.Cooldown -= dt;
            if (tower.Cooldown > 0f) continue;

            TowerRuntime runtime = GetTowerRuntime(tower);
            EnemyData target = FindTarget(tower.View.anchoredPosition, runtime.Range);
            if (target == null) continue;

            tower.Cooldown = runtime.FireInterval;
            target.Hp -= runtime.Damage;
            if (runtime.SlowTime > 0f) target.SlowTimer = Mathf.Max(target.SlowTimer, runtime.SlowTime);
            if (target.Hp <= 0f) KillEnemy(target);
        }
    }

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
        int index = _enemies.IndexOf(enemy);
        if (index < 0) return;

        Destroy(enemy.View.gameObject);
        _enemies.RemoveAt(index);
        _gold += enemy.Reward;
        RefreshHud();
    }

    private TowerProfile GetTowerProfile(TowerType type)
    {
        if (type == TowerType.Pea)
        {
            return new TowerProfile(buildCost: 50, upgradeBaseCost: 38, damage: 22f, range: 150f, fireInterval: 0.58f, slowTime: 0f);
        }

        return new TowerProfile(buildCost: 70, upgradeBaseCost: 45, damage: 12f, range: 138f, fireInterval: 0.86f, slowTime: 1.15f);
    }

    private TowerRuntime GetTowerRuntime(TowerData tower)
    {
        TowerProfile profile = GetTowerProfile(tower.Type);
        int lvOffset = tower.Level - 1;

        float damage = profile.Damage * (1f + lvOffset * 0.55f);
        float range = profile.Range + lvOffset * 16f;
        float fireInterval = Mathf.Max(0.2f, profile.FireInterval * (1f - lvOffset * 0.12f));
        float slowTime = profile.SlowTime + lvOffset * 0.25f;

        return new TowerRuntime(damage, range, fireInterval, slowTime);
    }

    private int GetUpgradeCost(TowerData tower)
    {
        TowerProfile profile = GetTowerProfile(tower.Type);
        return profile.UpgradeBaseCost + (tower.Level - 1) * 26;
    }

    private Color GetTowerColor(TowerType type, int level)
    {
        Color baseColor = type == TowerType.Pea ? _peaColor : _iceColor;
        float t = Mathf.Clamp01((level - 1) * 0.14f);
        return Color.Lerp(baseColor, Color.white, t);
    }

    private static string TowerCode(TowerType type)
    {
        return type == TowerType.Pea ? "P" : "I";
    }

    private bool EnsurePathPoints()
    {
        if (_pathPoints.Count > 0) return true;

        _pathPoints.Clear();
        for (int i = 0; i < _path.Count; i++)
        {
            Vector2Int node = _path[i];
            _pathPoints.Add(GetCellPosition(node.x, node.y));
        }

        if (_pathPoints.Count == 0)
        {
            Debug.LogError("Path is empty. Check path configuration.");
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

    private void SelectTower(TowerType towerType)
    {
        _selectedTower = towerType;

        ((Image)_peaButton.targetGraphic).color = towerType == TowerType.Pea
            ? new Color(0.22f, 0.8f, 0.28f, 1f)
            : new Color(0.42f, 0.42f, 0.42f, 1f);

        ((Image)_iceButton.targetGraphic).color = towerType == TowerType.Ice
            ? new Color(0.2f, 0.74f, 0.95f, 1f)
            : new Color(0.42f, 0.42f, 0.42f, 1f);

        TowerProfile profile = GetTowerProfile(_selectedTower);
        SetTip("Selected " + TowerCode(_selectedTower) + " (build " + profile.BuildCost + ")", 0.8f);
    }

    private void TogglePause()
    {
        if (_gameOver) return;

        _paused = !_paused;
        Time.timeScale = _paused ? 0f : _speed;
        RefreshSpeedButtons();
        SetTip(_paused ? "Paused" : "Resume", 0.6f);
    }

    private void SetSpeed(float speed)
    {
        _speed = speed;
        _paused = false;
        Time.timeScale = _speed;
        RefreshSpeedButtons();
        SetTip("Speed x" + _speed.ToString("0"), 0.6f);
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
        _gameOver = true;
        _paused = false;
        Time.timeScale = 1f;
        RefreshSpeedButtons();
        SetTip("Game Over", 999f);

        var overlay = CreateRect("GameOver", _board.parent);
        overlay.anchorMin = new Vector2(0f, 0f);
        overlay.anchorMax = new Vector2(1f, 1f);
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        AddImage(overlay.gameObject, new Color(0f, 0f, 0f, 0.62f));

        var text = CreateText("GameOverText", overlay, "Game Over", 56, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 86f);
        text.rectTransform.sizeDelta = new Vector2(500f, 100f);

        CreateCenterButton("RestartButton", overlay, "Restart", new Vector2(220f, 70f), new Vector2(0f, -18f),
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }

    private void RefreshHud()
    {
        _goldText.text = "Gold: " + _gold;
        _waveText.text = "Wave: " + _wave;
        _lifeText.text = "Carrot: " + _life;
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
