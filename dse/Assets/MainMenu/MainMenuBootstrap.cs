using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Monta a tela inicial do jogo (mapa medieval + personagens + título + botão)
/// em tempo de execução. Toda a arte vem de sprites em Resources/MainMenu/,
/// compostos a partir dos pacotes Kenney (medieval-rts e toon-characters).
///
/// O layout usa a resolução de referência 1920x1080 (mesma do mockup), então as
/// coordenadas abaixo correspondem 1:1 à arte. Basta colocar este componente em
/// um GameObject de uma cena vazia.
/// </summary>
public class MainMenuBootstrap : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Nome da cena do jogo a carregar ao clicar em Começar. " +
             "Precisa estar nas Build Settings.")]
    public string gameSceneName = "MapaMax";

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
        cam.backgroundColor = new Color(0.15f, 0.68f, 0.38f); // verde grama
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
        // ---- Canvas raiz ----
        var canvasGo = new GameObject("MenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasGo.transform;

        // ---- Camadas (de trás para frente) ----
        AddStretched("map", root);          // mapa de fundo
        AddStretched("vignette", root);     // escurecimento p/ contraste do texto

        // personagens (apoiados na base da tela)
        AddSpriteBottom("hero_left", 455f, 1058f, 470f, root);
        AddSpriteBottom("hero_right", 1470f, 1052f, 470f, root);

        // título e subtítulo
        AddSprite("title", 960f, 225f, root);
        AddSprite("subtitle", 960f, 450f, root);

        // botão
        BuildPlayButton(root);
    }

    void BuildPlayButton(Transform parent)
    {
        _btnNormal = Resources.Load<Sprite>(Dir + "button_normal");
        _btnHover = Resources.Load<Sprite>(Dir + "button_hover");

        var img = AddSprite("button_normal", 960f, 670f, parent);
        img.raycastTarget = true;

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.SpriteSwap;
        var state = new SpriteState
        {
            highlightedSprite = _btnHover,
            pressedSprite = _btnHover,
            selectedSprite = _btnHover
        };
        btn.spriteState = state;

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

    /// Cria uma Image a partir de um sprite, centrada em (cx,cy) no espaço 1920x1080,
    /// com o tamanho nativo do sprite.
    Image AddSprite(string res, float cx, float cy, Transform parent)
    {
        var sp = Resources.Load<Sprite>(Dir + res);
        var img = NewImage(res, parent);
        img.sprite = sp;
        var rt = img.rectTransform;
        Center(rt);
        if (sp != null) rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);
        rt.anchoredPosition = new Vector2(cx - RefW * 0.5f, RefH * 0.5f - cy);
        return img;
    }

    /// Cria uma Image apoiada pela base: centro horizontal cx, base em bottomY,
    /// altura fixa (largura proporcional ao sprite).
    Image AddSpriteBottom(string res, float cx, float bottomY, float height, Transform parent)
    {
        var sp = Resources.Load<Sprite>(Dir + res);
        var img = NewImage(res, parent);
        img.sprite = sp;
        var rt = img.rectTransform;
        Center(rt);
        float w = (sp != null) ? sp.rect.width * height / sp.rect.height : height;
        rt.sizeDelta = new Vector2(w, height);
        float cy = bottomY - height * 0.5f;
        rt.anchoredPosition = new Vector2(cx - RefW * 0.5f, RefH * 0.5f - cy);
        return img;
    }

    /// Image esticada para cobrir toda a tela.
    Image AddStretched(string res, Transform parent)
    {
        var sp = Resources.Load<Sprite>(Dir + res);
        var img = NewImage(res, parent);
        img.sprite = sp;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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

    static void Center(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
