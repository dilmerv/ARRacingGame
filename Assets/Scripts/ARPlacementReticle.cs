using UnityEngine;
using UnityEngine.Events;

public class ARPlacementReticle : MonoBehaviour
{
    private const string RAYCAST_LAYER = "ARMeshLiDAR";

    public PlacedObjectState placedObject = new PlacedObjectState();

    public Vector3 offset = Vector3.zero;
    
    public UnityEvent OnObjectPlaced = new UnityEvent();

    private Camera arCamera = null;

    private GameObject customReticle;

    void Awake()
    {
        arCamera = FindObjectOfType<Camera>();
        customReticle = Instantiate(Resources.Load<GameObject>("Prefabs/Reticle"));
        customReticle.transform.parent = transform;
        customReticle.SetActive(false);
    }

    void Update()
    {
        if(placedObject.Placement != null) return;

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

            // check if we are over UI but ignore the logger label otherwise block it
            bool isOverUI = touch.finger.screenPosition.IsPointOverUIObject(new string[] { "Logger" });

            if (isOverUI) return;

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (placedObject != null && placedObject.Placement == null)
                {
                    placedObject.Placement = Instantiate(placedObject.Prefab, location, Quaternion.identity);
                    placedObject.Placement.transform.parent = transform;
                    
                    var placedObjectItem = placedObject.Placement.AddComponent<PlacedObjectItem>();
                    placedObjectItem.PlayerItem = placedObject.PlayerItem;

                    Logger.Instance.LogInfo("Object Created...");
                    
                    OnObjectPlaced?.Invoke();

                    customReticle.SetActive(false);
                }
            }
        }
    }
}
