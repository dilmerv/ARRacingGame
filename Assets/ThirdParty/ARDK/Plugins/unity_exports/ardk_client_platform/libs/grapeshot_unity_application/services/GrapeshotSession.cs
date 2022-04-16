// Copyright 2013-2020 Niantic, Inc. All Rights Reserved.

using System;
using Grapeshot;
using ror.schema.upload;

namespace ARDK.Grapeshot {
  /// <summary>
  /// Wrapper around grapeshot API's Session, which runs an upload session for a single file.
  /// </summary>
  public interface IGrapeshotSession : IDisposable {
    /// <summary>
    /// Is the upload process of the session finished.
    /// </summary>
    /// <returns>True if finished, false if in progress.</returns>
    bool IsFinished();
    
    /// <summary>
    /// Wrapper around grapeshot session's Process(), which prosecutes all free chunk tickets.
    /// </summary>
    void Process();
  }

  public class GrapeshotSession : IGrapeshotSession {
    private Session _session;

    public GrapeshotSession(
      Guid guid,
      UploadSessionInfo uploadSessionInfo,
      Driver driver,
      Rate rates,
      Action<ChunkFetcherParams> chunkFetcherCallback,
      Action<bool> uploadSuccessCallback,
      Action<UploadChunkResponse> uploadChunkResponseCallback
    ) {
      _session = new Session(
        guid,
        uploadSessionInfo,
        driver,
        rates,
        new ApplicationChunkFetcher_FunctionApplicationBridge(cfParams => {
            if (chunkFetcherCallback != null) { chunkFetcherCallback.Invoke(cfParams); }
          }
        ),
        new FinishedCallback_FunctionApplicationBridge(success => {
          if (uploadSuccessCallback != null) { uploadSuccessCallback.Invoke(success); }
        }),
        new ResponseCallback_FunctionApplicationBridge(response => {
          if (uploadChunkResponseCallback != null) { uploadChunkResponseCallback.Invoke(response); }
        }));
    }

    public bool IsFinished() {
      var session = _session;
      if (session == null)
        return true;

      return session.isFinished();
    }

    public void Process() {
      _session.process();
    }

    public void Dispose() {
      var session = _session;
      if (session != null) {
        _session = null;
        session.Dispose();
      }
    }
  }
}