# Carta ENQUANTO (laço com condição) — design

## Contexto

O jogo (estilo code.org) deixa o jogador montar uma sequência de cartas. O motor
(`SequenceExecutor`) já interpreta controle de fluxo (`If/Else/EndIf`, `Repeat/EndRepeat`,
`RepeatUntil`) numa lista plana, contando profundidade com brackets. Falta uma carta **ENQUANTO**
clara e pedagógica: "enquanto [condição], faça [corpo]" — ex.: *enquanto caminho livre, ande; se
parede, vire*.

Decisões tomadas no brainstorming:
- **Condições (X):** caminho livre, parede à frente, não chegou no objetivo, inimigo à frente.
- **UX:** "bloco visual sobre a lista plana" — mantém a lista numerada e o motor; as linhas do
  corpo auto-indentam e ganham uma faixa colorida entre o `ENQUANTO` e o `FIM`. Sem drop-zone
  aninhada.
- **Cartas:** prontas, uma por condição (cada uma um asset `CardData`); a fase escolhe quais
  oferecer em `availableCards`.
- **FIM:** carta manual de arrastar (reusa `EndRepeat`), consistente com hoje.
- **`negateCondition`:** novo campo bool em `CardData` para cobrir os "não" sem dobrar o enum.

## Modelo de dados (`CardData.cs`)

- `enum CardAction`: **anexar `While` no fim** (vira índice 9 — não desloca os existentes).
- `enum ConditionTarget`: **anexar `AtGoal` no fim** (índice 4).
- Novo campo `public bool negateCondition;` (seção Condition). Vale para `While` e `If`.

Cartas (assets novos, `cardType: 2` Loop):

| Asset | cardAction | conditionTarget | negate |
|---|---|---|---|
| Card_While_PathClear ("Enquanto caminho livre") | While(9) | PathClear(3) | não |
| Card_While_NotAtGoal ("Enquanto não no objetivo") | While(9) | AtGoal(4) | sim |
| Card_While_EnemyAhead ("Enquanto inimigo à frente", futuro) | While(9) | EnemyAhead(2) | não |
| Card_EndRepeat (relabel → "Fim") | EndRepeat(7) | — | — |

## Execução (`SequenceExecutor.cs`)

- `EvaluateCondition` passa a receber `CardData` (não `ConditionTarget`), trata `AtGoal`
  (`character.IsAtGoal()`) e aplica `negateCondition`.
- `case CardAction.While`: espelha `RepeatUntil`, mas `while (isRunning && EvaluateCondition(card)
  && loops < 100)`. Reusa `FindEndRepeat` para achar o `EndRepeat`. (O mesmo guard `isRunning`
  entra no `RepeatUntil` para não andar além do objetivo.)
- `FindEndRepeat`: incluir `While` na contagem de profundidade.
- `If`/`RepeatUntil` passam a chamar `EvaluateCondition(card)`.

`If` aninhado dentro de `While` já funciona — *enquanto caminho livre: se não-parede ande, senão
vire* roda sem mudança extra.

## UI — bloco visual (`SequenceManager.cs` + `DropZone.cs`)

- `SequenceManager.RefreshBlocks()`: varre `lines` em ordem, conta profundidade
  (`While`/`Repeat`/`RepeatUntil` +1, `EndRepeat` −1; a linha do `EndRepeat` alinha com o abridor),
  e chama `DropZone.SetBlockDepth(depth)` em cada linha. Chamado no fim de `BuildSequence` e por
  `DropZone` após `PlaceCard`/`ClearCard`.
- `DropZone.SetBlockDepth(int)`: guarda a profundidade, reposiciona a carta colocada
  (`x = base + depth*indent`) e aplica uma faixa/tint quando `depth > 0`. `PlaceCard` usa a mesma
  base+indent ao instanciar. (Indentação só para blocos de laço na v1; `If/EndIf` fica para depois.)

## Assets e fases

- Criar os 3 assets `Card_While_*` (+ `.meta` com guid novo). Relabel `Card_EndRepeat` → "Fim".
- **Fase 1** (`Phase_1.asset`): adicionar `Card_TurnLeft` e `Card_TurnRight` ao `availableCards`
  (pedido do usuário).
- **Fase 2** (`Phase_2.asset`): adicionar `Card_While_PathClear` + `Card_EndRepeat` ao
  `availableCards`, tornando o L resolvível com um laço (enquanto caminho livre, ande → vire →
  enquanto caminho livre, ande).

## Verificação (no Editor)

1. Play na Fase 1 → a mão mostra Andar + Virar Esquerda + Virar Direita.
2. Na Fase 2, montar: `Enquanto caminho livre` / `Andar` / `Fim` / `Virar` / `Enquanto caminho
   livre` / `Andar` / `Fim`. As linhas do corpo aparecem **indentadas com faixa**.
3. Executar → personagem percorre o L em laço e vence.
4. Conferir o limite de segurança (não trava) e o pareamento da faixa ENQUANTO↔FIM.
