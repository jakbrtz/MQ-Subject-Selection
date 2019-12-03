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

        public void RemoveSubject(Subject subject)
        {
            if (subject.IsSubject)
                SelectedSubjects.Remove(subject);
            else
                SelectedCourses.Remove(subject);
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

        public IEnumerable<Subject> SelectedSubjectsSoFar(int time = 100)
        {
            return SubjectsInOrder.Take(time + 1).SelectMany(x => x);
        }

        public void Order()
        {
            Stopwatch timer3 = new Stopwatch();
            timer3.Start();

            SubjectsInOrder.Clear();
            for (int session = 0; session < MaxSubjects.Count; session++)
            {
                // Create a new semester and add it to SubjectsInOrder
                List<Subject> semester = new List<Subject>();
                SubjectsInOrder.Add(semester);
                // Fill the semester with subjects that can be chosen
                for (int subjectNumber = 0; subjectNumber < MaxSubjects[session]; subjectNumber++)
                {
                    // Prepare a list of what subjects could be chosen
                    IEnumerable<Subject> possibleSubjects = SelectedSubjects.Except(SelectedSubjectsSoFar());
                    // Do not pick subjects with forced times later than the current session
                    possibleSubjects = possibleSubjects.Where(subject => !(forcedTimes.ContainsKey(subject) && forcedTimes[subject] > session));
                    // Pick from subjects that are allowed during this semester
                    possibleSubjects = possibleSubjects.Where(subject => subject.GetPossibleTimes(MaxSubjects).Contains(session));
                    // Pick from subjects that have satisfied requisites
                    possibleSubjects = possibleSubjects.Where(subject => IsLeaf(subject, session));
                    // Favor lower level subjects
                    possibleSubjects = possibleSubjects.OrderBy(subject => subject.GetLevel());
                    // Favor subjects that have many other subjects relying on them
                    possibleSubjects = possibleSubjects.OrderByDescending(subject => SelectedSubjects.Except(SelectedSubjectsSoFar()).Count(other => IsAbove(parent: other, child: subject, includeElectives: true)));
                    // If any subjects are forced, filter them
                    IEnumerable<Subject> forcedSubjects = possibleSubjects
                        .Where(subject => forcedTimes.ContainsKey(subject) && forcedTimes[subject] <= session)
                        .OrderBy(subject => forcedTimes[subject]);
                    if (forcedSubjects.Any())
                        possibleSubjects = forcedSubjects;
                    // Pick the first item from that list
                    Subject nextSubject = possibleSubjects.FirstOrDefault();
                    // If no subject was chosen, go to the next semester
                    if (nextSubject == null) break;
                    // Add the selected subject to this semester
                    semester.Add(nextSubject);
                }
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
                if (option is Subject && SelectedSubjects.Contains(option) && !SelectedSubjectsSoFar().Contains(option) && !IsAbove(option as Subject, subject, false))
                    return false;
                //If the option is a decision that does not lead to a leaf then the subject is not a leaf
                else if (option is Decision && !IsLeaf(subject, time, option as Decision))
                    return false;
            return true;
        }

        private bool IsAbove(Subject parent, Subject child, bool includeElectives)
        {
            //Creates a list of all subjects that are selected and are descendants of this subject
            HashSet<Subject> descendants = new HashSet<Subject>();
            Queue<Subject> subjectsToAnalyze = new Queue<Subject>();
            subjectsToAnalyze.Enqueue(parent);
            while (subjectsToAnalyze.Any())
            {
                Subject currentSubject = subjectsToAnalyze.Dequeue();
                if (currentSubject == child) return true;
                descendants.Add(currentSubject);
                // TODO: what should the result be when it's an elective?
                
                Queue<Decision> decisionsToAnalyze = new Queue<Decision>();
                decisionsToAnalyze.Enqueue(currentSubject.Prerequisites);
                decisionsToAnalyze.Enqueue(currentSubject.Corequisites);
                while(decisionsToAnalyze.Any())
                {
                    Decision currentDecision = decisionsToAnalyze.Dequeue();
                    foreach (Option option in currentDecision.GetOptions())
                        if (option is Subject && SelectedSubjects.Contains(option as Subject) && !descendants.Contains(option as Subject))
                            subjectsToAnalyze.Enqueue(option as Subject);
                        else if (option is Decision && (includeElectives || !(option as Decision).IsElective()))
                            decisionsToAnalyze.Enqueue(option as Decision);
                }
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
