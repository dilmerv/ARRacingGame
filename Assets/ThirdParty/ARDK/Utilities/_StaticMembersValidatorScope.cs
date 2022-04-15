// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if MUST_VALIDATE_STATIC_MEMBERS

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.VirtualStudio.Remote;

namespace Niantic.ARDK.Utilities
{
  internal sealed class _StaticMembersValidatorScope:
    IDisposable
  {
    private static _StaticMembersValidatorScope _currentInstance;

    internal static void _ForValidatorOnly_CheckScopeExists()
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(_StaticMemberValidator));

      if (_currentInstance == null)
        throw new InvalidOperationException("To use the current static member on tests, you need a scope.");
    }

    public _StaticMembersValidatorScope()
    {
      if (_currentInstance != null)
        throw new InvalidOperationException("Another Validator Scope already exists.");

      _CheckCleanState();

      _currentInstance = this;
    }

    public void Dispose()
    {
      if (_currentInstance != this)
        throw new InvalidOperationException("We are not the current scope. Double dispose?");

      _currentInstance = null;
      NetworkSpawner._Deinitialize();
      ARSessionCameraFeedExtension._Deinitialize();

      _VirtualStudioManager._ResetInstance();
      _RemoteConnection.Deinitialize();

      _CheckCleanState();
    }

    private static void _CheckCleanState()
    {
      _MockNetworkingSessionsMediator._CheckActiveCountIsZero();
      _StaticMemberValidator._ForScopeOnly_CheckCleanState();
    }
  }
}

#endif
