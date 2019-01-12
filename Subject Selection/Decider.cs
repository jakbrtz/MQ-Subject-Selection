using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    public static class Decider
    {
        public static void AddSubject(Subject subject, Plan plan, Queue<Prerequisit> toAnalyze = null)
        {
            //This function can get called from the Form or from AnalyzeDecision. The bool variable keeps track of what called it
            bool alreadyAnalyzing = toAnalyze != null;
            //Originally, toAnalyze only contained the decisions whose outcomes may've changed by adding the new subject
            //However, there were so many reasons the outcome could change, that it was not worthwhile testing every reason
            //Instead, the algorithm just analyzes every decision
            if (!alreadyAnalyzing)
                toAnalyze = new Queue<Prerequisit>();
            foreach (Prerequisit decision in plan.Decisions)
                toAnalyze.Enqueue(decision);
            //Add the subject to the list
            plan.AddSubject(subject);
            //Consider the decision's prerequisits
            toAnalyze.Enqueue(subject.Prerequisits);
            //if AnalyzeDecision is not already running, start it
            if (!alreadyAnalyzing)
                AnalyzeDecision(toAnalyze, plan);
        }

        static void AnalyzeDecision(Queue<Prerequisit> toAnalyze, Plan plan)
        {
            //Iterate over the queue
            while (toAnalyze.Any())
            {
                Prerequisit decision = toAnalyze.Dequeue();

                plan.Decisions.Remove(decision); //TODO: better method than removing and adding options

                decision = decision.GetRemainingDecision(plan);

                if (decision.HasBeenMet(plan, decision.RequiredCompletionTime(plan)))
                {
                    //Ignore decisions that don't need to be picked
                }
                else if (decision.HasBeenBanned(plan))
                {
                    throw new Exception("//TODO: A banned decision should not be analyzed in the first place");
                }
                else if (decision.MustPickAllRemaining(plan))
                {
                    //if the decision needs to have everything selected, select everything. Add their prerequisits to the list
                    foreach (Criteria option in decision.GetRemainingOptions(plan))
                        if (option is Subject)
                            AddSubject(option as Subject, plan, toAnalyze);
                        else if (option is Prerequisit)
                            toAnalyze.Enqueue(option as Prerequisit);
                }
                else
                {
                    //This means that there is more than one option to choose from, so the user must make that choice
                    plan.Decisions.Add(decision);
                }
            }

            //Sort decisions by the complexity of the decision (this only affects covercheck)
            plan.Decisions.Sort(delegate (Prerequisit p1, Prerequisit p2)
            {
                int compare = 0;
                if (compare == 0) compare = p1.GetRemainingOptions(plan).Count          - p2.GetRemainingOptions(plan).Count;
                if (compare == 0) compare = p1.GetRemainingSubjects(plan).Count         - p2.GetRemainingSubjects(plan).Count;
                if (compare == 0) compare = p1.GetRemainingPick(plan)                   - p2.GetRemainingPick(plan);
                if (compare == 0) compare = p1.GetRemainingSubjects(plan)[0].GetLevel() - p2.GetRemainingSubjects(plan)[0].GetLevel();
                if (compare == 0) compare = p1.ToString().CompareTo(p2.ToString());
                return compare;
            });

            
            List<Prerequisit> helpIterater = new List<Prerequisit>(plan.Decisions);
            plan.Decisions.Clear();
            foreach (Prerequisit prerequisit in helpIterater)
                if (!prerequisit.IsCovered(plan))
                    plan.Decisions.Add(prerequisit);
                    
        }
    }
}
