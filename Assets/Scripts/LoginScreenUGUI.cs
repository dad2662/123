using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LoginScreenUGUI : MonoBehaviour
{
    private static bool s_LoggedIn;

    private const string DefaultUser = "admin";
    private const string DefaultPass = "123456";
    private const string UserKeyPrefix = "login_user_";
    private const string LastUserKey = "login_last_user";
    private const string LastPassKey = "login_last_pass";
    private const string RememberPassKey = "login_remember_pass";

    private sealed class InputVisual
    {
        public RectTransform Root;
        public Image Background;
        public Outline Border;
        public InputField Field;
        public Vector2 BasePos;
        public Coroutine ErrorFx;
    }

    private readonly Color _inputNormal = new Color(0.93f, 0.95f, 0.92f, 1f);
    private readonly Color _inputErrorFill = new Color(1f, 0.86f, 0.86f, 1f);
    private readonly Color _inputErrorBorder = new Color(1f, 0.35f, 0.35f, 0.95f);
    private readonly Color _okColor = new Color(0.65f, 1f, 0.72f, 1f);
    private readonly Color _warnColor = new Color(1f, 0.86f, 0.6f, 1f);
    private readonly Color _badColor = new Color(1f, 0.6f, 0.5f, 1f);

    private Font _font;
    private InputField _userInput;
    private InputField _passInput;
    private InputVisual _userVisual;
    private InputVisual _passVisual;
    private Toggle _rememberToggle;
    private Text _tipText;
    private Text _welcomeText;
    private RectTransform _panel;
    private CanvasGroup _canvasGroup;

    private bool _finished;
    private bool _closing;
    private float _resumeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (s_LoggedIn) return;
        if (FindObjectOfType<LoginScreenUGUI>() != null) return;
        new GameObject(nameof(LoginScreenUGUI)).AddComponent<LoginScreenUGUI>();
    }

    private void Start()
    {
        _resumeScale = Mathf.Max(1f, Time.timeScale);
        Time.timeScale = 0f;
        EnsureEventSystem();
        BuildUi();
        LoadRememberedAccount();
    }

    private void Update()
    {
        if (_finished) return;
        if (!Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (_closing || _finished) return;
        Time.timeScale = _resumeScale;
    }

    private void BuildUi()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasObj = new GameObject("LoginCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasObj.GetComponent<RectTransform>();
        AddImage(root.gameObject, new Color(0f, 0f, 0f, 0.58f));

        _panel = CreateRect("Panel", root);
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = new Vector2(560f, 440f);
        AddImage(_panel.gameObject, new Color(0.16f, 0.2f, 0.18f, 0.98f));

        var title = CreateText("Title", _panel, "塔防游戏登录", 38, TextAnchor.UpperCenter);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -24f);
        title.rectTransform.sizeDelta = new Vector2(0f, 56f);
        title.color = new Color(0.9f, 1f, 0.92f, 1f);

        CreateText("UserLabel", _panel, "账号", 24, TextAnchor.MiddleLeft).rectTransform.anchoredPosition = new Vector2(-180f, 112f);
        _userVisual = CreateInput("UserInput", _panel, new Vector2(0f, 66f), "请输入账号");
        _userInput = _userVisual.Field;

        CreateText("PassLabel", _panel, "密码", 24, TextAnchor.MiddleLeft).rectTransform.anchoredPosition = new Vector2(-180f, 34f);
        _passVisual = CreateInput("PassInput", _panel, new Vector2(0f, -12f), "请输入密码");
        _passInput = _passVisual.Field;
        _passInput.contentType = InputField.ContentType.Password;
        _passInput.ForceLabelUpdate();

        _rememberToggle = CreateToggle("RememberToggle", _panel, new Vector2(-128f, -86f), "记住密码");
        var showToggle = CreateToggle("ShowPassToggle", _panel, new Vector2(90f, -86f), "显示密码");
        showToggle.onValueChanged.AddListener(OnShowPasswordChanged);

        CreateButton("LoginButton", _panel, "登录", new Vector2(160f, 58f), new Vector2(-102f, -166f), OnLoginClicked);
        CreateButton("RegisterButton", _panel, "注册", new Vector2(160f, 58f), new Vector2(102f, -166f), OnRegisterClicked);
        CreateButton("GuestButton", _panel, "游客进入", new Vector2(150f, 42f), new Vector2(0f, -220f), OnGuestClicked);

        _tipText = CreateText("Tip", _panel, string.Empty, 22, TextAnchor.MiddleCenter);
        _tipText.rectTransform.anchoredPosition = new Vector2(0f, -122f);
        _tipText.rectTransform.sizeDelta = new Vector2(480f, 36f);
        _tipText.color = _warnColor;

        _welcomeText = CreateText("WelcomeText", root, string.Empty, 42, TextAnchor.MiddleCenter);
        _welcomeText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _welcomeText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _welcomeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _welcomeText.rectTransform.anchoredPosition = new Vector2(0f, 220f);
        _welcomeText.rectTransform.sizeDelta = new Vector2(1200f, 80f);
        _welcomeText.color = new Color(0.98f, 1f, 0.95f, 0f);

        _userInput.Select();
        _userInput.ActivateInputField();

        _userInput.onValueChanged.AddListener(_ => ClearInputError(_userVisual));
        _passInput.onValueChanged.AddListener(_ => ClearInputError(_passVisual));
    }

    private void OnShowPasswordChanged(bool show)
    {
        _passInput.contentType = show ? InputField.ContentType.Standard : InputField.ContentType.Password;
        _passInput.ForceLabelUpdate();
    }

    private void OnLoginClicked()
    {
        if (_finished || _closing) return;

        string user = _userInput.text.Trim();
        string pass = _passInput.text.Trim();
        bool userMissing = user.Length == 0;
        bool passMissing = pass.Length == 0;
        if (userMissing || passMissing)
        {
            SetTip("请输入账号和密码。", _badColor);
            if (userMissing) TriggerInputError(_userVisual);
            if (passMissing) TriggerInputError(_passVisual);
            return;
        }

        if (!Validate(user, pass))
        {
            SetTip("登录失败：账号或密码错误。", _badColor);
            TriggerInputError(_userVisual);
            TriggerInputError(_passVisual);
            return;
        }

        if (_rememberToggle.isOn)
        {
            PlayerPrefs.SetString(LastUserKey, user);
            PlayerPrefs.SetString(LastPassKey, pass);
            PlayerPrefs.SetInt(RememberPassKey, 1);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.DeleteKey(LastUserKey);
            PlayerPrefs.DeleteKey(LastPassKey);
            PlayerPrefs.DeleteKey(RememberPassKey);
        }

        Finish(user, false);
    }

    private void OnRegisterClicked()
    {
        if (_finished || _closing) return;

        string user = _userInput.text.Trim();
        string pass = _passInput.text.Trim();
        if (user.Length < 3 || pass.Length < 4)
        {
            SetTip("注册规则：账号至少 3 位，密码至少 4 位。", _warnColor);
            if (user.Length < 3) TriggerInputError(_userVisual);
            if (pass.Length < 4) TriggerInputError(_passVisual);
            return;
        }

        PlayerPrefs.SetString(UserKeyPrefix + user, pass);
        PlayerPrefs.SetString(LastUserKey, user);
        PlayerPrefs.SetString(LastPassKey, pass);
        PlayerPrefs.SetInt(RememberPassKey, 1);
        PlayerPrefs.Save();
        _rememberToggle.isOn = true;

        SetTip("注册成功，请点击登录。", _okColor);
    }

    private void OnGuestClicked()
    {
        if (_finished || _closing) return;
        Finish("游客", true);
    }

    private bool Validate(string user, string pass)
    {
        if (user == DefaultUser && pass == DefaultPass) return true;
        return PlayerPrefs.GetString(UserKeyPrefix + user, string.Empty) == pass;
    }

    private void LoadRememberedAccount()
    {
        bool rememberPass = PlayerPrefs.GetInt(RememberPassKey, 0) == 1;
        string lastUser = PlayerPrefs.GetString(LastUserKey, string.Empty);
        string lastPass = PlayerPrefs.GetString(LastPassKey, string.Empty);

        if (lastUser.Length > 0) _userInput.text = lastUser;
        if (rememberPass) _passInput.text = lastPass;
        _rememberToggle.isOn = rememberPass;
    }

    private void Finish(string displayName, bool guest)
    {
        if (_closing) return;
        _closing = true;
        _finished = true;
        s_LoggedIn = true;
        Time.timeScale = _resumeScale;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        StartCoroutine(PlayCloseAnimation(displayName, guest));
    }

    private void SetTip(string value, Color color)
    {
        _tipText.text = value;
        _tipText.color = color;
    }

    private void TriggerInputError(InputVisual visual)
    {
        if (visual == null) return;
        if (visual.ErrorFx != null) StopCoroutine(visual.ErrorFx);
        visual.ErrorFx = StartCoroutine(PlayInputError(visual));
    }

    private void ClearInputError(InputVisual visual)
    {
        if (visual == null) return;
        if (visual.ErrorFx != null)
        {
            StopCoroutine(visual.ErrorFx);
            visual.ErrorFx = null;
        }

        visual.Root.anchoredPosition = visual.BasePos;
        visual.Background.color = _inputNormal;
        visual.Border.effectColor = Color.clear;
    }

    private IEnumerator PlayInputError(InputVisual visual)
    {
        visual.Background.color = _inputErrorFill;
        visual.Border.effectColor = _inputErrorBorder;
        visual.Border.effectDistance = new Vector2(2f, 2f);

        const float duration = 0.32f;
        const float freq = 65f;
        const float amplitude = 12f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(t / duration);
            float x = Mathf.Sin(t * freq) * amplitude * damper;
            visual.Root.anchoredPosition = visual.BasePos + new Vector2(x, 0f);
            yield return null;
        }

        visual.Root.anchoredPosition = visual.BasePos;
        visual.Background.color = _inputNormal;
        visual.Border.effectColor = Color.clear;
        visual.ErrorFx = null;
    }

    private IEnumerator PlayCloseAnimation(string displayName, bool guest)
    {
        SetTip(guest ? "游客模式" : "登录成功", _okColor);
        _welcomeText.text = guest
            ? "欢迎你，游客指挥官！"
            : "欢迎回来，" + displayName + "！";

        float welcomeFade = 0.18f;
        float hold = 0.32f;
        float fadeOut = 0.45f;
        float t = 0f;
        Vector2 panelStart = _panel.anchoredPosition;

        while (t < welcomeFade)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / welcomeFade);
            _welcomeText.color = new Color(0.98f, 1f, 0.95f, a);
            yield return null;
        }

        float holdTimer = 0f;
        while (holdTimer < hold)
        {
            holdTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOut);
            _canvasGroup.alpha = 1f - k;
            _panel.anchoredPosition = panelStart + Vector2.up * (k * 24f);
            _welcomeText.color = new Color(0.98f, 1f, 0.95f, 1f - k * 0.8f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private InputVisual CreateInput(string name, Transform parent, Vector2 anchoredPos, string placeholder)
    {
        var root = CreateRect(name, parent);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(360f, 54f);
        root.anchoredPosition = anchoredPos;
        var background = AddImage(root.gameObject, _inputNormal);
        var border = root.gameObject.AddComponent<Outline>();
        border.effectColor = Color.clear;
        border.effectDistance = new Vector2(2f, 2f);
        border.useGraphicAlpha = false;

        var textArea = CreateRect("TextArea", root);
        textArea.anchorMin = new Vector2(0f, 0f);
        textArea.anchorMax = new Vector2(1f, 1f);
        textArea.offsetMin = new Vector2(14f, 10f);
        textArea.offsetMax = new Vector2(-14f, -10f);

        var text = CreateText("Text", textArea, string.Empty, 24, TextAnchor.MiddleLeft);
        text.color = new Color(0.11f, 0.12f, 0.11f, 1f);
        text.supportRichText = false;

        var place = CreateText("Placeholder", textArea, placeholder, 22, TextAnchor.MiddleLeft);
        place.color = new Color(0.45f, 0.48f, 0.45f, 0.85f);

        var input = root.gameObject.AddComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = text;
        input.placeholder = place;
        input.lineType = InputField.LineType.SingleLine;

        return new InputVisual
        {
            Root = root,
            Background = background,
            Border = border,
            Field = input,
            BasePos = anchoredPos
        };
    }

    private Toggle CreateToggle(string name, Transform parent, Vector2 anchoredPos, string label)
    {
        var root = CreateRect(name, parent);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(220f, 30f);
        root.anchoredPosition = anchoredPos;

        var box = CreateRect("Box", root);
        box.anchorMin = new Vector2(0f, 0.5f);
        box.anchorMax = new Vector2(0f, 0.5f);
        box.pivot = new Vector2(0f, 0.5f);
        box.sizeDelta = new Vector2(22f, 22f);
        box.anchoredPosition = Vector2.zero;
        var boxImage = AddImage(box.gameObject, new Color(0.95f, 0.95f, 0.95f, 1f));

        var check = CreateRect("Checkmark", box);
        check.anchorMin = new Vector2(0.5f, 0.5f);
        check.anchorMax = new Vector2(0.5f, 0.5f);
        check.pivot = new Vector2(0.5f, 0.5f);
        check.sizeDelta = new Vector2(14f, 14f);
        var checkImage = AddImage(check.gameObject, new Color(0.12f, 0.72f, 0.24f, 1f));

        var labelText = CreateText("Label", root, label, 21, TextAnchor.MiddleLeft);
        labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
        labelText.rectTransform.anchorMax = new Vector2(1f, 1f);
        labelText.rectTransform.offsetMin = new Vector2(30f, 0f);
        labelText.rectTransform.offsetMax = Vector2.zero;
        labelText.color = new Color(0.9f, 0.95f, 0.9f, 1f);

        var toggle = root.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = false;
        return toggle;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        var rt = CreateRect(name, parent);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var image = AddImage(rt.gameObject, new Color(0.23f, 0.55f, 0.3f, 1f));
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var text = CreateText("Text", rt, label, 24, TextAnchor.MiddleCenter);
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
