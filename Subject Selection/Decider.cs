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
            foreach (Course course in plan.SelectedCourses)
            {
                toAnalyze.Enqueue(course.Prerequisites);
                toAnalyze.Enqueue(course.Corequisites);
            }
            plan.ClearDecisions();
            AnalyzeDecisions(toAnalyze, plan);
        }

        public static void AddContent(Content content, Plan plan)
        {
            plan.AddContents(new[] { content });
            AnalyzeAll(plan);
        }

        public static void MoveSubject(Subject subject, Plan plan, int time)
        {
            plan.ForceTime(subject, time);
            AnalyzeAll(plan);
        }

        public static void RemoveContent(Content content, Plan plan)
        {
            plan.RemoveContent(content);
            AnalyzeAll(plan);
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
                if (decision.Pick == decision.Options.Count && decision.Options.All(option => option is Decision))
                {
                    //If everything must be selected, select everything. Add the new decisions to the list
                    foreach (Option option in decision.Options)
                        toAnalyze.Enqueue(option as Decision);
                    continue;
                }

                //Replace the decision with only the part that still needs to be decided on
                decision = decision.GetRemainingDecision(plan);

                //Remove all reasons that have been met
                decision.GetReasonsPrerequisite().RemoveAll(reason => reason.Prerequisites.HasBeenCompleted(plan, reason is Subject subject ? subject.GetChosenTime(plan) : 100));
                decision.GetReasonsCorequisite().RemoveAll(reason => reason.Corequisites.HasBeenCompleted(plan, reason is Subject subject ? subject.GetChosenTime(plan) : 100));
                //If there are no more reasons to make a decision, don't analyze the decision
                if (!(decision.GetReasonsPrerequisite().Any() || decision.GetReasonsCorequisite().Any()))
                    continue;

                if (decision.Pick == decision.Options.Count)
                {
                    // Add all contents from this decision
                    IEnumerable<Content> contents = decision.Options.Where(option => option is Content).Cast<Content>();
                    plan.AddContents(contents);
                    // Add each content's prerequisites and corequisites to toAnalzye
                    foreach (Content content in contents)
                    {
                        toAnalyze.Enqueue(content.Prerequisites);
                        toAnalyze.Enqueue(content.Corequisites);
                    }
                    // Add all other decisions from this decision
                    foreach (Option option in decision.Options.Where(option => option is Decision))
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
                if (compare == 0) compare = p2.GetLevel()                   - p1.GetLevel();
                if (compare == 0) compare = p1.Options.Count                - p2.Options.Count;
                if (compare == 0) compare = p1.Pick                         - p2.Pick;
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
            //if (decisions.Sum(other => other.Pick) < decision.Pick)
            //    return false;

            //Check if any of the decisions are an obvious subset of the main decision
            bool subsetFound = false;
            foreach (Decision other in decisions.Where(other => other.Covers(decision)))
            {
                other.AddReasons(decision);
                subsetFound = true;
            }
            if (subsetFound)
                return true;

            //TODO: other heuristic checks

            // TODO: account for when electives aren't detected because they are missing like one subject in one of the lists

            return false;
        }

        public static bool Covers(this Decision cover, Decision maybeRedundant)
        {
            // This isn't a thorough check, because otherwise it would be possible for simple decisions to be CoveredBy very complicated decisions
            // Also, I do not want to think about how NCCWs would interact with this function

            // A quick check to speed up the time
            if (cover.Pick >= maybeRedundant.Pick)
                // Use the pigeonhole principle to compare the `pick` from both decisions
                if (cover.Pick - cover.Options.Except(maybeRedundant.Options).Count() >= maybeRedundant.Pick)
                    return true;

            // If maybeRedundant is made of other decisions, recursively check if the those decisions are covered
            if (!maybeRedundant.IsElective() && maybeRedundant.Options.Count(option => option is Decision decision && cover.Covers(decision)) >= maybeRedundant.Pick)
                return true;

            // If cover is made of other decisions and everything must be picked, recursively check if any of the decisions work as a cover
            if (cover.Options.Count() == cover.Pick && cover.Options.Any(option => option is Decision decision && decision.Covers(maybeRedundant)))
                return true;

            return false;
        }
    }
}
