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
        // TODO: combine this class with Plan
        // All of these methods are static and use plan as a parameter

        public static void AddContent(Content content, Plan plan)
        {
            plan.AddContents(new[] { content });
            Analyze(plan);
        }

        public static void AddContents(IEnumerable<Content> contents, Plan plan)
        {
            plan.AddContents(contents);
            Analyze(plan);
        }

        public static void ForceSubject(Subject subject, Plan plan, Time time)
        {
            plan.ForceTime(subject, time);
            Analyze(plan);
        }

        public static void UnForceSubject(Subject subject, Plan plan)
        {
            plan.UnForceTime(subject);
            Analyze(plan);
        }

        public static void RemoveContent(Content content, Plan plan)
        {
            plan.RemoveContent(content);
            Analyze(plan);
        }

        public static void SetMaxCreditPoints(Time time, int creditPoints, Plan plan)
        {
            plan.SetMaxCreditPoints(time, creditPoints);
            Analyze(plan);
        }

        public static void SetMaxCreditPoints(Session session, int creditPoints, Plan plan)
        {
            plan.SetMaxCreditPoints(session, creditPoints);
            Analyze(plan);
        }

        public static void SetMaxCreditPoints(int creditPoints, Plan plan)
        {
            plan.SetMaxCreditPoints(creditPoints);
            Analyze(plan);
        }

        static void Analyze(Plan plan)
        {
            if (!plan.SelectedCourses.Any()) return;

            Stopwatch timer1 = new Stopwatch();
            Stopwatch timer2 = new Stopwatch();

            bool newInformationFound = true;
            while (newInformationFound)
            {
                newInformationFound = false;

                plan.ClearDecisions();

                // With all the new information about bannedSubjects, check again how this affects EarliestCompletionTimes

                plan.RefreshEarliestTimes();

                /* When a subject is selected to satisfy a course, it cannot be used to satisfy another course requisite
                 * For that reason, the algorithm will start by splitting each course requisite into all possible ways of picking that a subject can satisfy that requisite
                 */

                // Load the requisites from courses

                timer1.Restart();

                List<Option> megaDecisionMainOptions = plan.SelectedCourses.Select(course => course.Prerequisites).ToList<Option>();

                Decision megaDecision = new Decision(
                    plan.SelectedCourses.First(), // TODO: properly combine reasons
                    options: megaDecisionMainOptions,
                    selectionType: Selection.CP,
                    creditPoints: megaDecisionMainOptions.Sum(option => option.CreditPoints())
                ).GetSimplifiedDecision();

                Queue<Decision> toAnalyze = new Queue<Decision>();
                toAnalyze.Enqueue(megaDecision);

                // Analyze the corequisites of Courses (which is usually just "120cp at 2000 level or above")

                foreach (Course course in plan.SelectedCourses)
                    toAnalyze.Enqueue(course.Corequisites);

                // Analyze every decision that comes from the selected subjects and the courses

                foreach (Subject subject in plan.SelectedSubjects)
                {
                    toAnalyze.Enqueue(subject.Prerequisites);
                    toAnalyze.Enqueue(subject.Corequisites);
                }

                //Iterate over the queue
                while (toAnalyze.Any())
                {
                    //Consider the next decision in the queue
                    Decision decision = toAnalyze.Dequeue();

                    // Remember the original list of banned contents. It might be used later
                    Dictionary<Content, List<Content>> oldBannedContents = new Dictionary<Content, List<Content>>(plan.BannedContents);

                    //Remove this decision from the list of decisions (this will probably be added at the end of the loop)
                    plan.RemoveDecision(decision);

                    // GetRemainingDecision can be computationally expensive, so this part of the algorithm is repeated before and after GetRemainingDecision
                    if (decision.SelectionType != Selection.CP && decision.MustPickAll() && decision.Options.All(option => option is Decision))
                    {
                        //If everything must be selected, select everything. Add the new decisions to the list
                        foreach (Option option in decision.Options)
                            toAnalyze.Enqueue(option as Decision);
                        continue;
                    }

                    //Replace the decision with only the part that still needs to be decided on
                    Decision remainingDecision = decision.GetRemainingDecision(plan).GetSimplifiedDecision();

                    // Check if the remaining decision has nothing left to pick
                    if (remainingDecision.HasBeenCompleted())
                        continue;

                    // Make sure the resulting decision is allowed
                    if (remainingDecision.HasBeenBanned())
                        throw new InvalidOperationException("Decision got banned even though it needs to be completed");

                    // If everything must be picked, pick everything
                    if (remainingDecision.MustPickAll())
                    {
                        // Add all contents from this decision
                        List<Content> contents = remainingDecision.Options
                            .Where(option => option is Content && !plan.SelectedSubjects.Contains(option) && !plan.SelectedCourses.Contains(option))
                            .Cast<Content>().ToList();
                        plan.AddContents(contents);
                        // Add each content's prerequisites and corequisites to toAnalzye
                        foreach (Content content in contents)
                        {
                            toAnalyze.Enqueue(content.Prerequisites);
                            toAnalyze.Enqueue(content.Corequisites);
                        }
                        // Add all other decisions from this decision
                        foreach (Option option in remainingDecision.Options.Where(option => option is Decision))
                            toAnalyze.Enqueue(option as Decision);
                        // If any contents were added, restart the algorithm
                        if (contents.Any())
                        {
                            newInformationFound = true;
                            break;
                        }
                    }
                    else
                    {
                        // The program cannot determine what to do, so the human decides
                        plan.AddDecision(remainingDecision);
                        // If the decision resulted in new stuff getting banned, redo the other decisions
                        if (plan.BannedContents.Count != oldBannedContents.Count && !plan.BannedContents.Keys.All(oldBannedContents.ContainsKey))
                        {
                            foreach (Decision redoDecision in plan.Decisions)
                                toAnalyze.Enqueue(redoDecision);
                            plan.RefreshEarliestTimes();
                        }
                    }
                }

                timer1.Stop();
                Console.WriteLine("Making decisions:    " + timer1.ElapsedMilliseconds + "ms");

            }

            // Sort the decisions so it is nice for the user

            timer2.Restart();

            // Remove redundant decisions
            // decisions are ordered by unique decisions to avoid a strange interaction effect with CoveredBy
            foreach (Decision decision in plan.Decisions.OrderBy(decision => decision.Unique()).ToList())
                if (decision.CoveredBy(plan))
                    plan.RemoveDecision(decision);

            // Sort decisions by the complexity of the decision
            plan.Decisions.Sort(delegate (Decision p1, Decision p2)
            {
                int compare = 0;
                if (compare == 0) compare = (p2.Options.Any(option => option is Course) ? 1 : 0) - (p1.Options.Any(option => option is Course) ? 1 : 0);
                if (compare == 0) compare = (p1.IsElective() ? 1 : 0) - (p2.IsElective() ? 1 : 0);
                if (compare == 0) compare = p2.GetLevel() - p1.GetLevel();
                if (compare == 0) compare = p1.Options.Count - p2.Options.Count;
                if (compare == 0) if (p1.SelectionType == Selection.CP && p2.SelectionType == Selection.CP) compare = p1.CreditPoints() - p2.CreditPoints();
                if (compare == 0) compare = p1.RequiredCompletionTime(plan).AsNumber() - p2.RequiredCompletionTime(plan).AsNumber();
                if (compare == 0) compare = p1.ToString().CompareTo(p2.ToString());
                return compare;
            });

            timer2.Stop();
            Console.WriteLine("Removing repetition: " + timer2.ElapsedMilliseconds + "ms");
        }

        public static bool CoveredBy(this Decision decision, Plan plan)
        {
            //Make sure that the main decision isn't in the list of decisions
            List<Decision> decisions = new List<Decision>(plan.Decisions);
            decisions.Remove(decision);

            // If this is a non-unique elective, it is probably covered by the course
            if (decision.IsElective() && !decision.Unique() &&
                decisions.Cast<Option>().SumOfCreditPointsIsGreaterThanOrEqualTo(
                    other => other is Decision otherDecision && otherDecision.Unique() &&
                        (otherDecision.HasSameOptions(decision) || otherDecision.Options.All(option => decision.Contains(option))),
                    decision.CreditPoints()))
                return true;

            // Check if any of the decisions are a subset of the main decision
            // If the main decision and the other decision are both Unique, then don't compare them
            bool coverFound = false;
            foreach (Decision other in decisions)
            {
                if (decision.Unique() && other.Unique())
                    continue;
                if (!other.Unique() && decision.Unique() && !other.OnlyPickOne()) // TODO: what about when it's 30cp (unique) and 30cp at 2000+ level?
                    continue;
                if (other.Covers(decision)) 
                {
                    other.AddReasons(decision);
                    coverFound = true;
                }
            }
            if (coverFound)
                return true;

            return false;
        }

        public static Decision NextDecision(Plan plan, Decision originalDecision, out bool keepFilterSettings)
        {
            Decision offer = null;
            if (originalDecision != null)
                offer = plan.Decisions.Where(other => other.Options.All(option => originalDecision.Options.Contains(option))).OrderByDescending(decision => decision.Options.Count).FirstOrDefault();
            if (offer != null)
            {
                keepFilterSettings = true;
                return offer;
            }
            // If nothing was found, check if any decision has the same (Subject) reason as the decision that was just made
            keepFilterSettings = false;
            if (originalDecision != null)
                offer = plan.Decisions.FirstOrDefault(decision => !decision.IsElective() && originalDecision.GetReasons().Any(reason => reason is Subject && decision.GetReasons().Contains(reason)));
            if (offer != null)
                return offer;
            // If nothing was found, pick the first decision in the plan
            if (plan.Decisions.Any())
                offer = plan.Decisions.First();
            return offer;
        }
    }
}
