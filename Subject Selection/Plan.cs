using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Subject_Selection
{
    public class Plan
    {
        public List<List<Subject>> SubjectsInOrder { get; }
        public List<Decision> Decisions { get; }
        public HashSet<Subject> SelectedSubjects { get; }
        public HashSet<Subject> SelectedCourses { get; }

        readonly Dictionary<Subject, int> forcedTimes = new Dictionary<Subject, int>();
        public List<int> MaxSubjects { get; }
        public HashSet<Subject> BannedSubjects { get; }

        public Plan()
        {
            SubjectsInOrder = new List<List<Subject>>();
            Decisions = new List<Decision>();
            SelectedSubjects = new HashSet<Subject>();
            SelectedCourses = new HashSet<Subject>();
            MaxSubjects = new List<int>();
            BannedSubjects = new HashSet<Subject>();
        }

        public Plan(Plan other)
        {
            SubjectsInOrder = other.SubjectsInOrder.Select(x => x.ToList()).ToList();
            Decisions = new List<Decision>(other.Decisions);
            SelectedSubjects = new HashSet<Subject>(other.SelectedSubjects);
            SelectedCourses = new HashSet<Subject>(other.SelectedCourses);
            MaxSubjects = new List<int>(other.MaxSubjects);
            BannedSubjects = new HashSet<Subject>(other.BannedSubjects);
        }

        public void AddSubjects(IEnumerable<Subject> subjects)
        {
            if (!subjects.Any())
                return;
            foreach (Subject subject in subjects)
                if (subject.IsSubject)
                    SelectedSubjects.Add(subject);
                else
                    SelectedCourses.Add(subject);
            Order();
            RefreshBannedSubjectsList();
        }

        public bool Contains(Subject option)
        {
            return SelectedSubjects.Contains(option) || SelectedCourses.Contains(option);
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

        public void ForceTime(Subject subject, int time)
        {
            if (forcedTimes.ContainsKey(subject) && forcedTimes[subject] == time)
                forcedTimes.Remove(subject);
            else
                forcedTimes[subject] = time;
            Order();
        }

        public override string ToString()
        {
            string output = "";
            foreach (List<Subject> semester in SubjectsInOrder)
                output += "[" + string.Join(" ", semester) + "] ";
            return output;
        }

        public List<Subject> SelectedSubjectsSoFar(int time = -1)
        {
            if (time == -1) time = SubjectsInOrder.Count;
            return SubjectsInOrder.Take(time).SelectMany(x => x).ToList();
        }

        public void Order()
        {
            Stopwatch timer3 = new Stopwatch();
            timer3.Start();

            SubjectsInOrder.Clear();
            for (int session = 0; session < MaxSubjects.Count; session++)
            {
                //Only select subjects which have not been selected yet
                SubjectsInOrder.Add(new List<Subject>(SelectedSubjects
                    .Except(SelectedSubjectsSoFar()).Where(subject =>
                    //Pick from subjects that are allowed during this semester
                    subject.GetPossibleTimes(this).Contains(session) &&
                    //Pick subjects that are forced into this spot
                    (forcedTimes.ContainsKey(subject) && forcedTimes[subject] == session ||
                    //Pick from subjects that have no remaining prerequisites or corequisites
                    IsLeaf(subject, session)))
                    //Favour lower level subjects
                    .OrderBy(subject => subject.GetLevel())
                    .OrderByDescending(subject => SelectedSubjects.Except(SelectedSubjectsSoFar()).Count(parent => IsAbove(parent, subject)))
                    //Favour subjects forced into this timeslot
                    .Except(SelectedSubjects.Where(subject => forcedTimes.ContainsKey(subject) && forcedTimes[subject] > session))
                    .OrderBy(subject => forcedTimes.ContainsKey(subject) ? forcedTimes[subject] : MaxSubjects.Count)
                    //Don't select more subjects than is allowed
                    .Take(MaxSubjects[session])));
                //TODO: include levels of how 'forced' a time is
            }

            if (SelectedSubjects.Except(SelectedSubjectsSoFar()).Any())
                Console.WriteLine("ERROR: Couldn't fit in all your subjects");

            timer3.Stop();
            Console.WriteLine("Ordering Plan:       " + timer3.ElapsedMilliseconds + "ms");
        }

        // IsLeaf and IsAbove are helper functions for Order()
        private bool IsLeaf(Subject subject, int time)
        {
            return IsLeaf(subject, time, subject.Prerequisites) && IsLeaf(subject, time, subject.Corequisites);
        }

        private bool IsLeaf(Subject subject, int time, Decision requisite)
        {
            //If the requisit is met, return true
            if (requisite.HasBeenCompleted(this, time))
                return true;
            //If the requisit is an elective and the recommended year has passed, count this as a leaf
            if (requisite.IsElective() && subject.GetLevel() <= time / 3 + 1)
                return true;
            //Consider each option
            foreach (Option option in requisite.GetOptions())
                //If the option is a subject that needs to be picked, hasn't been picked, and is not above the current subject: the subject is not a leaf
                if (option is Subject && SelectedSubjects.Contains(option) && !SelectedSubjectsSoFar().Contains(option) && !IsAbove(option as Subject, subject))
                    return false;
                //If the option is a decision that does not lead to a leaf then the subject is not a leaf
                else if (option is Decision && !IsLeaf(subject, time, option as Decision))
                    return false;
            return true;
        }

        private bool IsAbove(Subject parent, Subject child)
        {
            //Creates a list of all subjects that are selected and are descendants of this subject
            List<Subject> descendants = new List<Subject>();
            Queue<Subject> toAnalyze = new Queue<Subject>();
            toAnalyze.Enqueue(parent);
            while (toAnalyze.Any())
            {
                Subject current = toAnalyze.Dequeue();
                if (current == child) return true;
                descendants.Add(current);
                foreach (Subject subject in current.Prerequisites.GetSubjects().Intersect(SelectedSubjects).Except(descendants))
                    toAnalyze.Enqueue(subject);
                foreach (Subject subject in current.Corequisites.GetSubjects().Intersect(SelectedSubjects).Except(descendants))
                    toAnalyze.Enqueue(subject);
            }
            return false;
        }

        void RefreshBannedSubjectsList()
        {
            BannedSubjects.Clear();
            // Get all selected subjects and check them for NCCWs
            foreach (Subject subject in SelectedSubjects)
                foreach (string id in subject.NCCWs)
                    if (Parser.TryGetSubject(id, out Subject nccw))
                        BannedSubjects.Add(nccw);
            /* TODO: fix assumptions
             * This code assumes that when subject X is on subject Y's nccw list, then subject Y is on subject X's nccw list
             * I have found 45 exceptions to this assumption. Does that have a special meaning, or is it an incorrect data entry?
             */ 
            // Check which decisions force a banned subject
            foreach (Decision decision in Decisions)
                foreach (Subject subject in decision.ForcedBans())
                    BannedSubjects.Add(subject);
        }
    }
}
