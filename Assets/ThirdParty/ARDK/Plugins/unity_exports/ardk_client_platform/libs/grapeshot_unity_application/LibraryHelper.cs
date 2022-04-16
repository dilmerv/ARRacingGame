
// Copyright 2019 Niantic, Inc. All Rights Reserved.

namespace Grapeshot {

    public static class LibraryHelper {
        #if UNITY_EDITOR
        internal const string libraryName = "ardk_client_platform";

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
