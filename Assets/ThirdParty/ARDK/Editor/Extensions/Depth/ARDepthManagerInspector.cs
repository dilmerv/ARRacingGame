using Niantic.ARDK.AR;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Helpers;

using UnityEditor;

using UnityEngine;

namespace ARDK.Editor.Extensions.Depth
{
  [CustomEditor(typeof(ARDepthManager))]
  public class ARDepthManagerInspector : UnityEditor.Editor
  {
    private SerializedProperty _occlusionModeProperty;
    private SerializedProperty _textureFilterModeProperty;
    private SerializedProperty _interpolationProperty;
    private SerializedProperty _interpolationPreferenceProperty;
    

    private void OnEnable()
    {
      _occlusionModeProperty = serializedObject.FindProperty("_occlusionMode");
      _textureFilterModeProperty = serializedObject.FindProperty("_textureFilterMode");
      _interpolationProperty = serializedObject.FindProperty("_interpolation");
      _interpolationPreferenceProperty = serializedObject.FindProperty("_interpolationPreference");
    }

    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();
      serializedObject.Update();

      if (_occlusionModeProperty.enumValueIndex == (int)ARDepthManager.OcclusionMode.Auto ||
        _occlusionModeProperty.enumValueIndex == (int)ARDepthManager.OcclusionMode.DepthBuffer)
      {
        var useLinear = EditorGUILayout.Toggle
        (
          "Prefer Smooth Edges",
          _textureFilterModeProperty.enumValueIndex != (int)FilterMode.Point
        );

        _textureFilterModeProperty.enumValueIndex = useLinear
          ? (int)FilterMode.Bilinear
          : (int)FilterMode.Point;
      }
      // Default to point filtering when using the screen space mesh technique
      else _textureFilterModeProperty.enumValueIndex = (int)FilterMode.Point;

      _interpolationProperty.enumValueIndex = (int)((InterpolationMode)EditorGUILayout.EnumPopup
        ("Interpolation", (InterpolationMode)_interpolationProperty.enumValueIndex));

      var interpolationAdapter =
        ((ARDepthManager)target).GetComponent<ARDepthInterpolationAdapter>();

      var isInterpolationAdapterPresentAndEnabled =
        interpolationAdapter != null && interpolationAdapter.enabled;
      
      if (_interpolationProperty.enumValueIndex > 0)
      {
        if (!isInterpolationAdapterPresentAndEnabled)
        {
          _interpolationPreferenceProperty.floatValue = EditorGUILayout.Slider
            ("Interpolation Preference", _interpolationPreferenceProperty.floatValue, 0.1f, 1.0f);

          EditorGUILayout.HelpBox
          (
            "When in motion, the interpolation preference sets whether to align depth pixels " +
            "with closer (0.1) or distant (1.0) pixels in the color image.",
            MessageType.None
          );
        }
        else
          EditorGUILayout.HelpBox("Using interpolation preference adapter.", MessageType.None);
      }
      
      serializedObject.ApplyModifiedProperties();

      var isRenderingManagerPresent =
        ((ARDepthManager)target).GetComponent<ARRenderingManager>() != null;
      if (!isRenderingManagerPresent)
      {
        EditorGUILayout.HelpBox
        (
          "Missing AR Rendering Manager component. To inject depth information to the rendering pipeline, " +
          "please make sure to add this AR Depth Manager to the renderer manually.",
          MessageType.Warning
        );
      }
    }
  }
}
