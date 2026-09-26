using System.Runtime.InteropServices;

namespace LocalHttpServer;

internal static class ComputerInformation
{
    public static object GetCurrent()
    {
        GCMemoryInfo memory = GC.GetGCMemoryInfo();

        return new
        {
            machineName = Environment.MachineName,
            userName = Environment.UserName,
            operatingSystem = RuntimeInformation.OSDescription,
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            processorCount = Environment.ProcessorCount,
            runtime = RuntimeInformation.FrameworkDescription,
            systemDirectory = Environment.SystemDirectory,
            totalAvailableMemoryBytes = memory.TotalAvailableMemoryBytes,
            currentProcessMemoryBytes = Environment.WorkingSet,
            serverTime = DateTimeOffset.Now
        };
    }
}
