using System.Collections.Generic;

namespace ARDK.Grapeshot {
  // Information needed to perform an upload through Grapeshot through signed URL. Authentication
  // is needed per operation, which includes each chunk upload and compose step.
  public interface IGrapeshotUploadData {
    // Google Cloud Storage bucket that the file is uploaded to
    string GcsBucket { get; }
    // Number of chunks to divid file into
    int NumberOfChunks { get; }
    // Info for compose step
    ComposeData ComposeData { get; }
    // Info for each chunk
    List<ChunkData> ChunkData { get; }
  }

  public class GrapeshotUploadData : IGrapeshotUploadData {
    public string GcsBucket { get; private set; }
    public int NumberOfChunks { get; private set; }
    public ComposeData ComposeData { get; private set; }
    public List<ChunkData> ChunkData { get; private set; }

    public GrapeshotUploadData(
      string gcsBucket,
      int numOfChunks,
      ComposeData composeData,
      List<ChunkData> chunkData
    ) {
      GcsBucket = gcsBucket;
      NumberOfChunks = numOfChunks;
      ComposeData = composeData;
      ChunkData = chunkData;
    }
  }

  /// <summary>
  /// Information needed for compose step.
  /// </summary>
  public struct ComposeData {
    // File path where the final, re-stitched file goes to
    public string TargetFilePath { get; private set; }
    // Auth info
    public Authentication Authentication { get; private set; }

    public ComposeData(string targetFilePath, Authentication authentication) : this() {
      TargetFilePath = targetFilePath;
      Authentication = authentication;
    }
  }

  /// <summary>
  /// Information for each chunk upload
  /// </summary>
  public struct ChunkData {
    // File path where each chunk is stored.
    public string FilePath { get; private set; }
    // Auth for upload step
    public Authentication UploadAuthentication { get; private set; }
    // Auth for delete step
    public Authentication DeleteAuthentication { get; private set; }

    public ChunkData(
      string filePath,
      Authentication uploadAuthentication,
      Authentication deleteAuthentication
    ) : this() {
      FilePath = filePath;
      UploadAuthentication = uploadAuthentication;
      DeleteAuthentication = deleteAuthentication;
    }
  }

  /// <summary>
  /// Signed URL authentication is needed per operation, which includes each chunk upload and
  /// compose step.
  /// </summary>
  public struct Authentication {
    // Signed url auth
    public string Authorization { get; private set; }
    // Signed url auth date
    public string Date { get; private set; }

    public Authentication(string authorization, string date) : this() {
      Authorization = authorization;
      Date = date;
    }
  }
}
