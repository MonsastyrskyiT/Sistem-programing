using System.Diagnostics;

namespace ParalerProgram;

internal static class FileSearchService
{
    public static SearchResult SearchSequential(string rootPath, string extension)
    {
        var state = new SearchState(extension);
        var stopwatch = Stopwatch.StartNew();
        long count = CountDirectorySequential(new DirectoryInfo(rootPath), state);
        stopwatch.Stop();
        return new SearchResult(count, stopwatch.Elapsed, state.AccessErrors);
    }

    public static SearchResult SearchParallel(string rootPath, string extension)
    {
        var state = new SearchState(extension);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
        };

        var stopwatch = Stopwatch.StartNew();
        long count = CountDirectoryParallel(new DirectoryInfo(rootPath), state, options);
        stopwatch.Stop();
        return new SearchResult(count, stopwatch.Elapsed, state.AccessErrors);
    }

    private static long CountDirectorySequential(DirectoryInfo directory, SearchState state)
    {
        long count = CountFiles(directory, state);

        foreach (DirectoryInfo child in GetDirectories(directory, state))
        {
            if (!IsReparsePoint(child))
                count += CountDirectorySequential(child, state);
        }

        return count;
    }

    private static long CountDirectoryParallel(
        DirectoryInfo directory,
        SearchState state,
        ParallelOptions options)
    {
        long count = CountFiles(directory, state);
        DirectoryInfo[] children = GetDirectories(directory, state);

        Parallel.ForEach(children, options, child =>
        {
            if (IsReparsePoint(child)) return;

            long childCount = CountDirectoryParallel(child, state, options);
            Interlocked.Add(ref count, childCount);
        });

        return count;
    }

    private static long CountFiles(DirectoryInfo directory, SearchState state)
    {
        try
        {
            long count = 0;
            foreach (FileInfo file in directory.EnumerateFiles())
            {
                if (string.Equals(file.Extension, state.Extension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }
        catch (Exception exception) when (IsFileSystemAccessError(exception))
        {
            state.ReportAccessError();
            return 0;
        }
    }

    private static DirectoryInfo[] GetDirectories(DirectoryInfo directory, SearchState state)
    {
        try
        {
            return directory.GetDirectories();
        }
        catch (Exception exception) when (IsFileSystemAccessError(exception))
        {
            state.ReportAccessError();
            return Array.Empty<DirectoryInfo>();
        }
    }

    private static bool IsReparsePoint(DirectoryInfo directory)
    {
        try
        {
            return directory.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception exception) when (IsFileSystemAccessError(exception))
        {
            return true;
        }
    }

    private static bool IsFileSystemAccessError(Exception exception) =>
        exception is UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException;

    private sealed class SearchState(string extension)
    {
        private int _accessErrors;

        public string Extension { get; } = extension;
        public int AccessErrors => Volatile.Read(ref _accessErrors);
        public void ReportAccessError() => Interlocked.Increment(ref _accessErrors);
    }
}

internal readonly record struct SearchResult(
    long FileCount,
    TimeSpan Elapsed,
    int AccessErrors);
