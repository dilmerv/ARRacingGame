// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_EDITOR
using System;

using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.Internals.EditorUtilities
{
  /// <summary>
  /// This drawer does the actual autofilling by grabbing the appropritate component to fill this
  /// property with. The fact that this is a property drawer means that it's limited to being able
  /// to work on the actively selected GameObject.
  /// </summary>
  [CustomPropertyDrawer(typeof(_AutofillAttribute))]
  internal class _AutofillDrawer: PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      //Only run if we don't have an object filled
      if (property.propertyType == SerializedPropertyType.ObjectReference &&
        property.objectReferenceValue == null)
      {
        //Use the autofill type to decide which kind of GetComponent we call to fill the field
        switch (((_AutofillAttribute) attribute).autofillType)
        {
          case _AutofillAttribute.AutofillType.Self:
            property.objectReferenceValue =
              Selection.activeGameObject.GetComponent(fieldInfo.FieldType);

            break;

          case _AutofillAttribute.AutofillType.Parent:
            property.objectReferenceValue =
              Selection.activeGameObject.GetComponentInParent(fieldInfo.FieldType);

            break;

          case _AutofillAttribute.AutofillType.Children:
            property.objectReferenceValue =
              Selection.activeGameObject.GetComponentInChildren(fieldInfo.FieldType);

            break;

          default:

            throw new ArgumentOutOfRangeException();
        }
      }

      //Still use the normal property field to show the UI
      EditorGUI.PropertyField(position, property);
    }
  }
}
#endif
