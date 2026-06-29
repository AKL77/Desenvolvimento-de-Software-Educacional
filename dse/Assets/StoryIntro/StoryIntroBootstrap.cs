using System.Collections;
using System.Collections.Generic;
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
/// vai para o próximo. Apenas o botão "Próximo" avança a história.
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

    [Header("Cidade medieval (2a parte da história)")]
    [Tooltip("Fundo da cidade medieval, mostrado quando Pitágoras chega ao reino. " +
             "Opcional: se vazio, carrega Resources/Story/city_bg automaticamente.")]
    public Sprite cityBackgroundSprite;
    [Tooltip("Retrato de Alan Turing. Aparece à direita, acima da caixa de texto, " +
             "somente quando Alan está falando. " +
             "Opcional: se vazio, carrega Resources/Story/alan automaticamente.")]
    public Sprite alanSprite;

    const float RefW = 1920f, RefH = 1080f;
    const string Dir = "Story/";
    const int LightSegmentIndex = 5;
    const int FallSegmentIndex = 7;
    const int LoadingSegmentIndex = 9;

    // A partir deste trecho a história se passa na cidade medieval (fundo novo).
    const int CitySegmentIndex = 10;

    // Trechos em que Alan Turing está falando (ou em que sua imagem surge).
    // O retrato só aparece nesses índices; em narração ou falas do Pitágoras, fica oculto.
    static readonly HashSet<int> AlanSegments = new HashSet<int>
    {
        17, 18, 20, 21, 22, 24
    };

    // Nome de quem fala em cada trecho de diálogo (exibido acima da caixa de texto).
    // Trechos sem entrada aqui são narração e não mostram nome.
    static readonly Dictionary<int, string> Speakers = new Dictionary<int, string>
    {
        { 14, "Pitágoras" },
        { 18, "Alan Turing" },
        { 19, "Pitágoras" },
        { 20, "Alan Turing" },
        { 21, "Alan Turing" },
        { 22, "Alan Turing" },
        { 23, "Pitágoras" },
        { 24, "Alan Turing" },
    };

    static readonly string[] Story =
    {
        // ── 1a parte: o laboratório ─────────────────────────────────────
        "O ano é 2180. Pitágoras é um brilhante cientista da computação na Mistic, a maior empresa de tecnologia do mundo.",
        "Com sua mente lógica, ele lidera um projeto capaz de mudar a humanidade: uma máquina do tempo.",
        "Apaixonado pelo trabalho, passava dias e noites no laboratório, perdendo a noção do tempo.",
        "Naquela noite, sozinho, ele acreditava ter enfim encontrado o código-chave para fazer a máquina funcionar.",
        "Revisou cada linha, conferiu todos os sistemas e, respirando fundo, clicou em \"Executar\".",
        "No mesmo instante, uma luz intensa tomou conta do laboratório, e ele precisou proteger os olhos.",   // [luz/erro]
        "Os equipamentos vibraram, as luzes piscaram sem controle e rachaduras se abriram ao seu redor.",
        "De repente, o chão desapareceu, e Pitágoras caiu com a máquina em um buraco escuro e sem fim.",       // [queda]
        "O vento cortava seu rosto e os sons do laboratório ficaram cada vez mais distantes.",
        "Até que, de repente, tudo parou, e ele sentiu o chão firme sob seus pés.",                            // [loading]

        // ── 2a parte: a cidade medieval (índice 10 em diante) ───────────
        "Aos poucos, Pitágoras abriu os olhos. Ele não estava mais no laboratório: havia casas de pedra, ruas estreitas e um grande castelo ao fundo.",
        "A cidade parecia muito antiga, como nos livros de História. Pitágoras entendeu: havia viajado no tempo.",
        "Ele não sabia para quando — só sabia que precisava voltar para casa.",
        "Procurando pela máquina, viu um pedaço de metal caído ao seu lado.",
        "— Isso deve ser parte da máquina... Mas onde estão os outros pedaços?",
        "Ele guarda o primeiro pedaço.\n\nItem encontrado: Pedaço 1 da máquina do tempo.",
        "De repente, uma voz sai dos óculos inteligentes:\n— Olá, Pitágoras!",
        "Ele leva um susto, e uma imagem aparece à sua frente.",
        "— Meu nome é Alan Turing. Vim ajudar você a voltar para o futuro.",
        "— Prazer, Alan. Mas quem é você, e por que acham que pode me ajudar?",
        "— Também fui cientista da computação. No século XX, estudei como uma máquina pode seguir instruções para resolver problemas.",
        "— Por isso a Mistic me recriou aqui, para guiar você. Para voltar, você terá de reconstruir a máquina, achando os pedaços perdidos pelo reino.",
        "— Não será simples: use lógica, algoritmos e boas escolhas para avançar.",
        "— Entendi! Por onde eu começo?",
        "— O próximo pedaço está na entrada do castelo. Caminhe até lá para encontrá-lo.",
    };

    Text _text;
    Text _speakerText;
    Image _backgroundImage;
    Image _cityImage;
    Image _alanImage;
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
        _cityImage = AddBackground(cityBackgroundSprite, "city_bg", root);
        _cityImage.enabled = false; // só aparece na 2a parte (cidade medieval)
        _lightOverlay = AddFullScreenImage("LightOverlay", root, new Color(1f, 1f, 1f, 0f));
        _errorImage = AddEffectImage("ErrorEffect", root, errorSprite, new Vector2(0f, 110f));
        _blackOverlay = AddFullScreenImage("BlackOverlay", root, new Color(0f, 0f, 0f, 0f));
        _loadingImage = AddEffectImage("LoadingEffect", root, loadingSprite, new Vector2(0f, 110f));
        _errorGifPlayer = AddGifPlayer(_errorImage, "Story/error_gif", errorSprite);
        _loadingGifPlayer = AddGifPlayer(_loadingImage, "Story/loading_gif", loadingSprite);

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
        trt.offsetMax = new Vector2(-54f, -64f); // deixa espaço no topo p/ o nome de quem fala

        // Nome de quem está falando (acima do texto, fonte menor e colorida).
        // Fica oculto em trechos de narração.
        var nameGo = new GameObject("SpeakerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        nameGo.transform.SetParent(panel.transform, false);
        _speakerText = nameGo.GetComponent<Text>();
        _speakerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _speakerText.fontSize = 27;
        _speakerText.fontStyle = FontStyle.Bold;
        _speakerText.alignment = TextAnchor.UpperLeft;
        _speakerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _speakerText.verticalOverflow = VerticalWrapMode.Overflow;
        _speakerText.raycastTarget = false;
        var srt = _speakerText.rectTransform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0f, 1f); // topo-esquerda da caixa
        srt.sizeDelta = new Vector2(600f, 34f);
        srt.anchoredPosition = new Vector2(54f, -16f);
        _speakerText.gameObject.SetActive(false);

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

        // Retrato do Alan Turing: lado direito, acima da caixa de texto.
        // Começa oculto; ApplyVisualState o mostra apenas quando Alan fala.
        float panelHeight = panelSp != null ? panelSp.rect.height : 360f;
        float panelTopY = 60f + panelHeight; // borda superior da caixa de texto
        AddAlanPortrait(root, panelTopY);    // base colada no topo da caixa, sem espaço vazio
    }

    void AddAlanPortrait(Transform parent, float bottomY)
    {
        // Usa o sprite atribuído no Inspector ou, por padrão, o de Resources/Story/alan.
        Sprite sprite = alanSprite != null ? alanSprite : Resources.Load<Sprite>(Dir + "alan");

        _alanImage = NewImage("AlanPortrait", parent);
        _alanImage.sprite = sprite;
        _alanImage.preserveAspect = true;
        _alanImage.raycastTarget = false;

        var rt = _alanImage.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f); // canto inferior direito

        // Altura do retrato: cabe entre o topo da caixa e o topo da tela, com um teto
        // menor para o Alan não ficar grande demais.
        float maxH = Mathf.Min(440f, Mathf.Max(200f, RefH - bottomY - 20f));

        if (sprite != null)
        {
            float scale = Mathf.Min(maxH / sprite.rect.height, 1f);
            rt.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }
        else
        {
            float aspect = 360f / 540f;
            rt.sizeDelta = new Vector2(maxH * aspect, maxH);
        }

        rt.anchoredPosition = new Vector2(-90f, bottomY);
        _alanImage.gameObject.SetActive(false);
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
        ApplySpeaker(i);
        _typing = StartCoroutine(TypeRoutine(Story[i]));
    }

    // Mostra o nome de quem fala (acima do texto), ou oculta em trechos de narração.
    void ApplySpeaker(int segmentIndex)
    {
        if (_speakerText == null) return;

        if (Speakers.TryGetValue(segmentIndex, out string speaker))
        {
            _speakerText.text = speaker;
            _speakerText.color = GetSpeakerColor(speaker);
            _speakerText.gameObject.SetActive(true);
        }
        else
        {
            _speakerText.gameObject.SetActive(false);
        }
    }

    static Color GetSpeakerColor(string speaker)
    {
        switch (speaker)
        {
            case "Alan Turing": return new Color(0.45f, 0.78f, 1f);   // azul claro
            case "Pitágoras":   return new Color(1f, 0.78f, 0.35f);   // âmbar
            default:            return new Color(0.85f, 0.85f, 0.85f);
        }
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
        // 2a parte: a história se passa na cidade medieval.
        if (segmentIndex >= CitySegmentIndex)
        {
            ApplyCityState(segmentIndex);
            return;
        }

        // Fora da cidade, garante cenário/retrato da 2a parte ocultos.
        if (_cityImage != null) _cityImage.enabled = false;
        SetAlanVisible(false);

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

    // Estado visual da 2a parte: cidade medieval ao fundo, sem efeitos do
    // laboratório. O retrato do Alan aparece só nos trechos em que ele fala.
    void ApplyCityState(int segmentIndex)
    {
        StopLightFade();
        StopBlackFade();
        SetAlpha(_lightOverlay, 0f);
        SetAlpha(_blackOverlay, 0f);
        SetGifVisible(_errorImage, _errorGifPlayer, false);
        SetGifVisible(_loadingImage, _loadingGifPlayer, false);

        if (_backgroundImage != null) _backgroundImage.enabled = false;
        if (_cityImage != null) _cityImage.enabled = true;

        SetAlanVisible(AlanSegments.Contains(segmentIndex));
    }

    void SetAlanVisible(bool visible)
    {
        if (_alanImage == null) return;
        if (_alanImage.gameObject.activeSelf != visible)
            _alanImage.gameObject.SetActive(visible);
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
