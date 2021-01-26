using UnityEngine;

public class TargetCollisionState : MonoBehaviour
{
    public PlayerItem PlayerItem { get;set; }

    void OnCollisionEnter(Collision other) 
    {
        var collisionState = other.gameObject.GetComponent<TargetCollisionState>();
        
        if(other.transform.gameObject.layer == LayerMask.NameToLayer("Car") && !PlayerItem.TargetReached)
        {
            PlayerItem.TargetReached = true;
            PlayerMissionManager.Instance.HandleMissionCompleted();
            transform.gameObject.SetActive(false);
        }
    }
}