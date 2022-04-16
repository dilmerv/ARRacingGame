using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ARPlacementReticle : MonoBehaviour
{
    private const string RAYCAST_LAYER = "ARMeshLiDAR";

    public PlacedObjectState placedObject = new PlacedObjectState();

    public Vector3 offset = Vector3.zero;

    public UnityEvent OnObjectPlaced = new UnityEvent();

    private bool objectPlaced = false;

    public Camera mainCamera = null;

    private GameObject customReticle;

    private TextMeshProUGUI reticleOverlayText;

    void Awake()
    {
        customReticle = Instantiate(Resources.Load<GameObject>("Prefabs/Reticle"));
        customReticle.transform.parent = transform;
        customReticle.transform.position = Vector3.zero;
        customReticle.SetActive(false);
        reticleOverlayText = customReticle.GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        if(placedObject.Placement != null || objectPlaced) return;

        Logger.Instance.LogInfo($"{mainCamera.transform.position}");

        var ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        var hasHit = Physics.Raycast(ray, out var hit, 10, LayerMask.GetMask(RAYCAST_LAYER));

        if(hasHit)
        {
            customReticle.SetActive(true);
            customReticle.transform.position = hit.point;
            customReticle.transform.up = hit.normal;

            if (PlayerMissionManager.Instance.CarWasPlaced)
            {
                // check distances
                var distance = Vector3.Distance(CarController.Instance.transform.position, transform.GetChild(0).position);
                reticleOverlayText.text = $"{string.Format("{0:0.#}", distance)}";

                if(distance >= placedObject.PlayerItem.MinDistance)
                {
                    GetComponentInChildren<MeshRenderer>().material = GameManager.Instance.GlobalGameSettings.AvailableReticleMaterial;
                    PlaceObject(hit.point);
                }
                else
                {
                    GetComponentInChildren<MeshRenderer>().material = GameManager.Instance.GlobalGameSettings.UnavailableReticleMaterial;
                }
            }
            else
            {
                PlaceObject(hit.point);
            }
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
                    objectPlaced = true;

                    customReticle.SetActive(false);
                }
            }
        }
    }
}
