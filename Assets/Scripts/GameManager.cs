using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject carPrefab = null;

    [SerializeField]
    private Camera arCamera = null;

    [SerializeField]
    private LayerMask layersToInclude = default;

    private GameObject carControllerGo = null;

    void Awake() => EnhancedTouchSupport.Enable();

    void Update()
    {
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

        if (activeTouches.Count > 0)
        {
            var touch = activeTouches[0];

            bool isOverUI = touch.screenPosition.IsPointOverUIObject();

            if (isOverUI) return;

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                var ray = arCamera.ScreenPointToRay(touch.screenPosition);
                var hasHit = Physics.Raycast(ray, out var hit, float.PositiveInfinity, layersToInclude);

                if (hasHit && carControllerGo == null)
                {
                    carControllerGo = Instantiate(carPrefab, hit.point, Quaternion.identity);

                    Logger.Instance.LogInfo("Car Created...");
                    
                    var carController = carControllerGo.GetComponentInChildren<CarController>();
                    PlayerInputController.Instance.Bind(carController);

                    Logger.Instance.LogInfo("PlayerInputController Bound...");
                }
            }
        }
    }
}
