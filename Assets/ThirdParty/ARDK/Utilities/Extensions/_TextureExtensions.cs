// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Utilities.Extensions
{
  /// Generates additional Texture2Ds that are not supported by default
  internal static class _TextureExtensions
  {
    /// Generates a gray texture.
    /// @note
    ///   This class is mainly used to generate a black texture in the YCbCr format. Set the
    ///   'TextureY' field to Texture2D.black and the 'TextureCbCr' to this texture to get black.
    public static Texture2D gray
    {
      get
      {
        var texture = new Texture2D(2, 2);
        texture.SetPixels(new[] {Color.gray, Color.gray, Color.gray, Color.gray});
        texture.Apply();

        return texture;
      }
    }
  }
}
