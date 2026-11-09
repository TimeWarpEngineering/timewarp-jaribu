namespace TimeWarp.Jaribu;

/// <summary>
/// Tags a test method or class for filtering and categorization.
/// Can be applied multiple times to add multiple tags.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class TestTagAttribute : Attribute
{
  /// <summary>
  /// Gets the tag name.
  /// </summary>
  public string Tag { get; }

  /// <summary>
  /// Creates a tag.
  /// </summary>
  /// <param name="tag">The tag name, for example <c>Fast</c> or <c>Integration</c>. Null, empty, and whitespace are rejected.</param>
  /// <exception cref="ArgumentNullException"><paramref name="tag"/> is null.</exception>
  /// <exception cref="ArgumentException"><paramref name="tag"/> is empty or whitespace.</exception>
  public TestTagAttribute(string tag)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(tag);
    Tag = tag;
  }
}
