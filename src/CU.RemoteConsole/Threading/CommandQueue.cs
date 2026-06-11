using System.Collections.Concurrent;
using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Threading;

public sealed class CommandQueue
{
    private readonly ConcurrentQueue<CommandRequest> queue = new ConcurrentQueue<CommandRequest>();
    private int maxDepth;

    public CommandQueue(int maxDepth)
    {
        this.maxDepth = maxDepth;
    }

    public int Count => queue.Count;

    public void UpdateMaxDepth(int maxDepth)
    {
        this.maxDepth = maxDepth < 1 ? 1 : maxDepth;
    }

    public bool TryEnqueue(CommandRequest request)
    {
        if (queue.Count >= maxDepth)
        {
            return false;
        }

        queue.Enqueue(request);
        return true;
    }

    public bool TryDequeue(out CommandRequest request)
    {
        if (queue.TryDequeue(out var item))
        {
            request = item;
            return true;
        }

        request = null!;
        return false;
    }
}
