using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

public static class BlockUIExtensions
{
    public static bool IsPointOverUIObject(this Vector2 pos, string[] ignoredUIElements = null)
    {
        PointerEventData eventPosition = new PointerEventData(EventSystem.current);
        eventPosition.position = new Vector2(pos.x, pos.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventPosition, results);
        
        IEnumerable<string> uiComponentNames = results.Select(x => x.gameObject.name);

        return results.Count > 0 && !uiComponentNames.Any(ui => ignoredUIElements.Contains(ui));;
    }
}