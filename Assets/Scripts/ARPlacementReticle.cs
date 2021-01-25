using UnityEngine;
using UnityEngine.Events;

public class ARPlacementReticle : MonoBehaviour
{
    private const string RAYCAST_LAYER = "ARMeshLiDAR";

    public GameObject customPlacement;

    public Vector3 offset = Vector3.zero;
    
    public UnityEvent OnObjectPlaced = new UnityEvent();

    private Camera arCamera = null;

    private GameObject customReticle;

    private GameObject objectPlaced;

    void Awake()
    {
        arCamera = FindObjectOfType<Camera>();
        customReticle = Instantiate(Resources.Load<GameObject>("Prefabs/Reticle"));
        customReticle.transform.parent = transform;
        customReticle.SetActive(false);
    }

    void Update()
    {
        if(objectPlaced != null) return;

        var ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        var hasHit = Physics.Raycast(ray, out var hit, float.PositiveInfinity, LayerMask.GetMask(RAYCAST_LAYER));

        if(hasHit)
        {
            customReticle.SetActive(true);
            customReticle.transform.position = hit.point + offset;
            customReticle.transform.up = hit.normal;

            PlaceObject(hit.point);
        }
    }

    void PlaceObject(Vector3 location)
    {
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

        if (activeTouches.Count > 0)
        {
            var touch = activeTouches[0];

            bool isOverUI = touch.screenPosition.IsPointOverUIObject();

            if (isOverUI) return;

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (customPlacement != null && objectPlaced == null)
                {
                    objectPlaced = Instantiate(customPlacement, location, Quaternion.identity);
                    objectPlaced.transform.parent = transform;
                    
                    Logger.Instance.LogInfo("Object Created...");
                    
                    var carController = objectPlaced.GetComponentInChildren<CarController>();

                    if(carController != null)
                    {
                        PlayerInputController.Instance.Bind(carController);
                        Logger.Instance.LogInfo("PlayerInputController Bound...");
                    }

                    OnObjectPlaced?.Invoke();
                }
            }
        }
    }
}
