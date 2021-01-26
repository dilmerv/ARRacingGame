using System.Linq;
using DilmerGames.Core.Singletons;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMissionManager : Singleton<PlayerMissionManager>
{
    [SerializeField]
    public UnityEvent OnMissionCompleted = new UnityEvent();
    
    [SerializeField]
    private PlayerMission[] playerMissions = null;

    [SerializeField]
    private int currentMissionNum = 0;

    private PlayerMission currentMission = null;

    public int MissionTargetCount
    {
        get
        {
            return currentMission == null ? 0 : 
                currentMission.PlayerItems.Count(i => i.ItemType == ItemType.Flag);
        }
    }

    private void Awake() => ActivateMission();

    public void HandleMissionCompleted()
    {
        var currentTargets = currentMission == null ? 0 : 
                currentMission.PlayerItems.Count(i => i.ItemType == ItemType.Flag && !i.TargetReached);

        if(currentTargets == 0)
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
            go.transform.parent = transform.parent;
            var reticle = go.AddComponent<ARPlacementReticle>();
            reticle.placedObject.Prefab = currentStep.Prefab;
            reticle.placedObject.PlayerItem = currentStep;
            currentStep.PlacementState = PlacementState.PrefabCreated;

            reticle.OnObjectPlaced.AddListener(() => 
            {
                currentStep.PlacementState = PlacementState.Placed;
            });
            
            Logger.Instance.LogInfo($"ItemType: {currentStep.ItemType} created");
        }
    }

    public void ActivateMission()
    {
        if(playerMissions.Length > 0 && currentMission == null)
        {
            currentMission = playerMissions[currentMissionNum];
            currentMission.PlayerMissionState = PlayerMissionState.NoSet;

            foreach(var i in currentMission.PlayerItems)
            {
                i.PlacementState = PlacementState.NoSet;
            }
        }
    }
}