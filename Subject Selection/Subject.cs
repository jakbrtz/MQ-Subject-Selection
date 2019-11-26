using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    abstract public class Criteria
    {
        //I have made this superclass to allow prerequisites so be made of other prerequisites
        public abstract bool HasBeenMet(Plan plan, int time);
        public abstract bool HasBeenBanned(Plan plan);
        public abstract int EarliestCompletionTime(List<int> MaxSubjects);
        public bool CanBePicked(Plan plan, int time) { return !HasBeenMet(plan, time) && !HasBeenBanned(plan); }
    }

    public class Subject : Criteria
    {
        public string ID { get; }
        public string Name { get; }
        public List<int> Semesters { get; }
        public Prerequisite Prerequisites { get; }
        public string[] NCCWs { get; }
        public bool IsSubject { get; }

        public Subject(string id, string name, string times, string prerequisites, string nccws)
        {
            ID = id;
            Name = name;

            Semesters = new List<int>();
            foreach (string time in times.Split('\n'))
                if (time.StartsWith("S"))
                    Semesters.Add(int.Parse(time.Substring(1, 1)) - 1);
            Semesters = Semesters.Distinct().ToList();

            // TODO: parse more types of times
            if (!Semesters.Any())
            {
                Semesters.Add(0);
                Semesters.Add(1);
            }

            Prerequisites = new Prerequisite(this, prerequisites);
            NCCWs = nccws.Split(new string[] { ", " }, StringSplitOptions.None);

            IsSubject = true;
        }

        public Subject(string document)
        {
            Prerequisites = new Prerequisite(this);
            Prerequisites.LoadFromDocument(document, out string name, out string code);
            ID = code;
            Name = name;
            Semesters = new List<int> { 2 };
            NCCWs = new string[0];
            IsSubject = false;
        }

        public override string ToString()
        {
            return ID;
        }

        public override bool HasBeenMet(Plan plan, int time)
        {
            if (!plan.Contains(this)) return false;
            if (IsSubject)
                return plan.SelectedSubjectsSoFar(time).Contains(this);
            return true;
        }

        private bool checkingForBan = false;
        public override bool HasBeenBanned(Plan plan)
        {
            if (plan.BannedSubjects.Contains(this))
                return true;
            // The `checkingForBan` flag is used to avoid problems with cyclic prerequisites (look at you, BIOL2220 and BIOL2230)
            if (checkingForBan)
                return true;
            checkingForBan = true;
            // A subject can be banned if it's prerequisites are banned
            bool result = Prerequisites.HasBeenBanned(plan);
            checkingForBan = false;
            return result;
        }

        public override int EarliestCompletionTime(List<int> MaxSubjects)
        {
            //Find the first time after the prerequisites has been satisfied which also allows for the semester
            int time = Prerequisites.EarliestCompletionTime(MaxSubjects) + 1;
            while (!Semesters.Contains(time % 3)) time++; //TODO %6 (3 new semesters)
            return time;
        }

        public List<int> GetPossibleTimes(Plan plan)
        {
            List<int> output = new List<int>();
            for (int time = EarliestCompletionTime(plan.MaxSubjects); time < plan.MaxSubjects.Count; time++)
                if (Semesters.Contains(time % 3))
                    output.Add(time);
            return output;
        }

        public int GetChosenTime(Plan plan)
        {
            if (IsSubject)
                return 100;
            return plan.SubjectsInOrder.FindIndex(semester => semester.Contains(this));
        }
        
    }

    public partial class Prerequisite : Criteria
    {
        List<Subject> reasons = new List<Subject>();
        string criteria;
        List<Criteria> options;
        int pick;
        Selection selectionType;
        int earliestCompletionTime = -1;

        public Prerequisite(Criteria reason, string criteria = "", List<Criteria> options = null, int pick = 1, Selection selectionType = Selection.OR)
        {
            if (reason is Subject)
                reasons.Add(reason as Subject);
            else if (reason is Prerequisite)
                reasons.AddRange((reason as Prerequisite).GetReasons());
            this.criteria = criteria;
            this.options = options;
            this.pick = pick;
            this.selectionType = selectionType;

            ToString();
        }

        public List<Criteria> GetOptions()
        {
            if (options == null)
                LoadFromCriteria();
            return options;
        }

        public int GetPick()
        {
            if (options == null)
                LoadFromCriteria();
            return pick;
        }

        public Selection GetSelectionType()
        {
            if (options == null)
                LoadFromCriteria();
            return selectionType;
        }

        public List<Criteria> GetRemainingOptions(Plan plan)
        {
            int requiredCompletionTime = RequiredCompletionTime(plan);
            return GetOptions().Where(criteria => criteria.CanBePicked(plan, requiredCompletionTime)).ToList();
        }

        public int GetRemainingPick(Plan plan)
        {
            int requiredCompletionTime = RequiredCompletionTime(plan);
            return GetPick() - GetOptions().Count(criteria => criteria.HasBeenMet(plan, requiredCompletionTime));
        }

        public override bool HasBeenMet(Plan plan, int time)
        {
            // Start by checking the study plan for the earliest subject that requires this decision
            if (time == -1) time = plan.SubjectsInOrder.FindIndex(semester => semester.Intersect(reasons).Any());
            // Recursively count the number of options that have been met
            // This could be done in one line of LINQ, but this version of the code excecutes faster
            int countMetCriteria = 0;
            foreach (Criteria criteria in GetOptions())
            {
                if (criteria.HasBeenMet(plan, time))
                {
                    countMetCriteria++;
                    if (countMetCriteria >= GetPick())
                        return true;
                }
            }
            return false;
        }

        public override bool HasBeenBanned(Plan plan)
        {
            // This function could be determined in a single line, but it would be inefficient:
            // return GetOptions().Count(option => option.CanBePicked(plan, RequiredCompletionTime(plan))) < GetRemainingPick(plan);
            
            // Assume electives cannot be banned
            if (IsElective()) return false;
            // If there is nothing to pick from, it cannot be banned
            int remainingPick = GetRemainingPick(plan);
            if (remainingPick == 0)
                return false;
            // This is a simple catch to check for bans without checking recursively
            if (remainingPick > GetOptions().Count)
                return true;
            // This compares the number of options that can be picked with the number of options that need to be picked
            int requiredCompletionTime = RequiredCompletionTime(plan);
            int countRemainingOptions = 0;
            foreach (Criteria option in GetOptions())
            {
                if (option.CanBePicked(plan, requiredCompletionTime))
                {
                    countRemainingOptions++;
                    if (countRemainingOptions == remainingPick)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<Subject> ForcedBans()
        {
            // For each criteria, check if it forces a subject to be banned
            // Count how often a subject is banned by a criteria
            // If it gets banned by too many criteria, then it gets banned by the entire prerequisite

            Dictionary<Subject, int> counts = new Dictionary<Subject, int>();
            foreach (Criteria criteria in GetOptions())
            {
                if (criteria is Subject)
                {
                    foreach (string ID in (criteria as Subject).NCCWs)
                    {
                        Subject subject = Parser.GetSubject(ID);
                        if (subject == null) continue;
                        if (!counts.ContainsKey(subject))
                            counts[subject] = 0;
                        counts[subject]++;
                    }
                }
                else if (criteria is Prerequisite)
                {
                    foreach (Subject subject in (criteria as Prerequisite).ForcedBans())
                    {
                        if (!counts.ContainsKey(subject))
                            counts[subject] = 0;
                        counts[subject]++;
                    }
                }
            }

            return counts.Where(kvp => GetOptions().Count - GetPick() < kvp.Value).Select(kvp => kvp.Key).ToList();
        }

        public List<Subject> GetSubjects()
        {
            List<Subject> output = new List<Subject>();
            foreach (Criteria option in GetOptions())
                if (option is Subject)
                    output.Add(option as Subject);
                else if (option is Prerequisite)
                    output.AddRange((option as Prerequisite).GetSubjects());
            return output;
        }

        public List<Subject> GetRemainingSubjects(Plan plan)
        {
            List<Subject> output = new List<Subject>();
            foreach (Criteria option in GetRemainingOptions(plan))
                if (option is Subject)
                    output.Add(option as Subject);
                else if (option is Prerequisite)
                    output.AddRange((option as Prerequisite).GetRemainingSubjects(plan));
            return output;
        }

        public bool MustPickAll()
        {
            return GetPick() == GetOptions().Count;
        }

        public Prerequisite GetRemainingDecision(Plan plan)
        {
            //If the prerequisite is met then there should be nothing to return
            if (HasBeenMet(plan, RequiredCompletionTime(plan))) return new Prerequisite(this);
            //If there is only one option to pick from then pick it
            List<Criteria> remainingCriteria = GetRemainingOptions(plan);
            if (remainingCriteria.Count == 1)
            {
                Criteria lastOption = remainingCriteria[0];
                if (lastOption is Prerequisite)
                    return (lastOption as Prerequisite).GetRemainingDecision(plan);
            }
            // Figure out how many options still need to be picked
            int remainingPick = GetRemainingPick(plan);
            //Create a new list to store the remaining prerequisites
            List<Criteria> remainingOptions = new List<Criteria>();
            foreach (Criteria option in remainingCriteria)
            {
                if (option is Subject)
                    remainingOptions.Add(option);
                else if (option is Prerequisite)
                {
                    Prerequisite remainingDecision = (option as Prerequisite).GetRemainingDecision(plan);
                    if (remainingPick == 1 && remainingDecision.GetPick() == 1)
                        remainingOptions.AddRange(remainingDecision.GetOptions());
                    else
                        remainingOptions.Add(remainingDecision);
                }
            }
            string newcriteria = "";
            if (selectionType == Selection.CP)
                newcriteria = CopyCriteria(remainingPick);
            return new Prerequisite(this, newcriteria, remainingOptions, remainingPick, selectionType);
        }

        public override int EarliestCompletionTime(List<int> MaxSubjects)
        {
            if (earliestCompletionTime > -1) return earliestCompletionTime;
            //If there are no prerequisites, then the subject can be done straight away
            if (GetOptions().Count == 0)
                return -1;
            //Lock the value to avoid infinite loops
            earliestCompletionTime = 100;
            //This makes finding the time based on credit points a lot faster
            if (IsElective())
            {
                int count = 0;
                int time = -1;
                while (count < GetPick())
                {
                    time++;
                    count += MaxSubjects[time];
                }
                return earliestCompletionTime = time;
            }
            //cache the result
            return earliestCompletionTime =
                //Get a list of all the option's earliest completion times
                GetOptions().ConvertAll(criteria => criteria.EarliestCompletionTime(MaxSubjects))
                .OrderBy(x => x).ElementAt(GetPick() - 1);
        }

        public int RequiredCompletionTime(Plan plan)
        {
            return reasons.Min(reason => reason.GetChosenTime(plan));
        }

        public void AddReasons(Prerequisite prerequisite)
        {
            reasons = reasons.Union(prerequisite.reasons).ToList();
        }

        public List<Subject> GetReasons()
        {
            return reasons;
        }

        public bool HasElectivePrerequisite()
        {
            return IsElective() || GetOptions().Exists(criteria => criteria is Prerequisite && (criteria as Prerequisite).HasElectivePrerequisite());
        }
    }

    public enum Selection { AND, OR, CP }
}
