using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;

using UnityEngine;

namespace Niantic.ARDK.AR
{
  public interface ISemanticBufferProcessor: 
    IAwarenessBufferProcessor
  {
    /// The CPU copy of the latest awareness buffer.
    ISemanticBuffer AwarenessBuffer { get; }
    
    /// The number of classes available.
    uint ChannelCount { get; }
    
    /// Returns the possible semantic classes that a pixel can be interpreted.
    string[] Channels { get; }

    /// Returns the semantics of the specified pixel. 
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns The result is a 32-bit packed unsigned integer
    /// where each bit is a binary indicator for a class.
    uint GetSemantics(int viewportX, int viewportY);
    
    /// Returns an array of channel indices that are present for the specified pixel.
    /// @note This query allocates garbage.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns An array of channel indices present for the pixel.
    int[] GetChannelIndicesAt(int viewportX, int viewportY);

    /// Returns an array of channel names that are present for the specified pixel.
    /// @note This query allocates garbage.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns An array of channel names present for the pixel.
    string[] GetChannelNamesAt(int viewportX, int viewportY);

    /// Check if a pixel is of a certain semantics class.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @param channelIndex Channel index of the semantic class to look for.
    /// @returns True if the semantic channel exists at the given coordinates.
    bool DoesChannelExistAt(int viewportX, int viewportY, int channelIndex);

    /// Check if a pixel is of a certain semantics class.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @param channelName Name of the semantic class to look for.
    /// @returns True if the semantic channel exists at the given coordinates.
    bool DoesChannelExistAt(int viewportX, int viewportY, string channelName);

    /// Pushes the current state of the semantics buffer to the specified target texture.
    /// The resulting texture will contain a display aligned representation of the specified channel.
    /// @note Only use this call if you absolutely need the texture to be display aligned.
    ///   It is faster to create a texture from the awareness buffer itself.
    /// @param channel The semantic channel index to create a texture of.
    /// @param texture The target texture. If this texture does not exist, it will be created.
    ///   It is the responsibility of the caller to release this texture.
    /// @param orientation The target orientation of the texture. This determines the resolution of
    ///   the container. This has to be either landscape or portrait. 
    void CopyToAlignedTextureARGB32(int channel, ref Texture2D texture, ScreenOrientation orientation);
    
    /// Pushes the current state of the semantics buffer to the specified target texture.
    /// The resulting texture will contain a display aligned representation of the specified channels.
    /// @param channels The semantic channel indices to create a texture of.
    /// @param texture The target texture. If this texture does not exist, it will be created.
    ///   It is the responsibility of the caller to release this texture.
    /// @param orientation The target orientation of the texture. This determines the resolution of
    ///   the container. This has to be either landscape or portrait. 
    void CopyToAlignedTextureARGB32(int[] channels, ref Texture2D texture, ScreenOrientation orientation);
  }
}