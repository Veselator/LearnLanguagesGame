using System;
using System.Collections.Generic;
using LearnLanguages.Gameplay.Conquest;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Gameplay.Commands
{
    public interface IMapCommandProcessor
    {
        bool IsBusy { get; }
        event Action<IMapCommand> CommandRejected;
        event Action<IMapCommand> CommandExecuted;
        void Enqueue(IMapCommand command);
        void Clear();
    }

    /// <summary>
    /// Executes map commands one by one. A command may take time (e.g. a battle), the next waits for it.
    /// AI commands are paced so a human can follow them on the map.
    /// </summary>
    public sealed class MapCommandProcessor : IMapCommandProcessor, ITickable
    {
        private const float AiCommandDelay = 0.35f;

        private readonly Queue<IMapCommand> _queue = new Queue<IMapCommand>();
        private readonly MapCommandContext _context;
        private readonly IConquestService _conquest;

        private bool _executing;
        private float _delay;

        public MapCommandProcessor(MapCommandContext context, IConquestService conquest)
        {
            _context = context;
            _conquest = conquest;
        }

        public bool IsBusy => _executing || _queue.Count > 0;
        public event Action<IMapCommand> CommandRejected;
        public event Action<IMapCommand> CommandExecuted;

        public void Enqueue(IMapCommand command) => _queue.Enqueue(command);

        public void Clear() => _queue.Clear();

        public void Tick()
        {
            if (_executing || _queue.Count == 0 || _conquest.IsGameOver) return;
            if (_delay > 0f)
            {
                _delay -= Time.deltaTime;
                return;
            }

            var command = _queue.Dequeue();
            if (!command.CanExecute(_context))
            {
                CommandRejected?.Invoke(command);
                return;
            }

            _executing = true;
            var isAi = !_context.Players.Get(command.PlayerId).IsHuman;
            command.Execute(_context, () =>
            {
                _executing = false;
                if (isAi && !(command is EndTurnCommand)) _delay = AiCommandDelay;
                CommandExecuted?.Invoke(command);
            });
        }
    }
}
