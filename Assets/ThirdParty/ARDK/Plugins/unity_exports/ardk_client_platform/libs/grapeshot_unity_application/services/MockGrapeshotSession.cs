using System;

namespace ARDK.Grapeshot {
  /// <summary>
  /// Mock class of grapeshot session.
  /// </summary>
  public class MockSession : IGrapeshotSession {
    public bool IsFinished() {
      return IsSessionFinished;
    }

    public void Process() {
      throw new NotImplementedException();
    }

    public void Dispose() {
      throw new NotImplementedException();
    }

    private bool IsSessionFinished { get; set; }
  }
}