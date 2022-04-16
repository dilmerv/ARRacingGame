// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

namespace Niantic.ARDK.Utilities.Extensions
{
  // Extensions to the Stream class.
  // Considering Stream already throws in many situations, this class adds methods to either
  // read data or throw, instead of returning magic values or similar.
  internal static class _StreamExtensions
  {
    /// Reads a single byte from the stream, or throws if there are no more bytes to read.
    /// This way, we don't need to deal with a possible -1 (returned as int) when for our
    /// code the entire action should succeed or throw.
    public static byte ReadByteOrThrow(this Stream stream)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      int result = stream.ReadByte();
      if (result == -1)
        throw new IOException("End of stream encountered when we still needed more data.");

      return (byte)result;
    }

    /// Reads length bytes from stream, putting it into buffer, starting at startIndex.
    /// Either the entire amount is read, or an exception is thrown, so there's no need to evaluate
    /// results and update the startIndex/length in a future call, like it is required when using
    /// the default stream.Read method.
    public static void ReadOrThrow(this Stream stream, byte[] buffer, int startIndex, int length)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      if (buffer == null)
        throw new ArgumentNullException(nameof(buffer));

      if (startIndex > buffer.Length || startIndex < 0)
        throw new ArgumentOutOfRangeException(nameof(startIndex));

      if (length < 0 || startIndex + length > buffer.Length)
        throw new ArgumentOutOfRangeException(nameof(length));

      while (length > 0)
      {
        int read = stream.Read(buffer, startIndex, length);
        if (read == 0)
          throw new IOException("Could not read the entire required buffer.");

        if (read < 0)
          throw new IOException("Error reading the current buffer.");

        startIndex += read;
        length -= read;
      }
    }
  }
}
