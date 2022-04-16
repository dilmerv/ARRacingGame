using System.Linq;
using DilmerGames.Core.Singletons;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMissionManager : Singleton<PlayerMissionManager>
{
    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    public UnityEvent OnMissionCompleted = new UnityEvent();
    
    [SerializeField]
    private PlayerMission[] playerMissions = null;

    [SerializeField]
    private int currentMissionNum = 0;

    [SerializeField]
    private Vector3 reticleOffset = Vector3.zero;

    private PlayerMission currentMission = null;

    public int MissionTargetCount
    {
        get
        {
            return currentMission == null ? 0 : 
                currentMission.PlayerItems.Count(i => i.ItemType == ItemType.Flag);
        }
    }

    private void Awake() => StartMission();

    public bool CarWasPlaced
    {
        get
        {
            return currentMission.PlayerItems.Any(c => c.ItemType == ItemType.Car && 
                c.PlacementState == PlacementState.Placed);
        }
    }

    public void HandleMissionCompleted()
    {
        var remainingTargetCount = currentMission == null ? 0 : 
                currentMission.PlayerItems.Count(i => i.ItemType == ItemType.Flag && !i.TargetReached);

        Logger.Instance.LogInfo($"Mission Targets remaining {remainingTargetCount}");

        if(remainingTargetCount == 0)
        {
            currentMission.PlayerMissionState = PlayerMissionState.Completed;
            OnMissionCompleted?.Invoke();
        }
    }

    void Update() 
    {
        if(currentMission.PlayerMissionState == PlayerMissionState.NoSet
        || currentMission.PlayerMissionState == PlayerMissionState.Started)
            CheckPlacementMissionStatus();
    }

    void CheckPlacementMissionStatus()
    {
        if(currentMission.PlayerItems.All(s => s.PlacementState == PlacementState.Placed))
        {
            return;
        }

        var currentStep = currentMission.PlayerItems.Where(i => i.PlacementState != PlacementState.Placed)
            .OrderBy(i => i.Priority)
            .FirstOrDefault();

        if(currentStep != null && currentStep.PlacementState == PlacementState.NoSet)
        {
            var go = new GameObject($"{currentStep.ItemType}");
            go.transform.parent = transform;
            var reticle = go.AddComponent<ARPlacementReticle>();
            reticle.mainCamera = mainCamera;
            reticle.placedObject.Prefab = currentStep.Prefab;
            reticle.placedObject.PlayerItem = currentStep;
            reticle.offset = reticleOffset;
            currentStep.PlacementState = PlacementState.PrefabCreated;

            reticle.OnObjectPlaced.AddListener(() => 
            {
                currentStep.PlacementState = PlacementState.Placed;

                Logger.Instance.LogInfo($"ItemType: {currentStep.ItemType} placed");
            });
            
            Logger.Instance.LogInfo($"ItemType: {currentStep.ItemType} created");
        }
    }

    public void StartMission()
    {
        // destroy all created objects
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if(playerMissions.Length > 0)
        {
            Logger.Instance.LogInfo("Mission Activated...");

            currentMission = playerMissions[currentMissionNum];
            currentMission.PlayerMissionState = PlayerMissionState.NoSet;

            foreach(var i in currentMission.PlayerItems)
            {
                i.PlacementState = PlacementState.NoSet;
                i.TargetReached = false;
            }
            int targets = currentMission.PlayerItems.Count(i => i.ItemType == ItemType.Flag);
            Logger.Instance.LogInfo($"Mission Targets required {targets}");
        }
    }
}