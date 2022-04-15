using System;

namespace ARDK.Grapeshot {
  public class MockBackendTopology : IBackendTopology {
    public void Dispose() {
      throw new NotImplementedException();
    }

    public Guid Stage { get; private set; }
  }
}