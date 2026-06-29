using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Componente de arrastar para as cartas (paleta e sequência).
/// A lógica fica no PhaseController; aqui só repassamos os eventos de drag.
/// </summary>
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PhaseController controller;
    public bool fromPalette;
    public int action;     // PhaseController.Move como int
    public int seqIndex;   // índice na sequência (quando fromPalette == false)

    public void OnBeginDrag(PointerEventData e) { controller.BeginGhost(action, e); }
    public void OnDrag(PointerEventData e) { controller.MoveGhost(e); }
    public void OnEndDrag(PointerEventData e) { controller.EndDrag(this, e); }
}
