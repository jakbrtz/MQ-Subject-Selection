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
        static void AnalyzeAll(Plan plan)
        {
            Queue<Decision> toAnalyze = new Queue<Decision>();
            foreach (Subject subject in plan.SelectedSubjects)
            {
                toAnalyze.Enqueue(subject.Prerequisites);
                toAnalyze.Enqueue(subject.Corequisites);
            }
            foreach (Subject course in plan.SelectedCourses)
            {
                toAnalyze.Enqueue(course.Prerequisites);
                toAnalyze.Enqueue(course.Corequisites);
            }
            plan.ClearDecisions();
            AnalyzeDecisions(toAnalyze, plan);
        }

        public static void AddSubject(Subject subject, Plan plan)
        {
            //Add the subject to the list
            plan.AddSubjects(new[] { subject });

            AnalyzeAll(plan);
            return;

            // Create an empty queue of things to consider
            Queue<Decision> toAnalyze = new Queue<Decision>();
            // Consider the new subject's prerequisites and corequisites
            toAnalyze.Enqueue(subject.Prerequisites);
            toAnalyze.Enqueue(subject.Corequisites);
            // Reconsider all existing decisions
            foreach (Decision decision in plan.Decisions.Except(toAnalyze))
                toAnalyze.Enqueue(decision);
            // Analyze every decision in toAnalyze
            AnalyzeDecisions(toAnalyze, plan);
        }

        public static void MoveSubject(Subject subject, Plan plan, int time)
        {
            //Record whether the subject is being pushed backwards or forwards
            int originalTime = plan.SubjectsInOrder.FindIndex(semester => semester.Contains(subject));
            //Move the subject to the relevant time slot
            plan.ForceTime(subject, time);
            //Analyze all decisions that might've changed due to the move
            Queue<Decision> toAnalyze = new Queue<Decision>(plan.Decisions);
            foreach (Subject sub in plan.SelectedSubjects.Where(sub => sub.Prerequisites.GetSubjects().Contains(subject)))
                toAnalyze.Enqueue(sub.Prerequisites);
            foreach (Subject sub in plan.SelectedSubjects.Where(sub => sub.Corequisites.GetSubjects().Contains(subject)))
                toAnalyze.Enqueue(sub.Corequisites);
            toAnalyze.Enqueue(subject.Prerequisites);
            toAnalyze.Enqueue(subject.Corequisites);
            AnalyzeDecisions(toAnalyze, plan);
        }

        static void AnalyzeDecisions(Queue<Decision> toAnalyze, Plan plan)
        {
            Stopwatch timer1 = new Stopwatch();
            Stopwatch timer2 = new Stopwatch();
            timer1.Start();
            
            //Iterate over the queue
            while (toAnalyze.Any())
            {
                //Consider the next decision in the queue
                Decision decision = toAnalyze.Dequeue();

                //Remove this decision from the list of decisions (this will probably be added at the end of the loop)
                plan.RemoveDecision(decision);

                // GetRemainingDecision is computationally expensive, so I'm repeating this loop before and after that method
                if (decision.MustPickAll() && decision.GetOptions().All(option => option is Decision))
                {
                    //If everything must be selected, select everything. Add the new decisions to the list
                    foreach (Option option in decision.GetOptions())
                        toAnalyze.Enqueue(option as Decision);
                    continue;
                }

                //Replace the decision with only the part that still needs to be decided on
                decision = decision.GetRemainingDecision(plan);

                //Remove all reasons that have been met
                decision.GetReasonsPrerequisite().RemoveAll(reason => reason.Prerequisites.HasBeenCompleted(plan, reason.GetChosenTime(plan)));
                decision.GetReasonsCorequisite().RemoveAll(reason => reason.Corequisites.HasBeenCompleted(plan, reason.GetChosenTime(plan)));
                //If there are no more reasons to make a decision, don't analyze the decision
                if (!(decision.GetReasonsPrerequisite().Any() || decision.GetReasonsCorequisite().Any()))
                    continue;

                if (decision.MustPickAll())
                {
                    // Add all subjects from this decision
                    IEnumerable<Subject> subjects = decision.GetOptions().Where(option => option is Subject).Cast<Subject>();
                    plan.AddSubjects(subjects);
                    // Add each subject's prerequisites and corequisites to toAnalzye
                    foreach (Subject subject in subjects)
                    {
                        toAnalyze.Enqueue(subject.Prerequisites);
                        toAnalyze.Enqueue(subject.Corequisites);
                    }
                    // Add all other decisions from this decision
                    foreach (Option option in decision.GetOptions().Where(option => option is Decision))
                        toAnalyze.Enqueue(option as Decision);
                    // Reconsider all existing decisions
                    foreach (Decision redoDecision in plan.Decisions.Except(toAnalyze))
                        toAnalyze.Enqueue(redoDecision);
                    //  Skip to the next iteration
                    continue;
                }

                //If the code reached here, then the program cannot determine what to do, so the human decides
                plan.AddDecision(decision);
                
            }

            timer1.Stop();
            timer2.Start();

            foreach (Decision decision in new List<Decision>(plan.Decisions))
                if (decision.CoveredBy(plan))
                    plan.RemoveDecision(decision);

            //Sort decisions by the complexity of the decision
            plan.Decisions.Sort(delegate (Decision p1, Decision p2)
            {
                int compare = 0;
                if (compare == 0) compare = p2.GetSubjects()[0].GetLevel() - p1.GetSubjects()[0].GetLevel();
                if (compare == 0) compare = p1.GetOptions().Count          - p2.GetOptions().Count;
                if (compare == 0) compare = p1.GetSubjects().Count         - p2.GetSubjects().Count;
                if (compare == 0) compare = p1.GetPick()                   - p2.GetPick();
                if (compare == 0) compare = p1.RequiredCompletionTime(plan) - p2.RequiredCompletionTime(plan);
                if (compare == 0) compare = p1.ToString().CompareTo(p2.ToString());
                return compare;
            });

            timer2.Stop();
            Console.WriteLine("Making decisions:    " + timer1.ElapsedMilliseconds + "ms");
            Console.WriteLine("Removing repetition: " + timer2.ElapsedMilliseconds + "ms");
        }

        public static bool CoveredBy(this Decision decision, Plan plan)
        {
            //Make sure that the main decision isn't in the list of decisions
            List<Decision> decisions = new List<Decision>(plan.Decisions);
            decisions.Remove(decision);

            //Remove all decisions that don't have any overlap with the main decision 
            //decisions = decisions.Where(other => other.GetSubjects().Intersect(decision.GetSubjects()).Any()).ToList();
            //if (decisions.Count == 0)
            //    return false;

            //Check if the sum of the decisions' pick is more than the main decision's pick
            //if (decisions.Sum(other => other.GetPick()) < decision.GetPick())
            //    return false;

            //Check if any of the decisions are an obvious subset of the main decision
            bool subsetFound = false;
            foreach (Decision other in decisions.Where(other => Covers(other, decision)))
            {
                other.AddReasons(decision);
                subsetFound = true;
            }
            if (subsetFound)
                return true;

            //TODO: other heuristic checks

            // TODO: account for when electives aren't detected because they are missing like one subject in one of the lists

            return false;

            bool Covers(Decision cover, Decision maybeRedundant)
            {
                // This isn't a thorough check, because otherwise it would be possible for simple decisions to be CoveredBy very complicated decisions
                // Also, I do not want to think about NCCWs would interact with this function

                // A quick check to speed up the time
                if (cover.GetPick() >= maybeRedundant.GetPick())
                    // Use the pigeonhole principle to compare the `pick` from both decisions
                    if (cover.GetPick() - cover.GetOptions().Except(maybeRedundant.GetOptions()).Count() >= maybeRedundant.GetPick())
                        return true;

                // If maybeRedundant is made of other decisions, recursively check if the smaller decision covers the larger decision's options
                if (!maybeRedundant.IsElective() && maybeRedundant.GetOptions().Count(option => option is Decision && Covers(cover, option as Decision)) >= maybeRedundant.GetPick())
                    return true;

                return false;
            }
        }
    }
}
