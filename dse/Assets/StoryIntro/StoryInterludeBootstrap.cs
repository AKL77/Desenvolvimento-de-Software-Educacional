using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Diálogo (estilo visual novel) exibido entre fases, por cima da cena do jogo.
/// Reaproveita o visual da introdução: fundo de cidade medieval, retrato do Alan
/// Turing à direita e caixa de texto na parte inferior com efeito de digitação.
/// O botão "Próximo" avança um trecho por vez; ao terminar, o overlay se destrói
/// e chama o callback recebido (normalmente: carregar a próxima fase).
///
/// É totalmente construído por código a partir dos sprites em Resources/Story/,
/// então não precisa de cena nem prefab próprios. Use o método estático
/// <see cref="TryPlay"/> para disparar o diálogo associado a uma fase concluída.
/// </summary>
public class StoryInterludeBootstrap : MonoBehaviour
{
    // ── Conteúdo: diálogos exibidos ao concluir cada fase (por phaseId) ──────
    // O texto das falas foi resumido a partir do roteiro original.
    class Dialogue
    {
        public string[] lines;
        public Dictionary<int, string> speakers;
        public HashSet<int> alanSegments;
    }

    static readonly Dictionary<string, Dialogue> Dialogues = new Dictionary<string, Dialogue>
    {
        // Final da 1a fase: Alan explica o que é um algoritmo e dá a próxima missão.
        ["phase_1"] = new Dialogue
        {
            lines = new[]
            {
                "— Muito bem, Pitágoras! Essa foi fácil, não foi?",
                "— Foi sim! Eu só precisei escolher os movimentos certos.",
                "— Exatamente. E sabe o que você acabou de criar? Um algoritmo.",
                "— Um algoritmo?",
                "— Um algoritmo é uma sequência de passos para alcançar um objetivo. O seu era chegar à entrada do castelo, e para isso você organizou os movimentos na ordem certa.",
                "— Então um algoritmo pode ser algo simples, como caminhar até um lugar?",
                "— Sim. Mas, dependendo do desafio, eles podem ficar bem mais complexos. Não se preocupe: você vai aprender aos poucos.",
                "— Entendi! E onde está o próximo pedaço da máquina?",
                "— Do outro lado do castelo, perto da saída. O caminho mais curto é atravessá-lo por dentro.",
                "— Mas cuidado: o castelo é protegido por guardas. Se um deles estiver na sua frente, você terá de desviar.",
            },
            speakers = new Dictionary<int, string>
            {
                { 0, "Alan Turing" }, { 1, "Pitágoras" }, { 2, "Alan Turing" },
                { 3, "Pitágoras" },   { 4, "Alan Turing" }, { 5, "Pitágoras" },
                { 6, "Alan Turing" }, { 7, "Pitágoras" },   { 8, "Alan Turing" },
                { 9, "Alan Turing" },
            },
            alanSegments = new HashSet<int> { 0, 2, 4, 6, 8, 9 },
        },
    };

    /// <summary>
    /// Toca o diálogo de fim de fase associado a <paramref name="phaseId"/>, se houver.
    /// Retorna true e executa <paramref name="onComplete"/> ao final; retorna false
    /// (sem criar nada) quando a fase não tem diálogo associado.
    /// </summary>
    public static bool TryPlay(string phaseId, Action onComplete)
    {
        if (string.IsNullOrEmpty(phaseId) || !Dialogues.TryGetValue(phaseId, out Dialogue d))
            return false;

        var go = new GameObject("StoryInterlude");
        var boot = go.AddComponent<StoryInterludeBootstrap>();
        boot._dialogue = d;
        boot._onComplete = onComplete;
        return true;
    }

    const float RefW = 1920f, RefH = 1080f;
    const string Dir = "Story/";
    public float typingDelay = 0.028f;

    Dialogue _dialogue;
    Action _onComplete;

    Text _text;
    Text _speakerText;
    Image _alanImage;
    Coroutine _typing;
    bool _isTyping;
    int _index;

    void Start()
    {
        EnsureEventSystem();
        BuildUI();
        ShowSegment(0);
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
        var canvasGo = new GameObject("StoryInterludeCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // por cima da cena do jogo
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // Bloqueador de cliques: impede interagir com o jogo atrás do diálogo.
        var blocker = NewImage("InputBlocker", root);
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = true;
        StretchFull(blocker.rectTransform);

        // Fundo da cidade medieval (mesmo sprite da introdução).
        AddCityBackground(root);

        // Caixa de texto (parte inferior).
        var panelSp = Resources.Load<Sprite>(Dir + "story_panel");
        var panel = NewImage("TextBox", root);
        panel.sprite = panelSp;
        panel.raycastTarget = false;
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        if (panelSp != null) prt.sizeDelta = new Vector2(panelSp.rect.width, panelSp.rect.height);
        prt.anchoredPosition = new Vector2(0f, 60f);

        // Texto do diálogo.
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
        trt.offsetMin = new Vector2(54f, 96f);
        trt.offsetMax = new Vector2(-54f, -64f);

        // Nome de quem está falando (acima do texto).
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
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0f, 1f);
        srt.sizeDelta = new Vector2(600f, 34f);
        srt.anchoredPosition = new Vector2(54f, -16f);
        _speakerText.gameObject.SetActive(false);

        // Botão "Próximo".
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

        // Retrato do Alan Turing (lado direito, acima da caixa).
        float panelHeight = panelSp != null ? panelSp.rect.height : 360f;
        float panelTopY = 60f + panelHeight;
        AddAlanPortrait(root, panelTopY);
    }

    void AddCityBackground(Transform parent)
    {
        var sp = Resources.Load<Sprite>(Dir + "city_bg");
        var img = NewImage("CityBackground", parent);
        img.sprite = sp;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (sp != null) ? sp.rect.width / sp.rect.height : 16f / 9f;
    }

    void AddAlanPortrait(Transform parent, float bottomY)
    {
        Sprite sprite = Resources.Load<Sprite>(Dir + "alan");

        _alanImage = NewImage("AlanPortrait", parent);
        _alanImage.sprite = sprite;
        _alanImage.preserveAspect = true;
        _alanImage.raycastTarget = false;

        var rt = _alanImage.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);

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
        SetAlanVisible(_dialogue.alanSegments != null && _dialogue.alanSegments.Contains(i));
        ApplySpeaker(i);
        _typing = StartCoroutine(TypeRoutine(_dialogue.lines[i]));
    }

    void ApplySpeaker(int segmentIndex)
    {
        if (_speakerText == null) return;
        if (_dialogue.speakers != null && _dialogue.speakers.TryGetValue(segmentIndex, out string speaker))
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
            case "Alan Turing": return new Color(0.45f, 0.78f, 1f);
            case "Pitágoras":   return new Color(1f, 0.78f, 0.35f);
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
            if (_typing != null) StopCoroutine(_typing);
            _text.text = _dialogue.lines[_index];
            _isTyping = false;
            return;
        }

        if (_index >= _dialogue.lines.Length - 1)
        {
            Finish();
            return;
        }

        ShowSegment(_index + 1);
    }

    void Finish()
    {
        var cb = _onComplete;
        _onComplete = null;
        Destroy(gameObject);
        cb?.Invoke();
    }

    void SetAlanVisible(bool visible)
    {
        if (_alanImage == null) return;
        if (_alanImage.gameObject.activeSelf != visible)
            _alanImage.gameObject.SetActive(visible);
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
