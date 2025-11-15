#!/usr/bin/dotnet --
#:package TimeWarp.Amuru
#:package TimeWarp.Nuru
#:property NoWarn=CA1303;CA1852;CA2007;CA1031;CA1854;CA1307;CA1812;IL2026;IL3050
#:property GeneratePackageOnBuild=false

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeWarp.Amuru;
using TimeWarp.Nuru;

// ============================================================================
// GitHub Work Thread Analyzer
// Generates a stack-based view of your work across repositories
// ============================================================================

var builder = new NuruAppBuilder();
builder.AddAutoHelp();
builder.AddRoute("report {days:int?}", ReportHandler, "Generate work thread report for the last N days (default: 30)");
var app = builder.Build();
return await app.RunAsync(args).ConfigureAwait(false);

async Task ReportHandler(int? days)
{
    var daysBack = days ?? 30;
    var sinceDate = DateTime.Now.AddDays(-daysBack).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    Console.WriteLine($"📊 Analyzing GitHub activity since {sinceDate}...\n");

// Get username
var userResult = await Shell.Builder("gh")
    .WithArguments("api", "user", "--jq", ".login")
    .CaptureAsync()
    .ConfigureAwait(false);
if (!userResult.Success)
{
    Console.WriteLine("❌ Failed to get GitHub username. Is gh CLI authenticated?");
    return;
}
var username = userResult.Stdout.Trim();

// Fetch all repos with recent activity
Console.WriteLine("🔍 Fetching repositories with recent activity...");

// Get user repos
var userRepoQuery = $@"
query {{
  user(login: ""{username}"") {{
    repositories(first: 100, orderBy: {{field: PUSHED_AT, direction: DESC}}) {{
      nodes {{
        nameWithOwner
        pushedAt
      }}
    }}
  }}
}}
";

var userRepoResult = await Shell.Builder("gh")
    .WithArguments("api", "graphql", "-f", $"query={userRepoQuery}")
    .CaptureAsync()
    .ConfigureAwait(false);

if (!userRepoResult.Success)
{
    Console.WriteLine("❌ Failed to fetch user repositories");
    return;
}

var userRepoData = JsonDocument.Parse(userRepoResult.Stdout);
var userRepos = userRepoData.RootElement
    .GetProperty("data")
    .GetProperty("user")
    .GetProperty("repositories")
    .GetProperty("nodes")
    .EnumerateArray()
    .Where(r => DateTime.Parse(r.GetProperty("pushedAt").GetString()!, CultureInfo.InvariantCulture) >= DateTime.Parse(sinceDate, CultureInfo.InvariantCulture))
    .Select(r => r.GetProperty("nameWithOwner").GetString()!)
    .ToList();

Console.WriteLine($"   • User repos: {userRepos.Count}");

// Get organization repos
var orgRepoQuery = $@"
query {{
  organization(login: ""TimeWarpEngineering"") {{
    repositories(first: 100, orderBy: {{field: PUSHED_AT, direction: DESC}}) {{
      nodes {{
        nameWithOwner
        pushedAt
      }}
    }}
  }}
}}
";

var orgRepoResult = await Shell.Builder("gh")
    .WithArguments("api", "graphql", "-f", $"query={orgRepoQuery}")
    .CaptureAsync()
    .ConfigureAwait(false);

List<string> orgRepos = [];
if (orgRepoResult.Success)
{
    var orgRepoData = JsonDocument.Parse(orgRepoResult.Stdout);
    orgRepos = orgRepoData.RootElement
        .GetProperty("data")
        .GetProperty("organization")
        .GetProperty("repositories")
        .GetProperty("nodes")
        .EnumerateArray()
        .Where(r => DateTime.Parse(r.GetProperty("pushedAt").GetString()!, CultureInfo.InvariantCulture) >= DateTime.Parse(sinceDate, CultureInfo.InvariantCulture))
        .Select(r => r.GetProperty("nameWithOwner").GetString()!)
        .ToList();

    Console.WriteLine($"   • Org repos: {orgRepos.Count}");
}
else
{
    Console.WriteLine("   ⚠️  Could not fetch organization repos");
}

// Combine and deduplicate
var repos = userRepos.Concat(orgRepos).Distinct().ToList();

Console.WriteLine($"📁 Found {repos.Count} total repositories with activity\n");

// Fetch commits for each repo
var allActivities = new List<Activity>();

foreach (var repo in repos)
{
    Console.WriteLine($"  📦 Fetching {repo}...");

    // Get commits - GraphQL doesn't support author filtering, so we'll get all commits and filter client-side
    var commitQuery = $@"
query {{
  repository(owner: ""{repo.Split('/')[0]}"", name: ""{repo.Split('/')[1]}"") {{
    defaultBranchRef {{
      target {{
        ... on Commit {{
          history(first: 100, since: ""{sinceDate}T00:00:00Z"") {{
            nodes {{
              oid
              message
              committedDate
              author {{
                name
                user {{
                  login
                }}
              }}
            }}
          }}
        }}
      }}
    }}
    refs(refPrefix: ""refs/heads/"", first: 50) {{
      nodes {{
        name
        target {{
          ... on Commit {{
            history(first: 50, since: ""{sinceDate}T00:00:00Z"") {{
              totalCount
            }}
          }}
        }}
      }}
    }}
  }}
}}
";

    var commitResult = await Shell.Builder("gh")
        .WithArguments("api", "graphql", "-f", $"query={commitQuery}")
        .CaptureAsync()
        .ConfigureAwait(false);

    if (commitResult.Success)
    {
        try
        {
            var commitData = JsonDocument.Parse(commitResult.Stdout);
            var repoNode = commitData.RootElement.GetProperty("data").GetProperty("repository");

            if (repoNode.TryGetProperty("defaultBranchRef", out var branchRef) &&
                branchRef.ValueKind != JsonValueKind.Null)
            {
                var commits = branchRef
                    .GetProperty("target")
                    .GetProperty("history")
                    .GetProperty("nodes")
                    .EnumerateArray();

                foreach (var commit in commits)
                {
                    // Filter by username
                    var author = commit.GetProperty("author");
                    if (author.TryGetProperty("user", out var user) &&
                        user.ValueKind != JsonValueKind.Null &&
                        user.TryGetProperty("login", out var login) &&
                        login.GetString() == username)
                    {
                        allActivities.Add(new Activity
                        {
                            Type = "Commit",
                            Repo = repo,
                            Date = DateTime.Parse(commit.GetProperty("committedDate").GetString()!, CultureInfo.InvariantCulture),
                            Message = commit.GetProperty("message").GetString()!.Split('\n')[0],
                            Sha = commit.GetProperty("oid").GetString()![..7]
                        });
                    }
                }
            }

            // Get branch info
            if (repoNode.TryGetProperty("refs", out var refs))
            {
                var branches = refs.GetProperty("nodes").EnumerateArray()
                    .Where(b => b.GetProperty("target").GetProperty("history").GetProperty("totalCount").GetInt32() > 0)
                    .Select(b => b.GetProperty("name").GetString()!)
                    .ToList();

                if (branches.Count > 0)
                {
                    allActivities.Add(new Activity
                    {
                        Type = "Branch",
                        Repo = repo,
                        Date = DateTime.Parse(sinceDate, CultureInfo.InvariantCulture),
                        Message = $"Active branches: {string.Join(", ", branches)}",
                        Sha = ""
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ⚠️  Error parsing commits: {ex.Message}");
        }
    }

    // Get PRs
    var prResult = await Shell.Builder("gh")
        .WithArguments("pr", "list", "--repo", repo, "--author", username, "--state", "all", "--json", "number,title,createdAt,closedAt,state", "--limit", "50")
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);

    if (prResult.Success && prResult.Stdout.Trim() != "[]")
    {
        try
        {
            var prs = JsonSerializer.Deserialize(prResult.Stdout, SourceGenerationContext.Default.ListPullRequest);
            if (prs != null)
            {
                foreach (var pr in prs.Where(p => p.CreatedAt >= DateTime.Parse(sinceDate, CultureInfo.InvariantCulture)))
                {
                    allActivities.Add(new Activity
                    {
                        Type = string.Equals(pr.State, "MERGED", StringComparison.Ordinal) || string.Equals(pr.State, "CLOSED", StringComparison.Ordinal) ? "PR Closed" : "PR Created",
                        Repo = repo,
                        Date = pr.CreatedAt,
                        Message = $"#{pr.Number}: {pr.Title}",
                        Sha = pr.Number.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
        }
        catch (JsonException) { }
    }

    // Get issues
    var issueResult = await Shell.Builder("gh")
        .WithArguments("issue", "list", "--repo", repo, "--author", username, "--state", "all", "--json", "number,title,createdAt,closedAt", "--limit", "50")
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);

    if (issueResult.Success && issueResult.Stdout.Trim() != "[]")
    {
        try
        {
            var issues = JsonSerializer.Deserialize(issueResult.Stdout, SourceGenerationContext.Default.ListIssue);
            if (issues != null)
            {
                foreach (var issue in issues.Where(i => i.CreatedAt >= DateTime.Parse(sinceDate, CultureInfo.InvariantCulture)))
                {
                    allActivities.Add(new Activity
                    {
                        Type = "Issue",
                        Repo = repo,
                        Date = issue.CreatedAt,
                        Message = $"#{issue.Number}: {issue.Title}",
                        Sha = issue.Number.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
        }
        catch (JsonException) { }
    }
}

// Filter out repos with only Branch activities (no actual commits/PRs/issues)
allActivities = allActivities
    .Where(a => a.Type != "Branch")
    .OrderBy(a => a.Date)
    .ToList();

Console.WriteLine($"\n✅ Collected {allActivities.Count} activities\n");
Console.WriteLine("=" .PadRight(80, '='));
Console.WriteLine("📝 WORK THREAD REPORT");
Console.WriteLine("=" .PadRight(80, '='));
Console.WriteLine();

// Analyze and present threads
var groupedByRepo = allActivities
    .GroupBy(a => a.Repo)
    .OrderBy(g => g.Min(a => a.Date));

// Build thread visualization
var repoSessions = new List<RepoSession>();
string? currentRepo = null;
var currentActivities = new List<Activity>();

foreach (var activity in allActivities)
{
    if (currentRepo != activity.Repo)
    {
        // Save previous session
        if (currentRepo != null && currentActivities.Count > 0)
        {
            repoSessions.Add(new RepoSession
            {
                Repo = currentRepo,
                StartDate = currentActivities.First().Date,
                EndDate = currentActivities.Last().Date,
                Activities = new List<Activity>(currentActivities)
            });
        }

        // Start new session
        currentRepo = activity.Repo;
        currentActivities.Clear();
    }

    currentActivities.Add(activity);
}

// Add last session
if (currentRepo != null && currentActivities.Count > 0)
{
    repoSessions.Add(new RepoSession
    {
        Repo = currentRepo,
        StartDate = currentActivities.First().Date,
        EndDate = currentActivities.Last().Date,
        Activities = new List<Activity>(currentActivities)
    });
}

// Calculate stack depth
var maxDepth = 0;
var stack = new Stack<RepoSession>();
foreach (var session in repoSessions)
{
    while (stack.Count > 0 && stack.Peek().EndDate < session.StartDate)
    {
        _ = stack.Pop();
    }
    stack.Push(session);
    maxDepth = Math.Max(maxDepth, stack.Count);
}

Console.WriteLine($"📊 Summary:");
Console.WriteLine($"   • Period: {sinceDate} to {DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
Console.WriteLine($"   • Repositories: {repos.Count}");
Console.WriteLine($"   • Activities: {allActivities.Count}");
Console.WriteLine($"   • Max Stack Depth: {maxDepth}");
Console.WriteLine();

// Display threads
Console.WriteLine("🧵 Work Threads (Most Recent First):");
Console.WriteLine();

var repoColors = new Dictionary<string, string>();
var colors = new[] { "🔵", "🟢", "🟡", "🟠", "🔴", "🟣", "🟤", "⚫" };
var colorIndex = 0;

foreach (var session in Enumerable.Reverse(repoSessions))
{
    if (!repoColors.ContainsKey(session.Repo))
    {
        repoColors[session.Repo] = colors[colorIndex % colors.Length];
        colorIndex++;
    }

    var icon = repoColors[session.Repo];
    var duration = (session.EndDate - session.StartDate).TotalDays;
    var durationStr = duration < 1 ? $"{duration * 24:F1}h" : $"{duration:F1}d";

    Console.WriteLine($"{icon} [{session.Repo.Split('/')[1]}] {session.StartDate:MMM dd HH:mm} → {session.EndDate:MMM dd HH:mm} ({durationStr})");

    // Show key activities
    foreach (var activity in session.Activities.Take(5))
    {
        var prefix = activity == session.Activities.Last() ? "  └─" : "  ├─";
        Console.WriteLine($"{prefix} {activity.Type}: {activity.Message}");
    }

    if (session.Activities.Count > 5)
    {
        Console.WriteLine($"  └─ ... and {session.Activities.Count - 5} more activities");
    }

    Console.WriteLine();
}

// Show open threads (repos with recent activity)
Console.WriteLine("🎯 Open Work Threads (likely need attention):");
Console.WriteLine();

var recentThreshold = DateTime.Now.AddDays(-7);
var recentRepos = repoSessions
    .Where(s => s.EndDate >= recentThreshold)
    .GroupBy(s => s.Repo)
    .OrderByDescending(g => g.Max(s => s.EndDate));

foreach (var repoGroup in recentRepos)
{
    var lastActivity = repoGroup.OrderByDescending(s => s.EndDate).First();
    var icon = repoColors[repoGroup.Key];
    Console.WriteLine($"{icon} {repoGroup.Key}");
    Console.WriteLine($"   Last activity: {lastActivity.EndDate:MMM dd HH:mm} ({(DateTime.Now - lastActivity.EndDate).TotalDays:F1}d ago)");
    Console.WriteLine($"   {lastActivity.Activities.Last().Message}");
    Console.WriteLine();
}

// Repository breakdown
Console.WriteLine("📈 Repository Breakdown:");
Console.WriteLine();

foreach (var repoGroup in groupedByRepo)
{
    var icon = repoColors.GetValueOrDefault(repoGroup.Key, "•");
    Console.WriteLine($"{icon} {repoGroup.Key}");
    Console.WriteLine($"   Commits: {repoGroup.Count(a => a.Type == "Commit")}");
    Console.WriteLine($"   PRs: {repoGroup.Count(a => a.Type.Contains("PR"))}");
    Console.WriteLine($"   Issues: {repoGroup.Count(a => a.Type == "Issue")}");
    Console.WriteLine($"   First: {repoGroup.Min(a => a.Date):MMM dd}");
    Console.WriteLine($"   Last: {repoGroup.Max(a => a.Date):MMM dd}");
    Console.WriteLine();
}
}

// ============================================================================
// Data Models
// ============================================================================

record Activity
{
    public required string Type { get; init; }
    public required string Repo { get; init; }
    public required DateTime Date { get; init; }
    public required string Message { get; init; }
    public required string Sha { get; init; }
}

record RepoSession
{
    public required string Repo { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required List<Activity> Activities { get; init; }
}

record PullRequest
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("closedAt")]
    public DateTime? ClosedAt { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "";
}

record Issue
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("closedAt")]
    public DateTime? ClosedAt { get; set; }
}

[JsonSerializable(typeof(List<PullRequest>))]
[JsonSerializable(typeof(List<Issue>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
