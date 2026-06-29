using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Tela de introdução com história (estilo visual novel). Fundo do laboratório,
/// caixa de texto na parte inferior com efeito de digitação caractere por
/// caractere, dividida em trechos. Um botão "Próximo" avança: se o texto ainda
/// está sendo digitado, completa o trecho atual na hora; se já está completo,
/// vai para o próximo. Clicar em qualquer lugar da tela também avança.
/// Ao terminar o último trecho, carrega a próxima cena.
///
/// Construída por código a partir de sprites em Resources/Story/.
/// Basta colocar este componente em um GameObject de uma cena vazia.
/// </summary>
public class StoryIntroBootstrap : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Cena carregada ao final da história. Precisa estar nas Build Settings.")]
    public string nextSceneName = "MapaMax";

    [Tooltip("Tempo (segundos) entre cada caractere digitado.")]
    public float typingDelay = 0.028f;

    [Header("Visual Effects")]
    public Sprite laboratoryBackgroundSprite;
    public Sprite errorSprite;
    public Sprite loadingSprite;
    public float lightFadeDuration = 2f;
    public float blackFadeDuration = 1.6f;
    [Range(0f, 1f)] public float lightOverlayAlpha = 0.78f;

    const float RefW = 1920f, RefH = 1080f;
    const string Dir = "Story/";
    const int LightSegmentIndex = 18;
    const int FallSegmentIndex = 25;
    const int LoadingSegmentIndex = 28;

    static readonly string[] Story =
    {
        "O ano é 2180.",
        "Pitágoras é um cientista da computação brilhante e trabalha na Mistic, a maior empresa de tecnologia do mundo.",
        "Nos últimos anos, ele tem se dedicado a um projeto capaz de mudar para sempre a forma como a humanidade vive: uma máquina do tempo.",
        "Por dominar profundamente a computação e ter uma mente extremamente lógica, Pitágoras se tornou um dos principais cientistas à frente do projeto.",
        "Para ele, aquele trabalho era mais do que uma pesquisa. Era a chance de realizar algo que muitas pessoas acreditavam ser impossível.",
        "Pitágoras gostava tanto do que fazia que, muitas vezes, perdia completamente a noção do tempo.",
        "Passava dias e noites dentro do laboratório, saindo apenas quando precisava ir ao banheiro ou quando a fome se tornava impossível de ignorar.",
        "Naquela noite, enquanto analisava uma sequência de códigos, Pitágoras sentiu que estava perto de uma descoberta decisiva.",
        "Por um instante, pensou em verificar a data e a hora.",
        "Imediatamente, seus óculos inteligentes responderam ao comando mental e projetaram a informação diante de seus olhos: 23:56 h, 28 de outubro.",
        "O laboratório estava silencioso. Todos já haviam ido embora.",
        "As luzes frias dos equipamentos eram a única companhia de Pitágoras naquela imensa sala.",
        "Mesmo assim, ele não pensou em ir para casa. Seus olhos estavam fixos na tela.",
        "Depois de tantas tentativas, erros e noites sem dormir, ele acreditava ter encontrado o código-chave para fazer a máquina do tempo funcionar.",
        "Com o coração acelerado, Pitágoras revisou cada linha do programa.",
        "Conferiu os cálculos, analisou os comandos e verificou todos os sistemas da máquina.",
        "Tudo parecia estar correto.",
        "Então, respirou fundo e clicou em \"Executar\".",
        "No mesmo instante, uma luz intensa tomou conta do laboratório.",
        "O brilho era tão forte que Pitágoras precisou erguer os braços para proteger os olhos.",
        "Os equipamentos começaram a vibrar. As luzes piscaram sem controle.",
        "O chão tremeu sob seus pés, e um som profundo ecoou por todo o laboratório.",
        "Antes que Pitágoras pudesse reagir, rachaduras começaram a se abrir ao seu redor.",
        "A máquina do tempo brilhou ainda mais forte, como se estivesse puxando toda a energia do lugar.",
        "De repente, o chão desapareceu.",
        "Pitágoras e a máquina do tempo começaram a cair em um buraco escuro e aparentemente sem fim.",
        "O vento cortava seu rosto, os sons do laboratório ficaram distantes, e tudo ao seu redor se transformou em sombras e flashes de luz.",
        "Ele não sabia se estava caindo por segundos, minutos ou horas.",
        "Até que, de repente, tudo parou.",
        "Pitágoras sentiu o impacto do chão sob seus pés.",
        "Aos poucos, abriu os olhos.",
        "Ele não estava mais no laboratório.",
    };

    Text _text;
    Image _backgroundImage;
    Image _lightOverlay;
    Image _blackOverlay;
    Image _errorImage;
    Image _loadingImage;
    StoryGifPlayer _errorGifPlayer;
    StoryGifPlayer _loadingGifPlayer;
    Coroutine _typing;
    Coroutine _lightFade;
    Coroutine _blackFade;
    bool _isTyping;
    int _index;

    void Start()
    {
        EnsureCamera();
        EnsureEventSystem();
        BuildUI();
        ShowSegment(0);
    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
        cam.orthographic = true;
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("StoryCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // Fundo e efeitos visuais da historia.
        _backgroundImage = AddBackground(laboratoryBackgroundSprite, "lab_bg", root);
        _lightOverlay = AddFullScreenImage("LightOverlay", root, new Color(1f, 1f, 1f, 0f));
        _errorImage = AddEffectImage("ErrorEffect", root, errorSprite, new Vector2(0f, 110f));
        _blackOverlay = AddFullScreenImage("BlackOverlay", root, new Color(0f, 0f, 0f, 0f));
        _loadingImage = AddEffectImage("LoadingEffect", root, loadingSprite, new Vector2(0f, 110f));
        _errorGifPlayer = AddGifPlayer(_errorImage, "Story/error_gif", errorSprite);
        _loadingGifPlayer = AddGifPlayer(_loadingImage, "Story/loading_gif", loadingSprite);

        // Catcher de clique em qualquer lugar (atrás da caixa, avança a história)
        var catcher = NewImage("ClickCatcher", root);
        catcher.color = new Color(0, 0, 0, 0);
        catcher.raycastTarget = true;
        StretchFull(catcher.rectTransform);
        AddButton(catcher.gameObject, null, null);

        // Caixa de texto (parte inferior)
        var panelSp = Resources.Load<Sprite>(Dir + "story_panel");
        var panel = NewImage("TextBox", root);
        panel.sprite = panelSp;
        panel.raycastTarget = false;
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        if (panelSp != null) prt.sizeDelta = new Vector2(panelSp.rect.width, panelSp.rect.height);
        prt.anchoredPosition = new Vector2(0f, 60f);

        // Texto da narração
        var txtGo = new GameObject("StoryText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtGo.transform.SetParent(panel.transform, false);
        _text = txtGo.GetComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 40;
        _text.color = new Color(0.93f, 0.96f, 1f);
        _text.alignment = TextAnchor.UpperLeft;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        _text.lineSpacing = 1.15f;
        _text.raycastTarget = false;
        var trt = _text.rectTransform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(54f, 96f);   // deixa espaço embaixo p/ o botão
        trt.offsetMax = new Vector2(-54f, -34f);

        // Botão "Próximo" (canto inferior direito da caixa)
        var btnSp = Resources.Load<Sprite>(Dir + "story_next_normal");
        var btnHi = Resources.Load<Sprite>(Dir + "story_next_hover");
        var btnImg = NewImage("NextButton", panel.transform);
        btnImg.sprite = btnSp;
        btnImg.raycastTarget = true;
        var brt = btnImg.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(1f, 0f);
        if (btnSp != null) brt.sizeDelta = new Vector2(btnSp.rect.width, btnSp.rect.height);
        brt.anchoredPosition = new Vector2(-34f, 26f);
        AddButton(btnImg.gameObject, btnImg, btnHi);
    }

    void AddButton(GameObject go, Image target, Sprite hover)
    {
        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        if (target != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.targetGraphic = target;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = hover,
                pressedSprite = hover,
                selectedSprite = hover
            };
        }
        else
        {
            btn.transition = Selectable.Transition.None;
        }
        btn.onClick.AddListener(OnAdvance);
    }

    void ShowSegment(int i)
    {
        _index = i;
        if (_typing != null) StopCoroutine(_typing);
        ApplyVisualState(i);
        _typing = StartCoroutine(TypeRoutine(Story[i]));
    }

    IEnumerator TypeRoutine(string full)
    {
        _isTyping = true;
        _text.text = "";
        for (int i = 0; i < full.Length; i++)
        {
            _text.text = full.Substring(0, i + 1);
            yield return new WaitForSeconds(typingDelay);
        }
        _isTyping = false;
    }

    public void OnAdvance()
    {
        if (_isTyping)
        {
            // completa o trecho atual imediatamente
            if (_typing != null) StopCoroutine(_typing);
            _text.text = Story[_index];
            _isTyping = false;
            return;
        }

        if (_index >= Story.Length - 1)
        {
            // fim da história -> próxima cena
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogError($"[StoryIntro] A cena '{nextSceneName}' não está nas Build Settings.");
            return;
        }

        ShowSegment(_index + 1);
    }

    // ---------- Helpers ----------

    void ApplyVisualState(int segmentIndex)
    {
        bool isFall = segmentIndex >= FallSegmentIndex;
        bool isLoading = segmentIndex >= LoadingSegmentIndex;

        if (isFall)
        {
            if (isLoading)
            {
                StopBlackFade();
                SetAlpha(_blackOverlay, 1f);
                SetAlpha(_lightOverlay, 0f);
                if (_backgroundImage != null)
                    _backgroundImage.enabled = false;
            }

            bool fadeStillVisible = _blackOverlay != null && _blackOverlay.color.a < 1f;
            SetGifVisible(_errorImage, _errorGifPlayer, fadeStillVisible && !isLoading);
            SetGifVisible(_loadingImage, _loadingGifPlayer, isLoading);
            StopLightFade();
            StartBlackFade();
        }
        else
        {
            SetGifVisible(_errorImage, _errorGifPlayer, segmentIndex >= LightSegmentIndex);
            SetGifVisible(_loadingImage, _loadingGifPlayer, false);
            StopBlackFade();
            if (_backgroundImage != null)
                _backgroundImage.enabled = true;

            SetAlpha(_blackOverlay, 0f);

            if (segmentIndex >= LightSegmentIndex)
                StartLightFade();
            else
            {
                StopLightFade();
                SetAlpha(_lightOverlay, 0f);
            }
        }
    }

    void StartLightFade()
    {
        if (_lightOverlay == null || _lightFade != null) return;
        _lightFade = StartCoroutine(FadeImageAlpha(_lightOverlay, _lightOverlay.color.a, lightOverlayAlpha, lightFadeDuration));
    }

    void StopLightFade()
    {
        if (_lightFade == null) return;
        StopCoroutine(_lightFade);
        _lightFade = null;
    }

    void StartBlackFade()
    {
        if (_blackOverlay == null) return;

        if (_blackOverlay.color.a >= 1f)
        {
            if (_backgroundImage != null)
                _backgroundImage.enabled = false;
            SetGifVisible(_errorImage, _errorGifPlayer, false);
            SetAlpha(_lightOverlay, 0f);
            return;
        }

        if (_backgroundImage != null)
            _backgroundImage.enabled = true;

        if (_blackFade == null)
            _blackFade = StartCoroutine(FadeToBlack());
    }

    void StopBlackFade()
    {
        if (_blackFade == null) return;
        StopCoroutine(_blackFade);
        _blackFade = null;
    }

    IEnumerator FadeToBlack()
    {
        float elapsed = 0f;
        float fromBlack = _blackOverlay.color.a;

        while (elapsed < blackFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = blackFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / blackFadeDuration);
            SetAlpha(_blackOverlay, Mathf.Lerp(fromBlack, 1f, t));
            yield return null;
        }

        SetAlpha(_blackOverlay, 1f);
        if (_backgroundImage != null)
            _backgroundImage.enabled = false;
        SetGifVisible(_errorImage, _errorGifPlayer, false);
        SetAlpha(_lightOverlay, 0f);
        _blackFade = null;
    }

    IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            SetAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(image, to);
        _lightFade = null;
    }

    void SetGifVisible(Image image, StoryGifPlayer player, bool visible)
    {
        if (image == null) return;
        bool wasActive = image.gameObject.activeSelf;
        image.gameObject.SetActive(visible);
        if (visible)
        {
            SetAlpha(image, 1f);
            if (!wasActive)
                player?.PlayFromStart();
        }
        else
        {
            player?.Stop();
        }
    }

    void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }

    Image AddBackground(Sprite sprite, string fallbackResource, Transform parent)
    {
        var sp = sprite != null ? sprite : Resources.Load<Sprite>(Dir + fallbackResource);
        var img = NewImage("LabBackground", parent);
        img.sprite = sp;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (sp != null) ? sp.rect.width / sp.rect.height : 16f / 9f;
        return img;
    }

    Image AddFullScreenImage(string name, Transform parent, Color color)
    {
        var img = NewImage(name, parent);
        img.color = color;
        img.raycastTarget = false;
        StretchFull(img.rectTransform);
        return img;
    }

    Image AddEffectImage(string name, Transform parent, Sprite sprite, Vector2 anchoredPosition)
    {
        var img = NewImage(name, parent);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.gameObject.SetActive(false);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        if (sprite != null)
        {
            float maxW = 620f;
            float maxH = 410f;
            float scale = Mathf.Min(maxW / sprite.rect.width, maxH / sprite.rect.height, 1.25f);
            rt.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }
        else
        {
            rt.sizeDelta = new Vector2(500f, 360f);
        }

        rt.anchoredPosition = anchoredPosition;
        return img;
    }

    StoryGifPlayer AddGifPlayer(Image image, string resourcePath, Sprite fallback)
    {
        if (image == null) return null;

        var player = image.gameObject.AddComponent<StoryGifPlayer>();
        player.Setup(resourcePath, fallback);
        return player;
    }

    Image NewImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
