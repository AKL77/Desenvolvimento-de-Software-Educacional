# Estrada autotiling no grid da Fase 1

## Contexto

O grid da Fase 1 (`GridLevel_Phase1.asset`, 5x5) é renderizado por `GridVisualizer.cs`, que
instancia um `GridTile.prefab` por célula e chama `GridTileView.Setup(...)`. Antes desta
mudança, o fundo de cada célula era só uma cor sólida (`Image.color`) variando por
`GridTileType` (Walkable/Blocked/Goal). O pedido foi trocar isso por uma estrada visual de
terra, usando sprites do pacote CC0 `kenney_medieval-rts`, que segue o caminho andável até o
castelo (goal).

## Tiles usados (pasta `kenney_medieval-rts/PNG/Retina/Tile/`)

| Arquivo no projeto | Origem no pack | Uso |
|---|---|---|
| `Assets/Images/grama.png` | `medievalTile_58.png` | Fundo (grama lisa) de toda célula não bloqueada, atrás da estrada |
| `Assets/Images/estrada_horizontal.png` | `medievalTile_04.png` | Trecho reto na horizontal (liga esquerda+direita) |
| `Assets/Images/estrada_vertical.png` | `medievalTile_03.png` | Trecho reto na vertical (liga cima+baixo) |
| `Assets/Images/estrada_curva.png` | `medievalTile_18.png` | Curva de 90° (liga 2 lados adjacentes) |
| `Assets/Images/estrada_t.png` | `medievalTile_06.png` | Bifurcação em T (liga 3 lados) |
| `Assets/Images/estrada_ponta.png` | `medievalTile_31.png` | Ponta morta (liga 1 lado só) |
| `Assets/Images/castelo.png` | `medievalStructure_02.png` | Ícone do goal (substitui a estrela TextMeshPro) |

Todos importados como `spriteMode: 2` (Multiple, convenção que o Unity já usa nesse projeto),
100 pixels-per-unit, pivot central.

## Como o grid decide o que desenhar

`GridLevelData.tiles` é um array flat `GridTileType[]`, indexado `tiles[y*width + x]`
(linha-major, origem embaixo-à-esquerda). `GridVisualizer.BuildGrid()` percorre todas as
células e, pra cada uma que não é `Blocked`, calcula uma **bitmask de conectividade** com os 4
vizinhos (cima/baixo/esquerda/direita), considerando "conectado" qualquer vizinho que também
não seja `Blocked`:

```
1 = cima, 2 = baixo, 4 = esquerda, 8 = direita
```

Essa mask é passada pra `GridTileView.Setup(type, roadMask)`, que decide o sprite e a rotação
em `ApplyRoad(mask)`:

- **1 vizinho conectado** → ponta morta (`estrada_ponta`)
- **2 vizinhos opostos** (cima+baixo ou esquerda+direita) → reto dedicado, sem rotação
  (`estrada_vertical` ou `estrada_horizontal`)
- **2 vizinhos adjacentes** (ex: cima+direita) → curva (`estrada_curva`), rotacionada
- **3 vizinhos** → T (`estrada_t`), rotacionado
- **4 vizinhos** → cross (reaproveita o T a 0°, não existe sprite de cruzamento dedicado —
  não ocorre na Fase 1 atual)

### Orientação-base de cada sprite (antes de rotacionar)

Como a arte do pack não é modular (cada peça já vem com uma curva "pintada"), cada sprite tem
uma única orientação de referência a 0°, documentada em `GridTileView.cs`:

- `roadCorner` liga **esquerda+baixo**
- `roadDeadEnd` liga **cima** apenas
- `roadT` liga **cima+esquerda+direita** (falta embaixo)
- `roadStraightH`/`roadStraightV` não rotacionam (são sprites dedicados pra cada eixo)

A rotação roda em passos de 90° no sentido anti-horário (Z positivo no Unity UI), seguindo o
ciclo `cima → esquerda → baixo → direita → cima`. Rotacionar o par-base de cada peça nesse
ciclo gera as 4 combinações possíveis sem precisar de mais sprites.

## Mudanças de código

- **`GridTileView.cs`**: novo `Image roadIcon`; campos de sprite `roadStraightH`,
  `roadStraightV`, `roadCorner`, `roadDeadEnd`, `roadT`; `Setup` ganhou parâmetro opcional
  `int roadMask = 0`; novo método privado `ApplyRoad(mask)` com a lógica de classificação +
  rotação acima.
- **`GridVisualizer.cs`**: `BuildGrid()` agora calcula `RoadConnectivityMask(pos)` pra cada
  célula não bloqueada e passa pro `Setup`.
- **`GridTile.prefab`**: novo filho `RoadIcon` (Image), posicionado entre `Border` e
  `GoalIcon` na hierarquia/z-order; `Background` ganhou o sprite `grama.png` (antes não tinha
  sprite, só cor); componente `GridTileView` wireado com os 5 sprites de estrada.

## Fora de escopo

- Sprite de cruzamento (4 vias) dedicado — a Fase 1 não tem nenhuma célula com os 4 vizinhos
  conectados, então o fallback (reaproveitar o T) nunca é exercitado hoje.
- Aplicar a mesma lógica em outras fases — só a Fase 1 foi pedida; o sistema é genérico
  (baseado em `GridLevelData`) então deve funcionar em qualquer fase sem mudança de código,
  mas não foi testado em outro layout.

## Verificação

1. Abrir a cena que usa `GridVisualizer` com `levelData = GridLevel_Phase1`, dar Play.
2. Conferir visualmente que a estrada sai do início (0,2), vira pra baixo, segue reto até
   (3,1), sobe até o T em (3,2), e de lá vai tanto pro castelo (4,2) quanto pro ramal até a
   ponta morta em (2,3).
3. Se alguma peça individual estiver girada errado, o ajuste é só na constante de rotação
   daquele caso em `ApplyRoad` — não precisa trocar sprite nem mexer no resto da lógica.
