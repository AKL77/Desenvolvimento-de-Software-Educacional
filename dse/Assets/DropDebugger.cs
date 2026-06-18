using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DropDebugger : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log($"Mouse released — {results.Count} objects hit:");
            foreach (RaycastResult result in results)
            {
                Debug.Log($"  → {result.gameObject.name}");
            }
        }
    }
}
