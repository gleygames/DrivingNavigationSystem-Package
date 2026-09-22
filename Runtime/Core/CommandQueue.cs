using System.Collections.Generic;
using Gley.Common;

namespace Gley.NavigationSystem
{
    internal class CommandQueue
    {
        private const int DefaultCapacity = 16;
        private const int DefaultMaxRounds = 3;

        private readonly List<NavigationCommand> pending;
        private readonly List<NavigationCommand> processing;

        public int MaxRounds { get; }
        public int PendingCount { get { return pending.Count; } }

        public CommandQueue()
        {
            pending = new List<NavigationCommand>(DefaultCapacity);
            processing = new List<NavigationCommand>(DefaultCapacity);
            MaxRounds = DefaultMaxRounds;
        }

        public void Enqueue(NavigationCommand command)
        {
            pending.Add(command);
        }

        public void Process(INavigationCommandExecutor executor)
        {
            int rounds = 0;
            while (pending.Count > 0)
            {
                if (rounds >= MaxRounds)
                {
                    CustomLogger.LogError("NavigationManager: API calls made from event handlers kept queuing more calls for more than " + MaxRounds + " rounds in one frame. " + pending.Count + " queued calls were dropped.");
                    pending.Clear();
                    return;
                }
                rounds++;

                processing.Clear();
                for (int i = 0; i < pending.Count; i++)
                {
                    processing.Add(pending[i]);
                }
                pending.Clear();

                for (int i = 0; i < processing.Count; i++)
                {
                    executor.ExecuteNavigationCommand(processing[i]);
                }
                processing.Clear();
            }
        }

        public void Clear()
        {
            pending.Clear();
            processing.Clear();
        }
    }
}
