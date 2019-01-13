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
            //Add the subject to the list
            plan.AddSubject(subject);
            //Check whether this was called by the user or by AnalyzeDecisions
            if (toAnalyze == null)
            {
                //Restart the Decisions list
                plan.Decisions.Clear();
                toAnalyze = new Queue<Prerequisit>(plan.SelectedSubjects.ConvertAll(sub => sub.Prerequisits));
                //Start analyzing
                AnalyzeDecision(toAnalyze, plan);
            }
            else
            {
                //Consider the new subject's prerequisits
                toAnalyze.Enqueue(subject.Prerequisits);
                //Reconsider all existing decisions
                foreach (Prerequisit decision in plan.Decisions)
                    toAnalyze.Enqueue(decision);
            }
        }

        public static void MoveSubject(Subject subject, Plan plan, int time)
        {
            //Record whether the subject is being pushed backwards or forwards
            int originalTime = plan.SubjectsInOrder.FindIndex(semester => semester.Contains(subject));
            //Move the subject to the relevant time slot
            plan.ForceTime(subject, time);
            //Analyze all decisions that might've changed due to the move
            Queue<Prerequisit> toAnalyze = new Queue<Prerequisit>(plan.Decisions);
            foreach (Subject sub in plan.SelectedSubjects.Where(sub => sub.Prerequisits.GetSubjects().Contains(subject)))
                toAnalyze.Enqueue(sub.Prerequisits);
            AnalyzeDecision(toAnalyze, plan);
        }

        static void AnalyzeDecision(Queue<Prerequisit> toAnalyze, Plan plan)
        {
            DateTime start = DateTime.Now;
            
            //Iterate over the queue
            while (toAnalyze.Any())
            {
                //Consider the next decision in the queue
                Prerequisit decision = toAnalyze.Dequeue();

                //Remove this decision from the list of decisions (this will probably be added at the end of the loop)
                plan.Decisions.Remove(decision); //TODO: better method than removing and adding options

                //Remove all reasons that have been met
                decision.GetReasons().RemoveAll(reason => reason.HasBeenMet(plan, reason.GetChosenTime(plan)));
                //If there are no more reasons to make a decision, don't analyze the decision
                if (!decision.GetReasons().Any())
                    continue;

                //Replace the decision with only the part that still needs to be decided on
                decision = decision.GetRemainingDecision(plan);

                if (decision.MustPickAllRemaining(plan))
                {
                    //If everything must be selected, select everything. Add the new prerequisits to the list
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

            DateTime middle = DateTime.Now;

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

            DateTime end = DateTime.Now;
            Console.WriteLine("Making decisions:    " + (middle-start).Milliseconds + "ms");
            Console.WriteLine("Removing repetition: " + (end-middle).Milliseconds + "ms");
        }
    }
}
