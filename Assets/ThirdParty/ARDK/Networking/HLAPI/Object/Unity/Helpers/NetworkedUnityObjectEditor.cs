// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Niantic.ARDK.Networking.HLAPI.Routing;
using UnityEditor;

using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity.Helpers {
  /// <summary>
  /// Custom editor for the NetworkedUnityObject class.
  /// Handles automated value setting for prefab and instance IDs.
  /// </summary>
  [CustomEditor(typeof(NetworkedUnityObject))]
  internal sealed class NetworkedUnityObjectEditor : Editor {

    public override void OnInspectorGUI() {
      if (!Application.isPlaying) {
        var netUniObj = (NetworkedUnityObject) target;

        // Make sure the prefab ID on this object is set properly.
        UpdateInstanceId(netUniObj); 
        UpdateNetworkedBehavioursList(netUniObj);
      }

      // Still use the normal property field to show the UI
      base.OnInspectorGUI();
    }

    /// <summary>
    /// Populate the list of NetworkedBehaviours attached to this NetworkedUnityObject so that
    /// they can all be started up at creation.
    /// </summary>
    private void UpdateNetworkedBehavioursList(NetworkedUnityObject obj) {
      // If we're in the prefab stage, we can handle things knowing that we're modifying the prefab
      // directly.
      if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) {
        // Regenerate the behaviour list in place
        obj._behaviours = obj.GetComponents<NetworkedBehaviour>();
      } else if (PrefabUtility.IsPartOfPrefabInstance(obj) &&
                 !PrefabUtility.IsPrefabAssetMissing(obj)) {
        // If we're editting a prefab instance, the property must be editted through the
        // SerializedProperty flow, which is a bit janky but ensures that the property gets marked
        // as a prefab override. If the user wants to apply these changes to the prefab, it will
        // then be clear what has and hasn't been overridden.
        var behaviours = obj.GetComponents<NetworkedBehaviour>();
        var behavioursProperty = serializedObject.FindProperty("_behaviours");

        // If these are the same size, we can assume that no component has been added or removed,
        // and thus we can save the time from destroying and rebuilding, which gets expensive in
        // an OnGUI context.
        if (behaviours.Length != behavioursProperty.arraySize) {
          behavioursProperty.ClearArray();

          for (var i = 0; i < behaviours.Length; i++) {
            behavioursProperty.InsertArrayElementAtIndex(i);
            var element = behavioursProperty.GetArrayElementAtIndex(i);
            element.objectReferenceValue = behaviours[i];
          }
          serializedObject.ApplyModifiedProperties();
        }
      }
    }

    /// <summary>
    /// Determines if this object is an instance, and then sets its IDs accordingly.
    /// </summary>
    /// <param name="netUniObj"></param>
    private void UpdateInstanceId(NetworkedUnityObject netUniObj)
    {
      // We want to first ensure we are selecting an instance, not a prefab.
      if
      (
        UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null &&
        PrefabUtility.IsPartOfPrefabInstance(netUniObj) &&
        !PrefabUtility.IsPrefabAssetMissing(netUniObj)
      )
      {
        if ((ulong)netUniObj.PrefabId == 0)
        {
          // We need to sync this instance to its prefab
          var prefabPath =
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(netUniObj);
          var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
          var prefabNetUniObj = prefab.GetComponent<NetworkedUnityObject>(); 
          SetPrefabId(serializedObject, (ulong)prefabNetUniObj.PrefabId);
        }
        else
        {
          // Check the property modifications to see if the user has messed with the prefab id.
          // We don't want this to be altered on prefab instances, so revert if they have.
          var propertyModifications = PrefabUtility.GetPropertyModifications(netUniObj);
          var prefabIdModification =
            propertyModifications.FirstOrDefault(mod => mod.propertyPath == "_prefabId");
          if (prefabIdModification != null) {
            PrefabUtility.RevertPropertyOverride(
              serializedObject.FindProperty("_prefabId"),
              InteractionMode.AutomatedAction
            );
          }
        }

        // If this instance still has a 0 raw Id, regenerate it.
        if ((ulong)netUniObj.Id == 0) {
          GenerateRawId(serializedObject);
        }
      }
    }

    /// <summary>
    /// Sets an object's prefab Id to the specified ulong and saves it.
    /// </summary>
    private static void SetPrefabId(SerializedObject serializedObject, ulong id) {
      var prefabIdField = serializedObject.FindProperty("_prefabId");
      prefabIdField.longValue = (long)id;
      serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Generates a new raw Id for an object and saves it.
    /// </summary>
    private static void GenerateRawId(SerializedObject serializedObject) {
      var rawIdField = serializedObject.FindProperty("_rawId");
      rawIdField.longValue = NetworkedUnityObject.GenerateId();
      serializedObject.ApplyModifiedProperties();
    }
  }
}
#endif