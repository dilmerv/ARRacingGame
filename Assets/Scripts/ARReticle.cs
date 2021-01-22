using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARReticle : MonoBehaviour
{
    [SerializeField]
    private GameObject customGameObject;

    [SerializeField]
    private LayerMask layersToInclude = default;

    [SerializeField]
    private Vector3 offset = Vector3.zero;
    
    [SerializeField]
    private Camera arCamera = null;

    void Awake()
    {
        customGameObject.SetActive(false);
    }

    void Update()
    {
        var ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        var hasHit = Physics.Raycast(ray, out var hit, float.PositiveInfinity, layersToInclude);

        if(hasHit)
        {
            customGameObject.SetActive(true);
            customGameObject.transform.position = hit.point + offset;
            customGameObject.transform.up = hit.normal;
        }
    }
}
