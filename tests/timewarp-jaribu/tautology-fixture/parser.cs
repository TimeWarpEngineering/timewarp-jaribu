namespace TautologyFixture;

#region Purpose
// Disposable production subject. A trailing using must not become the implied claim.
#endregion

/// <summary>Disposable subject. A trailing using must not become the implied claim.</summary>
public sealed class Parser : IDisposable
{
  /// <summary>True after <see cref="Dispose"/>.</summary>
  public bool Disposed { get; private set; }

  /// <summary>Returns <paramref name="text"/>.</summary>
  public string Parse(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    ObjectDisposedException.ThrowIf(Disposed, this);
    return text;
  }

  /// <summary>Marks the instance disposed.</summary>
  public void Dispose()
  {
    Disposed = true;
  }
}
