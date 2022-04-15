// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Internals.EditorUtilities
{
  /// <summary>
  /// This attribute sets an object field to be set while the gameobject is selected in the editor
  /// by grabbing the appropritate component to fill this property with. Grabbing it during
  /// Editor-time saves us from having to call GetComponent on a bunch of objects as we're
  /// creating them at runtime, and this helps save us from forgetting to set or having to do a
  /// bunch of unnecessary drag-and-drop. This should typically be combined with [SerializeField]
  /// </summary>
  internal class _AutofillAttribute: PropertyAttribute
  {
    /// <summary>
    /// Local enum for the different fill types.
    /// </summary>
    public enum AutofillType
    {
      Self,
      Parent,
      Children
    }

    /// <summary>
    /// This attribute's autofill type.
    /// </summary>
    public readonly AutofillType autofillType;

    /// <summary>
    /// Default attribute option, just does a plain GetComponent
    /// </summary>
    public _AutofillAttribute()
      : this(AutofillType.Self)
    {}

    /// <summary>
    /// Attribute option with a different fill type. Should only really be used if the value isn't
    /// AutofillType.Self
    /// </summary>
    /// <param name="autofillType">The variant on GetComponent that should be used
    /// to fill this field</param>
    public _AutofillAttribute(AutofillType autofillType)
    {
      this.autofillType = autofillType;
    }
  }
}
