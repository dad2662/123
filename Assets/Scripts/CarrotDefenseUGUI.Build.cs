using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CarrotDefenseUGUI : MonoBehaviour
{
    // UI construction, grid setup, and build/upgrade interactions.
    private void BuildUi()
    {
        // 构建战斗界面（顶部资源栏、底部操作栏、棋盘容器）。
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
        AddImage(root.gameObject, new Color(0.12f, 0.10f, 0.08f, 1f));

        var topShade = CreateRect("TopShade", root);
        StretchTop(topShade, 170f);
        AddImage(topShade.gameObject, new Color(0f, 0f, 0f, 0.22f));

        var bottomShade = CreateRect("BottomShade", root);
        bottomShade.anchorMin = new Vector2(0f, 0f);
        bottomShade.anchorMax = new Vector2(1f, 0f);
        bottomShade.pivot = new Vector2(0.5f, 0f);
        bottomShade.sizeDelta = new Vector2(0f, 190f);
        bottomShade.anchoredPosition = Vector2.zero;
        AddImage(bottomShade.gameObject, new Color(0f, 0f, 0f, 0.26f));

        _waveText = CreateText("WaveValue", root, "0", 86, TextAnchor.UpperLeft);
        _waveText.rectTransform.anchorMin = new Vector2(0f, 1f);
        _waveText.rectTransform.anchorMax = new Vector2(0f, 1f);
        _waveText.rectTransform.pivot = new Vector2(0f, 1f);
        _waveText.rectTransform.anchoredPosition = new Vector2(56f, -28f);
        _waveText.rectTransform.sizeDelta = new Vector2(140f, 100f);
        _waveText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);

        var waveLabel = CreateText("WaveLabel", root, "Wave", 48, TextAnchor.UpperLeft);
        waveLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
        waveLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
        waveLabel.rectTransform.pivot = new Vector2(0f, 1f);
        waveLabel.rectTransform.anchoredPosition = new Vector2(188f, -36f);
        waveLabel.rectTransform.sizeDelta = new Vector2(200f, 70f);
        waveLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.82f);

        _lifeText = CreateText("LifeValue", root, "0", 86, TextAnchor.UpperRight);
        _lifeText.rectTransform.anchorMin = new Vector2(1f, 1f);
        _lifeText.rectTransform.anchorMax = new Vector2(1f, 1f);
        _lifeText.rectTransform.pivot = new Vector2(1f, 1f);
        _lifeText.rectTransform.anchoredPosition = new Vector2(-48f, -28f);
        _lifeText.rectTransform.sizeDelta = new Vector2(140f, 100f);
        _lifeText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);

        var lifeLabel = CreateText("LifeLabel", root, "Lifes", 48, TextAnchor.UpperRight);
        lifeLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
        lifeLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        lifeLabel.rectTransform.pivot = new Vector2(1f, 1f);
        lifeLabel.rectTransform.anchoredPosition = new Vector2(-188f, -36f);
        lifeLabel.rectTransform.sizeDelta = new Vector2(220f, 70f);
        lifeLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.82f);

        _scoreText = CreateText("ScoreValue", root, "0", 84, TextAnchor.LowerLeft);
        _scoreText.rectTransform.anchorMin = new Vector2(0f, 0f);
        _scoreText.rectTransform.anchorMax = new Vector2(0f, 0f);
        _scoreText.rectTransform.pivot = new Vector2(0f, 0f);
        _scoreText.rectTransform.anchoredPosition = new Vector2(32f, 20f);
        _scoreText.rectTransform.sizeDelta = new Vector2(300f, 100f);
        _scoreText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);

        var scoreLabel = CreateText("ScoreLabel", root, "Score", 46, TextAnchor.LowerLeft);
        scoreLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        scoreLabel.rectTransform.anchorMax = new Vector2(0f, 0f);
        scoreLabel.rectTransform.pivot = new Vector2(0f, 0f);
        scoreLabel.rectTransform.anchoredPosition = new Vector2(22f, 104f);
        scoreLabel.rectTransform.sizeDelta = new Vector2(220f, 70f);
        scoreLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.82f);

        _goldText = CreateText("SoulsValue", root, "0", 84, TextAnchor.LowerRight);
        _goldText.rectTransform.anchorMin = new Vector2(1f, 0f);
        _goldText.rectTransform.anchorMax = new Vector2(1f, 0f);
        _goldText.rectTransform.pivot = new Vector2(1f, 0f);
        _goldText.rectTransform.anchoredPosition = new Vector2(-24f, 20f);
        _goldText.rectTransform.sizeDelta = new Vector2(300f, 100f);
        _goldText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);

        var soulsLabel = CreateText("SoulsLabel", root, "Souls", 46, TextAnchor.LowerRight);
        soulsLabel.rectTransform.anchorMin = new Vector2(1f, 0f);
        soulsLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
        soulsLabel.rectTransform.pivot = new Vector2(1f, 0f);
        soulsLabel.rectTransform.anchoredPosition = new Vector2(-20f, 104f);
        soulsLabel.rectTransform.sizeDelta = new Vector2(220f, 70f);
        soulsLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.82f);

        _tipText = CreateText("CenterTip", root, string.Empty, 42, TextAnchor.MiddleCenter);
        _tipText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _tipText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _tipText.rectTransform.sizeDelta = new Vector2(1100f, 120f);
        _tipText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        _tipText.color = new Color(0.95f, 0.95f, 0.90f, 0.9f);

        var toolbar = CreateRect("Toolbar", root);
        toolbar.anchorMin = new Vector2(0.5f, 0f);
        toolbar.anchorMax = new Vector2(0.5f, 0f);
        toolbar.pivot = new Vector2(0.5f, 0f);
        toolbar.sizeDelta = new Vector2(1220f, 126f);
        toolbar.anchoredPosition = new Vector2(0f, 24f);
        AddImage(toolbar.gameObject, new Color(0.12f, 0.12f, 0.12f, 0.62f));

        _peaButton = CreateCenterButton("PlantSlot0", toolbar, "植物", new Vector2(112f, 92f), new Vector2(-520f, 62f), () => SelectPlantSlot(0));
        _iceButton = CreateCenterButton("PlantSlot1", toolbar, "植物", new Vector2(112f, 92f), new Vector2(-396f, 62f), () => SelectPlantSlot(1));
        _fireButton = CreateCenterButton("PlantSlot2", toolbar, "植物", new Vector2(112f, 92f), new Vector2(-272f, 62f), () => SelectPlantSlot(2));
        _cannonButton = CreateCenterButton("PlantSlot3", toolbar, "植物", new Vector2(112f, 92f), new Vector2(-148f, 62f), () => SelectPlantSlot(3));
        _poisonButton = CreateCenterButton("PlantSlot4", toolbar, "植物", new Vector2(112f, 92f), new Vector2(-24f, 62f), () => SelectPlantSlot(4));

        _plantPrevButton = CreateCenterButton("PlantPrev", toolbar, "上一页", new Vector2(110f, 92f), new Vector2(92f, 62f), PrevPlantPage);
        _plantNextButton = CreateCenterButton("PlantNext", toolbar, "下一页", new Vector2(110f, 92f), new Vector2(214f, 62f), NextPlantPage);
        _pauseButton = CreateCenterButton("PauseButton", toolbar, "暂停", new Vector2(110f, 92f), new Vector2(336f, 62f), TogglePause);
        _x1Button = CreateCenterButton("X1Button", toolbar, "1x", new Vector2(96f, 92f), new Vector2(458f, 62f), () => SetSpeed(1f));
        _x2Button = CreateCenterButton("X2Button", toolbar, "2x", new Vector2(96f, 92f), new Vector2(560f, 62f), () => SetSpeed(2f));
        RefreshTowerPlantButtons();

        _board = CreateRect("Board", root);
        _board.anchorMin = new Vector2(0.5f, 0.5f);
        _board.anchorMax = new Vector2(0.5f, 0.5f);
        _board.pivot = new Vector2(0.5f, 0.5f);
        _board.sizeDelta = new Vector2(Cols * CellSize + 20f, Rows * CellSize + 20f);
        _board.anchoredPosition = new Vector2(0f, -12f);
        _board.localScale = new Vector3(1f, 0.86f, 1f);
        AddImage(_board.gameObject, new Color(0.32f, 0.34f, 0.2f, 0.98f));

        BuildLineupPanel(root);
        BuildBattlePopup(root);
    }

    private void BuildLineupPanel(RectTransform root)
    {
        _lineupPlantButtons.Clear();

        _lineupPanel = CreateRect("LineupPanel", root);
        _lineupPanel.anchorMin = Vector2.zero;
        _lineupPanel.anchorMax = Vector2.one;
        _lineupPanel.offsetMin = Vector2.zero;
        _lineupPanel.offsetMax = Vector2.zero;
        AddImage(_lineupPanel.gameObject, new Color(0f, 0f, 0f, 0.78f));

        var card = CreateRect("LineupCard", _lineupPanel);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(1240f, 760f);
        card.anchoredPosition = new Vector2(0f, 10f);
        AddImage(card.gameObject, new Color(0.16f, 0.16f, 0.14f, 0.98f));

        var title = CreateText("Title", card, "选择 7 个植物进入本局", 50, TextAnchor.UpperCenter);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(900f, 86f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -26f);
        title.color = new Color(0.95f, 0.95f, 0.9f, 0.98f);

        _lineupSelectedText = CreateText("Selected", card, string.Empty, 28, TextAnchor.UpperCenter);
        _lineupSelectedText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        _lineupSelectedText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        _lineupSelectedText.rectTransform.pivot = new Vector2(0.5f, 1f);
        _lineupSelectedText.rectTransform.sizeDelta = new Vector2(1080f, 60f);
        _lineupSelectedText.rectTransform.anchoredPosition = new Vector2(0f, -96f);
        _lineupSelectedText.color = new Color(0.85f, 0.91f, 1f, 0.96f);

        for (int i = 0; i < LineupPlantsPerPage; i++)
        {
            int col = i % 5;
            int row = i / 5;
            float x = -420f + col * 210f;
            float y = 120f - row * 125f;

            int captured = i;
            Button button = CreateCenterButton(
                "LineupPlant_" + i,
                card,
                "--",
                new Vector2(192f, 94f),
                new Vector2(x, y),
                () => ToggleLineupPlantSlot(captured));
            _lineupPlantButtons.Add(button);
        }

        _lineupPrevPageButton = CreateCenterButton("LineupPrev", card, "上一页", new Vector2(150f, 84f), new Vector2(-270f, -220f), PrevLineupPage);
        _lineupNextPageButton = CreateCenterButton("LineupNext", card, "下一页", new Vector2(150f, 84f), new Vector2(-100f, -220f), NextLineupPage);
        _lineupConfirmButton = CreateCenterButton("LineupConfirm", card, "开始战斗", new Vector2(240f, 84f), new Vector2(330f, -220f), ConfirmLineupSelection);

        var detailBg = CreateRect("LineupDetailBg", card);
        detailBg.anchorMin = new Vector2(0.5f, 0.5f);
        detailBg.anchorMax = new Vector2(0.5f, 0.5f);
        detailBg.pivot = new Vector2(0.5f, 0.5f);
        detailBg.sizeDelta = new Vector2(1100f, 118f);
        detailBg.anchoredPosition = new Vector2(0f, -130f);
        AddImage(detailBg.gameObject, new Color(0.14f, 0.18f, 0.22f, 0.92f));

        _lineupDetailText = CreateText("LineupDetail", detailBg, string.Empty, 22, TextAnchor.MiddleLeft);
        _lineupDetailText.rectTransform.anchorMin = new Vector2(0f, 0f);
        _lineupDetailText.rectTransform.anchorMax = new Vector2(1f, 1f);
        _lineupDetailText.rectTransform.offsetMin = new Vector2(16f, 12f);
        _lineupDetailText.rectTransform.offsetMax = new Vector2(-16f, -12f);
        _lineupDetailText.color = new Color(0.90f, 0.96f, 1f, 0.98f);

        _lineupTipText = CreateText("LineupTip", card, string.Empty, 30, TextAnchor.MiddleLeft);
        _lineupTipText.rectTransform.anchorMin = new Vector2(0f, 0f);
        _lineupTipText.rectTransform.anchorMax = new Vector2(0f, 0f);
        _lineupTipText.rectTransform.pivot = new Vector2(0f, 0f);
        _lineupTipText.rectTransform.sizeDelta = new Vector2(860f, 64f);
        _lineupTipText.rectTransform.anchoredPosition = new Vector2(48f, 22f);
        _lineupTipText.color = new Color(0.90f, 0.94f, 0.84f, 0.95f);

        _lineupPanel.gameObject.SetActive(false);
    }

    private void BuildBattlePopup(RectTransform root)
    {
        _battlePopupPanel = CreateRect("BattlePopupPanel", root);
        _battlePopupPanel.anchorMin = Vector2.zero;
        _battlePopupPanel.anchorMax = Vector2.one;
        _battlePopupPanel.offsetMin = Vector2.zero;
        _battlePopupPanel.offsetMax = Vector2.zero;
        AddImage(_battlePopupPanel.gameObject, new Color(0f, 0f, 0f, 0.72f));

        var card = CreateRect("PopupCard", _battlePopupPanel);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(920f, 500f);
        AddImage(card.gameObject, new Color(0.16f, 0.16f, 0.15f, 1f));

        _battlePopupTitleText = CreateText("Title", card, "提示", 56, TextAnchor.UpperCenter);
        _battlePopupTitleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        _battlePopupTitleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        _battlePopupTitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        _battlePopupTitleText.rectTransform.sizeDelta = new Vector2(800f, 90f);
        _battlePopupTitleText.rectTransform.anchoredPosition = new Vector2(0f, -30f);
        _battlePopupTitleText.color = new Color(0.96f, 0.95f, 0.9f, 0.98f);

        _battlePopupContentText = CreateText("Content", card, string.Empty, 34, TextAnchor.MiddleCenter);
        _battlePopupContentText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _battlePopupContentText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _battlePopupContentText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _battlePopupContentText.rectTransform.sizeDelta = new Vector2(800f, 220f);
        _battlePopupContentText.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        _battlePopupContentText.color = new Color(0.94f, 0.96f, 1f, 0.98f);
        _battlePopupContentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _battlePopupContentText.verticalOverflow = VerticalWrapMode.Overflow;

        _battlePopupPrimaryButton = CreateCenterButton(
            "PrimaryButton",
            card,
            "确定",
            new Vector2(220f, 88f),
            new Vector2(140f, -170f),
            OnBattlePopupPrimaryClicked);
        _battlePopupSecondaryButton = CreateCenterButton(
            "SecondaryButton",
            card,
            "取消",
            new Vector2(220f, 88f),
            new Vector2(-140f, -170f),
            OnBattlePopupSecondaryClicked);
        _battlePopupSecondaryButton.gameObject.SetActive(false);
        _battlePopupPanel.gameObject.SetActive(false);
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

                float shade = 0.84f + Hash01(x, y, 3) * 0.28f;
                var image = AddImage(cellRt.gameObject, Tint(isPath ? _pathColor : _grassColor, shade));
                image.raycastTarget = !isPath;
                AddGroundDecor(cellRt, x, y, isPath);

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
        var castle = CreateRect("Castle", _board);
        castle.anchorMin = new Vector2(0.5f, 0.5f);
        castle.anchorMax = new Vector2(0.5f, 0.5f);
        castle.pivot = new Vector2(0.5f, 0.5f);
        castle.sizeDelta = new Vector2(CellSize * 2.55f, CellSize * 2.15f);
        castle.anchoredPosition = goalPos + new Vector2(0f, CellSize * 0.1f);
        AddImage(castle.gameObject, new Color(0.56f, 0.51f, 0.42f, 1f));

        var yard = CreateRect("Yard", castle);
        yard.anchorMin = new Vector2(0.5f, 0.5f);
        yard.anchorMax = new Vector2(0.5f, 0.5f);
        yard.pivot = new Vector2(0.5f, 0.5f);
        yard.sizeDelta = castle.sizeDelta - new Vector2(26f, 26f);
        AddImage(yard.gameObject, new Color(0.50f, 0.45f, 0.36f, 1f));

        for (int i = 0; i < 4; i++)
        {
            var tower = CreateRect("WallTower_" + i, castle);
            tower.anchorMin = new Vector2((i == 0 || i == 3) ? 0f : 1f, (i < 2) ? 1f : 0f);
            tower.anchorMax = tower.anchorMin;
            tower.pivot = new Vector2((i == 0 || i == 3) ? 0f : 1f, (i < 2) ? 1f : 0f);
            tower.sizeDelta = new Vector2(CellSize * 0.42f, CellSize * 0.58f);
            tower.anchoredPosition = new Vector2((i == 0 || i == 3) ? -6f : 6f, (i < 2) ? 6f : -6f);
            AddImage(tower.gameObject, new Color(0.47f, 0.42f, 0.34f, 1f));
        }

        var mark = CreateText("Mark", castle, "城堡", 30, TextAnchor.MiddleCenter);
        mark.color = new Color(0.15f, 0.11f, 0.08f, 0.9f);
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
            obstacleRt.sizeDelta = Vector2.one * (CellSize - 18f);
            obstacleRt.anchoredPosition = cell.Position;

            AddImage(obstacleRt.gameObject, new Color(0.28f, 0.32f, 0.18f, 1f));
            var label = CreateText("Label", obstacleRt, "丛", 24, TextAnchor.MiddleCenter);
            label.color = new Color(0.82f, 0.9f, 0.75f, 0.92f);

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
        if (_gameOver || !_matchReady || cell.IsPath) return;

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
            SetTip("金币不足，无法清理障碍", 0.9f);
            return;
        }

        _gold -= obstacle.ClearCost;
        _gold += obstacle.Bonus;
        _score += obstacle.Bonus * 4;

        Destroy(obstacle.View.gameObject);
        cell.Obstacle = null;
        cell.View.color = _grassColor;

        SetTip("清理成功，获得 +" + obstacle.Bonus + " 金币", 1.1f);
        RefreshHud();
    }

    private void TryBuildTower(CellData cell)
    {
        // 空地建塔：扣费 -> 生成模型 -> 注册到塔列表。
        PlantKindData selectedKind = GetSelectedPlantKind();
        if (selectedKind == null) return;

        if (_gold < selectedKind.BuildCost)
        {
            SetTip("金币不足，无法建塔", 0.9f);
            return;
        }

        _gold -= selectedKind.BuildCost;

        var towerRt = CreateRect("Tower", _board);
        towerRt.anchorMin = new Vector2(0.5f, 0.5f);
        towerRt.anchorMax = new Vector2(0.5f, 0.5f);
        towerRt.pivot = new Vector2(0.5f, 0.5f);
        towerRt.sizeDelta = Vector2.one * (CellSize - 14f);
        towerRt.anchoredPosition = cell.Position;
        AddImage(towerRt.gameObject, new Color(0f, 0f, 0f, 0f)).raycastTarget = false;

        TowerModelData model = BuildTowerModel(towerRt);
        ApplyTowerModelStyle(model, selectedKind.Family, 1);

        var label = CreateText("Label", towerRt, GetPlantKindCode(selectedKind.KindId) + " Lv1", 22, TextAnchor.MiddleCenter);
        label.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        label.rectTransform.anchoredPosition = new Vector2(0f, -17f);

        var tower = new TowerData
        {
            Type = selectedKind.Family,
            KindId = selectedKind.KindId,
            Level = 1,
            Hp = GetTowerMaxHp(1),
            View = towerRt,
            Model = model,
            Label = label,
            Cooldown = 0.06f,
            IdlePhase = Random.Range(0f, Mathf.PI * 2f),
            AttackAnimTimer = 0f
        };

        cell.Tower = tower;
        cell.View.color = _builtCellColor;
        _towers.Add(tower);

        SetTip("已建造 " + selectedKind.DisplayName, 0.8f);
        RefreshHud();
    }

    private void TryUpgradeTower(TowerData tower)
    {
        // 升级炮塔会提升基础属性并刷新模型外观。
        if (tower.Level >= MaxTowerLevel)
        {
            SetTip("炮塔已满级", 0.8f);
            return;
        }

        int upgradeCost = GetUpgradeCost(tower);
        if (_gold < upgradeCost)
        {
            SetTip("升级需要 " + upgradeCost + " 金币（当前 " + _gold + "）", 0.95f);
            return;
        }

        _gold -= upgradeCost;
        tower.Level++;
        tower.Hp = GetTowerMaxHp(tower.Level);
        tower.Cooldown = Mathf.Min(tower.Cooldown, 0.05f);

        tower.Label.text = GetPlantKindCode(tower.KindId) + " Lv" + tower.Level;
        ApplyTowerModelStyle(tower.Model, tower.Type, tower.Level);
        TowerRuntime runtime = GetTowerRuntime(tower);

        SetTip(GetPlantKindName(tower.KindId) + " 升至 " + tower.Level + " 级（消耗 " + upgradeCost + " 金币）", 1.05f);
        RefreshHud();
        ShowBattlePopup(
            "升级成功",
            GetPlantKindName(tower.KindId) + " 已升级到 Lv" + tower.Level + "\n"
                + "伤害 " + runtime.Damage.ToString("0.0")
                + "  射程 " + runtime.Range.ToString("0")
                + "  攻速 " + runtime.FireInterval.ToString("0.00") + "s",
            "确定",
            null);
    }

}
