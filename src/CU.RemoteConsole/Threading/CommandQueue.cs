using System.Collections.Concurrent;
using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Threading;

public sealed class CommandQueue
{
    private readonly ConcurrentQueue<CommandRequest> queue = new ConcurrentQueue<CommandRequest>();
    private readonly int maxDepth;

    public CommandQueue(int maxDepth)
    {
        this.maxDepth = maxDepth;
    }

    public int Count => queue.Count;

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
        return queue.TryDequeue(out request);
    }
}
