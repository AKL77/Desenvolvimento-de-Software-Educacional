using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Fase jogável baseada em cartas de movimento (estrutura reutilizável).
///
/// O jogador clica nos óculos inteligentes (canto inferior direito) para abrir o
/// painel com a paleta de cartas, a área "Sequência de ações" e o botão Executar.
/// As cartas são arrastadas da paleta para a sequência. Ao executar, o personagem
/// (Pitágoras) percorre o caminho do mapa seguindo as cartas. Se chegar ao objetivo,
/// a fase é concluída; se sair do caminho ou errar o destino, volta ao início.
///
/// Toda a fase é definida por um <see cref="LevelData"/> (mapa, grade, início,
/// objetivo, caminho permitido, cartas, mensagens). Para criar novas fases, basta
/// montar outro LevelData (ver <see cref="BuildFase1Level"/>).
/// </summary>
public class PhaseController : MonoBehaviour
{
    public enum Move { Forward, Back, Right, Left }

    [System.Serializable]
    public class CardDef
    {
        public Move action;
        public string sprite;   // Resources path
    }

    /// <summary>Configuração completa e reutilizável de uma fase.</summary>
    public class LevelData
    {
        public string mapResource;
        public string characterResource;
        public string glassesResource;

        // Grade sobre o mapa (em pixels da imagem do mapa).
        public float mapWidth = 1920f;
        public float mapHeight = 1080f;
        public float cellSize = 240f;

        public Vector2Int startCell;
        public Vector2Int goalCell;
        public HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();

        public List<CardDef> cards = new List<CardDef>();
        public int maxCards = 12;
        public float moveDuration = 0.45f;

        public string msgSuccess;
        public string msgWrongEnd;
        public string msgOffPath;
        public string nextSceneOnWin = "MainMenu";
    }

    const float RefW = 1920f, RefH = 1080f;
    const float CardW = 116f, CardH = 155f;
    const float CharDispW = 150f, CharDispH = 200f, CharRaise = 0.04f;

    LevelData _lvl;

    RectTransform _canvasRT, _mapRT, _charRT, _glassesRT;
    Image _charImg;
    GameObject _panel, _ghost, _winButton;
    RectTransform _ghostRT, _seqZoneRT;
    Text _messageText;
    GameObject _messageBox;

    readonly List<Move> _seq = new List<Move>();
    Font _font;
    bool _running, _won;
    Vector2Int _cur;
    Coroutine _msgRoutine;

    void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _lvl = BuildFase1Level();
        EnsureCamera();
        EnsureEventSystem();
        BuildUI();
        ResetCharacter(true);
    }

    // ----------------------------------------------------------------
    //  DADOS DA FASE 1 (base para novas fases — duplicar e ajustar)
    // ----------------------------------------------------------------
    LevelData BuildFase1Level()
    {
        var lvl = new LevelData
        {
            mapResource = "Phase1/map",
            characterResource = "Phase1/character",
            glassesResource = "Phase1/glasses",
            mapWidth = 1920f, mapHeight = 1080f, cellSize = 240f,
            startCell = new Vector2Int(1, 1),
            goalCell = new Vector2Int(6, 1),
            moveDuration = 0.45f,
            maxCards = 12,
            msgSuccess = "Parabéns! Você levou Pitágoras até o castelo.",
            msgWrongEnd = "Ops! Essa sequência não levou Pitágoras até o objetivo. Tente reorganizar as cartas.",
            msgOffPath = "Pitágoras saiu do caminho! Revise a sequência de ações.",
            nextSceneOnWin = "MainMenu",
        };
        // Caminho permitido (colunas, linhas) — começa à esquerda e entra no castelo
        int[,] path =
        {
            {1,1},{2,1},{3,1},{3,2},{4,2},{5,2},{5,1},{6,1}
        };
        for (int i = 0; i < path.GetLength(0); i++)
            lvl.pathCells.Add(new Vector2Int(path[i, 0], path[i, 1]));

        lvl.cards.Add(new CardDef { action = Move.Forward, sprite = "Phase1/card_forward" });
        lvl.cards.Add(new CardDef { action = Move.Back, sprite = "Phase1/card_back" });
        lvl.cards.Add(new CardDef { action = Move.Right, sprite = "Phase1/card_right" });
        lvl.cards.Add(new CardDef { action = Move.Left, sprite = "Phase1/card_left" });
        return lvl;
    }

    static Vector2Int Delta(Move m)
    {
        switch (m)
        {
            case Move.Forward: return new Vector2Int(0, -1); // para cima
            case Move.Back: return new Vector2Int(0, 1);     // para baixo
            case Move.Right: return new Vector2Int(1, 0);
            case Move.Left: return new Vector2Int(-1, 0);
        }
        return Vector2Int.zero;
    }

    Sprite CardSprite(Move m)
    {
        foreach (var c in _lvl.cards)
            if (c.action == m) return Resources.Load<Sprite>(c.sprite);
        return null;
    }

    // ----------------------------------------------------------------
    //  Setup
    // ----------------------------------------------------------------
    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.68f, 0.38f);
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
        var canvasGo = new GameObject("PhaseCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasRT = canvasGo.GetComponent<RectTransform>();
        var root = canvasGo.transform;

        // Mapa de fundo (cobre a tela)
        var map = NewImage("Map", root, Color.white);
        map.sprite = Resources.Load<Sprite>(_lvl.mapResource);
        map.raycastTarget = false;
        _mapRT = map.rectTransform;
        _mapRT.anchorMin = _mapRT.anchorMax = _mapRT.pivot = new Vector2(0.5f, 0.5f);
        var fit = map.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = _lvl.mapWidth / _lvl.mapHeight;

        // Personagem (filho do mapa para alinhar com o caminho)
        _charImg = NewImage("Pitagoras", _mapRT, Color.white);
        _charImg.sprite = Resources.Load<Sprite>(_lvl.characterResource);
        _charImg.raycastTarget = false;
        _charImg.preserveAspect = true;
        _charRT = _charImg.rectTransform;

        // Mensagem (topo) — oculta
        _messageBox = NewImage("MessageBox", root, new Color(0.06f, 0.09f, 0.16f, 0.9f)).gameObject;
        var mrt = ((Image)_messageBox.GetComponent<Image>()).rectTransform;
        mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 1f);
        mrt.pivot = new Vector2(0.5f, 1f);
        mrt.sizeDelta = new Vector2(1300f, 120f);
        mrt.anchoredPosition = new Vector2(0f, -30f);
        _messageText = NewText("Msg", _messageBox.transform, 36, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f));
        _messageText.fontStyle = FontStyle.Bold;
        StretchFull(_messageText.rectTransform);
        _messageText.rectTransform.offsetMin = new Vector2(24, 8);
        _messageText.rectTransform.offsetMax = new Vector2(-24, -8);
        _messageBox.SetActive(false);

        BuildPanel(root);
        BuildGlasses(root);
    }

    void BuildGlasses(Transform root)
    {
        var g = NewImage("Glasses", root, Color.white);
        g.sprite = Resources.Load<Sprite>(_lvl.glassesResource);
        g.preserveAspect = true;
        var rt = g.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(150f, 150f);
        rt.anchoredPosition = new Vector2(-40f, 40f);
        _glassesRT = rt;
        var btn = g.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = g;
        btn.onClick.AddListener(TogglePanel);

        // dica
        var hint = NewText("GlassesHint", g.transform, 22, TextAnchor.UpperCenter, Color.white);
        hint.text = "Cartas";
        hint.fontStyle = FontStyle.Bold;
        var hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(1f, 0f); hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0, 28); hrt.anchoredPosition = new Vector2(0, -2);
    }

    void BuildPanel(Transform root)
    {
        _panel = NewImage("CardPanel", root, new Color(0.10f, 0.13f, 0.21f, 0.96f)).gameObject;
        var prt = ((Image)_panel.GetComponent<Image>()).rectTransform;
        prt.anchorMin = new Vector2(0f, 0f); prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.offsetMin = new Vector2(0f, 0f); prt.offsetMax = new Vector2(0f, 300f);

        // Paleta de cartas (esquerda)
        var palLabel = NewText("PalLabel", _panel.transform, 26, TextAnchor.UpperLeft, new Color(0.8f, 0.88f, 1f));
        palLabel.text = "Cartas:"; palLabel.fontStyle = FontStyle.Bold;
        var plrt = palLabel.rectTransform;
        plrt.anchorMin = plrt.anchorMax = new Vector2(0f, 1f); plrt.pivot = new Vector2(0f, 1f);
        plrt.sizeDelta = new Vector2(200, 30); plrt.anchoredPosition = new Vector2(24, -14);

        for (int i = 0; i < _lvl.cards.Count; i++)
        {
            var def = _lvl.cards[i];
            var card = MakeCard(def.action, _panel.transform);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = new Vector2(24 + i * (CardW + 8), -50);
            var drag = card.AddComponent<CardDrag>();
            drag.controller = this; drag.fromPalette = true; drag.action = (int)def.action;
        }

        // Área da sequência (centro)
        var seqLabel = NewText("SeqLabel", _panel.transform, 26, TextAnchor.UpperLeft, new Color(0.8f, 0.88f, 1f));
        seqLabel.text = "Sequência de ações:"; seqLabel.fontStyle = FontStyle.Bold;
        var slrt = seqLabel.rectTransform;
        slrt.anchorMin = slrt.anchorMax = new Vector2(0f, 1f); slrt.pivot = new Vector2(0f, 1f);
        slrt.sizeDelta = new Vector2(500, 30); slrt.anchoredPosition = new Vector2(560, -14);

        var seqZone = NewImage("SeqZone", _panel.transform, new Color(1f, 1f, 1f, 0.06f));
        seqZone.raycastTarget = true;
        _seqZoneRT = seqZone.rectTransform;
        _seqZoneRT.anchorMin = _seqZoneRT.anchorMax = new Vector2(0f, 1f); _seqZoneRT.pivot = new Vector2(0f, 1f);
        _seqZoneRT.sizeDelta = new Vector2(960, 170);
        _seqZoneRT.anchoredPosition = new Vector2(560, -50);

        // Botões (direita)
        var exec = MakeButton("EXECUTAR", _panel.transform, new Color(0.20f, 0.62f, 0.26f), 34);
        var ert = exec.GetComponent<RectTransform>();
        ert.anchorMin = ert.anchorMax = new Vector2(1f, 1f); ert.pivot = new Vector2(1f, 1f);
        ert.sizeDelta = new Vector2(330, 96); ert.anchoredPosition = new Vector2(-30, -50);
        exec.GetComponent<Button>().onClick.AddListener(OnExecute);

        var clr = MakeButton("Limpar", _panel.transform, new Color(0.55f, 0.28f, 0.28f), 28);
        var clrt = clr.GetComponent<RectTransform>();
        clrt.anchorMin = clrt.anchorMax = new Vector2(1f, 1f); clrt.pivot = new Vector2(1f, 1f);
        clrt.sizeDelta = new Vector2(330, 70); clrt.anchoredPosition = new Vector2(-30, -158);
        clr.GetComponent<Button>().onClick.AddListener(ClearSequence);

        _panel.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Cartas / sequência
    // ----------------------------------------------------------------
    GameObject MakeCard(Move action, Transform parent)
    {
        var img = NewImage("Card_" + action, parent, Color.white);
        img.sprite = CardSprite(action);
        img.raycastTarget = true;
        img.rectTransform.sizeDelta = new Vector2(CardW, CardH);
        return img.gameObject;
    }

    void RebuildSequence()
    {
        // limpa
        for (int i = _seqZoneRT.childCount - 1; i >= 0; i--)
            Destroy(_seqZoneRT.GetChild(i).gameObject);

        for (int i = 0; i < _seq.Count; i++)
        {
            var card = MakeCard(_seq[i], _seqZoneRT);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f); crt.pivot = new Vector2(0f, 0.5f);
            crt.sizeDelta = new Vector2(CardW - 6, CardH - 8);
            crt.anchoredPosition = new Vector2(10 + i * (CardW - 6 + 8), 0);
            var drag = card.AddComponent<CardDrag>();
            drag.controller = this; drag.fromPalette = false; drag.seqIndex = i;
        }
    }

    int IndexAt(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_seqZoneRT, e.position, null, out var lp);
        // lp.x relativo ao canto esquerdo (pivot 0)
        float step = (CardW - 6) + 8;
        int idx = Mathf.Clamp(Mathf.RoundToInt((lp.x - 10) / step), 0, _seq.Count);
        return idx;
    }

    bool OverSeqZone(PointerEventData e)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_seqZoneRT, e.position, null);
    }

    // chamado pelo CardDrag
    public void BeginGhost(int action, PointerEventData e)
    {
        if (_running) return;
        _ghost = NewImage("Ghost", _canvasRT, Color.white).gameObject;
        var img = _ghost.GetComponent<Image>();
        img.sprite = CardSprite((Move)action);
        img.raycastTarget = false;
        _ghostRT = img.rectTransform;
        _ghostRT.sizeDelta = new Vector2(CardW, CardH);
        MoveGhost(e);
    }

    public void MoveGhost(PointerEventData e)
    {
        if (_ghostRT == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, e.position, null, out var lp);
        _ghostRT.anchoredPosition = lp;
    }

    public void EndDrag(CardDrag drag, PointerEventData e)
    {
        if (_ghost != null) { Destroy(_ghost); _ghost = null; _ghostRT = null; }
        if (_running) return;

        bool over = OverSeqZone(e);
        if (drag.fromPalette)
        {
            if (over && _seq.Count < _lvl.maxCards)
                _seq.Insert(Mathf.Clamp(IndexAt(e), 0, _seq.Count), (Move)drag.action);
        }
        else
        {
            int from = drag.seqIndex;
            if (from < 0 || from >= _seq.Count) return;
            var act = _seq[from];
            _seq.RemoveAt(from);
            if (over)
                _seq.Insert(Mathf.Clamp(IndexAt(e), 0, _seq.Count), act);
            // se soltou fora da área, a carta é removida (já removida acima)
        }
        RebuildSequence();
    }

    void ClearSequence()
    {
        if (_running) return;
        _seq.Clear();
        RebuildSequence();
    }

    void TogglePanel()
    {
        if (_panel == null) return;
        bool open = !_panel.activeSelf;
        _panel.SetActive(open);
        // Sobe os óculos para cima do painel quando aberto (evita sobreposição);
        // volta ao canto inferior quando fechado.
        if (_glassesRT != null)
            _glassesRT.anchoredPosition = new Vector2(-40f, open ? 320f : 40f);
    }

    // ----------------------------------------------------------------
    //  Execução
    // ----------------------------------------------------------------
    void OnExecute()
    {
        if (_running || _won) return;
        if (_seq.Count == 0) { ShowMessage("Monte uma sequência de cartas primeiro!", 2.5f); return; }
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        _running = true;
        HideMessage();
        ResetCharacter(true);
        yield return new WaitForSeconds(0.2f);

        foreach (var move in _seq)
        {
            Vector2Int next = _cur + Delta(move);
            if (!_lvl.pathCells.Contains(next))
            {
                yield return StartCoroutine(ErrorFlash());
                ShowMessage(_lvl.msgOffPath, 0f);
                yield return new WaitForSeconds(1.2f);
                ResetCharacter(true);
                _running = false;
                yield break;
            }
            yield return StartCoroutine(MoveTo(next));
            _cur = next;
        }

        if (_cur == _lvl.goalCell)
        {
            _won = true;
            ShowMessage(_lvl.msgSuccess, 0f);
            ShowWinButton();
        }
        else
        {
            ShowMessage(_lvl.msgWrongEnd, 0f);
            yield return new WaitForSeconds(1.2f);
            ResetCharacter(true);
        }
        _running = false;
    }

    IEnumerator MoveTo(Vector2Int cell)
    {
        Vector2 a = CellNorm(_cur);
        Vector2 b = CellNorm(cell);
        // vira o boneco na horizontal
        if (cell.x > _cur.x) _charRT.localScale = new Vector3(1, 1, 1);
        else if (cell.x < _cur.x) _charRT.localScale = new Vector3(-1, 1, 1);

        float t = 0f, dur = Mathf.Max(0.05f, _lvl.moveDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            SetCharNorm(Vector2.Lerp(a, b, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        SetCharNorm(b);
    }

    IEnumerator ErrorFlash()
    {
        for (int i = 0; i < 3; i++)
        {
            _charImg.color = new Color(1f, 0.4f, 0.4f);
            yield return new WaitForSeconds(0.12f);
            _charImg.color = Color.white;
            yield return new WaitForSeconds(0.12f);
        }
    }

    void ResetCharacter(bool toStart)
    {
        if (toStart) _cur = _lvl.startCell;
        _charImg.color = Color.white;
        _charRT.localScale = Vector3.one;
        SetCharNorm(CellNorm(_cur));
    }

    // posição normalizada (0..1) do centro da célula no rect do mapa
    Vector2 CellNorm(Vector2Int cell)
    {
        float fx = (cell.x + 0.5f) * _lvl.cellSize / _lvl.mapWidth;
        float ay = 1f - (cell.y + 0.5f) * _lvl.cellSize / _lvl.mapHeight;
        return new Vector2(fx, ay);
    }

    void SetCharNorm(Vector2 n)
    {
        float hw = (CharDispW * 0.5f) / _lvl.mapWidth;
        float hh = (CharDispH * 0.5f) / _lvl.mapHeight;
        float ay = n.y + CharRaise;
        _charRT.anchorMin = new Vector2(n.x - hw, ay - hh);
        _charRT.anchorMax = new Vector2(n.x + hw, ay + hh);
        _charRT.offsetMin = Vector2.zero;
        _charRT.offsetMax = Vector2.zero;
    }

    // ----------------------------------------------------------------
    //  Mensagens / vitória
    // ----------------------------------------------------------------
    void ShowMessage(string text, float autoHide)
    {
        _messageText.text = text;
        _messageBox.SetActive(true);
        if (_msgRoutine != null) StopCoroutine(_msgRoutine);
        if (autoHide > 0f) _msgRoutine = StartCoroutine(HideAfter(autoHide));
    }

    IEnumerator HideAfter(float s)
    {
        yield return new WaitForSeconds(s);
        HideMessage();
    }

    void HideMessage()
    {
        if (_messageBox != null) _messageBox.SetActive(false);
    }

    void ShowWinButton()
    {
        if (_winButton != null) return;
        _winButton = MakeButton("Concluir", _canvasRT, new Color(0.20f, 0.62f, 0.26f), 34);
        var rt = _winButton.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(360, 96); rt.anchoredPosition = new Vector2(0, -170);
        _winButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (Application.CanStreamedLevelBeLoaded(_lvl.nextSceneOnWin))
                SceneManager.LoadScene(_lvl.nextSceneOnWin);
        });
        if (_panel != null) _panel.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Helpers de UI
    // ----------------------------------------------------------------
    Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    Text NewText(string name, Transform parent, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = color; t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    GameObject MakeButton(string label, Transform parent, Color color, int fontSize)
    {
        var img = NewImage("Btn_" + label, parent, color);
        img.raycastTarget = true;
        var btn = img.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = img;
        var t = NewText("Label", img.transform, fontSize, TextAnchor.MiddleCenter, Color.white);
        t.fontStyle = FontStyle.Bold; t.text = label;
        StretchFull(t.rectTransform);
        return img.gameObject;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
