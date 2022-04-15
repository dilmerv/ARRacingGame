// Copyright 2013-2020 Niantic, Inc. All Rights Reserved.

using FlatBuffers;
using ror.schema.upload;

namespace ARDK.Grapeshot {
  /// <summary>
  /// GCS provider for signed url related authentication and info. This helper class takes in the
  /// provided information and creates a flatbuffer object out of it.
  /// </summary>
  public static class GcsInfoProvider {
    private const string FORWARD_SLASH = "/";
    private const string BACKWARD_SLASH = "\\";

    /// <summary>
    /// Create authentication for GCS Signed URL.
    /// </summary>
    /// <param name="fbb">flatbuffer for construct data on.</param>
    /// <param name="authorization">Signed url string.</param>
    /// <param name="date">Date string.</param>
    /// <returns>A flatbuffer offset of type UploadAuth.</returns>
    public static Offset<UploadAuth> WriteGcsSignedUrlAuth(
      FlatBufferBuilder fbb,
      string authorization,
      string date
    ) {
      // Add compose auth
      var gcsSignedAuthComposeOffset = fbb.CreateString(authorization);
      var dateComposeOffset = fbb.CreateString(date);
      var gcsComposedOffset = GCSSignedAuth.CreateGCSSignedAuth(
        fbb,
        gcsSignedAuthComposeOffset,
        dateComposeOffset
      );
      var gcsSignedComposedUrlOffset = GCSSignedComposeURL.CreateGCSSignedComposeURL(
        fbb,
        gcsComposedOffset
      );

      return UploadAuth.CreateUploadAuth(
        fbb,
        ServiceAuths.GCSSignedComposeURL,
        gcsSignedComposedUrlOffset.Value
      );
    }

    /// <summary>
    /// Create GCS service info for each upload chunk.
    /// </summary>
    /// <param name="flatBufferSize">Initial size of flatbuffer.</param>
    /// <param name="bucketName">GCS bucket name in string.</param>
    /// <param name="filePath">File path string for each chunk.</param>
    /// <returns>Returns a flatbuffer object with upload service info per chunk.</returns>
    public static UploadServiceInfo CreateGcsServiceInfoPerChunk(
      int flatBufferSize,
      string bucketName,
      string filePath
    ) {
      var fbb = new FlatBufferBuilder(flatBufferSize);

      // Populate info about uploading to GCS bucket per chunk
      var bucketOffset = fbb.CreateString(bucketName);
      var objectOffset = fbb.CreateString(StripFilePathSlash(filePath));
      var serviceInfoOffset = GCSServiceInfo.CreateGCSServiceInfo(fbb, objectOffset, bucketOffset);

      var offset = UploadServiceInfo.CreateUploadServiceInfo(
        fbb,
        ServiceInfos.GCSServiceInfo,
        serviceInfoOffset.Value
      );

      fbb.Finish(offset.Value);
      return UploadServiceInfo.GetRootAsUploadServiceInfo(fbb.DataBuffer);
    }

    /// <summary>
    /// Create GCS signed write auth flatbuffer object for each upload chunk.
    /// </summary>
    /// <param name="flatBufferSize">Initial size of flatbuffer.</param>
    /// <param name="uploadAuth">Upload authorization string.</param>
    /// <param name="uploadAuthDate">Upload auth date string.</param>
    /// <param name="deleteAuth">Delete authorization string.</param>
    /// <param name="deleteAuthDate">Delete auth date string.</param>
    /// <returns>Returns a flatbuffer object with upload auth per chunk.</returns>
    public static UploadAuth CreateGcsSignedWriteUploadAuthPerChunk(
      int flatBufferSize,
      string uploadAuth,
      string uploadAuthDate,
      string deleteAuth,
      string deleteAuthDate
    ) {
      var fbb = new FlatBufferBuilder(flatBufferSize);

      // Populate upload authentication info
      var createAuthorizationOffset = fbb.CreateString(uploadAuth);
      var createDateOffset = fbb.CreateString(uploadAuthDate);
      var createAuthOffset = GCSSignedAuth.CreateGCSSignedAuth(
        fbb,
        createAuthorizationOffset,
        createDateOffset
      );

      // Populate delete authentication info
      var deleteAuthorizationOffset = fbb.CreateString(deleteAuth);
      var deleteDateOffset = fbb.CreateString(deleteAuthDate);
      var deleteAuthOffset =  GCSSignedAuth.CreateGCSSignedAuth(
        fbb,
        deleteAuthorizationOffset,
        deleteDateOffset
      );
      var gcsSignedAuthOffset = GCSSignedWriteURL.CreateGCSSignedWriteURL(
        fbb,
        createAuthOffset,
        deleteAuthOffset
      );

      // Populate upload auth info by combining the previous data
      var offset = UploadAuth.CreateUploadAuth(
        fbb,
        ServiceAuths.GCSSignedWriteURL,
        gcsSignedAuthOffset.Value
      );

      fbb.Finish(offset.Value);

      return UploadAuth.GetRootAsUploadAuth(fbb.DataBuffer);
    }

    /// <summary>
    /// Check and strip file path slashes. Note that these are not OS dependent, strings are usually
    /// from server settings.
    /// </summary>
    /// <param name="filePath">File path string, typically from server setting</param>
    /// <returns>Sanitized file path string.</returns>
    public static string StripFilePathSlash(string filePath) {
      if (filePath.StartsWith(FORWARD_SLASH) || filePath.StartsWith(BACKWARD_SLASH)) {
        filePath = filePath.Substring(1);
      }

      return filePath;
    }
  }
}
