using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMission", menuName = "Create Player Mission", order = 0)]
public class PlayerMission : ScriptableObject
{
    public PlayerItem[] PlayerItems;

    public PlayerMissionState PlayerMissionState = PlayerMissionState.NoSet;
}

[Serializable]
public class PlayerItem
{
    public ItemType ItemType = ItemType.NoSet;

    public int Priority = 0;

    public float MinDistance = 1;

    public PlacementState PlacementState = PlacementState.NoSet;

    public GameObject Prefab;

    public bool TargetReached = false;
}