namespace TimeWarp.Jaribu;

#region Purpose
// Picks the implied claim for a test that has no [Proves] attribute.
#endregion

#region Design
// The implied claim is the most specific production method the test called
// directly. Direct calls sit at the shallowest recorded depth. Accessors are
// skipped when any other method was entered. Dispose and a reset are not the
// claim when another direct call remains. A reset is Reset, Clear…, or any
// member that runs both before and after the other direct calls. Among what
// remains, the call with the smallest set of callees is the most specific.
// An earlier call wins a tie, so a trailing getter-sized duplicate does not
// replace the call that started the work.
#endregion

/// <summary>Chooses the production token prove mode treats as the implied claim.</summary>
internal static class TautologyInference
{
  /// <summary>
  /// Returns the token of the most specific direct production call, or null when
  /// <paramref name="production"/> has no candidate.
  /// </summary>
  public static string? InferToken(List<TautologyProbe.Hit> production)
  {
    ArgumentNullException.ThrowIfNull(production);
    List<int> direct = DirectIndexes(production);
    if (direct.Count == 0)
    {
      return null;
    }

    List<int> candidates = ExcludeCleanup(production, direct);
    if (candidates.Count == 0)
    {
      return null;
    }

    int best = candidates[0];
    int bestSize = SubtreeSize(production, best);
    for (int index = 1; index < candidates.Count; index++)
    {
      int candidate = candidates[index];
      int size = SubtreeSize(production, candidate);
      if (size < bestSize)
      {
        best = candidate;
        bestSize = size;
      }
    }

    return production[best].Token;
  }

  private static List<int> DirectIndexes(List<TautologyProbe.Hit> production)
  {
    bool anyMethod = false;
    foreach (TautologyProbe.Hit hit in production)
    {
      if (!IsAccessor(hit.Token))
      {
        anyMethod = true;
        break;
      }
    }

    List<int> direct = [];
    int min = int.MaxValue;
    for (int index = 0; index < production.Count; index++)
    {
      TautologyProbe.Hit hit = production[index];
      if (anyMethod && IsAccessor(hit.Token))
      {
        continue;
      }

      if (hit.Depth < min)
      {
        min = hit.Depth;
        direct.Clear();
        direct.Add(index);
      }
      else if (hit.Depth == min)
      {
        direct.Add(index);
      }
    }

    return direct;
  }

  private static List<int> ExcludeCleanup(List<TautologyProbe.Hit> production, List<int> direct)
  {
    List<int> remaining = DropWhenAnotherRemains(production, direct, IsDisposeName);
    remaining = DropWhenAnotherRemains(production, remaining, IsResetName);
    HashSet<string> resets = SandwichResets(production, remaining);
    if (resets.Count == 0)
    {
      return remaining;
    }

    bool anyOther = false;
    foreach (int index in remaining)
    {
      if (!resets.Contains(production[index].Token))
      {
        anyOther = true;
        break;
      }
    }

    if (!anyOther)
    {
      return remaining;
    }

    List<int> kept = [];
    foreach (int index in remaining)
    {
      if (!resets.Contains(production[index].Token))
      {
        kept.Add(index);
      }
    }

    return kept;
  }

  private static List<int> DropWhenAnotherRemains(
    List<TautologyProbe.Hit> production,
    List<int> candidates,
    Func<string, bool> drop)
  {
    bool anyOther = false;
    foreach (int index in candidates)
    {
      if (!drop(MethodName(production[index].Token)))
      {
        anyOther = true;
        break;
      }
    }

    if (!anyOther)
    {
      return candidates;
    }

    List<int> kept = [];
    foreach (int index in candidates)
    {
      if (!drop(MethodName(production[index].Token)))
      {
        kept.Add(index);
      }
    }

    return kept;
  }

  private static HashSet<string> SandwichResets(List<TautologyProbe.Hit> production, List<int> candidates)
  {
    Dictionary<string, List<int>> positions = [];
    foreach (int index in candidates)
    {
      string token = production[index].Token;
      if (!positions.TryGetValue(token, out List<int>? list))
      {
        list = [];
        positions[token] = list;
      }

      list.Add(index);
    }

    HashSet<string> resets = [];
    foreach (KeyValuePair<string, List<int>> entry in positions)
    {
      if (entry.Value.Count < 2)
      {
        continue;
      }

      int first = entry.Value[0];
      int last = entry.Value[^1];
      foreach (int index in candidates)
      {
        if (index > first && index < last && production[index].Token != entry.Key)
        {
          resets.Add(entry.Key);
          break;
        }
      }
    }

    return resets;
  }

  private static int SubtreeSize(List<TautologyProbe.Hit> production, int index)
  {
    int depth = production[index].Depth;
    int count = 0;
    for (int cursor = index + 1; cursor < production.Count; cursor++)
    {
      if (production[cursor].Depth <= depth)
      {
        break;
      }

      count++;
    }

    return count;
  }

  private static bool IsAccessor(string token)
  {
    string name = MethodName(token);
    return name.StartsWith("get_", StringComparison.Ordinal)
      || name.StartsWith("set_", StringComparison.Ordinal)
      || name.StartsWith("add_", StringComparison.Ordinal)
      || name.StartsWith("remove_", StringComparison.Ordinal);
  }

  private static bool IsDisposeName(string name)
  {
    return name is "Dispose" or "DisposeAsync";
  }

  private static bool IsResetName(string name)
  {
    if (name is "Reset" or "ResetAsync")
    {
      return true;
    }

    if (!name.StartsWith("Clear", StringComparison.Ordinal))
    {
      return false;
    }

    if (name.Length == 5)
    {
      return true;
    }

    char next = name[5];
    return next is >= 'A' and <= 'Z';
  }

  private static string MethodName(string token)
  {
    int paren = token.IndexOf('(', StringComparison.Ordinal);
    string head = paren >= 0 ? token[..paren] : token;
    int dot = head.LastIndexOf('.', StringComparison.Ordinal);
    return dot < 0 ? head : head[(dot + 1)..];
  }
}
