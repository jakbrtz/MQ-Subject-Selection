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
                other.GetOptions().Exists(criteria => criteria is Prerequisit && prerequisit.IsSubset(criteria as Prerequisit));
        }

        public static bool IsCovered(this Prerequisit prerequisit, List<Prerequisit> decisions, List<Subject> selectedSubjects)
        {
            //Make sure that the main prerequisit isn't in the list of decisions
            decisions = decisions.Where(decision => decision != prerequisit).ToList();

            //Remove all prerequisits that don't have any overlap with the main prerequisit
            //TODO: shorten this line
            decisions = decisions.Where(decision => decision.GetRemainingSubjects(selectedSubjects).Any(subject => prerequisit.GetRemainingSubjects(selectedSubjects).Contains(subject))).ToList();
            if (decisions.Count == 0)
                return false;

            //Check if any of the prerequisists are an obvious subset of the main requisit
            if (decisions.Exists(decision => decision.IsSubset(prerequisit) && decision.GetPick() >= prerequisit.GetPick()))
                return true;
            
            //TODO: other heuristic checks

            return AllOptionsMeetPrerequisit(prerequisit, decisions, selectedSubjects);
        }

        static bool AllOptionsMeetPrerequisit(Prerequisit prerequisit, List<Prerequisit> decisions, List<Subject> selectedSubjects)
        {
            if (prerequisit.HasBeenMet(selectedSubjects))
            {
                return true;
            }
            else if (decisions.Count == 0)
            {
                return false;
            }
            else
            {
                foreach (Subject option in decisions[0].GetRemainingSubjects(selectedSubjects)) //TODO: better method
                {
                    List<Subject> newSelection = new List<Subject>(selectedSubjects);
                    List<Prerequisit> newDecisions = new List<Prerequisit>(decisions);
                    Decider.AddSubject(option, newSelection, decisions);
                    if (!AllOptionsMeetPrerequisit(prerequisit, newDecisions, newSelection))
                        return false;
                }
                return true;
            }
        }
    }
}
