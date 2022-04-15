// Not sure if copied or inspired by https://gist.github.com/ikriz/b0f9d96205629e19859e

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.External {

  [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
  public class EnumFlagDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      System.Type propertyType = fieldInfo.FieldType;
      string[] enumNames = System.Enum.GetNames(propertyType);
      int[] enumValues = System.Enum.GetValues(propertyType) as int[];

      int currentVal = property.intValue;
      int enumMask = 0;
      for (int i = 0; i < enumValues.Length; i++) {
        // If the value isn't 0 (aka none)
        if (enumValues[i] != 0) {
          if ((enumValues[i] & currentVal) == enumValues[i]) {
            enumMask |= (1 << i);
          }
        }
      }

      int newEnumMask = EditorGUI.MaskField(position, label, enumMask, enumNames);
      if (newEnumMask != enumMask) {
        int newVal = 0;
        for (int i = 0; i < enumValues.Length; i++) {
          if ((newEnumMask & (1 << i)) != 0) {
            newVal |= enumValues[i];
          }
        }

        property.intValue = newVal;
      }
    }
  }

}

#endif
