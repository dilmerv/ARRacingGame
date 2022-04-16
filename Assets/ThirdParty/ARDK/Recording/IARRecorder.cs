// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Used to record an AR session.
  /// </summary>
  public interface IARRecorder
  {
    /// <summary>
    /// Starts recording an AR session.
    /// </summary>
    /// <param name="recordingConfig">The configs to use for this recording.</param>
    void Start(ARRecorderConfig recordingConfig);

    /// <summary>
    /// Stops recording a session.
    /// </summary>
    /// <param name="previewConfig">Preview config for recordings.</param>
    /// <param name="previewCallback">
    /// The callback to call once preview post-processing on the recording has finished.
    /// </param>
    /// <param name="researchConfig">Research config for recordings.</param>
    /// <param name="researchCallback">
    /// The callback to call once research post-processing on the recording has finished.
    /// </param>
    /// <param name="unpackConfig">Configs for unpacking the frames raw to a directory.</param>
    /// <param name="unpackCallback">The callback to call when unpacking is finished.</param>
    void Stop(
      ARRecordingPreviewConfig previewConfig,
      Action<ARRecordingPreviewResults> previewCallback,
      ARRecordingResearchConfig researchConfig,
      Action<ARRecordingResearchResults> researchCallback,
      ARRecordingUnpackConfig unpackConfig,
      Action<ARRecordingUnpackResults> unpackCallback);

    /// <summary>
    /// Get the progress for processing the preview video for an AR Recording.
    /// </summary>
    /// <returns>a value between 0 and 1</returns>
    float PreviewProgress();

    /// <summary>
    /// Get the progress for processing research data for an AR Recording.
    /// </summary>
    /// <returns>a value between 0 and 1</returns>
    float ResearchProgress();

    /// <summary>
    /// The progress of unpacking a recording into raw frames.
    /// </summary>
    /// <returns></returns>
    float UnpackProgress();

    /// <summary>
    /// Cancel processing the preview video for an AR recording
    /// Causes the preview callback to be called immediately
    /// </summary>
    void CancelPreview();

    /// <summary>
    /// Cancel processing the research data for an AR recording.
    /// Causes the researchs callback to be called immediately
    /// </summary>
    void CancelResearch();

    /// <summary>
    /// Cancels progress for unpacking raw frames.
    /// </summary>
    void CancelUnpack();

    /// <summary>
    /// Archives temporary AR recording directories into a gzipped .tar
    /// </summary>
    /// <param name="sourceDirectoryPath">
    /// The source directory path to archive.
    /// </param>
    /// <param name="destinationArchivePath">
    /// Target destination archive path.
    /// </param>
    void ArchiveWorkingDirectory(
          String sourceDirectoryPath,
          String destinationArchivePath);

    /// <summary>
    /// Stores the name of the application.
    /// Calling this method multiple times will add multiple entries to recording data
    /// The recorder *must* be started before calling this method
    /// </summary>
    void SetApplicationName(String applicationName);

    /// <summary>
    /// Stores the point of interest, represented as a string.
    /// Calling this method multiple times will add multiple entries to recording data
    /// The recorder *must* be started before calling this method
    /// </summary>
    void SetPointOfInterest(String pointOfInterest);
  }
}