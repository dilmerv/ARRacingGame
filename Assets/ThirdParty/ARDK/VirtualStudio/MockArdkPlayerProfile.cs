// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio
{
  [Serializable]
  public sealed class MockArdkPlayerProfile
  {
    [SerializeField] private string _playerName;
    [SerializeField] private bool _isActive = false;

    [SerializeField] private bool _usingAR = false;
    [SerializeField] private bool _usingNetwork = false;
    [SerializeField] private bool _usingARNetworking = false;

    public string PlayerName
    {
      get { return _playerName; }
    }

    public bool IsActive
    {
      get { return _isActive; }
      internal set { _isActive = value; }
    }

    public bool UsingAR
    {
      get { return _usingAR; }
      internal set { _usingAR = value; }
    }

    public bool UsingNetwork
    {
      get { return _usingNetwork; }
      internal set { _usingNetwork = value; }
    }

    public bool UsingARNetworking
    {
      get { return _usingARNetworking; }
      internal set { _usingARNetworking = value; }
    }

    // Todo (long term):
    // Revisit idea of enabling using either Mock or Remote AR in conjunction with Mock networking
    internal RuntimeEnvironment RuntimeEnvironment
    {
      get { return RuntimeEnvironment.Mock; }
    }

    internal Func<MockArdkPlayerProfile, GameObject> SpawnPlayerObjectDelegate;

    public MockArdkPlayerProfile
    (
      string playerName,
      bool usingNetwork,
      bool usingAR,
      bool usingARNetworking,
      bool isActive = true
    )
    {
      _playerName = playerName;
      _usingNetwork = usingNetwork;
      _usingAR = usingAR;
      _usingARNetworking = usingARNetworking;
      _isActive = isActive;
    }

    internal GameObject SpawnPlayerObject()
    {
      return SpawnPlayerObjectDelegate(this);
    }

    public MockPlayer GetPlayer()
    {
      return _VirtualStudioManager.Instance.GetPlayer(_playerName);
    }

    public override string ToString()
    {
      return string.Format
      (
        "{0}'s Profile (Networked [{1}], AR [{2}], ARNetworking [{3}])",
        PlayerName,
        UsingNetwork,
        UsingAR,
        UsingARNetworking
      );
    }
  }
}