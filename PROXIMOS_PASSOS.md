# Próximos Passos — Continuar em outra sessão

Este documento resume o estado atual do trabalho desta sessão (correção de bug de visibilidade das
cartas + sistema de Fase 2) e o que falta fazer. Ver também `PROJETO.md` (visão geral da
arquitetura) e o plano salvo em
`C:\Users\antoniotolio\.claude\plans\esta-marcado-quando-eu-distributed-tiger.md`.

---

## ✅ O que já está funcionando

- Cartas aparecem corretamente na mão do jogador (`CardsPanel`), na posição certa, arrastáveis.
- `RectMask2D` do `Viewport` desabilitado de propósito (ver `PROJETO.md` → "Dívidas técnicas
  conhecidas") — workaround pro bug de clipping travado.
- Botão "Próxima Fase" foi adicionado ao `VictoryPanel.prefab` (visual + lógica em
  `VictoryPanel.cs` / `PhaseData.nextPhase`).
- `Phase_2.asset`, `GridLevel_Phase2.asset` (grid 6x6 em L) e `Card_TurnRight.asset` foram criados
  via `DSE > Setup Phase 2 (Turns)`.
- Sequência completa "andar até o fim → Fase Concluída!" funciona na Fase 1.

## 🚧 O que falta (bloqueadores pra Fase 2 funcionar de ponta a ponta)

### 1. `PhaseManager.gridVisualizer` está `None` na cena
**Sintoma:** ao avançar de fase, as cartas mudam mas o grid continua o mesmo.
**Causa:** esse campo foi adicionado ao `PhaseManager.cs` depois que `DSE > Configure Scene
(MapaMax)` já tinha rodado, então nunca foi linkado no Inspector.
**Como corrigir:** abrir a cena `MapaMax.unity`, selecionar o objeto `UI` na Hierarchy, achar o
componente `Phase Manager` no Inspector, e arrastar o `MapPanel` (que tem o componente
`GridVisualizer`) pro campo "Grid Visualizer". Salvar a cena.
- Tentei fazer isso via MCP (`update_component`) nesta sessão mas as chamadas deram timeout sem
  confirmação — precisa ser feito manualmente no Editor ou retentado via MCP numa sessão nova.

### 2. Botão "Próxima Fase" não está aparecendo no popup de vitória
**Sintoma:** só aparece "Resetar", mesmo a Fase 1 tendo `nextPhase` apontando pra Fase 2.
**O que já foi verificado:**
- `Phase_1.asset` tem a linha `nextPhase: {fileID: 11400000, guid: e5508f95bd43ddb439177fa7243ddcc5, type: 2}`
  (guid da `Phase_2.asset`) — editado direto no YAML.
- `VictoryPanel.Show(PhaseData currentPhase)` agora recebe a fase atual como parâmetro (em vez de
  ler de `PhaseManager.Instance.currentPhase` dentro do próprio `Show()`, que estava retornando
  `NULL` de forma inconsistente — suspeita de timing entre domain reload e singleton, nunca
  totalmente explicado).
- `SequenceExecutor.OnSuccess()` chama `VictoryPanel.Instance?.Show(PhaseManager.Instance.currentPhase)`.
**Hipótese mais provável pro próximo passo:** o arquivo `Phase_1.asset` foi editado direto no disco
(YAML manual), e o Unity pode não ter reimportado esse asset ainda dentro da sessão de Play em que
foi testado — o `ScriptableObject` carregado em memória pode estar com o valor antigo de
`nextPhase` (null) até uma reimportação/recompilação acontecer. **Próximo passo:** parar o Play,
selecionar `Phase_1.asset` no Project, confirmar no Inspector se o campo "Next Phase" mostra
`Phase_2` (Phase Data). Se não mostrar, reatribuir manualmente arrastando `Phase_2.asset` pro
campo, salvar, e testar de novo.

### 3. Confirmar fluxo completo da Fase 2
Depois de corrigir os dois itens acima:
- Vencer a Fase 1 → clicar "Próxima Fase" → confirmar que o grid muda pra 6x6 em L.
- Confirmar que `Card_TurnLeft` e `Card_TurnRight` aparecem na mão junto com `Card_MoveForward`.
- Completar o L (4x Andar, 1x Virar Esquerda, 5x Andar) e confirmar que "Fase Concluída!" aparece
  de novo, **sem** botão "Próxima Fase" (já que `Phase_2.nextPhase` está vazio).

## 📦 Ainda não feito (do plano original, não bloqueante)

- **Abrir PR novo** (separado do [PR #1](https://github.com/AKL77/Desenvolvimento-de-Software-Educacional/pull/1))
  com todo o trabalho desta sessão: fix de visibilidade das cartas, sistema de Fase 2, fix de
  `IGridCharacter.GetPositionAhead()`. Branch ainda não foi criada — está tudo direto na branch
  `feature/grid-movement-system` (a mesma do PR #1). Antes de abrir o PR, decidir se cria uma branch
  nova a partir daqui ou se de fato vai tudo junto no #1 (o plano original pedia separado).
- **Fluxograma + explicação no chat** das mudanças desta sessão vs. PR #1 (pedido pelo usuário, não
  chegou a ser feito por causa do tempo gasto debugando o bug de visibilidade).
- Limpar os `Debug.Log` de diagnóstico em `CardView.cs` e `PhaseManager.cs` quando não forem mais
  necessários (o usuário pediu pra mantê-los por enquanto).

## 🗂️ Arquivos tocados nesta sessão (resumo)

| Arquivo | O que mudou |
|---|---|
| `dse/Assets/CardCarousel.cs` | fix de timing (`EnsureBasePosition` lazy) + offset recalibrado pra `x=0` (sem `RectMask2D`) |
| `dse/Assets/UIManager.cs` | força rebuild de layout/canvas depois de ativar `CardsPanel` |
| `dse/Assets/Cards/CardView.cs` | logs de debug de Awake/Setup/RefreshVisuals |
| `dse/Assets/Phases/PhaseManager.cs` | logs de debug, campo `gridVisualizer`, troca de grid por fase |
| `dse/Assets/Phases/PhaseData.cs` | campo novo `nextPhase` |
| `dse/Assets/UI/VictoryPanel.cs` | botão/lógica "Próxima Fase", `Show()` agora recebe `PhaseData` |
| `dse/Assets/UI/Prefabs/VictoryPanel.prefab` | novo `NextPhaseButton` (editado via YAML direto) |
| `dse/Assets/SequenceExecutor.cs` | `GetPositionAhead()` real pra condições `WallAhead`/`PathClear`; `OnSuccess` passa a fase atual pro `VictoryPanel` |
| `dse/Assets/Character & Grid/IGridCharacter.cs`, `GridCharacter.cs`, `MockGridCharacter.cs` | método novo `GetPositionAhead()` |
| `dse/Assets/Editor/Phase2SetupWizard.cs` | gera `Card_TurnRight`, `GridLevel_Phase2` (6x6 L), `Phase_2.asset` |
| `dse/Assets/Phases/Phase_1.asset` | só carta `Andar pra frente`; `nextPhase` → `Phase_2` |
| `PROJETO.md` (novo) | documentação completa da arquitetura |
| `PROXIMOS_PASSOS.md` (este arquivo) | handoff pra próxima sessão |

## ⚠️ Nota sobre o MCP do Unity

Durante toda a sessão, a conexão MCP com o Unity (porta 8090) ficou extremamente instável — a
maioria das chamadas (`get_gameobject`, `update_component`, `recompile_scripts`) deu timeout no
lado do cliente, mesmo quando a ação tinha sucesso no lado do Unity (confirmado via print/log do
usuário depois). Recomendo, na próxima sessão, confirmar visualmente no Editor qualquer mudança
feita via MCP antes de assumir que funcionou.
