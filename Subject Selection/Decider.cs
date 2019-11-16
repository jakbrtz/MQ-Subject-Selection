using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Subject_Selection
{
    public static class Decider
    {
        public static void AddSubject(Subject subject, Plan plan, Queue<Prerequisite> toAnalyze = null)
        {
            //Add the subject to the list
            plan.AddSubject(subject);
            // Create an empty queue of things to consider
            bool createNewQueue = toAnalyze == null;
            if (createNewQueue)
                toAnalyze = new Queue<Prerequisite>();
            else
                toAnalyze.Clear();
            // Consider the new subject's prerequisites
            toAnalyze.Enqueue(subject.Prerequisites);
            // Reconsider all existing decisions
            foreach (Prerequisite decision in plan.Decisions)
                toAnalyze.Enqueue(decision);
            // If AnalyzeDecision isn't already running, run it
            if (createNewQueue)
                AnalyzeDecision(toAnalyze, plan);
        }

        public static void MoveSubject(Subject subject, Plan plan, int time)
        {
            //Record whether the subject is being pushed backwards or forwards
            int originalTime = plan.SubjectsInOrder.FindIndex(semester => semester.Contains(subject));
            //Move the subject to the relevant time slot
            plan.ForceTime(subject, time);
            //Analyze all decisions that might've changed due to the move
            Queue<Prerequisite> toAnalyze = new Queue<Prerequisite>(plan.Decisions);
            foreach (Subject sub in plan.SelectedSubjects.Where(sub => sub.Prerequisites.GetSubjects().Contains(subject)))
                toAnalyze.Enqueue(sub.Prerequisites);
            toAnalyze.Enqueue(subject.Prerequisites);
            AnalyzeDecision(toAnalyze, plan);
        }

        static void AnalyzeDecision(Queue<Prerequisite> toAnalyze, Plan plan)
        {
            Stopwatch timer1 = new Stopwatch();
            Stopwatch timer2 = new Stopwatch();
            timer1.Start();
            
            //Iterate over the queue
            while (toAnalyze.Any())
            {
                //Consider the next decision in the queue
                Prerequisite decision = toAnalyze.Dequeue();

                //Remove this decision from the list of decisions (this will probably be added at the end of the loop)
                plan.RemoveDecision(decision);

                //Remove all reasons that have been met
                decision.GetReasons().RemoveAll(reason => reason.HasBeenMet(plan, reason.GetChosenTime(plan)));
                //If there are no more reasons to make a decision, don't analyze the decision
                if (!decision.GetReasons().Any())
                    continue;

                //Replace the decision with only the part that still needs to be decided on
                decision = decision.GetRemainingDecision(plan);

                if (decision.MustPickAllRemaining(plan))
                {
                    //If everything must be selected, select everything. Add the new prerequisites to the list
                    foreach (Criteria option in decision.GetOptions())
                        if (option is Subject)
                            AddSubject(option as Subject, plan, toAnalyze);
                        else if (option is Prerequisite)
                            toAnalyze.Enqueue(option as Prerequisite);
                }
                else
                {
                    //This means that there is more than one option to choose from, so the user must make that choice
                    plan.AddDecision(decision);
                }
            }

            timer1.Stop();
            timer2.Start();

            //Sort decisions by the complexity of the decision (this only affects covercheck)
            plan.Decisions.Sort(delegate (Prerequisite p1, Prerequisite p2)
            {
                int compare = 0;
                //if (compare == 0) compare = p1.RequiredCompletionTime(plan)             - p2.RequiredCompletionTime(plan);
                if (compare == 0) compare = p1.GetOptions().Count                       - p2.GetOptions().Count;
                if (compare == 0) compare = p1.GetRemainingSubjects(plan).Count         - p2.GetRemainingSubjects(plan).Count;
                if (compare == 0) compare = p1.GetRemainingPick(plan)                   - p2.GetRemainingPick(plan);
                if (compare == 0) compare = p1.GetRemainingSubjects(plan)[0].GetLevel() - p2.GetRemainingSubjects(plan)[0].GetLevel();
                if (compare == 0) compare = p1.ToString().CompareTo(p2.ToString());
                return compare;
            });
            
            List<Prerequisite> helpIterater = new List<Prerequisite>(plan.Decisions);
            plan.ClearDecisions();
            foreach (Prerequisite prerequisite in helpIterater)
                if (!prerequisite.IsCovered(plan))
                    plan.AddDecision(prerequisite);

            timer2.Stop();
            Console.WriteLine("Making decisions:    " + timer1.ElapsedMilliseconds + "ms");
            Console.WriteLine("Removing repetition: " + timer2.ElapsedMilliseconds + "ms");
        }
    }
}
