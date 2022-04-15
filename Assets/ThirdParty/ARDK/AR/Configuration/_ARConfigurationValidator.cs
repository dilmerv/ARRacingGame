// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Configuration
{
  internal static class _ARConfigurationValidator
  {
    /** Compatibility Cases
     *  Currently it's not possible to have anything other than IARWorldTrackingConfiguration,
     *  so we don't handle those cases in detail. If FaceTracking is re-added to ARDK, need to handle
     *  Face and World tracking switches somehow.
     *
     *  Between WorldTrackingConfigurations, the only valid MappingRole transitions are from
     *  Mapping to Localizing and back. You can NOT transition to or out of the MapperIfHost role.
     */

    public static bool IsCompatibleWithConfig
    (
      this IARConfiguration c1,
      IARConfiguration c2,
      out string message
    )
    {
      message = string.Empty;
      if (c1 == null)
        return true;

      var prevWorldConfig = c1 as IARWorldTrackingConfiguration;
      var currWorldConfig = c2 as IARWorldTrackingConfiguration;

      var failureReason = string.Empty;
      if (prevWorldConfig != null && currWorldConfig != null)
      {
        var prevHasReuseRole = prevWorldConfig.MappingRole != MappingRole.MapperIfHost;
        var currHasReuseRole = currWorldConfig.MappingRole != MappingRole.MapperIfHost;

        if (prevHasReuseRole != currHasReuseRole)
          failureReason = "MappingRole";

        var sameMapLayer =
          prevWorldConfig.MapLayerIdentifier.Equals(currWorldConfig.MapLayerIdentifier);

        if (!sameMapLayer)
          failureReason = "MapLayerIdentifier";
      }

      if (!string.IsNullOrEmpty(failureReason))
      {
        message =
          string.Format
          (
            "Given configuration's {0} value is incompatible with the one previously used to run this session",
            failureReason
          );

        return false;
      }

      return true;
    }

    public static bool IsCompatibleWithSession
    (
      this IARConfiguration config,
      IARSession session,
      out string message
    )
    {
      message = string.Empty;

      var worldConfig = config as IARWorldTrackingConfiguration;

      if (worldConfig == null)
        return true;

      var hasMapLayer = !worldConfig.MapLayerIdentifier.IsEmpty();

      if (hasMapLayer && !worldConfig.IsSharedExperienceEnabled)
      {
        message = "Only configurations where IsSharedExperienceEnabled is true can use map layers.";
        return false;
      }

      var hasResuleRole = worldConfig.MappingRole != MappingRole.MapperIfHost;
      if (hasMapLayer && !hasResuleRole)
      {
        message = "Only configurations where MappingRole is not MapperIfHost can use map layers.";
        return false;
      }

      if (!hasMapLayer && hasResuleRole)
      {
        message = "Only configurations with a valid MapLayerIdentifier value can use map layers.";
        return false;
      }

      var isEditorSession = session.RuntimeEnvironment != RuntimeEnvironment.LiveDevice;
      if (isEditorSession && (hasMapLayer || hasResuleRole))
      {
        message = "Map layers are not supported by Virtual Studio AR sessions.";
        return false;
      }

      return true;
    }

    private static bool IsValidConfiguration(this IARConfiguration config, out string message)
    {
      message = string.Empty;

      if (config is IARWorldTrackingConfiguration worldConfig)
      {
        var hasHeadingAlignment = (worldConfig.WorldAlignment == WorldAlignment.GravityAndHeading);
        if (worldConfig.IsSharedExperienceEnabled && hasHeadingAlignment)
        {
          message =
            "Configuration with SharedExperienceEnabled can not use GravityAndHeading world " +
            "alignment.";

          return false;
        }

        if (worldConfig.IsMeshingEnabled && hasHeadingAlignment)
        {
          message =
            "Configuration with IsMeshingEnabled can not use GravityAndHeading world alignment.";

          return false;
        }
      }

      return true;
    }

    private static void SetMissingValues(this IARConfiguration config)
    {
      var worldConfig = config as IARWorldTrackingConfiguration;

      if (worldConfig == null)
        return;


      var isDepthEnabled = worldConfig.IsDepthEnabled;
      var isPointCloudEnabled = worldConfig.DepthPointCloudSettings.IsEnabled;

      if (isPointCloudEnabled && !isDepthEnabled)
      {
        ARLog._WarnRelease
        (
          "Enabling depth because depth point clouds were enabled. Use the ARDepthManager " +
          "component or the IARWorldTrackingConfiguration properties to further configure depth " +
          "functionality."
        );

        worldConfig.IsDepthEnabled = true;
      }

      if (worldConfig.IsMeshingEnabled && !isDepthEnabled)
      {
        ARLog._WarnRelease
        (
          "Enabling depth because meshing was enabled. Use the ARDepthManager component or " +
          "the IARWorldTrackingConfiguration properties to further configure depth functionality."
        );

        worldConfig.IsDepthEnabled = true;
      }

      var needsContextAwarenessUrl =
        isDepthEnabled ||
        worldConfig.IsMeshingEnabled ||
        worldConfig.IsSemanticSegmentationEnabled;

      var hasEmptyUrl = string.IsNullOrEmpty(ArdkGlobalConfig.GetContextAwarenessUrl());

      if (needsContextAwarenessUrl && hasEmptyUrl)
      {
        ARLog._Debug("Context Awareness URL was not set. The default URL will be used.");
        ArdkGlobalConfig.SetContextAwarenessUrl("");
      }
    }

    private static void CheckDeviceSupport(this IARConfiguration config)
    {
      var worldConfig = config as IARWorldTrackingConfiguration;

      if (worldConfig == null)
        return;

      if (worldConfig.IsDepthEnabled &&
        !ARWorldTrackingConfigurationFactory.CheckDepthEstimationSupport())
      {
        ARLog._Error
        (
          "Depth estimation is not supported on this device. " +
          "Unexpected behaviour or crashes may occur."
        );
      }

      if (worldConfig.IsMeshingEnabled &&
        !ARWorldTrackingConfigurationFactory.CheckMeshingSupport())
      {
        ARLog._Error
        (
          "Meshing is not supported on this device. " +
          "Unexpected behaviour or crashes may occur."
        );
      }

      if (worldConfig.IsSemanticSegmentationEnabled &&
        !ARWorldTrackingConfigurationFactory.CheckSemanticSegmentationSupport())
      {
        ARLog._Error
        (
          "Semantic segmentation is not supported on this device. " +
          "Unexpected behaviour or crashes may occur."
        );
      }
    }

    public static bool RunAllChecks
    (
      IARSession arSession,
      IARConfiguration newConfiguration
    )
    {
      string sessionCheckMessage;
      if (!newConfiguration.IsCompatibleWithSession(arSession, out sessionCheckMessage))
      {
        ARLog._Error(sessionCheckMessage);
        return false;
      }

      string configCheckMessage;
      if (!arSession.Configuration.IsCompatibleWithConfig(newConfiguration, out configCheckMessage))
      {
        ARLog._Error(configCheckMessage);
        return false;
      }

      string validConfigCheckMessage;
      if (!newConfiguration.IsValidConfiguration(out validConfigCheckMessage))
      {
        ARLog._Error(validConfigCheckMessage);
        return false;
      }

      newConfiguration.SetMissingValues();

      // ARDK's device support checks serve as recommendations, not a hard block.
      // Devices are still able to try to run unsupported features.
      newConfiguration.CheckDeviceSupport();

      return true;
    }
  }
}
