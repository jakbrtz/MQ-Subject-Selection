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
        public static bool IsSubset(this Prerequisit prerequisit, Prerequisit other)
        {
            return prerequisit.GetOptions().All(option => other.GetOptions().Contains(option)) || 
                other.GetOptions().Exists(criteria => criteria is Prerequisit && prerequisit.IsSubset(criteria as Prerequisit))
                 && prerequisit.GetPick() >= other.GetPick();
        }

        public static bool IsCovered(this Prerequisit prerequisit, Plan plan)
        {
            //Make sure that the main prerequisit isn't in the list of decisions
            List<Prerequisit> decisions = plan.Decisions.Where(decision => decision != prerequisit).ToList();

            //Remove all prerequisits that don't have any overlap with the main prerequisit 
            //decisions = decisions.Where(decision => decision.GetRemainingSubjects(plan).Any(subject => prerequisit.GetRemainingSubjects(plan).Contains(subject))).ToList();
            //if (decisions.Count == 0)
            //    return false;

            //Check if the sum of the prerequisits' pick is more than the main prerequisit's pick
            if (decisions.Sum(decision => decision.GetRemainingPick(plan)) < prerequisit.GetRemainingPick(plan))
                return false;

            //Check if any of the prerequisists are an obvious subset of the main prequisit
            if (decisions.Exists(decision => decision.IsSubset(prerequisit)))
            {
                foreach (Prerequisit decision in decisions.Where(decision => decision.IsSubset(prerequisit)))
                    decision.AddReasons(prerequisit);
                return true;
            }
                
            

            //TODO: other heuristic checks
            return false;
            return AllOptionsMeetPrerequisit(prerequisit, plan);
        }

        static bool AllOptionsMeetPrerequisit(Prerequisit prerequisit, Plan plan)
        {
            /* TODO: consider this:
             * (1 MATH331-332) (1 MATH235 MATH288 MATH331)
             * The algorithm says the second one is redundant, because it contains a prerequisit of everything from the first option
             */

            if (prerequisit.HasBeenMet(plan, prerequisit.RequiredCompletionTime(plan)))
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
                    if (!AllOptionsMeetPrerequisit(prerequisit, recursivePlan))
                        return false;
                }
                return true;
            }
        }
    }
}
