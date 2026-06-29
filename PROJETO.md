# DSE — Documentação do Projeto

Jogo educacional de lógica (estilo code.org) feito em Unity. O aluno arrasta **cartas de comando**
(Andar pra frente, Virar esquerda/direita, Se/Então/Senão, Enquanto/Faça...) para uma sequência e
aperta **Executar** para um personagem (triângulo) andar num grid sobre o mapa, seguindo as
instruções na ordem.

---

## 1. Visão geral da arquitetura

```
┌─────────────┐      define       ┌──────────────┐
│  PhaseData  │ ────────────────► │  PhaseManager │  (Singleton, carrega a fase ativa)
│ (ScriptableO)│                  └──────┬───────┘
└─────────────┘                          │
   availableCards: List<CardData>        │ instancia 1 CardSlot por carta disponível
   gridLevel: GridLevelData               ▼
   lineCount: int                  ┌──────────────┐
                                    │  CardView     │  (carta arrastável na mão do jogador)
                                    └──────┬───────┘
                                           │ drag & drop
                                           ▼
                                    ┌──────────────┐
                                    │  DropZone     │  (1 por linha de sequência)
                                    └──────┬───────┘
                                           │ Executar
                                           ▼
                                  ┌─────────────────┐
                                  │ SequenceExecutor │  (interpreta a lista de CardData)
                                  └────────┬────────┘
                                           │ chama
                                           ▼
                                  ┌─────────────────┐
                                  │  IGridCharacter  │  (GridCharacter ou MockGridCharacter)
                                  └────────┬────────┘
                                           │ lê/anda em
                                           ▼
                                  ┌─────────────────┐
                                  │  GridVisualizer  │  (desenha o grid + calcula posições)
                                  │  GridLevelData   │  (layout walkable/blocked/goal)
                                  └─────────────────┘
```

---

## 2. Sistema de Grid (`Assets/Character & Grid/`)

| Arquivo | Responsabilidade |
|---|---|
| `GridTileType.cs` | enum `Walkable / Blocked / Goal` |
| `GridLevelData.cs` | ScriptableObject: `width`, `height`, `tiles[]`, `startPosition`, `startFacing`, `goalPosition` |
| `GridTileView.cs` | componente visual de 1 tile (cor por tipo + ícone de estrela no goal) |
| `GridVisualizer.cs` | constrói o grid (`width x height` tiles) dentro do `MapPanel`, calcula a posição de cada célula |
| `TriangleGraphic.cs` | `UI.Graphic` customizado — desenha o triângulo branco do personagem |
| `IGridCharacter.cs` | interface: `MoveForward / TurnLeft / TurnRight / IsAtGoal / ResetToStart / IsWalkable / IsGoal / GetPositionAhead` |
| `GridCharacter.cs` | implementação real (DOTween anima posição/rotação) |
| `MockGridCharacter.cs` | implementação de teste (sem grid real, tudo "andável") |

### Fluxograma de execução do grid

```
GridVisualizer.Start()
  └─ BuildGrid() — instancia width*height GridTileView dentro do MapPanel
       tileW = MapPanel.rect.width / levelData.width
       tileH = MapPanel.rect.height / levelData.height

GridCharacter.Start()
  └─ ResetToStart() — usa visualizer.GetTileAnchoredPosition(startPosition)
       ⚠ depende do grid já ter sido construído (tileW/tileH calculados)
       Por isso GridVisualizer tem [DefaultExecutionOrder(-100)]

SequenceExecutor.ExecuteSequence()
  para cada CardData na sequência:
    MoveForward  → character.MoveForward() (verifica IsWalkable da casa na frente)
    TurnLeft/Right → gira 90° no lugar (não move), relativo à direção atual
    If           → avalia condição (WallAhead/PathClear na casa real na frente)
                     verdadeiro → executa o bloco "Então"
                     falso      → pula para o bloco "Senão" (Else)
    RepeatUntil  → repete o bloco interno até a condição ser satisfeita (== "Enquanto")
    Repeat       → repete o bloco interno N vezes (repeatValue)
  a cada passo: character.IsAtGoal()? → VictoryPanel.Show()
  fim da sequência sem chegar no goal → character.ResetToStart() (OnFailure)
```

### Troca de grid por fase
`PhaseManager.LoadPhase()` aplica `phase.gridLevel` no `GridVisualizer` e reseta o `GridCharacter`,
permitindo que cada fase tenha seu próprio layout de grid (ex: Fase 1 = 5x5 linha reta, Fase 2 = 6x6
com curva).

---

## 3. Sistema de Cartas (`Assets/Cards/`, `Assets/Phases/`)

| Arquivo | Responsabilidade |
|---|---|
| `CardData.cs` | ScriptableObject: `cardId`, `displayName`, `cardType`, `cardAction`, `artwork`, `cardColor`, `repeatValue`, `conditionTarget`, `description` |
| `CardView.cs` | componente da carta na UI: aplica cor/sprite/texto, drag & drop (ghost card) |
| `CardSlot.prefab` | prefab visual da carta (fundo colorido + texto + badge de repeat) |
| `PhaseData.cs` | define quais `CardData` ficam disponíveis numa fase (`availableCards`), `lineCount`, `gridLevel` |
| `PhaseManager.cs` | carrega a fase ativa: instancia as cartas disponíveis, configura o grid, reconstrói a sequência |
| `CardCarousel.cs` | scroll horizontal das cartas disponíveis (setas esquerda/direita) |
| `DropZone.cs` | uma "linha" de sequência onde uma carta pode ser solta |
| `SequenceManager.cs` | cria as `lineCount` `DropZone`s, monta a lista final de `CardData` pra executar |
| `SequenceExecutor.cs` | interpreta a lista — ver fluxograma acima |

### `CardAction` (em `CardData.cs`)
```
Movement: MoveForward, TurnLeft, TurnRight
Condition: If, Else, EndIf
Loop: Repeat, EndRepeat, RepeatUntil   (RepeatUntil = "Enquanto")
```

### Limite de cartas por fase
Cada `PhaseData.availableCards` é uma lista fechada — a Fase 1 só tem `Card_MoveForward`; a Fase 2
adiciona `Card_TurnLeft` + `Card_TurnRight`. `PhaseManager.LoadPhase()` só instancia o que está
nessa lista.

---

## 4. UI / painéis (`Assets/UIManager.cs`)

```
Estado inicial: mapa em FULLSCREEN
  SidePanel (sequência) e CardsPanel (mão de cartas) — INATIVOS

Usuário clica no botão do mapa → UIManager.SwitchToCompact()
  mapPanel anima pra um canto (DOTween)
  após 0.2s (DOVirtual.DelayedCall):
    sidePanel.SetActive(true)
    cardsPanel.SetActive(true)
    Canvas.ForceUpdateCanvases() + LayoutRebuilder.ForceRebuildLayoutImmediate(cardsPanel)
```

### 🐛 Bug real encontrado nesta sessão (corrigido)

**Sintoma:** a carta na mão do jogador não aparecia — área ficava branca/vazia, mesmo com todos os
dados (sprite, cor, ativo, layer) comprovadamente corretos via log.

**Causa raiz (2 bugs em cadeia):**

1. **`CardCarousel.basePosition` capturado tarde demais.** `PhaseManager.Start()` chama
   `cardCarousel.Refresh()` **antes** de `CardCarousel.Start()` rodar (porque `CardsPanel` está
   inativo no início — `Awake`/`Start` de filhos de um objeto inativo só rodam quando ele é
   ativado). Isso fazia o carrossel mover o `Content` pra `x=0` em vez da posição correta
   (`x=-161.8`), empurrando a carta pra fora da área visível.
   → **Fix:** captura "lazy" de `basePosition` na primeira chamada real de `SnapToIndex`, em vez de
   depender de `Awake`/`Start`. Ver `CardCarousel.EnsureBasePosition()`.

2. **`RectMask2D` com clipping "preso".** Mesmo com a posição corrigida, o `Viewport` (que tem
   `RectMask2D`) calculado enquanto `CardsPanel` ainda estava inativo nunca recalculava o clip rect
   corretamente depois de ativado, deixando os filhos permanentemente invisíveis. Confirmado
   desabilitando o componente manualmente — a carta passou a aparecer.
   → **Fix aplicado: `RectMask2D` foi DESABILITADO** (ver "Dívidas técnicas conhecidas" abaixo —
   isso não é a solução definitiva, é um workaround).

**Como foi confirmado:** instanciamos uma carta de teste direto no Canvas raiz (fora do
`CardsPanel`/`Viewport`) — ela renderizou perfeitamente, provando que `CardView`/`CardSlot` estavam
100% corretos e o problema era específico da hierarquia `CardsPanel > Viewport (RectMask2D) >
Content`.

### ⚠️ Dívidas técnicas conhecidas

**`RectMask2D` do `Viewport` (`UI/CardsPanel/Viewport`) está desabilitado.**
- **Por quê:** o clip rect dele é calculado enquanto `CardsPanel` está inativo (modo mapa-cheio
  inicial) e nunca é recalculado corretamente depois que o painel é ativado em
  `UIManager.SwitchToCompact()` — mesmo forçando `Canvas.ForceUpdateCanvases()` e
  `LayoutRebuilder.ForceRebuildLayoutImmediate()` depois da ativação, o clipping continuava
  escondendo todos os filhos. Desabilitar o componente foi o único fix que funcionou.
- **O que isso quebra:** o `CardCarousel` existe pra paginar cartas quando há mais do que cabem na
  tela (usando as setas esquerda/direita). Sem o `RectMask2D`, não há mais recorte visual — se uma
  fase tiver muitas cartas disponíveis, elas vão simplesmente ficar visíveis em fileira, podendo
  ultrapassar a área do `CardsPanel` visualmente (overflow), em vez de ficar escondidas/paginadas.
- **Próximo passo recomendado:** investigar uma forma de recalcular o clip rect do `RectMask2D`
  explicitamente depois da ativação (ex: via reflection chamando o método interno de rebuild), ou
  trocar a abordagem de clipping (ex: um `Mask` comum com `Image`, ou nunca deixar `CardsPanel`
  ficar inativo — manter sempre ativo e controlar visibilidade só por `CanvasGroup.alpha`/posição).
  Isso só vale a pena revisitar quando alguma fase tiver cartas suficientes pra precisar do
  carrossel de verdade (hoje a Fase 1 tem 1 carta, a Fase 2 tem 3 — nenhuma estressa o recorte).

---

## 5. Fases existentes

| Fase | Grid | Cartas disponíveis | Objetivo |
|---|---|---|---|
| Fase 1 (`Phase_1.asset`) | 5x5, linha reta na row do meio | `Andar pra frente` | andar até o fim da linha |
| Fase 2 (`Phase_2.asset`) | 6x6, L-shape (anda e vira 1x) | `Andar pra frente`, `Virar esquerda`, `Virar direita` | virar e completar o L |

---

## 6. Editor tooling (`Assets/Editor/`)

| Script | Menu | Função |
|---|---|---|
| `GridSetupWizard.cs` | `DSE > Setup Grid (Etapa 1)` | cria `GridLevel_Phase1.asset`, prefabs `GridTile`/`CharacterTriangle`/`VictoryPanel` |
| `GridSceneConfigurator.cs` | `DSE > Configure Scene (MapaMax)` | linka `GridVisualizer`, `GridCharacter`, `VictoryPanel`, `PhaseManager.gridVisualizer` na cena |
| `Phase2SetupWizard.cs` | `DSE > Setup Phase 2 (Turns)` | cria `Card_TurnRight`, `GridLevel_Phase2` (6x6 L-shape), `Phase_2.asset` |

---

## 7. Próximas etapas (do `DSE_GRID_STATUS.md`)

| Etapa | O que falta |
|---|---|
| Etapa 3 | Tiles Blocked + condição `WallAhead`/`PathClear` usadas no card `If` em fases reais (lógica já existe em `SequenceExecutor`) |
| Etapa 4 | Cards `Repeat`/`RepeatUntil` com indicador visual de loop ativo |
| Etapa 5 | Seleção de fase no mapa-mundi (hoje só existe 1 `currentPhase` fixo por cena) |
| Etapa 6 | Highlight do slot ativo durante a execução da sequência |
| Etapa 7 | Animação de vitória/derrota mais elaborada |
| Arte das cartas | Visual rústico/medieval (papel rasgado) com ícone + categoria + descrição — ficou pra trás depois da regressão visual desta sessão; reverti pro card colorido simples que já funcionava |
