// Copyright 2013-2020 Niantic, Inc. All Rights Reserved.

using ror.schema.upload;

namespace ARDK.Grapeshot {
  /// <summary>
  /// A wrapper around grapeshot API's UploadChunkResponse to add testability and expose the
  /// necessary parameters.
  /// </summary>
  public interface IGrapeshotUploadChunkResponse {
    /// <summary>
    /// Upload status of the chunk
    /// </summary>
    ChunkStatus Status { get; }
    
    /// <summary>
    /// Error message if the chunk upload fails
    /// </summary>
    string StatusMessage { get; }
  }

  public class GrapeshotUploadChunkResponse : IGrapeshotUploadChunkResponse {
    public GrapeshotUploadChunkResponse(UploadChunkResponse response) {
      Status = response.Status;
      StatusMessage = response.StatusMessage;
    }

    public ChunkStatus Status { get; private set; }
    public string StatusMessage { get; private set; }
  }
}