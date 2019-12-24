﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Subject_Selection
{
    public class Plan
    {
        public Dictionary<Time, List<Subject>> SubjectsInOrder { get; }
        public List<Decision> Decisions { get; }
        public HashSet<Subject> SelectedSubjects { get; }
        public HashSet<Course> SelectedCourses { get; }

        readonly Dictionary<Subject, Time> forcedTimes = new Dictionary<Subject, Time>();

        readonly public Dictionary<Time, int> MaxSubjects = new Dictionary<Time, int>();
        public HashSet<Content> BannedContents { get; }

        public Plan()
        {
            SubjectsInOrder = new Dictionary<Time, List<Subject>>();
            Decisions = new List<Decision>();
            SelectedSubjects = new HashSet<Subject>();
            SelectedCourses = new HashSet<Course>();
            BannedContents = new HashSet<Content>();
        }

        public Plan(Plan other)
        {
            SubjectsInOrder = new Dictionary<Time, List<Subject>>();
            foreach (KeyValuePair<Time, List<Subject>> semester in other.SubjectsInOrder)
                SubjectsInOrder.Add(semester.Key, semester.Value);

            Decisions = new List<Decision>(other.Decisions);
            SelectedSubjects = new HashSet<Subject>(other.SelectedSubjects);
            SelectedCourses = new HashSet<Course>(other.SelectedCourses);
            BannedContents = new HashSet<Content>(other.BannedContents);

            forcedTimes = new Dictionary<Subject, Time>(other.forcedTimes);
            MaxSubjects = new Dictionary<Time, int>(other.MaxSubjects);
        }

        public void AddContents(IEnumerable<Content> contents)
        {
            if (!contents.Any())
                return;
            foreach (Content content in contents)
                if (content is Subject)
                    SelectedSubjects.Add(content as Subject);
                else
                    SelectedCourses.Add(content as Course);
            Order();
            RefreshBannedSubjectsList();
        }

        public void RemoveContent(Content content)
        {
            if (content is Subject)
                SelectedSubjects.Remove(content as Subject);
            else
                SelectedCourses.Remove(content as Course);
            Order();
            RefreshBannedSubjectsList();
        }

        public void AddDecision(Decision decision)
        {
            Decisions.Add(decision);
            RefreshBannedSubjectsList();
        }

        public void RemoveDecision(Decision decision)
        {
            if (Decisions.Remove(decision))
                RefreshBannedSubjectsList();
        }

        public void ClearDecisions()
        {
            Decisions.Clear();
            RefreshBannedSubjectsList();
        }

        public List<Time> IterateTimes()
        {
            List<Time> output = new List<Time>();
            Time current = Time.Early;
            for (int i = 0; i < MaxSubjects.Count; i++)
            {
                current = current.Next();
                output.Add(current);
            }
            return output;
        }

        public void ForceTime(Subject subject, Time time)
        {
            if (forcedTimes.ContainsKey(subject) && forcedTimes[subject].year == time.year && forcedTimes[subject].session == time.session)
                forcedTimes.Remove(subject);
            else
                forcedTimes[subject] = time;
            Order();
        }

        public override string ToString()
        {
            string output = "";
            foreach (List<Subject> semester in SubjectsInOrder.Values)
                output += "[" + string.Join(" ", semester) + "] ";
            return output;
        }

        public IEnumerable<Subject> SelectedSubjectsSoFar(Time time)
        {
            return SubjectsInOrder.Where(semester => semester.Key.IsEarlierThanOrAtTheSameTime(time)).SelectMany(kvp => kvp.Value);
        }

        public void Order()
        {
            Stopwatch timer3 = new Stopwatch();
            timer3.Start();

            SubjectsInOrder.Clear();
            foreach (Time semester in IterateTimes())
            {
                // Create a new semester and add it to SubjectsInOrder
                List<Subject> semesterClasses = new List<Subject>();
                SubjectsInOrder.Add(semester, semesterClasses);
                // Fill the semester with subjects that can be chosen
                for (int subjectNumber = 0; subjectNumber < MaxSubjects[semester]; subjectNumber++)
                {
                    // Prepare a list of what subjects could be chosen
                    IEnumerable<Subject> possibleSubjects = SelectedSubjects.Except(SelectedSubjectsSoFar(Time.All));
                    // Do not pick subjects with forced times later than the current session
                    possibleSubjects = possibleSubjects.Where(subject => !(forcedTimes.ContainsKey(subject) && semester.IsEarlierThan(forcedTimes[subject])));
                    // Pick from subjects that are allowed during this semester
                    possibleSubjects = possibleSubjects.Where(subject => subject.GetPossibleTimes(MaxSubjects).Contains(semester));
                    // Pick from subjects that have satisfied requisites
                    possibleSubjects = possibleSubjects.Where(subject => IsLeaf(subject, semester));
                    // Favor lower level subjects
                    possibleSubjects = possibleSubjects.OrderBy(subject => subject.GetLevel());
                    // Favor subjects that cannot fit in many semesters
                    possibleSubjects = possibleSubjects.OrderBy(subject => subject.Semesters.Count);
                    // Favor subjects that have many other subjects relying on them
                    possibleSubjects = possibleSubjects.OrderByDescending(subject => SelectedSubjects.Except(SelectedSubjectsSoFar(Time.All)).Count(other => IsAbove(parent: other, child: subject)));
                    // If any subjects are forced, filter them
                    IEnumerable<Subject> forcedSubjects = possibleSubjects
                        .Where(subject => forcedTimes.ContainsKey(subject) && forcedTimes[subject].IsEarlierThanOrAtTheSameTime(semester))
                        .OrderBy(subject => forcedTimes[subject]);
                    if (forcedSubjects.Any())
                        possibleSubjects = forcedSubjects;
                    // Pick the first item from that list
                    Subject nextSubject = possibleSubjects.FirstOrDefault();
                    // If no subject was chosen, go to the next semester
                    if (nextSubject == null) break;
                    // Add the selected subject to this semester
                    semesterClasses.Add(nextSubject);
                }
            }

            if (SelectedSubjects.Except(SelectedSubjectsSoFar(Time.All)).Any())
                 Console.WriteLine("ERROR: Couldn't fit in all your subjects");

            timer3.Stop();
            Console.WriteLine("Ordering Plan:       " + timer3.ElapsedMilliseconds + "ms");
        }

        private bool IsLeaf(Subject subject, Time time)
        {
            // Detects if this subject is relying on other subjects to be picked first
            return IsLeaf(subject, time.Previous(), subject.Prerequisites) && IsLeaf(subject, time, subject.Corequisites);
        }

        private bool IsLeaf(Subject subject, Time time, Decision requisite)
        {
            /* Back when I started writing this program I was naive and thought that prerequisites made a happy little tree
             * For that reason, I wrote this function that checks if a subject was a leaf on that tree
             * Thanks to elective decisions and corequisite pairs, I can't use a tree
             * That's why this function's got to be so complicated. I've got to write another helper function called IsAbove that detects cycles. It ignores elective decisions.
             */

            //If the requisit is met, return true
            if (requisite.HasBeenCompleted(this, time))
                return true;
            //If the requisit is an elective and the recommended year has passed, count this as a leaf
            if (requisite.IsElective() && subject.GetLevel() <= time.year)
                return true;
            //Consider each option
            foreach (Option option in requisite.Options)
                //If the option is a subject that needs to be picked, hasn't been picked, and is not above the current subject: the subject is not a leaf
                if (option is Content content && SelectedSubjects.Contains(content) && !SelectedSubjectsSoFar(time).Contains(content) && !IsAbove(content, subject))
                    return false;
                //If the option is a decision that does not lead to a leaf then the subject is not a leaf
                else if (option is Decision decision && !IsLeaf(subject, time, decision))
                    return false;
            return true;
        }

        private bool IsAbove(Content parent, Subject child)
        {
            /* This is a breadth-first search of the parent's requisites to check if the child is a requisite of a parent,
             *   such that the parent, child, and all subjects between, have all been selected by the user
             * Even though the parameters are of Content type, this function only gets called with Subject parameters. 
             * This is why the variables are called things like `currentSubject` instead of `currentContent`
             * The algorithm does not consider elective prerequisites
             */

            // Create a list of all subjects that are selected and are descendants of this subject
            HashSet<Content> descendants = new HashSet<Content>();
            // Create a list of subjects that need to be analyzed
            Queue<Content> subjectsToAnalyze = new Queue<Content>();
            subjectsToAnalyze.Enqueue(parent);
            while (subjectsToAnalyze.Any())
            {
                Content currentSubject = subjectsToAnalyze.Dequeue();
                // Check whether this has already been 
                if (descendants.Contains(currentSubject)) continue;
                // Add this to the list of subjects that has already been analyzed
                descendants.Add(currentSubject);
                
                // Do a breadth-first search through the subject's requisites to find all direct requisites of that subject
                Queue<Decision> decisionsToAnalyze = new Queue<Decision>();
                decisionsToAnalyze.Enqueue(currentSubject.Prerequisites);
                decisionsToAnalyze.Enqueue(currentSubject.Corequisites);
                while(decisionsToAnalyze.Any())
                {
                    Decision currentDecision = decisionsToAnalyze.Dequeue();
                    // If the decision is an elective, skip this decision
                    if (currentDecision.IsElective())
                        continue;
                    // Add every option in this decision to the appropriate queue
                    foreach (Option option in currentDecision.Options)
                    {
                        if (option is Content content && SelectedSubjects.Contains(option) && !subjectsToAnalyze.Contains(option))
                            subjectsToAnalyze.Enqueue(content);
                        if (option is Decision decision)
                            decisionsToAnalyze.Enqueue(decision);
                        // If the child is found, return true
                        if (currentSubject == child) return true;
                    }
                }
            }
            return false;
        }

        void RefreshBannedSubjectsList()
        {
            BannedContents.Clear();
            // Get all selected subjects and check them for NCCWs
            foreach (Subject subject in SelectedSubjects)
                foreach (string id in subject.NCCWs)
                    if (Parser.TryGetSubject(id, out Subject nccw))
                        BannedContents.Add(nccw);
            // Get all selected courses and check them for NCCWs
            foreach (Course course in SelectedCourses)
                foreach (string id in course.NCCWs)
                    if (Parser.TryGetOption(id, out Option nccw))
                        if (nccw is Content content)
                            BannedContents.Add(content);
            /* TODO: fix assumptions
             * This code assumes that when subject X is on subject Y's nccw list, then subject Y is on subject X's nccw list
             * I have found 45 exceptions to this assumption. Does that have a special meaning, or is it an incorrect data entry?
             */
            // Check which decisions force a banned subject
            foreach (Decision decision in Decisions)
                foreach (Content subject in decision.ForcedBans())
                    BannedContents.Add(subject);
        }
    }
}
