using System;
using System.ComponentModel;

using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Assertions;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// Add this to any mesh in the Editor in order to have it semantically
  /// segmented as a certain channel.
  public class MockSemanticLabel: MonoBehaviour
  {
    public enum ChannelName
    {
      sky, ground, artificial_ground, water, building, foliage, grass
    }

    public ChannelName Channel;

    private MaterialPropertyBlock materialPropertyBlock;

    private void Awake()
    {
      var bits = ToBits(Channel);
      var color = ToColor(bits);

      materialPropertyBlock = new MaterialPropertyBlock();
      materialPropertyBlock.SetColor("PackedColor", color);
      materialPropertyBlock.SetColor("DebugColor", _debugColors[(int)Channel]);
      GetComponent<Renderer>().SetPropertyBlock(materialPropertyBlock);

      ARLog._DebugFormat("GameObject: {0} - Channel: {1} - Bits: {2}", false, gameObject.name, Channel, ToBinaryString(bits));
    }

    // Channel format copied from semantic_buffer.cpp
    static uint ToBits(ChannelName channel)
    {
      return 1u << (_NativeSemanticBuffer.BitsPerPixel - 1 - (int)channel);
    }

    static Color32 ToColor(uint buffer)
    {
      byte r = (byte)((buffer) & 0xFF);
      byte g = (byte)((buffer >> 8) & 0xFF);
      byte b = (byte)((buffer >> 16) & 0xFF);
      byte a = (byte)((buffer >> 24) & 0xFF);

      return new Color32(r, g, b, a);
    }

    public static uint ToInt(Color32 color)
    {
      uint buffer = 0;

      uint A = (uint) color.a << 24;
      uint B = (uint) color.b << 16;
      uint G = (uint) color.g << 8;
      uint R = color.r;


      buffer = R + G + B + A;

      return buffer;
    }

    public static string ToBinaryString(uint val)
    {
      var bits = Convert.ToString(val, 2).ToCharArray();
      var numBits = bits.Length;

      var a = new char[39]; // 32 bits + 7 spaces

      var ai = a.Length - 1;
      var si = numBits - 1;

      var spaceCount = 0;
      while (si >= 0)
      {
        if (spaceCount == 4)
        {
          spaceCount = 0;
          a[ai--] = ' ';
        }

        spaceCount++;

        try
        {
          a[ai--] = bits[si--];
        }
        catch (Exception e)
        {
          throw e;
        }
      }

      // Pad leading zeroes
      while (ai >= 0)
      {
        if (spaceCount == 4)
        {
          spaceCount = 0;
          a[ai--] = ' ';
        }

        spaceCount++;
        a[ai--] = '0';
      }

      return new string(a);
    }

#region DEBUG
    private static Color[] _debugColors = { Color.blue, new Color(165/255f, 42/255f, 42/255f, 1), Color.gray, Color.cyan, Color.yellow, Color.red, Color.green};
#endregion
  }
}
