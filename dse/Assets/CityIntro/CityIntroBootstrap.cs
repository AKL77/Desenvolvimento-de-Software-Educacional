using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Cena da cidade medieval (cenário inicial da 1ª fase, por enquanto como plano
/// de fundo). Mostra o mapa e continua a narrativa numa caixa de texto inferior,
/// trecho por trecho, com efeito de digitação.
///
/// - Quando Alan Turing fala, o retrato dele aparece à direita, acima da caixa
///   (como holograma), e permanece visível durante o diálogo.
/// - Narração e falas de Pitágoras não exigem o Alan (mas ele continua como
///   holograma depois de aparecer).
/// - Ao encontrar o 1º pedaço da máquina, ele aparece no inventário (canto sup. esq.).
/// - No fim, mostra o objetivo: seguir até a entrada do castelo.
///
/// UI construída por código a partir de sprites em Resources/City e Resources/Story.
/// </summary>
public class CityIntroBootstrap : MonoBehaviour
{
    [Tooltip("Segundos entre cada caractere digitado.")]
    public float typingDelay = 0.026f;

    const float RefW = 1920f, RefH = 1080f;

    enum Who { Narr, Pita, Alan }

    // Índice do trecho em que o pedaço é encontrado (mostra o inventário).
    const int ItemSegment = 3;

    static readonly string[] Lines =
    {
        "Pitágoras abre os olhos e percebe que não está mais no laboratório. À sua volta há casas de pedra, ruas estreitas e um grande castelo ao fundo — uma cidade muito antiga, como nos livros de História.",
        "Aos poucos ele entende: viajou no tempo. Não sabe para quando, mas tem certeza de uma coisa: precisa voltar para casa. Ele procura a máquina, mas só encontra um pedaço de metal caído no chão e pensa:",
        "— Isso deve ser parte da máquina... Mas onde estão os outros pedaços?",
        "Pitágoras guarda o pedaço no inventário.\n\nItem encontrado: Pedaço 1 da máquina do tempo.",
        "De repente, uma voz sai dos óculos inteligentes:\n\n— Olá, Pitágoras! Desculpe o susto. Meu nome é Alan Turing e vim ajudar você a voltar para o futuro.",
        "— Oi, Alan. Mas quem é você? E por que a Mystic achou que poderia me ajudar?",
        "— Eu também fui um cientista da computação, no século XX. Muito antes dos computadores de hoje, eu já estudava como uma máquina podia seguir instruções para resolver problemas. Por isso a Mystic me recriou para ajudar você.",
        "— Para voltar para casa, você precisará reconstruir a máquina do tempo, encontrando os pedaços perdidos pelo reino. Mas não será simples: vai precisar de lógica, algoritmos e boas escolhas.",
        "— Entendi! Por onde eu começo?",
        "— Você já encontrou o primeiro pedaço. O próximo está na entrada do castelo: caminhe até lá para encontrá-lo.",
    };

    static readonly Who[] Speakers =
    {
        Who.Narr, Who.Narr, Who.Pita, Who.Narr, Who.Alan,
        Who.Pita, Who.Alan, Who.Alan, Who.Pita, Who.Alan,
    };

    static readonly Color Cyan = new Color(0.60f, 0.82f, 1f);
    static readonly Color Warm = new Color(1f, 0.86f, 0.55f);

    Font _font;
    Text _name, _text, _invCaption, _objText;
    GameObject _panel, _nextButton, _alan, _inventory, _objective, _invWindow;
    Coroutine _typing;
    bool _isTyping, _finished, _alanShown;
    int _index;

    void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        cam.backgroundColor = new Color(0.15f, 0.5f, 0.3f);
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
        var canvasGo = new GameObject("CityCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // Mapa de fundo
        AddBackground("City/city_map", root);

        // Holograma do Alan (à direita, acima da caixa) — começa oculto
        var alanImg = NewImage("Alan", root);
        alanImg.sprite = Resources.Load<Sprite>("City/alan");
        alanImg.color = new Color(0.80f, 0.92f, 1f, 0.95f); // tom de holograma
        alanImg.raycastTarget = false;
        var art = alanImg.rectTransform;
        art.anchorMin = art.anchorMax = new Vector2(1f, 0f);
        art.pivot = new Vector2(1f, 0f);
        float ah = 480f;
        float aw = alanImg.sprite != null ? alanImg.sprite.rect.width * ah / alanImg.sprite.rect.height : ah;
        art.sizeDelta = new Vector2(aw, ah);
        art.anchoredPosition = new Vector2(-140f, 372f);
        _alan = alanImg.gameObject;
        _alan.SetActive(false);

        // Inventário (canto superior esquerdo) — começa oculto
        _inventory = new GameObject("Inventory", typeof(RectTransform));
        _inventory.transform.SetParent(root, false);
        var invrt = _inventory.GetComponent<RectTransform>();
        invrt.anchorMin = invrt.anchorMax = new Vector2(0f, 1f);
        invrt.pivot = new Vector2(0f, 1f);
        invrt.anchoredPosition = new Vector2(40f, -36f);
        invrt.sizeDelta = new Vector2(170f, 210f);
        var invImg = NewImage("Slot", _inventory.transform);
        invImg.sprite = Resources.Load<Sprite>("City/inv_piece");
        invImg.raycastTarget = true; // clicável para abrir o inventário
        var slotrt = invImg.rectTransform;
        slotrt.anchorMin = new Vector2(0.5f, 1f); slotrt.anchorMax = new Vector2(0.5f, 1f);
        slotrt.pivot = new Vector2(0.5f, 1f); slotrt.sizeDelta = new Vector2(170f, 170f);
        slotrt.anchoredPosition = Vector2.zero;
        var slotBtn = invImg.gameObject.AddComponent<Button>();
        slotBtn.transition = Selectable.Transition.None;
        slotBtn.onClick.AddListener(OpenInventory);
        _invCaption = NewText("InvCaption", _inventory.transform, 24, TextAnchor.UpperCenter, Color.white);
        _invCaption.text = "Pedaço 1";
        _invCaption.fontStyle = FontStyle.Bold;
        var caprt = _invCaption.rectTransform;
        caprt.anchorMin = new Vector2(0f, 1f); caprt.anchorMax = new Vector2(1f, 1f);
        caprt.pivot = new Vector2(0.5f, 1f); caprt.offsetMin = new Vector2(0, -210); caprt.offsetMax = new Vector2(0, -174);
        _inventory.SetActive(false);

        // Caixa de texto (parte inferior)
        var panelSp = Resources.Load<Sprite>("Story/story_panel");
        var panel = NewImage("TextBox", root);
        panel.sprite = panelSp;
        panel.raycastTarget = false;
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        if (panelSp != null) prt.sizeDelta = new Vector2(panelSp.rect.width, panelSp.rect.height);
        prt.anchoredPosition = new Vector2(0f, 60f);
        _panel = panel.gameObject;

        // Nome do falante
        _name = NewText("Speaker", panel.transform, 34, TextAnchor.UpperLeft, Cyan);
        _name.fontStyle = FontStyle.Bold;
        var nrt = _name.rectTransform;
        nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(1f, 1f);
        nrt.pivot = new Vector2(0f, 1f);
        nrt.offsetMin = new Vector2(54f, -64f); nrt.offsetMax = new Vector2(-54f, -22f);

        // Texto da narração/fala
        _text = NewText("Story", panel.transform, 40, TextAnchor.UpperLeft, new Color(0.93f, 0.96f, 1f));
        var trt = _text.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(54f, 96f); trt.offsetMax = new Vector2(-54f, -74f);

        // Botão Próximo
        var btnSp = Resources.Load<Sprite>("Story/story_next_normal");
        var btnHi = Resources.Load<Sprite>("Story/story_next_hover");
        var btnImg = NewImage("NextButton", panel.transform);
        btnImg.sprite = btnSp;
        btnImg.raycastTarget = true;
        var brt = btnImg.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(1f, 0f);
        if (btnSp != null) brt.sizeDelta = new Vector2(btnSp.rect.width, btnSp.rect.height);
        brt.anchoredPosition = new Vector2(-34f, 26f);
        AddButton(btnImg.gameObject, btnImg, btnHi);
        _nextButton = btnImg.gameObject;

        // Banner de objetivo (no fim) — começa oculto
        _objective = NewImage("Objective", root, new Color(0.06f, 0.09f, 0.16f, 0.86f)).gameObject;
        var ort = ((Image)_objective.GetComponent<Image>()).rectTransform;
        ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 1f);
        ort.pivot = new Vector2(0.5f, 1f);
        ort.sizeDelta = new Vector2(1200f, 120f);
        ort.anchoredPosition = new Vector2(0f, -40f);
        _objText = NewText("ObjText", _objective.transform, 38, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.6f));
        _objText.fontStyle = FontStyle.Bold;
        _objText.text = "Objetivo: siga até a entrada do castelo para encontrar a próxima peça da máquina do tempo.";
        StretchFull(_objText.rectTransform);
        _objText.rectTransform.offsetMin = new Vector2(30, 8);
        _objText.rectTransform.offsetMax = new Vector2(-30, -8);
        _objective.SetActive(false);

        BuildInventoryWindow(root);
    }

    // Janela do inventário (oculta) — abre ao clicar no slot e mostra os itens.
    void BuildInventoryWindow(Transform root)
    {
        _invWindow = new GameObject("InventoryWindow", typeof(RectTransform));
        _invWindow.transform.SetParent(root, false);
        StretchFull(_invWindow.GetComponent<RectTransform>());

        // Fundo escuro: clicar fora fecha a janela
        var backdrop = NewImage("Backdrop", _invWindow.transform, new Color(0f, 0f, 0f, 0.55f));
        backdrop.raycastTarget = true;
        StretchFull(backdrop.rectTransform);
        var bdBtn = backdrop.gameObject.AddComponent<Button>();
        bdBtn.transition = Selectable.Transition.None;
        bdBtn.onClick.AddListener(CloseInventory);

        // Painel central
        var win = NewImage("Window", _invWindow.transform, new Color(0.11f, 0.14f, 0.22f, 0.98f));
        win.raycastTarget = true; // bloqueia cliques no fundo
        var wrt = win.rectTransform;
        wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
        wrt.sizeDelta = new Vector2(1040f, 620f);

        // Borda clara
        var border = NewImage("Border", win.transform, new Color(0.45f, 0.6f, 0.78f, 1f));
        StretchFull(border.rectTransform);
        var inner = NewImage("Inner", border.transform, new Color(0.11f, 0.14f, 0.22f, 1f));
        var irt = inner.rectTransform; StretchFull(irt);
        irt.offsetMin = new Vector2(6, 6); irt.offsetMax = new Vector2(-6, -6);

        // Título
        var title = NewText("Title", win.transform, 54, TextAnchor.UpperCenter, new Color(1f, 0.92f, 0.6f));
        title.fontStyle = FontStyle.Bold; title.text = "Inventário";
        var ttrt = title.rectTransform;
        ttrt.anchorMin = new Vector2(0f, 1f); ttrt.anchorMax = new Vector2(1f, 1f);
        ttrt.pivot = new Vector2(0.5f, 1f); ttrt.offsetMin = new Vector2(0f, -96f); ttrt.offsetMax = new Vector2(0f, -28f);

        // Ícone do item
        var icon = NewImage("ItemIcon", win.transform);
        icon.sprite = Resources.Load<Sprite>("City/inv_piece");
        var icrt = icon.rectTransform;
        icrt.anchorMin = icrt.anchorMax = new Vector2(0f, 1f); icrt.pivot = new Vector2(0f, 1f);
        icrt.sizeDelta = new Vector2(210f, 210f); icrt.anchoredPosition = new Vector2(60f, -150f);

        // Nome do item
        var iname = NewText("ItemName", win.transform, 42, TextAnchor.UpperLeft, Color.white);
        iname.fontStyle = FontStyle.Bold; iname.text = "Pedaço 1 da máquina do tempo";
        var inrt = iname.rectTransform;
        inrt.anchorMin = new Vector2(0f, 1f); inrt.anchorMax = new Vector2(1f, 1f); inrt.pivot = new Vector2(0f, 1f);
        inrt.offsetMin = new Vector2(300f, -230f); inrt.offsetMax = new Vector2(-50f, -150f);

        // Descrição
        var idesc = NewText("ItemDesc", win.transform, 30, TextAnchor.UpperLeft, new Color(0.82f, 0.87f, 0.97f));
        idesc.text = "Um fragmento metálico da máquina do tempo. Encontre os outros pedaços espalhados pelo reino para reconstruí-la.";
        var idrt = idesc.rectTransform;
        idrt.anchorMin = new Vector2(0f, 1f); idrt.anchorMax = new Vector2(1f, 1f); idrt.pivot = new Vector2(0f, 1f);
        idrt.offsetMin = new Vector2(300f, -400f); idrt.offsetMax = new Vector2(-50f, -240f);

        // Botão Fechar
        var closeImg = NewImage("Close", win.transform, new Color(0.72f, 0.24f, 0.22f, 1f));
        closeImg.raycastTarget = true;
        var crt = closeImg.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f); crt.pivot = new Vector2(0.5f, 0f);
        crt.sizeDelta = new Vector2(260f, 84f); crt.anchoredPosition = new Vector2(0f, 40f);
        var closeTxt = NewText("CloseTxt", closeImg.transform, 36, TextAnchor.MiddleCenter, Color.white);
        closeTxt.fontStyle = FontStyle.Bold; closeTxt.text = "Fechar"; StretchFull(closeTxt.rectTransform);
        var cbtn = closeImg.gameObject.AddComponent<Button>();
        cbtn.transition = Selectable.Transition.ColorTint; cbtn.targetGraphic = closeImg;
        cbtn.onClick.AddListener(CloseInventory);

        _invWindow.SetActive(false);
    }

    public void OpenInventory()
    {
        if (_invWindow != null) _invWindow.SetActive(true);
    }

    public void CloseInventory()
    {
        if (_invWindow != null) _invWindow.SetActive(false);
    }

    void ShowSegment(int i)
    {
        _index = i;

        if (Speakers[i] == Who.Alan) _alanShown = true;
        _alan.SetActive(_alanShown);

        if (i >= ItemSegment) _inventory.SetActive(true);

        switch (Speakers[i])
        {
            case Who.Alan: _name.text = "Alan Turing"; _name.color = Cyan; break;
            case Who.Pita: _name.text = "Pitágoras"; _name.color = Warm; break;
            default: _name.text = ""; break;
        }

        if (_typing != null) StopCoroutine(_typing);
        _typing = StartCoroutine(TypeRoutine(Lines[i]));
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
        if (_finished) return;

        if (_isTyping)
        {
            if (_typing != null) StopCoroutine(_typing);
            _text.text = Lines[_index];
            _isTyping = false;
            return;
        }

        if (_index >= Lines.Length - 1)
        {
            Finish();
            return;
        }

        ShowSegment(_index + 1);
    }

    void Finish()
    {
        _finished = true;
        if (_typing != null) StopCoroutine(_typing);
        _panel.SetActive(false);
        _alan.SetActive(false);
        _objective.SetActive(true);
        // O inventário e o mapa permanecem visíveis como cenário da fase.

        // Botão "Jogar" -> carrega a fase jogável
        var canvasRoot = _objective.transform.parent;
        var btn = NewImage("PlayPhaseButton", canvasRoot, new Color(0.20f, 0.62f, 0.26f, 1f));
        btn.raycastTarget = true;
        var brt = btn.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(420f, 100f);
        brt.anchoredPosition = new Vector2(0f, -190f);
        var label = NewText("Label", btn.transform, 40, TextAnchor.MiddleCenter, Color.white);
        label.fontStyle = FontStyle.Bold;
        label.text = "Jogar a fase ▶";
        StretchFull(label.rectTransform);
        var b = btn.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        b.targetGraphic = btn;
        b.onClick.AddListener(() =>
        {
            if (Application.CanStreamedLevelBeLoaded("Fase1Jogo"))
                SceneManager.LoadScene("Fase1Jogo");
        });
    }

    // ---------- Helpers ----------

    Image AddBackground(string res, Transform parent)
    {
        var sp = Resources.Load<Sprite>(res);
        var img = NewImage("Background", parent);
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
        else btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(OnAdvance);
    }

    Image NewImage(string name, Transform parent) => NewImage(name, parent, Color.white);

    Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text NewText(string name, Transform parent, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = _font;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
