using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMissionManager : MonoBehaviour
{
    [SerializeField]
    private UnityEvent OnMissionCompleted = new UnityEvent();
    
    [SerializeField]
    private PlayerMission[] playerMissions = null;

    [SerializeField]
    private int currentMissionNum = 0;

    private PlayerMission currentMission = null;

    private void Awake() => ActivateMission();

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
            currentMission.PlayerMissionState = PlayerMissionState.Completed;
            OnMissionCompleted?.Invoke();
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
            reticle.customPlacement = currentStep.Prefab;
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