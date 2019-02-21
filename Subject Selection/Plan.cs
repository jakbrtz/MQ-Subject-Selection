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
        public List<Prerequisit> Decisions { get; }
        public List<Subject> SelectedSubjects { get; }
        Dictionary<Subject, int> forcedTimes = new Dictionary<Subject, int>();
        public List<int> MaxSubjects { get; }

        public Plan()
        {
            SubjectsInOrder = new List<List<Subject>>();
            Decisions = new List<Prerequisit>();
            SelectedSubjects = new List<Subject>();
            MaxSubjects = new List<int>();
        }
        
        public Plan(Plan other)
        {
            SubjectsInOrder = other.SubjectsInOrder.Select(x => x.ToList()).ToList();
            Decisions = new List<Prerequisit>(other.Decisions);
            SelectedSubjects = new List<Subject>(other.SelectedSubjects);
            MaxSubjects = new List<int>(other.MaxSubjects);
        }

        public void AddSubject(Subject subject)
        {
            SelectedSubjects.Add(subject);
            Order();
            //TODO: don't reorder entire thing every time a subject is added
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

            int session = 0;
            SubjectsInOrder.Clear();
            while (SelectedSubjects.Except(SelectedSubjectsSoFar()).Any())
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
                    //Favour subjects forced into this timeslot
                    .Except(SelectedSubjects.Where(subject => forcedTimes.ContainsKey(subject) && forcedTimes[subject] > session))
                    .OrderBy(subject => forcedTimes.ContainsKey(subject) ? forcedTimes[subject] : MaxSubjects.Count)
                    //Don't select more subjects than is allowed
                    .Take(MaxSubjects[session])));
                //TODO: include levels of how 'forced' a time is

                session = session + 1;
            }
            
            timer3.Stop();
            Console.WriteLine("Ordering Subjects:   " + timer3.ElapsedMilliseconds + "ms");
        }

        //IsLeaf and IsAbove are helper functions for Order()
        private bool IsLeaf(Subject subject, int time, Prerequisit prerequisit = null)
        {
            //Look at the subject's prerequisits
            if (prerequisit == null) prerequisit = subject.Prerequisits;
            //If the prerequisit is met, return true
            if (prerequisit.HasBeenMet(this, time))
                return true;
            //If the prerequisit is an elective and the recommended year has passed, count this as a leaf
            if (prerequisit.IsElective() && subject.GetLevel() <= time/3 + 1)
                return true;
            //Consider each option
            foreach (Criteria criteria in prerequisit.GetOptions())
                //If the option is a subject that needs to be picked, hasn't been picked, and is not above the current subject: the subject is not a leaf
                if (criteria is Subject && SelectedSubjects.Contains(criteria) && !SelectedSubjectsSoFar().Contains(criteria) && !IsAbove(criteria as Subject, subject))
                    return false;
                //If the option is a prerequisit that is not a leaf then the subject is not a leaf
                else if (criteria is Prerequisit && !IsLeaf(subject, time, criteria as Prerequisit))
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
                foreach (Subject subject in current.Prerequisits.GetSubjects().Intersect(SelectedSubjects).Except(descendants))
                    toAnalyze.Enqueue(subject);
            }
            return false;
        }
    }
}
