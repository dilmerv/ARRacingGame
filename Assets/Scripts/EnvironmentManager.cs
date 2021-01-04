using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARMeshManager))]
public class EnvironmentManager : MonoBehaviour
{
    private ARMeshManager arMeshManager;

    void Awake()
    {
        arMeshManager = GetComponent<ARMeshManager>();
        arMeshManager.meshesChanged += ArMeshManager_meshesChanged;
    }

    private void ArMeshManager_meshesChanged(ARMeshesChangedEventArgs obj)
    {
        var addedMeshFilters = obj.added;

        foreach (var meshFilter in addedMeshFilters)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.localPosition = meshFilter.transform.localPosition;

            Logger.Instance.LogInfo($"Added a new mesh | Total: {addedMeshFilters.Count}");

            Logger.Instance.LogInfo($"Mesh Center {meshFilter.mesh.bounds.center}");
            Logger.Instance.LogInfo($"Mesh Size {meshFilter.mesh.bounds.size}");
        }
    }
}
