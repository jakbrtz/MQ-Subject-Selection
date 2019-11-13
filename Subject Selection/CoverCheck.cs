using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    //I think this file will end up being merged into Decider.cs
    public static class Extentions
    {
        public static bool IsSubset(this Prerequisite prerequisite, Prerequisite other)
        {
            return prerequisite.GetOptions().All(option => other.GetOptions().Contains(option)) || 
                other.GetOptions().Exists(criteria => criteria is Prerequisite && prerequisite.IsSubset(criteria as Prerequisite))
                 && prerequisite.GetPick() >= other.GetPick();
        }

        public static bool IsCovered(this Prerequisite prerequisite, Plan plan)
        {
            //Make sure that the main prerequisite isn't in the list of decisions
            List<Prerequisite> decisions = plan.Decisions.Where(decision => decision != prerequisite).ToList();

            //Remove all prerequisites that don't have any overlap with the main prerequisite 
            //decisions = decisions.Where(decision => decision.GetRemainingSubjects(plan).Any(subject => prerequisite.GetRemainingSubjects(plan).Contains(subject))).ToList();
            //if (decisions.Count == 0)
            //    return false;

            //Check if the sum of the prerequisites' pick is more than the main prerequisite's pick
            if (decisions.Sum(decision => decision.GetRemainingPick(plan)) < prerequisite.GetRemainingPick(plan))
                return false;

            //Check if any of the prerequisists are an obvious subset of the main prequisit
            if (decisions.Exists(decision => decision.IsSubset(prerequisite)))
            {
                foreach (Prerequisite decision in decisions.Where(decision => decision.IsSubset(prerequisite)))
                    decision.AddReasons(prerequisite);
                return true;
            }
                
            

            //TODO: other heuristic checks
            return false;
            return AllOptionsMeetPrerequisite(prerequisite, plan);
        }

        static bool AllOptionsMeetPrerequisite(Prerequisite prerequisite, Plan plan)
        {
            /* TODO: consider this:
             * (1 MATH331-332) (1 MATH235 MATH288 MATH331)
             * The algorithm says the second one is redundant, because it contains a prerequisite of everything from the first option
             */

            if (prerequisite.HasBeenMet(plan, prerequisite.RequiredCompletionTime(plan)))
            {
                return true;
            }
            else if (plan.Decisions.Count == 0)
            {
                return false;
            }
            else
            {
                foreach (Subject option in plan.Decisions[0].GetRemainingSubjects(plan))
                {
                    Plan recursivePlan = new Plan(plan);
                    Decider.AddSubject(option, recursivePlan);
                    if (!AllOptionsMeetPrerequisite(prerequisite, recursivePlan))
                        return false;
                }
                return true;
            }
        }
    }
}
