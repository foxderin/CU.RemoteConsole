using System.Collections.Generic;
using System.Linq;
using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Diagnostics;

public sealed class CommandHistory
{
   private readonly object gate = new object();
   private readonly Dictionary<string, CommandRecord> records = new Dictionary<string, CommandRecord>();
   private readonly Queue<string> order = new Queue<string>();
   private readonly int capacity;

   public CommandHistory(int capacity)
   {
       this.capacity = capacity < 1 ? 1 : capacity;
   }

    private void AddInternal(string queueId, CommandRecord record)
    {
        records[queueId] = record;
        order.Enqueue(queueId);
        EvictOverCapacity();
    }

    private void EvictOverCapacity()
    {
        while (order.Count > capacity)
        {
            records.Remove(order.Dequeue());
        }
    }

    public void Add(CommandRequest request)
    {
        lock (gate)
        {
            AddInternal(request.QueueId, new CommandRecord(request));
        }
    }

    public bool TryGet(string queueId, out CommandRecord? record)
    {
        lock (gate)
        {
            return records.TryGetValue(queueId, out record);
        }
    }

   public void Complete(CommandRequest request, CommandExecutionState state, string bridgeStatus, IReadOnlyList<string> output)
   {
       lock (gate)
       {
           if (!records.TryGetValue(request.QueueId, out var record))
           {
                AddInternal(request.QueueId, new CommandRecord(request));
                record = records[request.QueueId];
           }

           record.Complete(state, bridgeStatus, output);
       }
   }

    public IReadOnlyList<CommandRecord> Recent(int count)
    {
        lock (gate)
        {
            return order
                .Reverse()
                .Take(count < 1 ? 1 : count)
                .Select(id => records[id])
                .ToList();
        }
    }
}
