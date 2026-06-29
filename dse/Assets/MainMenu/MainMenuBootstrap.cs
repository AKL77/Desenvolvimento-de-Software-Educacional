using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Monta a tela inicial do jogo em tempo de execução: imagem de fundo (castelo),
/// placa de título e botão emoldurado no estilo pixel-art (pacote Kenney UI),
/// tudo posicionado à esquerda para emoldurar a arte do castelo.
///
/// A arte vem de sprites em Resources/MainMenu/. Layout na resolução de
/// referência 1920x1080 (mesma do mockup). Basta colocar este componente em um
/// GameObject de uma cena vazia.
/// </summary>
public class MainMenuBootstrap : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Cena carregada ao clicar em Começar (a introdução com história). " +
             "Precisa estar nas Build Settings.")]
    public string gameSceneName = "StoryIntro";

    const float RefW = 1920f, RefH = 1080f;
    const string Dir = "MainMenu/";

    Sprite _btnNormal, _btnHover;

    void Start()
    {
        EnsureCamera();
        EnsureEventSystem();
        BuildUI();
    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.18f);
        cam.orthographic = true;
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>(); // projeto usa só o novo Input System
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("MenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasGo.transform;

        // Fundo do castelo (cobre a tela preservando proporção)
        AddBackground("menu_bg", root);

        // Coluna de UI à esquerda, sobre o céu/floresta
        AddSprite("title", 560f, 250f, root);
        AddSprite("subtitle", 560f, 430f, root);
        BuildPlayButton(root);
    }

    void BuildPlayButton(Transform parent)
    {
        _btnNormal = Resources.Load<Sprite>(Dir + "button_normal");
        _btnHover = Resources.Load<Sprite>(Dir + "button_hover");

        var img = AddSprite("button_normal", 560f, 600f, parent);
        img.raycastTarget = true;

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.SpriteSwap;
        btn.spriteState = new SpriteState
        {
            highlightedSprite = _btnHover,
            pressedSprite = _btnHover,
            selectedSprite = _btnHover
        };
        btn.onClick.AddListener(OnPlayClicked);
    }

    public void OnPlayClicked()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError($"[MainMenu] A cena '{gameSceneName}' não está nas Build Settings. " +
                           "Adicione-a em File > Build Settings.");
    }

    // ---------- Helpers ----------

    /// Fundo que cobre toda a tela mantendo a proporção da imagem (recorta o excesso).
    Image AddBackground(string res, Transform parent)
    {
        var sp = Resources.Load<Sprite>(Dir + res);
        var img = NewImage(res, parent);
        img.sprite = sp;
        img.preserveAspect = true;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (sp != null) ? sp.rect.width / sp.rect.height : 16f / 9f;
        return img;
    }

    /// Image centrada em (cx,cy) no espaço 1920x1080, com o tamanho nativo do sprite.
    Image AddSprite(string res, float cx, float cy, Transform parent)
    {
        var sp = Resources.Load<Sprite>(Dir + res);
        var img = NewImage(res, parent);
        img.sprite = sp;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        if (sp != null) rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);
        rt.anchoredPosition = new Vector2(cx - RefW * 0.5f, RefH * 0.5f - cy);
        return img;
    }

    Image NewImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }
}
