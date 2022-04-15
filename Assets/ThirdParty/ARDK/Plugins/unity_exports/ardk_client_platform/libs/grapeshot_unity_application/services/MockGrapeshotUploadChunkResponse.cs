using ror.schema.upload;

namespace ARDK.Grapeshot {
  public class MockUploadChunkResponse : IGrapeshotUploadChunkResponse {
    public ChunkStatus Status { get; set; }
    public string StatusMessage { get; set; }
  }
}