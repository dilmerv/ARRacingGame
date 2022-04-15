// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.IO;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct PersistentKeyValueUpdatedArgs:
    IArdkEventArgs
  {
    public PersistentKeyValueUpdatedArgs(string key, byte[] value):
      this()
    {
      Key = key;
      _value = value;
    }

    public string Key { get; private set; }

    private readonly byte[] _value;
    public MemoryStream CreateValueReader()
    {
      return new MemoryStream(_value, false);
    }

    public byte[] CopyValue()
    {
      return (byte[])_value.Clone();
    }
  }
}
