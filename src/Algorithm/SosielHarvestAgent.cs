/// Name: NewDecisionOptionModel.cs
/// Description: 
/// Authors: Multiple.
/// Copyright: Garry Sotnik, Brooke A. Cassell, Robert M. Scheller.

using System.Collections.Generic;
using System.Linq;
using Landis.Extension.SOSIELHarvest.Configuration;
using SOSIEL.Entities;
using SOSIEL.Helpers;

namespace Landis.Extension.SOSIELHarvest.Algorithm
{
    /// <summary>
    /// SOSIEL Harvest agent model.
    /// </summary>
    /// <seealso cref="SOSIEL.Entities.Agent" />
    public sealed class SosielHarvestAgent : Agent
    {
        public AgentStateConfiguration AgentStateConfiguration { get; private set; }

        public override Agent Clone()
        {
            SosielHarvestAgent agent = (SosielHarvestAgent)base.Clone();

            return agent;
        }

        public override Agent CreateChild(string gender, string name)
        {
            SosielHarvestAgent child = (SosielHarvestAgent)base.CreateChild(gender, name);

            return child;
        }

        protected override Agent CreateInstance()
        {
            return new SosielHarvestAgent();
        }

        public void GenerateCustomParams()
        {

        }

        /// <summary>
        /// Creates agent instance based on agent archetype and agent configuration.
        /// </summary>
        /// <param name="agentConfiguration"></param>
        /// <param name="archetype"></param>
        /// <returns></returns>
        public static SosielHarvestAgent CreateAgent(AgentStateConfiguration agentConfiguration, AgentArchetype archetype, string name)
        {
            SosielHarvestAgent agent = new SosielHarvestAgent();

            agent.Id = name;
            agent.Archetype = archetype;
            agent.privateVariables = new Dictionary<string, dynamic>(agentConfiguration.PrivateVariables);

            agent.AssignedDecisionOptions = archetype.DecisionOptions.Where(r => agentConfiguration.AssignedDecisionOptions.Contains(r.Id)).ToList();
            agent.AssignedGoals = archetype.Goals.Where(g => agentConfiguration.AssignedGoals.Contains(g.Name)).ToList();

            agent.AssignedDecisionOptions.ForEach(kh => agent.DecisionOptionActivationFreshness.Add(kh, 1));

            // Generates goal importance.
            agentConfiguration.GoalsState.ForEach(kvp =>
            {
                var goalName = kvp.Key;
                var configuration = kvp.Value;

                var goal = agent.AssignedGoals.FirstOrDefault(g => g.Name == goalName);
                if (goal == null) return;

                double importance = configuration.Importance;

                GoalState goalState = new GoalState(agent, configuration.Value, configuration.FocalValue, importance, configuration.MinValue, configuration.MaxValue, configuration.MinValueReference, configuration.MaxValueReference);

                agent.InitialGoalStates.Add(goal, goalState);
            });

            // Initializes initial anticipated influence for each kh and goal assigned to the agent
            agent.AssignedDecisionOptions.ForEach(kh =>
            {
                Dictionary<string, double> source;

                if (kh.AutoGenerated && agent.Archetype.DoNothingAnticipatedInfluence != null)
                {
                    source = agent.Archetype.DoNothingAnticipatedInfluence;
                }
                else
                {
                    agentConfiguration.AnticipatedInfluenceState.TryGetValue(kh.Id, out source);
                }


                Dictionary<Goal, double> inner = new Dictionary<Goal, double>();

                agent.AssignedGoals.ForEach(g =>
                {
                    inner.Add(g, source != null && source.ContainsKey(g.Name) ? source[g.Name] : 0);
                });

                agent.AnticipationInfluence.Add(kh, inner);
            });


            InitializeDynamicVariables(agent);

            agent.AgentStateConfiguration = agentConfiguration;

            return agent;
        }

        private static void InitializeDynamicVariables(SosielHarvestAgent agent)
        {
            agent[AlgorithmVariables.IsActive] = true;
        }
    }
}
