// Copyright 2013-2020 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;
using FlatBuffers;
using Grapeshot;
using ror.schema;
using ror.schema.upload;
using Random = UnityEngine.Random;
using UnityEngine;

namespace ARDK.Grapeshot {
  /// <summary>
  /// This interface directly interacts with the swig-generated native grapeshot API. It
  /// specifically handles multiple chunk upload through GCS signed URLs.
  /// </summary>
  public interface IGcsSignedUrlUploadService {
    /// <summary>
    /// Create a unique session that operates uploading for the provided file.
    /// </summary>
    /// <param name="stage">stage identifier created from backend topology</param>>
    /// <param name="filePath">Where the file is stored.</param>
    /// <param name="proto">Proto containing list of signed urls for each chunk and compose.</param>
    /// <param name="chunkCount">Number of chunks to upload in a batch.</param>
    /// <param name="rates">Rate limits for upload session</param>
    /// <param name="uploadSuccessCallback">Callback when upload is finished.</param>
    /// <param name="uploadChunkResponseCallback">Callback when a single chunk is processed.</param>
    IGrapeshotSession CreateGrapeshotUploadSession(
      Guid stage,
      string filePath,
      IGrapeshotUploadData proto,
      int chunkCount,
      Rate[] rates,
      Action<bool> uploadSuccessCallback,
      Action<IGrapeshotUploadChunkResponse> uploadChunkResponseCallback
    );
  }

  public class GcsSignedUrlUploadService : IGcsSignedUrlUploadService {
    private const int FLAT_BUFFER_INITIAL_SIZE = 1024;

    public IGrapeshotSession CreateGrapeshotUploadSession(
      Guid stage,
      string filePath,
      IGrapeshotUploadData data,
      int chunkCount,
      Rate[] rates,
      Action<bool> uploadSuccessCallback,
      Action<IGrapeshotUploadChunkResponse> uploadChunkResponseCallback
    ) {
      var uploadSessionInfo = CreateUploadSessionInfo(
        data,
        chunkCount
      );

      // Create our session object that will handle the work of uploading.
      return new GrapeshotSession(
        Guid.Empty,
        uploadSessionInfo,
        new InterprocDriver(stage, new LocalUploadSessionDBAccessor()),
        MultiRate.createMultiRate(rates),
        (cfParams) => { ChunkFetchCallback(cfParams, filePath, data, chunkCount); },
        uploadSuccessCallback,
        (response) => { UploadChunkResponse(response, uploadChunkResponseCallback); }
      );
    }

    /// <summary>
    // Create upload session info that contains the default auth used for each chunk, as well as
    // the auth used for operations not on a single chunk (such as composing).
    /// </summary>
    /// <param name="proto">Titan protobuf with all upload data</param>
    /// <param name="chunkCount">number of chunks that the upload is divided into</param>
    /// <returns></returns>
    private static UploadSessionInfo CreateUploadSessionInfo(
      IGrapeshotUploadData data,
      int chunkCount
    ) {
      // Populate info about upload to GCS bucket
      var fbb = new FlatBufferBuilder(FLAT_BUFFER_INITIAL_SIZE);
      var bucketOffset = fbb.CreateString(data.GcsBucket);
      var objectOffset = fbb.CreateString(
        GcsInfoProvider.StripFilePathSlash(data.ComposeData.TargetFilePath)
      );
      var serviceInfoOffset = GCSServiceInfo.CreateGCSServiceInfo(fbb, objectOffset, bucketOffset);

      // Populate session authentication info
      var authOffset = GcsInfoProvider.WriteGcsSignedUrlAuth(
        fbb,
        data.ComposeData.Authentication.Authorization,
        data.ComposeData.Authentication.Date
      );

      // Populate the upload session info with the combined data above
      UploadSessionInfo.StartUploadSessionInfo(fbb);
      var sessionIdOffset = UUID.CreateUUID(
        fbb,
        (ulong) Random.Range(0, int.MaxValue),
        (ulong) Random.Range(0, int.MaxValue)
      );

      UploadSessionInfo.AddSession(fbb, sessionIdOffset);
      UploadSessionInfo.AddServiceInfoType(fbb, ServiceInfos.GCSServiceInfo);
      UploadSessionInfo.AddServiceInfo(fbb, serviceInfoOffset.Value);
      UploadSessionInfo.AddSessionAuth(fbb, authOffset);
      UploadSessionInfo.AddOutOf(fbb, (ulong) chunkCount);
      var uploadSessionInfoOffset = UploadSessionInfo.EndUploadSessionInfo(fbb);

      fbb.Finish(uploadSessionInfoOffset.Value);

      return UploadSessionInfo.GetRootAsUploadSessionInfo(fbb.DataBuffer);
    }

    private static void UploadChunkResponse(
      UploadChunkResponse response,
      Action<IGrapeshotUploadChunkResponse> uploadChunkResponseCallback
    ) {
      var wrapperResponse = new GrapeshotUploadChunkResponse(response);
      uploadChunkResponseCallback(wrapperResponse);
    }

    private static void ChunkFetchCallback(
      ChunkFetcherParams cfParams,
      string filePath,
      IGrapeshotUploadData data,
      int chunkCount
    ) {
      using (var fileStream = File.OpenRead(filePath)) {
        // Calculate the portion of data to be sending and convert it to a byte[].
        var chunkSize = fileStream.Length / chunkCount;
        var startIndex = ((int) cfParams.index * chunkSize);

        // The chunk size is an average chunk size, that may go over/under the end of the data. To
        // prevent this, we re-calculate if the chunk goes over the back to fit the back of
        // the array.
        if ((int) cfParams.index + 1 == chunkCount) {
          chunkSize = fileStream.Length - startIndex;
        }

        var chunkFbb = new FlatBufferBuilder(FLAT_BUFFER_INITIAL_SIZE);

        // Store the data to send into it.
        UploadChunkBody.StartDataVector(chunkFbb, (int) chunkSize);

        var buffer = new byte[chunkSize];

        fileStream.Seek(startIndex, SeekOrigin.Begin);
        var bytesRead = fileStream.Read(buffer, 0, buffer.Length);

        if (bytesRead > 0) {
          for (var i = chunkSize - 1; i >= 0; i--) {
            var b = buffer[i];
            chunkFbb.PutByte(b);
          }
        } else {
          Debug.LogError("Filestream is reading empty byte");
        }

        CreateChunkBodyAndUpload(cfParams, data, chunkFbb);
      }
    }

    private static void CreateChunkBodyAndUpload(
      ChunkFetcherParams cfParams,
      IGrapeshotUploadData data,
      FlatBufferBuilder chunkFbb
    ) {
      // Store the data to send into it.
      var dataOffset = chunkFbb.EndVector();

      // Create the chunk body as the root piece.
      UploadChunkBody.StartUploadChunkBody(chunkFbb);
      UploadChunkBody.AddData(chunkFbb, dataOffset);
      var chunkBodyOffset = UploadChunkBody.EndUploadChunkBody(chunkFbb);
      chunkFbb.Finish(chunkBodyOffset.Value);

      // Convert back into an object, and set the body return to that flatbuffer.
      var chunkBody = UploadChunkBody.GetRootAsUploadChunkBody(chunkFbb.DataBuffer);

      var chunkData = data.ChunkData[(int) cfParams.index];
      var authBody = GcsInfoProvider.CreateGcsSignedWriteUploadAuthPerChunk(
        FLAT_BUFFER_INITIAL_SIZE,
        chunkData.UploadAuthentication.Authorization,
        chunkData.UploadAuthentication.Date,
        chunkData.DeleteAuthentication.Authorization,
        chunkData.DeleteAuthentication.Date
      );
      var serviceBody = GcsInfoProvider.CreateGcsServiceInfoPerChunk(
        FLAT_BUFFER_INITIAL_SIZE,
        data.GcsBucket,
        chunkData.FilePath
      );

      cfParams.serviceInfoReturn.ret(serviceBody);
      cfParams.authReturn.ret(authBody);
      cfParams.bodyReturn.ret(chunkBody);
    }
  }
}
