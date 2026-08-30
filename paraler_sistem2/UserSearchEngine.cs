using System.Diagnostics;

namespace ParalerSistem2;

internal static class UserSearchEngine
{
    public static SearchComparison Search(IReadOnlyList<UserRecord> users, string keyword)
    {
        _ = SearchSequentialCore(users, keyword);
        _ = SearchParallelCore(users, keyword);

        var stopwatch = Stopwatch.StartNew();
        List<UserRecord> sequentialUsers = SearchSequentialCore(users, keyword);
        stopwatch.Stop();
        TimeSpan sequentialTime = stopwatch.Elapsed;

        stopwatch.Restart();
        List<UserRecord> parallelUsers = SearchParallelCore(users, keyword);
        stopwatch.Stop();

        return new SearchComparison(
            sequentialUsers,
            sequentialTime,
            parallelUsers,
            stopwatch.Elapsed);
    }

    private static List<UserRecord> SearchSequentialCore(
        IEnumerable<UserRecord> users,
        string keyword) =>
        users
            .Where(user => Matches(user, keyword))
            .ToList();

    private static List<UserRecord> SearchParallelCore(
        IEnumerable<UserRecord> users,
        string keyword) =>
        users
            .AsParallel()
            .AsOrdered()
            .Where(user => Matches(user, keyword))
            .ToList();

    private static bool Matches(UserRecord user, string keyword) =>
        user.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
        user.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}

internal readonly record struct SearchComparison(
    List<UserRecord> SequentialUsers,
    TimeSpan SequentialTime,
    List<UserRecord> ParallelUsers,
    TimeSpan ParallelTime);
