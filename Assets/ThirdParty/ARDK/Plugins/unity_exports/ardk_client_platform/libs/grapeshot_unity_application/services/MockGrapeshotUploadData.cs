using System.Collections.Generic;

namespace ARDK.Grapeshot {
  /// <summary>
  /// Mock grapeshot upload data structure
  /// </summary>
  public class MockGrapeshotUploadData : IGrapeshotUploadData {
    public string GcsBucket { get; private set; }
    public int NumberOfChunks { get; private set; }
    public ComposeData ComposeData { get; private set; }
    public List<ChunkData> ChunkData { get; private set; }
  }
}