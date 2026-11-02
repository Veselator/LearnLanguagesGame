using System.Collections.Generic;
using LearnLanguages.Gameplay.Commands;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Plans the turn of a computer player: one minimax decision after another on a simulated position,
    /// until the AI decides to end the turn.
    /// </summary>
    public sealed class AIPlanner
    {
        private readonly AIActionGenerator _generator = new AIActionGenerator();

        public List<IMapCommand> Plan(AIContext context, AIProfile profile, IAIDecisionObserver observer)
        {
            var world = new AIWorld(context);
            var state = new AISimulation(context, world);
            var evaluator = new AIEvaluator(profile.NormalizedPriorities);
            var minimax = new AIMinimax(profile, evaluator, _generator, context.Random, observer);

            var commands = new List<IMapCommand>();
            for (var i = 0; i < profile.MaxActionsPerTurn; i++)
            {
                var action = minimax.Decide(state);
                var command = action.ToCommand(context.Player.Id);
                if (command == null) break;
                commands.Add(command);
                action.Apply(state);
            }
            return commands;
        }
    }
}
