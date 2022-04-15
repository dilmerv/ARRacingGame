// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.SLAM;
using System;
using System.Collections.Generic;

using Niantic.ARDK.AR.Awareness.Depth.Generators;
using Niantic.ARDK.AR.ReferenceImage;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Configuration
{
  internal sealed class _SerializableARWorldTrackingConfiguration:
    _SerializableARConfiguration,
    IARWorldTrackingConfiguration
  {
    public PlaneDetection PlaneDetection { get; set; }

    public bool IsAutoFocusEnabled { get; set; }

    public bool IsSharedExperienceEnabled { get; set; }

    public MappingRole MappingRole { get; set; }

    public MapLayerIdentifier MapLayerIdentifier { get; set; }

    public bool IsDepthEnabled { get; set; }

    public uint DepthTargetFrameRate { get; set; }

    public bool IsSemanticSegmentationEnabled { get; set; }

    public uint SemanticTargetFrameRate { get; set; }

    public bool IsMeshingEnabled { get; set; }

    public uint MeshingTargetFrameRate { get; set; }

    public float MeshingTargetBlockSize { get; set; }

    public float MeshingRadius
    {
      get { return _meshingRadius; }
      set
      {
        if (value > 0 && value < 5)
        {
          ARLog._Error
          (
            "The smallest meshing radius possible is 5 meters. " +
            "Set the value to 0 for an infinite radius."
          );

          return;
        }

        _meshingRadius = value;
      }
    }

    private float _meshingRadius;

    private DepthPointCloudGenerator.Settings _depthPointCloudSettings;
    public DepthPointCloudGenerator.Settings DepthPointCloudSettings
    {
      get
      {
        var result = _depthPointCloudSettings;

        if (result == null)
        {
          result = new DepthPointCloudGenerator.Settings();
          _depthPointCloudSettings = result;
        }

        return result;
      }
      set
      {
        _depthPointCloudSettings = value;
      }
    }

    private static bool _allowDetectionImagesForTesting = false;

    internal static void SetAllowDetectionImagesForTesting(bool newState)
    {
      _allowDetectionImagesForTesting = newState;
    }

    private IReadOnlyCollection<IARReferenceImage> _detectionImages =
      EmptyArdkReadOnlyCollection<IARReferenceImage>.Instance;
    public IReadOnlyCollection<IARReferenceImage> DetectionImages
    {
      get
      {
        if (_allowDetectionImagesForTesting)
        {
          return _detectionImages;
        }
        else
        {
          ARLog._Warn("DetectionImages property is not supported in the Unity. Returning an empty collection.");
          return EmptyArdkReadOnlyCollection<IARReferenceImage>.Instance;
        }
      }
      set
      {
        if (_allowDetectionImagesForTesting)
        {
          _detectionImages = value;
        }
        else
        {
          throw new NotSupportedException();
        }
      }
    }

    public void SetDetectionImagesAsync
    (
      IReadOnlyCollection<IARReferenceImage> detectionImages,
      Action completionHandler
    )
    {
      if (_allowDetectionImagesForTesting)
      {
        _detectionImages = detectionImages;
        completionHandler();
      }
      else
      {
        throw new NotSupportedException();
      }
    }

    public override void CopyTo(IARConfiguration target)
    {
      if (!(target is IARWorldTrackingConfiguration worldTarget))
      {
        var msg =
          "ARWorldTrackingConfiguration cannot be copied into a non-ARWorldTrackingConfiguration.";

        throw new ArgumentException(msg);
      }

      base.CopyTo(target);

      worldTarget.PlaneDetection = PlaneDetection;
      worldTarget.IsAutoFocusEnabled = IsAutoFocusEnabled;

      worldTarget.IsSharedExperienceEnabled = IsSharedExperienceEnabled;
      worldTarget.MappingRole = MappingRole;
      worldTarget.MapLayerIdentifier = MapLayerIdentifier;

      worldTarget.IsDepthEnabled = IsDepthEnabled;
      worldTarget.DepthTargetFrameRate = DepthTargetFrameRate;
      worldTarget.DepthPointCloudSettings = DepthPointCloudSettings.Copy();

      worldTarget.IsSemanticSegmentationEnabled = IsSemanticSegmentationEnabled;
      worldTarget.SemanticTargetFrameRate = SemanticTargetFrameRate;

      worldTarget.IsMeshingEnabled = IsMeshingEnabled;
      worldTarget.MeshingTargetFrameRate = MeshingTargetFrameRate;
      worldTarget.MeshingTargetBlockSize = MeshingTargetBlockSize;
      worldTarget.MeshingRadius = MeshingRadius;

      // Not copying DetectionImages because ARReferenceImage is not supported in Editor.
    }
  }
}
