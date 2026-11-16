namespace TimeWarp.Jaribu;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;

#region Purpose
// Caches hook methods and Jaribu attribute values for each loaded class and method.
#endregion

#region Design
// A loaded test class does not gain hooks or attributes. The first read is kept
// for later runs of that type, including a constructor rejection.
// DefaultHookTimeout is applied when the timeout is read, not when it is cached,
// because a host can change that default between runs.
// Method attributes are keyed by MethodInfo. Inherited methods keep one entry.
// Input row order stays in InputRowOrder, which already caches per method.
// An exception that is not an attribute rejection is not cached.
#endregion

public static partial class TestRunner
{
  private static readonly ConcurrentDictionary<Type, ClassMetadata> ClassMetadataCache = new();
  private static readonly ConcurrentDictionary<MethodInfo, MethodMetadata> MethodMetadataCache = new();

  internal static ClassMetadata GetClassMetadata(Type testClass)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    return ClassMetadataCache.GetOrAdd(testClass, static type => ClassMetadata.Create(type));
  }

  internal static MethodMetadata GetMethodMetadata(MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(method);
    return MethodMetadataCache.GetOrAdd(method, static methodInfo => MethodMetadata.Create(methodInfo));
  }

  private static void ReadAttribute<T>(Func<T?> read, out T? value, out Exception? error)
    where T : class
  {
    try
    {
      value = read();
      error = null;
    }
    catch (Exception exception) when (exception is ArgumentException or CustomAttributeFormatException)
    {
      value = null;
      error = exception;
    }
  }

  private static string[] ReadTags(MemberInfo member, out Exception? error)
  {
    try
    {
      List<string> tags = [];
      foreach (TestTagAttribute tag in member.GetCustomAttributes<TestTagAttribute>(inherit: true))
      {
        tags.Add(tag.Tag);
      }

      error = null;
      return tags.Count == 0 ? [] : [.. tags];
    }
    catch (Exception exception) when (exception is ArgumentException or CustomAttributeFormatException)
    {
      error = exception;
      return [];
    }
  }

  private static Exception? ReadInputAttributes(MethodInfo method)
  {
    try
    {
      _ = InputRowOrder.Ordered(method);
      return null;
    }
    catch (Exception exception) when (exception is ArgumentException or CustomAttributeFormatException)
    {
      return exception;
    }
  }

  private static void Rethrow(Exception? error)
  {
    if (error is not null)
    {
      ExceptionDispatchInfo.Capture(error).Throw();
    }
  }

  /// <summary>
  /// Hooks and class attributes resolved once for <see cref="Type"/>.
  /// </summary>
  internal sealed class ClassMetadata
  {
    private ClassMetadata(
      HookResolveResult setup,
      HookResolveResult cleanUp,
      HookResolveResult setupOnce,
      HookResolveResult cleanUpOnce,
      TimeoutAttribute? timeout,
      Exception? timeoutError,
      string[] tags,
      Exception? tagError)
    {
      Setup = setup;
      CleanUp = cleanUp;
      SetupOnce = setupOnce;
      CleanUpOnce = cleanUpOnce;
      TimeoutValue = timeout;
      TimeoutError = timeoutError;
      TagValues = tags;
      TagError = tagError;
    }

    public HookResolveResult Setup { get; }
    public HookResolveResult CleanUp { get; }
    public HookResolveResult SetupOnce { get; }
    public HookResolveResult CleanUpOnce { get; }

    private TimeoutAttribute? TimeoutValue { get; }
    private Exception? TimeoutError { get; }
    private string[] TagValues { get; }
    private Exception? TagError { get; }

    public TimeoutAttribute? Timeout
    {
      get
      {
        Rethrow(TimeoutError);
        return TimeoutValue;
      }
    }

    public string[] Tags()
    {
      Rethrow(TagError);
      return TagValues;
    }

    public bool TryReject(out JaribuAttributeRead.Rejection rejection)
    {
      if (TimeoutError is not null)
      {
        rejection = new(nameof(TimeoutAttribute), "the class", TimeoutError);
        return true;
      }

      if (TagError is not null)
      {
        rejection = new(nameof(TestTagAttribute), "the class", TagError);
        return true;
      }

      rejection = default;
      return false;
    }

    public static ClassMetadata Create(Type testClass)
    {
      ReadAttribute(
        () => testClass.GetCustomAttribute<TimeoutAttribute>(inherit: true),
        out TimeoutAttribute? timeout,
        out Exception? timeoutError);
      string[] tags = ReadTags(testClass, out Exception? tagError);
      return new ClassMetadata(
        ResolveHook(testClass, "Setup"),
        ResolveHook(testClass, "CleanUp"),
        ResolveHook(testClass, "SetupOnce"),
        ResolveHook(testClass, "CleanUpOnce"),
        timeout,
        timeoutError,
        tags,
        tagError);
    }
  }

  /// <summary>
  /// Skip, tag, input, timeout, claim, and known-tautology values for one method.
  /// </summary>
  internal sealed class MethodMetadata
  {
    private MethodMetadata(
      SkipAttribute? skip,
      Exception? skipError,
      string[] tags,
      Exception? tagError,
      Exception? inputError,
      TimeoutAttribute? timeout,
      Exception? timeoutError,
      ProvesAttribute[] proves,
      Exception? provesError,
      KnownTautologyAttribute? knownTautology,
      Exception? knownTautologyError)
    {
      SkipValue = skip;
      SkipError = skipError;
      TagValues = tags;
      TagError = tagError;
      InputError = inputError;
      TimeoutValue = timeout;
      TimeoutError = timeoutError;
      ProvesValues = proves;
      ProvesError = provesError;
      KnownTautologyValue = knownTautology;
      KnownTautologyError = knownTautologyError;
    }

    private SkipAttribute? SkipValue { get; }
    private Exception? SkipError { get; }
    private string[] TagValues { get; }
    private Exception? TagError { get; }
    private Exception? InputError { get; }
    private TimeoutAttribute? TimeoutValue { get; }
    private Exception? TimeoutError { get; }
    private ProvesAttribute[] ProvesValues { get; }
    private Exception? ProvesError { get; }
    private KnownTautologyAttribute? KnownTautologyValue { get; }
    private Exception? KnownTautologyError { get; }

    public SkipAttribute? Skip
    {
      get
      {
        Rethrow(SkipError);
        return SkipValue;
      }
    }

    public TimeoutAttribute? Timeout
    {
      get
      {
        Rethrow(TimeoutError);
        return TimeoutValue;
      }
    }

    public string[] Tags()
    {
      Rethrow(TagError);
      return TagValues;
    }

    /// <summary>Claims on this method. A constructor rejection throws the cached exception.</summary>
    public ProvesAttribute[] Proves()
    {
      Rethrow(ProvesError);
      return ProvesValues;
    }

    /// <summary>Known-tautology opt-out. A constructor rejection throws the cached exception.</summary>
    public KnownTautologyAttribute? KnownTautology
    {
      get
      {
        Rethrow(KnownTautologyError);
        return KnownTautologyValue;
      }
    }

    public bool TryReject(out JaribuAttributeRead.Rejection rejection)
    {
      if (SkipError is not null)
      {
        rejection = new(nameof(SkipAttribute), "the method", SkipError);
        return true;
      }

      if (TagError is not null)
      {
        rejection = new(nameof(TestTagAttribute), "the method", TagError);
        return true;
      }

      if (InputError is not null)
      {
        rejection = new(nameof(InputAttribute), "the method", InputError);
        return true;
      }

      if (TimeoutError is not null)
      {
        rejection = new(nameof(TimeoutAttribute), "the method", TimeoutError);
        return true;
      }

      if (ProvesError is not null)
      {
        rejection = new(nameof(ProvesAttribute), "the method", ProvesError);
        return true;
      }

      if (KnownTautologyError is not null)
      {
        rejection = new(nameof(KnownTautologyAttribute), "the method", KnownTautologyError);
        return true;
      }

      rejection = default;
      return false;
    }

    public static MethodMetadata Create(MethodInfo method)
    {
      ReadAttribute(
        () => method.GetCustomAttribute<SkipAttribute>(),
        out SkipAttribute? skip,
        out Exception? skipError);
      string[] tags = ReadTags(method, out Exception? tagError);
      Exception? inputError = ReadInputAttributes(method);
      ReadAttribute(
        () => method.GetCustomAttribute<TimeoutAttribute>(inherit: false),
        out TimeoutAttribute? timeout,
        out Exception? timeoutError);
      ProvesAttribute[] proves = ReadProves(method, out Exception? provesError);
      ReadAttribute(
        () => method.GetCustomAttribute<KnownTautologyAttribute>(inherit: false),
        out KnownTautologyAttribute? known,
        out Exception? knownError);
      return new MethodMetadata(
        skip,
        skipError,
        tags,
        tagError,
        inputError,
        timeout,
        timeoutError,
        proves,
        provesError,
        known,
        knownError);
    }
  }

  private static ProvesAttribute[] ReadProves(MethodInfo method, out Exception? error)
  {
    try
    {
      ProvesAttribute[] claims = [.. method.GetCustomAttributes<ProvesAttribute>(inherit: false)];
      error = null;
      return claims;
    }
    catch (Exception exception) when (exception is ArgumentException or CustomAttributeFormatException)
    {
      error = exception;
      return [];
    }
  }
}
