using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.Core
{
    public sealed class ConsoleObserver : ICommandObserver
    {
        public void OnExecuting(CommandDefinition command)
            => Console.WriteLine($"[SQL] Executing: {command.CommandType} {command.CommandText} (timeout={command.TimeoutSeconds}s)");

        public void OnExecuted(CommandDefinition command, TimeSpan duration)
            => Console.WriteLine($"[SQL] Done in {duration.TotalMilliseconds:n0} ms");

        public void OnError(CommandDefinition command, Exception exception)
            => Console.WriteLine($"[SQL] ERROR: {exception.GetType().Name}: {exception.Message}");
    }
}
