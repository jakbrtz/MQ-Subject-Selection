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
        public static void AddContent(Content content, Plan plan)
        {
            plan.AddContents(new[] { content });
            Analyze(plan);
        }

        public static void MoveSubject(Subject subject, Plan plan, Time time)
        {
            plan.ForceTime(subject, time);
            Analyze(plan);
        }

        public static void RemoveContent(Content content, Plan plan)
        {
            plan.RemoveContent(content);
            Analyze(plan);
        }

        static void Analyze(Plan plan)
        {
            Stopwatch timer1 = new Stopwatch();
            Stopwatch timer2 = new Stopwatch();
            Stopwatch timer3 = new Stopwatch();
            timer1.Start();

            bool newInformationFound = true;
            while (newInformationFound)
            {
                newInformationFound = false;

                plan.ClearDecisions();

                /* When a subject is selected to satisfy a course, it cannot be used to satisfy another course requisite
                 * For that reason, the algorithm will start by splitting each course requisite into all possible ways of picking that a subject can satisfy that requisite
                 */

                // Load the requisites from courses

                List<Option> megaDecisionOptions = plan.SelectedCourses.Select(course => course.Prerequisites).ToList<Option>();
                Decision megaDecision = new Decision(plan.SelectedCourses.First(), options: megaDecisionOptions, pick: megaDecisionOptions.Count, selectionType: Selection.AND);


                /*
                // Split each decision into sub-decisions
                List<Decision> courseDecisions = new List<Decision>();
                while (toAnalyze.Any())
                {
                    Decision decision = toAnalyze.Dequeue();
                    // If everything must be picked and this decision contains other decisions, split the other decisions into separate decisions
                    if (decision.Pick == decision.Options.Count && decision.Options.Any(option => option is Decision))
                    {
                        // Create a decision containing only the contents, which are compulsory
                        List<Option> contents = decision.Options.Where(option => option is Content).ToList();
                        if (contents.Any())
                            courseDecisions.Add(new Decision(decision, options: contents, pick: contents.Count, selectionType: Selection.AND));
                        // All sub-decisions need to be checked for further sub-decisions
                        foreach (Option option in decision.Options.Where(option => option is Decision))
                            toAnalyze.Enqueue(option as Decision);
                    }
                    // Otherwise, this decision is ready for the next stage of processing
                    else
                        courseDecisions.Add(decision);
                }

                // For each selected subject, check if it's part of multiple decisions. Consider the result of that
                foreach (Subject subject in plan.SelectedSubjects) //TODO: also plan.SelectedCourses
                {
                    // Find all decisions that involve this subject, but ignore electives
                    List<Decision> relevantDecisions = courseDecisions.Where(decision => decision.Contains(subject) && !decision.IsElective()).ToList();
                    // If there aren't any decisions, skip to the next subject
                    if (!relevantDecisions.Any()) continue;
                    // Remove these subjects from the plan
                    foreach (Decision decision in relevantDecisions)
                        courseDecisions.Remove(decision);
                    // Prepare a list of every way this combination of decisions could end up in
                    List<Option> possibleResults = new List<Option>();
                    // The subject can only belong to one designated decision, so check what happens when each relevant decision gets designated
                    foreach (Decision designatedDecision in relevantDecisions)
                    {
                        // Remove the subject from each of the other decisions
                        var otherDecisions = relevantDecisions.Where(decision => decision != designatedDecision).Select(decision => decision.Without(subject));
                        // Remove the subject from the designated decision, but also reduce the Pick of that decision
                        List<Option> allDecision = otherDecisions.Cast<Option>().ToList();
                        allDecision.Add(designatedDecision.Without(subject, reducePick: true));
                        // Create a new decision which is a combination of all decisions when the subject is removed
                        // TODO: is the reason (designatedDecision) correct?
                        possibleResults.Add(new Decision(designatedDecision, options: allDecision, pick: allDecision.Count, selectionType: Selection.AND));
                    }
                    // Create a new decision that is a combination of all possible decisions
                    Decision resultingDecision = new Decision(possibleResults.First(), options: possibleResults);
                    // Simplify that decision
                    Decision simplifiedResultingDecision = resultingDecision.GetSimplifiedDecision();
                    // Make sure the resulting decision is allowed
                    if (simplifiedResultingDecision.Pick > simplifiedResultingDecision.Options.Count)
                        throw new Exception("Not enough options in this decision");
                    // Add that decision back into the plan
                    courseDecisions.Add(simplifiedResultingDecision);
                }

                */

                foreach (Subject subject in plan.SelectedSubjects) //TODO: also plan.SelectedCourses
                {
                    Decision withoutSubect = megaDecision.WithSelectedContent(subject, true);
                    Decision simplified = withoutSubect.GetSimplifiedDecision();
                    if (simplified.Pick > simplified.Options.Count)
                        throw new Exception("Not enough options in the decision");
                    megaDecision = simplified;
                }

                Queue<Decision> toAnalyze = new Queue<Decision>();

                /*
                foreach (Decision decision in courseDecisions)
                    toAnalyze.Enqueue(decision);
                    */

                toAnalyze.Enqueue(megaDecision); // It takes so long because megadecision's compulsory subjects should be explored first but they aren't

                timer1.Stop();
                Console.WriteLine("Reducing courses:    " + timer1.ElapsedMilliseconds + "ms");

                /* Now also analyze every decision that comes from the selected subjects and the courses
                 */

                timer2.Start();

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
                    HashSet<Content> oldBannedContents = new HashSet<Content>(plan.BannedContents);

                    //Remove this decision from the list of decisions (this will probably be added at the end of the loop)
                    plan.RemoveDecision(decision);

                    // GetRemainingDecision can be computationally expensive, so this part of the algorithm is repeated before and after GetRemainingDecision
                    if (decision.Pick == decision.Options.Count && decision.Options.All(option => option is Decision))
                    {
                        //If everything must be selected, select everything. Add the new decisions to the list
                        foreach (Option option in decision.Options)
                            toAnalyze.Enqueue(option as Decision);
                        continue;
                    }

                    //Replace the decision with only the part that still needs to be decided on
                    decision = decision.GetRemainingDecision(plan).GetSimplifiedDecision();

                    // Check if the remaining decision has nothing left to pick
                    if (decision.Pick == 0)
                        continue;

                    // Make sure the resulting decision is allowed
                    if (decision.Pick > decision.Options.Count)
                        throw new Exception("Not enough options in this decision");

                    // If everything must be picked, pick everything
                    if (decision.Pick == decision.Options.Count)
                    {
                        // Add all contents from this decision
                        List<Content> contents = decision.Options.Where(option => option is Content content && !plan.SelectedSubjects.Contains(content)).Cast<Content>().ToList();
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
                        plan.AddDecision(decision);
                        // If the decision resulted in new stuff getting banned, redo the other decisions
                        if (!plan.BannedContents.SetEquals(oldBannedContents))
                            foreach (Decision redoDecision in plan.Decisions)
                                toAnalyze.Enqueue(redoDecision);
                    }

                }

                timer2.Stop();
                Console.WriteLine("Making decisions:    " + timer2.ElapsedMilliseconds + "ms");

            }

            /* Sort the decisions and remove redundany ones
             */

            timer3.Start();

            foreach (Decision decision in new List<Decision>(plan.Decisions))
                if (decision.CoveredBy(plan))
                    plan.RemoveDecision(decision);

            //Sort decisions by the complexity of the decision
            plan.Decisions.Sort(delegate (Decision p1, Decision p2)
            {
                int compare = 0;
                if (compare == 0) compare = p2.GetLevel()                              - p1.GetLevel();
                if (compare == 0) compare = p1.Options.Count                           - p2.Options.Count;
                if (compare == 0) compare = p1.Pick                                    - p2.Pick;
                if (compare == 0) compare = p1.RequiredCompletionTime(plan).AsNumber() - p2.RequiredCompletionTime(plan).AsNumber();
                if (compare == 0) compare = p1.ToString().CompareTo(p2.ToString());
                return compare;
            });

            timer3.Stop();
            Console.WriteLine("Removing repetition: " + timer3.ElapsedMilliseconds + "ms");
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

            // If both decisions must have unique credit points, then they aren't allowed to cover each other
            if (cover.Unique() && maybeRedundant.Unique())
                return false;

            // A quick check to speed up the time
            if (cover.Pick >= maybeRedundant.Pick)
                // Use the pigeonhole principle to compare the `pick` from both decisions
                if (cover.Pick - cover.Options.Except(maybeRedundant.Options).Count() >= maybeRedundant.Pick)
                    return true;

            // If maybeRedundant is made of other decisions, recursively check if the those decisions are covered
            if (maybeRedundant.Options.Count(option => option is Decision decision && cover.Covers(decision)) >= maybeRedundant.Pick)
                return true;

            // If cover is made of other decisions and everything must be picked, recursively check if any of the decisions work as a cover
            if (cover.Options.Count() == cover.Pick && cover.Options.Any(option => option is Decision decision && decision.Covers(maybeRedundant)))
                return true;

            return false;
        }
    }
}
