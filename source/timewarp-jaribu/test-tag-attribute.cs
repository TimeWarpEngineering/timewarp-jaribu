namespace TimeWarp.Jaribu;

#region Purpose
// Tag on a test class or method so a run can filter by category.
#endregion

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
  /// <param name="tag">The tag name, for example <c>Fast</c> or <c>Integration</c>. Null is rejected. Empty and whitespace are kept.</param>
  /// <exception cref="ArgumentNullException"><paramref name="tag"/> is null.</exception>
  public TestTagAttribute(string tag)
  {
    ArgumentNullException.ThrowIfNull(tag);
    Tag = tag;
  }
}
