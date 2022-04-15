using Niantic.ARDK.Helpers;

using UnityEditor;

using UnityEngine;

namespace ARDK.Editor.Extensions.Rendering
{
  [CustomEditor(typeof(ARRenderingManager))]
  public class ARRenderingManagerInspector : UnityEditor.Editor
  {
    private enum Target
    {
      Camera = 0,
      Texture = 1
    }

    private SerializedProperty _renderTargetIdProperty;
    private SerializedProperty _cameraProperty;
    private SerializedProperty _textureProperty;
    private SerializedProperty _nearProperty;
    private SerializedProperty _farProperty;

    private void OnEnable()
    {
      _renderTargetIdProperty = serializedObject.FindProperty("_renderTargetId");
      _cameraProperty = serializedObject.FindProperty("_camera");
      _textureProperty = serializedObject.FindProperty("_targetTexture");
      _nearProperty = serializedObject.FindProperty("_nearClippingPlane");
      _farProperty = serializedObject.FindProperty("_farClippingPlane");
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("Render Target");
      _renderTargetIdProperty.intValue = (int)((Target)EditorGUILayout.EnumPopup
        ((Target)_renderTargetIdProperty.intValue));
      EditorGUILayout.EndHorizontal();
      
      if (_renderTargetIdProperty.intValue == 0)
      {
        _cameraProperty.objectReferenceValue = EditorGUILayout.ObjectField
          ("Camera", _cameraProperty.objectReferenceValue, typeof(Camera), true);

        // Autofill camera
        if (_cameraProperty.objectReferenceValue == null)
          _cameraProperty.objectReferenceValue = ((ARRenderingManager)target).GetComponent<Camera>();
      }

      _textureProperty.objectReferenceValue = _renderTargetIdProperty.intValue > 0
        ? EditorGUILayout.ObjectField
          ("Texture", _textureProperty.objectReferenceValue, typeof(RenderTexture), false)
        : null;
      
      // Only require clipping plane distances when targeting a render texture.
      // Otherwise, these values are copied from the target camera.
      if (_renderTargetIdProperty.intValue > 0)
      {
        _nearProperty.floatValue = EditorGUILayout.FloatField("Near Clip Plane", _nearProperty.floatValue);
        _farProperty.floatValue = EditorGUILayout.FloatField("Far Clip Plane", _farProperty.floatValue);
      }
      
      if (_renderTargetIdProperty.intValue > 0 && _textureProperty.objectReferenceValue == null)
      {
        EditorGUILayout.HelpBox
        (
          "If the target texture is not set, it will be automatically created.",
          MessageType.Info
        );
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
