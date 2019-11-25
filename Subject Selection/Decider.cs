﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Subject_Selection
{
    public static class Decider
    {
        public static void AddSubject(Subject subject, Plan plan)
        {
            //Add the subject to the list
            plan.AddSubject(subject);
            // Create an empty queue of things to consider
            Queue<Prerequisite> toAnalyze = new Queue<Prerequisite>();
            // Consider the new subject's prerequisites
            toAnalyze.Enqueue(subject.Prerequisites);
            // Reconsider all existing decisions
            foreach (Prerequisite decision in plan.Decisions.Except(toAnalyze))
                toAnalyze.Enqueue(decision);
            // Analyze every decision in toAnalyze
            AnalyzeDecisions(toAnalyze, plan);
        }

        static void AddSubjects(IEnumerable<Subject> subjects, Plan plan, Queue<Prerequisite> toAnalyze)
        {
            //Add the subject to the list
            plan.AddSubjects(subjects);
            // Consider the new subject's prerequisites
            foreach (Subject subject in subjects)
                toAnalyze.Enqueue(subject.Prerequisites);
            // Reconsider all existing decisions
            foreach (Prerequisite decision in plan.Decisions.Except(toAnalyze))
                toAnalyze.Enqueue(decision);
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
            AnalyzeDecisions(toAnalyze, plan);
        }

        static void AnalyzeDecisions(Queue<Prerequisite> toAnalyze, Plan plan)
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

                // GetRemainingDecision is computationally expensive, so I'm repeating this loop before and after that method
                if (decision.MustPickAll() && decision.GetOptions().All(criteria => criteria is Prerequisite))
                {
                    //If everything must be selected, select everything. Add the new prerequisites to the list
                    foreach (Criteria option in decision.GetOptions())
                        toAnalyze.Enqueue(option as Prerequisite);
                    continue;
                }

                //Replace the decision with only the part that still needs to be decided on
                decision = decision.GetRemainingDecision(plan);
                
                //Remove all reasons that have been met
                decision.GetReasons().RemoveAll(reason => reason.Prerequisites.HasBeenMet(plan, reason.GetChosenTime(plan)));
                //If there are no more reasons to make a decision, don't analyze the decision
                if (!decision.GetReasons().Any())
                    continue;

                if (decision.MustPickAll())
                {
                    // Add all the subjects from the decision
                    AddSubjects(decision.GetOptions().Cast<Subject>(), plan, toAnalyze);
                    // Put any prerequisites back into toAnalyze
                    foreach (Criteria option in decision.GetOptions().Where(option => option is Prerequisite))
                        toAnalyze.Enqueue(option as Prerequisite);
                    //  Skip to the next iteration
                    continue;
                }

                //If the code reached here, then the program cannot determine what to do, so the human decides
                plan.AddDecision(decision);
                
            }

            timer1.Stop();
            timer2.Start();

            foreach (Prerequisite decision in new List<Prerequisite>(plan.Decisions))
                if (decision.CoveredBy(plan))
                    plan.RemoveDecision(decision);

            //Sort decisions by the complexity of the decision
            plan.Decisions.Sort(delegate (Prerequisite p1, Prerequisite p2)
            {
                int compare = 0;
                //if (compare == 0) compare = p1.RequiredCompletionTime(plan) - p2.RequiredCompletionTime(plan);
                if (compare == 0) compare = p1.GetOptions().Count          - p2.GetOptions().Count;
                if (compare == 0) compare = p1.GetSubjects().Count         - p2.GetSubjects().Count;
                if (compare == 0) compare = p1.GetPick()                   - p2.GetPick();
                if (compare == 0) compare = p1.GetSubjects()[0].GetLevel() - p2.GetSubjects()[0].GetLevel();
                if (compare == 0) compare = p1.ToString().CompareTo(p2.ToString());
                return compare;
            });

            timer2.Stop();
            Console.WriteLine("Making decisions:    " + timer1.ElapsedMilliseconds + "ms");
            Console.WriteLine("Removing repetition: " + timer2.ElapsedMilliseconds + "ms");
        }

        public static bool IsSubset(this Prerequisite smaller, Prerequisite larger)
        {
            return smaller.GetPick() >= larger.GetPick() && smaller.GetOptions().All(option => larger.GetOptions().Contains(option))
                // || other.GetOptions().Exists(criteria => criteria is Prerequisite && prerequisite.IsSubset(criteria as Prerequisite))
                ;

            // TODO: account for when prerequisites are made up of other prerequisites (such as COGS2000)
            // TODO: account for when electives aren't detected because they are missing like one subject in one of the lists
        }

        public static bool CoveredBy(this Prerequisite prerequisite, Plan plan)
        {
            //Make sure that the main prerequisite isn't in the list of decisions
            List<Prerequisite> decisions = plan.Decisions.Where(decision => decision != prerequisite).ToList();

            //Remove all prerequisites that don't have any overlap with the main prerequisite 
            //decisions = decisions.Where(decision => decision.GetSubjects().Intersect(prerequisite.GetSubjects()).Any()).ToList();
            //if (decisions.Count == 0)
            //    return false;

            //Check if the sum of the prerequisites' pick is more than the main prerequisite's pick
            //if (decisions.Sum(decision => decision.GetPick()) < prerequisite.GetPick())
            //    return false;

            //Check if any of the prerequisists are an obvious subset of the main prequisit
            bool subsetFound = false;
            foreach (Prerequisite decision in decisions.Where(decision => decision.IsSubset(prerequisite)))
            {
                decision.AddReasons(prerequisite);
                subsetFound = true;
            }
            if (subsetFound)
                return true;

            //TODO: other heuristic checks

            return false;
        }
    }
}
