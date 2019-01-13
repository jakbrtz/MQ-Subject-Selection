using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    public class Plan
    {
        public List<List<Subject>> SubjectsInOrder { get; }
        public List<Prerequisit> Decisions { get; }
        public List<Subject> SelectedSubjects { get; }
        Dictionary<Subject, int> forcedTimes = new Dictionary<Subject, int>();

        public Plan()
        {
            SubjectsInOrder = new List<List<Subject>>();
            Decisions = new List<Prerequisit>();
            SelectedSubjects = new List<Subject>();
        }
        
        public Plan(Plan other)
        {
            SubjectsInOrder = other.SubjectsInOrder.Select(x => x.ToList()).ToList();
            Decisions = new List<Prerequisit>(other.Decisions);
            SelectedSubjects = new List<Subject>(other.SelectedSubjects);
        }

        public void AddSubject(Subject subject)
        {
            SelectedSubjects.Add(subject);
            Order();
        }

        public void ForceTime(Subject subject, int time)
        {
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
            int session = 1;
            SubjectsInOrder.Clear();
            while (SelectedSubjects.Except(SelectedSubjectsSoFar()).Any())
            {
                //Only select subjects which have not been selected yet
                SubjectsInOrder.Add(new List<Subject>(SelectedSubjects
                    .Except(SelectedSubjectsSoFar()).Where(subject =>
                    //Pick from subjects that are allowed during this semester
                    subject.GetPossibleTimes().Contains(session) &&
                    //Pick subjects that are forced into this spot
                    (forcedTimes.ContainsKey(subject) && forcedTimes[subject] == session ||
                    //Pick from subjects that have no remaining prerequisits
                    IsLeaf(subject, session)))
                    //Favour lower level subjects
                    .OrderBy(subject => subject.GetLevel())
                    //Favour subjects forced into this timeslot
                    .Except(SelectedSubjects.Where(subject => forcedTimes.ContainsKey(subject) && forcedTimes[subject] > session))
                    .OrderBy(subject => forcedTimes.ContainsKey(subject) ? forcedTimes[subject] : 100) //TODO: remove magic number
                    //Only select 4 subjects
                    .Take(4)));

                session = session + 1;
            }
        }

        //IsLeaf and IsAbove are helper functions for Order()
        private bool IsLeaf(Subject subject, int time, Prerequisit prerequisit = null)
        {
            //Look at the subject's prerequisits
            if (prerequisit == null) prerequisit = subject.Prerequisits;
            //If the prerequisit is met, return true
            if (prerequisit.HasBeenMet(this, time))
                return true;
            //Consider each option
            foreach (Criteria criteria in prerequisit.GetOptions())
                //If the option needs to be picked, hasn't been picked, and is not above the current subject: the subject is not a leaf
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
