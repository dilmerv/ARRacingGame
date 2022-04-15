using Niantic.ARDK.AR;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Helpers;

using UnityEditor;

using UnityEngine;

namespace ARDK.Editor.Extensions.Semantics
{
  [CustomEditor(typeof(ARSemanticSegmentationManager))]
  public class ARSemanticSegmentationManagerInspector: UnityEditor.Editor
  {
    private SerializedProperty _interpolationProperty;
    private SerializedProperty _interpolationPreferenceProperty;
    private SerializedProperty _suppressionChannelsProperty;
    
    private void OnEnable()
    {
      _interpolationProperty = serializedObject.FindProperty("_interpolation");
      _interpolationPreferenceProperty = serializedObject.FindProperty("_interpolationPreference");
      _suppressionChannelsProperty = serializedObject.FindProperty("_depthSuppressionChannels");
    }
    
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();
      serializedObject.Update();

      _suppressionChannelsProperty.isExpanded = EditorGUILayout.Foldout
        (_suppressionChannelsProperty.isExpanded, "Depth Suppression Channels");

      if (_suppressionChannelsProperty.isExpanded)
      {
        EditorGUI.indentLevel++;

        _suppressionChannelsProperty.arraySize = EditorGUILayout.IntField
        (
          "Number Of Channels",
          _suppressionChannelsProperty.arraySize
        );

        for (var i = 0; i < _suppressionChannelsProperty.arraySize; i++)
        {
          var item = _suppressionChannelsProperty.GetArrayElementAtIndex(i);
          EditorGUILayout.PropertyField(item, new GUIContent($"Element {i}"));
        }

        if (_suppressionChannelsProperty.arraySize > 0)
        {
          if (((ARSemanticSegmentationManager)target).GetComponent<ARDepthManager>() == null)
            EditorGUILayout.HelpBox("Please add an AR Depth Manager component to enable this feature.", MessageType.Error);
        }
        
        EditorGUI.indentLevel--;
      }

      _interpolationProperty.enumValueIndex = (int)((InterpolationMode)EditorGUILayout.EnumPopup
        ("Interpolation", (InterpolationMode)_interpolationProperty.enumValueIndex));

      var semanticsManager = (ARSemanticSegmentationManager)target;
      var depthManager = semanticsManager.GetComponent<ARDepthManager>();
      var isDepthManagerPresentAndEnabled = depthManager != null && depthManager.enabled;
      
      if (_interpolationProperty.enumValueIndex > 0)
      {
        if (!isDepthManagerPresentAndEnabled)
        {
          _interpolationPreferenceProperty.floatValue = EditorGUILayout.Slider
            ("Interpolation Preference", _interpolationPreferenceProperty.floatValue, 0.1f, 1.0f);

          EditorGUILayout.HelpBox
          (
            "When in motion, the interpolation preference sets whether to align semantic pixels " +
            "with closer (0.1) or distant (1.0) pixels in the color image.",
            MessageType.None
          );
        }
        else
        {
          EditorGUILayout.HelpBox
            ("Interpolation preference is driven by AR Depth Manager.", MessageType.Info);
        }
      }
      
      serializedObject.ApplyModifiedProperties();
      
      var isRenderingManagerPresent = semanticsManager.GetComponent<ARRenderingManager>() != null;
      if (!isRenderingManagerPresent)
      {
        EditorGUILayout.HelpBox
        (
          "Missing AR Rendering Manager component. To inject semantics information to the rendering pipeline, " +
          "please make sure to add this AR Semantic Segmentation Manager to the renderer manually.",
          MessageType.Warning
        );
      }
    }
  }
}
