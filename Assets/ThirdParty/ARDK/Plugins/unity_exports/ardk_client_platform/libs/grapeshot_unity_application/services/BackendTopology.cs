using System;
using Grapeshot;

namespace ARDK.Grapeshot {
  /// <summary>
  /// Before uploading, the user must create an upload topology. This topology will create the
  /// upload workers needed to do the upload, this needs to only be created once.
  /// </summary>
  public interface IBackendTopology : IDisposable {
    /// <summary>
    /// The stage to perform upload sessions on. A topology is mapped to one stage.
    /// </summary>
    Guid Stage { get; }
  }

  public class BackendTopology : IBackendTopology {
    private Topology _backendTopology;
    private Guid _stage;

    public BackendTopology() {
      if (_backendTopology == null) {
        _stage = Guid.NewGuid();
        _backendTopology = GrapeshotBackendTopology.createDefaultGCSTopology(_stage);
      }
    }

    public Guid Stage {
      get {
        return _stage;
      }
    }

    public void Dispose() {
      var backendTopology = _backendTopology;
      if (backendTopology != null) {
        _backendTopology = null;
        backendTopology.Dispose();
      }
    }
  }
}
