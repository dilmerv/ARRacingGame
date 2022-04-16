// Copyright 2021 Niantic, Inc. All Rights Reserved.
namespace Niantic.ARDK.Internals
{
  internal static class _ARDKLibrary
  {
#if UNITY_EDITOR
    #if IN_ROSETTA
                internal const string libraryName = "TODO_DOES_NOT_EXIST";
    #else
                internal const string libraryName = "ardk_client_platform";
    #endif

#elif UNITY_STANDALONE_OSX
		internal const string libraryName = "ardk_client_platform";

#elif UNITY_IOS
		internal const string libraryName = "__Internal";

#elif UNITY_ANDROID
		internal const string libraryName = "ardk_client_platform";

#else
		// TODO: Windows and Linux library name + support
		internal const string libraryName = "TODO_DOES_NOT_EXIST";
#endif
  }
}
