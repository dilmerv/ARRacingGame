// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  public sealed class IncorrectlyUsedNativeClassException
    : Exception
  {
    private const string IncorrectNativeClassMessage =
      "Using a Native class with no testing/non-native fallback on a non-native platform";

    public IncorrectlyUsedNativeClassException()
      : base(IncorrectNativeClassMessage)
    {
    }
  }
}
