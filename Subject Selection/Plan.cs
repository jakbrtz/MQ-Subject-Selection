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
        public List<Prerequisite> Decisions { get; }
        public List<Subject> SelectedSubjects { get; }
        public List<Subject> SelectedCourses { get; }

        readonly Dictionary<Subject, int> forcedTimes = new Dictionary<Subject, int>();
        public List<int> MaxSubjects { get; }
        // Remember results for Subject.HasBeenBanned
        public Dictionary<Subject, bool> SubjectIsBanned { get; }

        public Plan()
        {
            SubjectsInOrder = new List<List<Subject>>();
            Decisions = new List<Prerequisite>();
            SelectedSubjects = new List<Subject>();
            SelectedCourses = new List<Subject>();
            MaxSubjects = new List<int>();
            SubjectIsBanned = new Dictionary<Subject, bool>();
        }

        public Plan(Plan other)
        {
            SubjectsInOrder = other.SubjectsInOrder.Select(x => x.ToList()).ToList();
            Decisions = new List<Prerequisite>(other.Decisions);
            SelectedSubjects = new List<Subject>(other.SelectedSubjects);
            SelectedCourses = new List<Subject>(other.SelectedCourses);
            MaxSubjects = new List<int>(other.MaxSubjects);
            SubjectIsBanned = new Dictionary<Subject, bool>(other.SubjectIsBanned);
        }

        public void AddSubject(Subject subject)
        {
            if (subject.IsSubject)
            {
                SelectedSubjects.Add(subject);
            }
            else
            {
                SelectedCourses.Add(subject);
            }
            //TODO: don't reorder entire thing every time a subject is added
            Order();
            RefreshBannedSubjectsList();
        }

        public List<Subject> SelectedStuff()
        {
            return SelectedSubjects.Union(SelectedCourses).ToList();
        }

        public void AddDecision(Prerequisite decision)
        {
            Decisions.Add(decision);
            RefreshBannedSubjectsList();
        }

        public void RemoveDecision(Prerequisite decision)
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

        public Prerequisite PickNextDecision() //TODO
        {
            if (Decisions.Count == 0)
                return null;
            return Decisions[0];
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
                    //Pick from subjects that have no remaining prerequisits
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
            Console.WriteLine("Ordering Subjects:   " + timer3.ElapsedMilliseconds + "ms");
        }

        //IsLeaf and IsAbove are helper functions for Order()
        private bool IsLeaf(Subject subject, int time, Prerequisite prerequisite = null)
        {
            //Look at the subject's prerequisites
            if (prerequisite == null) prerequisite = subject.Prerequisites;
            //If the prerequisit is met, return true
            if (prerequisite.HasBeenMet(this, time))
                return true;
            //If the prerequisit is an elective and the recommended year has passed, count this as a leaf
            if (prerequisite.IsElective() && subject.GetLevel() <= time / 3 + 1)
                return true;
            //Consider each option
            foreach (Criteria criteria in prerequisite.GetOptions())
                //If the option is a subject that needs to be picked, hasn't been picked, and is not above the current subject: the subject is not a leaf
                if (criteria is Subject && SelectedSubjects.Contains(criteria) && !SelectedSubjectsSoFar().Contains(criteria) && !IsAbove(criteria as Subject, subject))
                    return false;
                //If the option is a prerequisit that is not a leaf then the subject is not a leaf
                else if (criteria is Prerequisite && !IsLeaf(subject, time, criteria as Prerequisite))
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
            }
            return false;
        }

        void RefreshBannedSubjectsList()
        {
            SubjectIsBanned.Clear();
            // Get all selected subjects and check them for NCCWs
            foreach (Subject subject in SelectedSubjects)
                foreach (string id in subject.NCCWs)
                    if (Parser.TryGetSubject(id, out Subject nccw))
                        SubjectIsBanned[nccw] = true;
            // TODO: make sure that all NCCW relations are undirected (looking at you, BIOL2610 - STAT2170)

            // Check which decisions force a banned subject
            foreach (Prerequisite decision in Decisions)
                foreach (Subject subject in decision.ForcedBans())
                    SubjectIsBanned[subject] = true;
        }
    }
}
