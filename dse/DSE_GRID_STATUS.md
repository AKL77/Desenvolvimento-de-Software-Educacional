# DSE — Grid de Jogo: Status e Próximos Passos

## Contexto do Jogo

Jogo educacional de lógica (estilo code.org) em Unity. O aluno arrasta **cartas de comandos** para uma sequência e pressiona Executar para um personagem andar num grid. A infraestrutura de cartas já existia; este documento cobre a implementação do grid visual.

---

## O que já foi criado (scripts C#)

Todos os arquivos abaixo estão criados e compiláveis no Unity:

| Arquivo | Status |
|---|---|
| `Assets/Character & Grid/GridTileType.cs` | ✅ Criado — enum Walkable/Blocked/Goal |
| `Assets/Character & Grid/GridLevelData.cs` | ✅ Criado — ScriptableObject com layout 5x5 |
| `Assets/Character & Grid/GridTileView.cs` | ✅ Criado — componente visual de cada tile |
| `Assets/Character & Grid/GridVisualizer.cs` | ✅ Criado — constrói o grid no MapPanel |
| `Assets/Character & Grid/TriangleGraphic.cs` | ✅ Criado — UI Graphic do triângulo branco |
| `Assets/Character & Grid/GridCharacter.cs` | ✅ Criado — IGridCharacter real com DOTween |
| `Assets/UI/VictoryPanel.cs` | ✅ Criado — popup "Fase Concluída!" |
| `Assets/Phases/PhaseData.cs` | ✅ Atualizado — ganhou campo `GridLevelData gridLevel` |
| `Assets/SequenceExecutor.cs` | ✅ Atualizado — `OnSuccess()` chama `VictoryPanel.Instance?.Show()` |
| `Assets/Editor/GridSetupWizard.cs` | ✅ Criado — cria assets/prefabs via menu |
| `Assets/Editor/GridSceneConfigurator.cs` | ✅ Criado — configura a cena via menu |

---

## O que ainda precisa ser feito no Unity

### Passo 1 — Deixar o Unity compilar os scripts
Abrir o Unity Editor com a cena `MapaMax.unity`. Aguardar a compilação de todos os scripts novos. Verificar no Console que não há erros.

### Passo 2 — Criar assets e prefabs (automatizado)
No menu do Unity, executar: **DSE > Setup Grid (Etapa 1)**

Isso cria automaticamente:
- `Assets/Phases/GridLevel_Phase1.asset` — grid 5x5, start=(0,0) facing Up, goal=(2,4)
- `Assets/Character & Grid/Prefabs/GridTile.prefab` — tile com background + ícone de estrela no goal
- `Assets/Character & Grid/Prefabs/CharacterTriangle.prefab` — triângulo branco 70px
- `Assets/UI/Prefabs/VictoryPanel.prefab` — overlay escuro + texto + botão Resetar

### Passo 3 — Configurar a cena (automatizado)
Com a cena `MapaMax.unity` aberta, executar: **DSE > Configure Scene (MapaMax)**

Isso faz automaticamente:
- Linka `GridLevel_Phase1` no `Phase_1.asset`
- Adiciona `GridVisualizer` ao `MapPanel` com `levelData` e `tilePrefab` linkados
- Cria filho `GridCharacter` no `MapPanel` com `TriangleGraphic` e links corretos
- Troca `characterReference` do `SequenceExecutor` de `MockGridCharacter` → `GridCharacter`
- Seta `stepDelay = 0.4f` no `SequenceExecutor`
- Instancia `VictoryPanel` no Canvas (inativo por padrão)
- Desativa o `MockGridCharacter`

### Passo 4 — Salvar e testar
- `Ctrl+S` para salvar a cena
- Pressionar **Play**
- Grid 5x5 deve aparecer sobre o mapa com tile dourado em (2,4)
- Triângulo branco na posição (0,0) apontando para cima
- Arrastar carta "Andar pra frente" para os slots e clicar Executar
- O triângulo deve deslizar suavemente entre os tiles
- Ao chegar em (2,4), aparece o popup "Fase Concluída!"

---

## Possíveis problemas e soluções

### Grid não aparece no MapPanel
- Verificar que `GridVisualizer.levelData` e `GridVisualizer.tilePrefab` estão linkados no Inspector
- O grid é construído em `Start()` com `Canvas.ForceUpdateCanvases()` — se o MapPanel tiver tamanho 0, os tiles ficam invisíveis
- Verificar que o `MapPanel` tem tamanho definido (não zero) no RectTransform

### Personagem não se move
- Verificar que `SequenceExecutor.characterReference` aponta para o `GridCharacter` (não o Mock)
- Verificar que `GridCharacter.levelData`, `GridCharacter.visualizer` e `GridCharacter.characterRect` estão linkados
- `stepDelay` deve ser >= `moveDuration` (padrão: stepDelay=0.4f, moveDuration=0.3f)

### VictoryPanel não aparece
- Verificar que existe um `VictoryPanel` no Canvas com o componente `VictoryPanel.cs`
- Verificar que `VictoryPanel.canvasGroup`, `VictoryPanel.messageText` e `VictoryPanel.restartButton` estão linkados

### DOTween não está instalado
- O projeto usa `DG.Tweening` (DOTween) — confirmar que está nos Packages
- `Assets/Plugins/Demigiant/DOTween/` deve existir (já existe no projeto)

---

## Arquitetura do fluxo de jogo

```
[Drag Card_MoveForward → DropZone]
         ↓
[Executar → SequenceExecutor.RunSequence()]
         ↓
[GridCharacter.MoveForward()]
  ├── walkable → DOTween anima triângulo, _position atualiza
  └── bloqueado → ignora, continua
         ↓
[SequenceExecutor verifica IsAtGoal()]
  ├── true  → VictoryPanel.Show()
  └── false → próxima carta
         ↓
[Sem goal ao fim → OnFailure() → GridCharacter.ResetToStart()]
```

---

## MCP Unity — Problema de porta

O MCP Unity está configurado na porta **8092** mas o servidor Node.js tentava conectar na porta 8090.

**Root cause:** O servidor lê a porta de `process.cwd()/ProjectSettings/McpUnitySettings.json`. O cwd do processo MCP não é o diretório do projeto Unity.

**Correção aplicada:** Adicionado suporte a `UNITY_PORT` env var em:
```
Library/PackageCache/com.gamelovers.mcp-unity.../Server~/build/unity/mcpUnity.js
```
Linha alterada:
```js
// ANTES:
const configPort = config.Port;
// DEPOIS:
const configPort = process.env.UNITY_PORT || config.Port;
```

**Para registrar o MCP com a porta correta**, rodar no terminal do Claude Code:
```
claude mcp remove mcp-unity -s local
claude mcp add mcp-unity -s local -e UNITY_PORT=8092 -- node "C:/Users/antoniotolio/Documents/facu-docs/dse/Desenvolvimento-de-Software-Educacional/dse/Library/PackageCache/com.gamelovers.mcp-unity@cce8b57de9cb/Server~/build/index.js"
```

Depois reiniciar o Claude Code para que o processo MCP reinicie com a env var.

---

## Etapas futuras (fora deste plano)

| Etapa | O que implementar |
|---|---|
| Etapa 2 | TurnLeft + TurnRight visuais (já funcionam no `GridCharacter`, só precisam de cartas) |
| Etapa 3 | Tiles Blocked + condições WallAhead/PathClear para o card `If` |
| Etapa 4 | Cards Repeat e RepeatUntil com loop visual |
| Etapa 5 | Múltiplas fases, seleção no mapa-mundi |
| Etapa 6 | Highlight do slot ativo durante execução |
| Etapa 7 | Animação de vitória/derrota elaborada |
