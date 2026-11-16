namespace TautologyFixture;

#region Purpose
// Disposable production subject. A trailing using must not become the implied claim.
// Parse has more callees than Dispose, so the smallest subtree alone would pick Dispose.
#endregion

/// <summary>
/// Disposable subject. A trailing using must not become the implied claim.
/// <see cref="Parse"/> calls more production methods than <see cref="Dispose"/>.
/// </summary>
public sealed class Parser : IDisposable
{
  private bool IsDisposed;

  /// <summary>True after <see cref="Dispose"/>.</summary>
  public bool Disposed => IsDisposed;

  /// <summary>Returns <paramref name="text"/>.</summary>
  public string Parse(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    ObjectDisposedException.ThrowIf(Disposed, this);
    return Copy(text);
  }

  /// <summary>Marks the instance disposed. Writes the field, so this method has no callee.</summary>
  public void Dispose()
  {
    IsDisposed = true;
  }

  /// <summary>Returns <paramref name="text"/>. A callee, so <see cref="Parse"/> is a larger subtree than <see cref="Dispose"/>.</summary>
  private static string Copy(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    return text;
  }
}
