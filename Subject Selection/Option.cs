using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    abstract public class Option
    {
        //I have made this superclass to allow decisions to be made of other decisions
        public abstract bool HasBeenCompleted(Plan plan, int requiredCompletionTime);
        public abstract bool HasBeenBanned(Plan plan, bool cyclesNotAllowed = false);
        public abstract int EarliestCompletionTime(List<int> MaxSubjects, bool cyclesNotAllowed = false);
    }

    public class Subject : Option
    {
        public string ID { get; }
        public string Name { get; }
        public List<int> Semesters { get; }
        public Decision Prerequisites { get; }
        public Decision Corequisites { get; }
        public string[] NCCWs { get; }
        public bool IsSubject { get; }

        public Subject(string id, string name, string times, string prerequisites, string corequisites, string nccws)
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

            Prerequisites = new Decision(this, prerequisites);
            Corequisites = new Decision(this, corequisites, reasonIsCorequisite: true);
            NCCWs = nccws.Split(new string[] { ", " }, StringSplitOptions.None);

            IsSubject = true;
        }

        public Subject(string document)
        {
            Prerequisites = new Decision(this);
            Prerequisites.LoadFromDocument(document, out string name, out string code);
            Corequisites = new Decision(this);
            ID = code;
            Name = name;
            Semesters = new List<int> { 2 };
            NCCWs = new string[1];
            IsSubject = false;
        }

        public override string ToString()
        {
            return ID;
        }

        public override bool HasBeenCompleted(Plan plan, int requiredCompletionTime)
        {
            if (!plan.Contains(this)) return false;
            if (IsSubject)
                return plan.SelectedSubjectsSoFar(requiredCompletionTime).Contains(this);
            return true;
        }

        private bool checkingForBan = false;
        public override bool HasBeenBanned(Plan plan, bool cyclesNotAllowed)
        {
            if (plan.BannedSubjects.Contains(this))
                return true;
            // The `checkingForBan` flag is used to avoid infinite loops from with cyclic prerequisites (looking at you, BIOL2220 and BIOL2230) and cyclic corequisites
            if (checkingForBan)
                return cyclesNotAllowed;
            checkingForBan = true;
            // A subject can be banned if it's prerequisites or corequisites are banned
            bool result = Prerequisites.HasBeenBanned(plan, true) || Corequisites.HasBeenBanned(plan, cyclesNotAllowed);
            checkingForBan = false;
            return result;
        }

        bool checkingForEarliestTime = false;
        public override int EarliestCompletionTime(List<int> MaxSubjects, bool cyclesNotAllowed = false)
        {
            if (ID == "BIOL3450")
                Console.WriteLine("here");

            // The `checkingForTime` flag is used to avoid infinite loops from with cyclic prerequisites (looking at you, BIOL2220 and BIOL2230) and cyclic corequisites (EDST4040)
            if (checkingForEarliestTime)
            {
                if (ID == "BIOL3450")
                    Console.WriteLine("here");
                return cyclesNotAllowed ? 100 : -1;
            }
            checkingForEarliestTime = true;
            // Find the first time after the prerequisites has been satisfied
            int timePrerequisites = Prerequisites.EarliestCompletionTime(MaxSubjects, true) + 1;
            // Find the first time that the corequisites has been satisfied
            int timeCorequisites = Corequisites.EarliestCompletionTime(MaxSubjects, cyclesNotAllowed);
            // Pick the later of these times
            int time = timePrerequisites > timeCorequisites ? timePrerequisites : timeCorequisites;
            // Make sure this value is not negative
            if (time < 0) time = 0;
            // Find a semester that this subject could happen in
            while (!Semesters.Contains(time % 3)) time++; //TODO %6 (3 new semesters)
            checkingForEarliestTime = false;

            if (ID == "BIOL3450")
                Console.WriteLine("here");

            return time;
        }

        private List<int> possibleTimes;
        public List<int> GetPossibleTimes(List<int> MaxSubjects)
        {
            if (possibleTimes != null) return possibleTimes;
            possibleTimes = new List<int>();
            for (int time = EarliestCompletionTime(MaxSubjects); time < MaxSubjects.Count; time++)
                if (Semesters.Contains(time % 3))
                    possibleTimes.Add(time);
            return possibleTimes;
        }

        public int GetChosenTime(Plan plan)
        {
            if (!IsSubject)
                return 100;
            return plan.SubjectsInOrder.FindIndex(semester => semester.Contains(this));
        }
    }

    public partial class Decision : Option
    {
        List<Subject> reasonsPrerequisite = new List<Subject>();
        List<Subject> reasonsCorequisite = new List<Subject>();
        string description;
        List<Option> options;
        int pick;
        Selection selectionType;

        public Decision(Option reason, string description = "", List<Option> options = null, int pick = 1, Selection selectionType = Selection.OR, bool reasonIsCorequisite = false)
        {
            if (reason is Subject)
            {
                if (reasonIsCorequisite)
                    reasonsCorequisite.Add(reason as Subject);
                else
                    reasonsPrerequisite.Add(reason as Subject);
            }
            else if (reason is Decision)
            {
                reasonsPrerequisite.AddRange((reason as Decision).GetReasonsPrerequisite());
                reasonsCorequisite.AddRange((reason as Decision).GetReasonsCorequisite());
            }
            this.description = description;
            this.options = options;
            this.pick = pick;
            this.selectionType = selectionType;

            ToString();
        }

        public List<Option> GetOptions()
        {
            if (options == null)
                LoadFromDescription();
            return options;
        }

        public int GetPick()
        {
            if (options == null)
                LoadFromDescription();
            return pick;
        }

        public Selection GetSelectionType()
        {
            if (options == null)
                LoadFromDescription();
            return selectionType;
        }

        public override bool HasBeenCompleted(Plan plan, int requiredCompletionTime)
        {
            // An "empty decision" (pick 0) is automatically met
            if (GetPick() == 0)
                return true;
            // Check the study plan for the earliest subject that requires this decision
            if (requiredCompletionTime == -1) requiredCompletionTime = RequiredCompletionTime(plan);
            // Recursively count the number of options that have been met, compare it to the number of options that need to be met
            // This could be done in one line of LINQ, but this version of the code excecutes faster
            // return GetPick() <= GetOptions().Count(option => option.HasBeenMet(plan, time));
            int countMetOptions = 0;
            foreach (Option option in GetOptions())
            {
                if (option.HasBeenCompleted(plan, requiredCompletionTime))
                {
                    countMetOptions++;
                    if (countMetOptions >= GetPick())
                        return true;
                }
            }
            return false;
        }

        public override bool HasBeenBanned(Plan plan, bool cyclesNotAllowed)
        {
            // This function could be determined in a single line, but it would be inefficient:
            // return GetOptions().Count(option => !option.HasBeenBanned(plan, cyclesNotAllowed)) < GetPick(plan);

            // Assume electives cannot be banned
            if (IsElective()) return false;
            // If there is nothing to pick from, it cannot be banned
            if (GetPick() <= 0)
                return false;
            // This is a simple catch to check for bans without checking recursively
            if (GetPick() > GetOptions().Count)
                return true;
            // This compares the number of options that can be picked with the number of options that need to be picked
            int countRemainingOptions = 0;
            foreach (Option option in GetOptions())
            {
                if (!option.HasBeenBanned(plan, cyclesNotAllowed))
                {
                    countRemainingOptions++;
                    if (countRemainingOptions == GetPick())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<Subject> ForcedBans()
        {
            // For each option, check if it forces a subject to be banned
            // Count how often a subject is banned by an option
            // If it gets banned by too many options, then it gets banned by the entire decision

            Dictionary<Subject, int> counts = new Dictionary<Subject, int>();
            foreach (Option option in GetOptions())
            {
                if (option is Subject)
                {
                    foreach (string ID in (option as Subject).NCCWs)
                    {
                        Subject subject = Parser.GetSubject(ID);
                        if (subject == null) continue;
                        if (!counts.ContainsKey(subject))
                            counts[subject] = 0;
                        counts[subject]++;
                    }
                }
                else if (option is Decision)
                {
                    foreach (Subject subject in (option as Decision).ForcedBans())
                    {
                        if (!counts.ContainsKey(subject))
                            counts[subject] = 0;
                        counts[subject]++;
                    }
                }
            }

            return counts.Where(kvp => GetOptions().Count - GetPick() < kvp.Value).Select(kvp => kvp.Key).ToList();
        }

        public List<Subject> GetSubjects(bool includeElectives = true)
        {
            List<Subject> output = new List<Subject>();
            foreach (Option option in GetOptions())
                if (option is Subject)
                    output.Add(option as Subject);
                else if (option is Decision && (includeElectives || !(option as Decision).IsElective()))
                    output.AddRange((option as Decision).GetSubjects());
            return output;
        }

        public Decision GetRemainingDecision(Plan plan)
        {
            // Figure out when this decision must be completed
            int requiredCompletionTime = RequiredCompletionTime(plan);
            // If the decision is met then there should be nothing to return
            if (HasBeenCompleted(plan, requiredCompletionTime)) return new Decision(this);
            // Only select the options that can be picked
            List<Option> remainingOptions = GetOptions().Where(option => !option.HasBeenCompleted(plan, requiredCompletionTime) && !option.HasBeenBanned(plan)).ToList();
            //If there is only one remaining option to pick from then pick it
            if (remainingOptions.Count == 1)
            {
                Option lastOption = remainingOptions.First();
                if (lastOption is Decision)
                    return (lastOption as Decision).GetRemainingDecision(plan);
            }
            // Figure out how many options still need to be picked
            int remainingPick = GetPick() - GetOptions().Count(option => option.HasBeenCompleted(plan, requiredCompletionTime));
            // Create a new list to store the remaining options
            List<Option> optionBuilder = new List<Option>();
            foreach (Option option in remainingOptions)
            {
                if (option is Subject)
                    optionBuilder.Add(option);
                else if (option is Decision)
                {
                    Decision remainingDecision = (option as Decision).GetRemainingDecision(plan);
                    if (remainingPick == 1 && remainingDecision.GetPick() == 1)
                        optionBuilder.AddRange(remainingDecision.GetOptions());
                    else
                        optionBuilder.Add(remainingDecision);
                }
            }
            string newDescription = "";
            if (selectionType == Selection.CP)
                newDescription = CopyDescription(remainingPick);
            return new Decision(this, newDescription, optionBuilder, remainingPick, selectionType);
        }

        public override int EarliestCompletionTime(List<int> MaxSubjects, bool cyclesNotAllowed)
        {
            // Some prerequisites have been parsed incorrectly so they are automatically banned
            if (GetOptions().Count < GetPick())
                return 100;
            // If there are no options, then the subject can be done straight away
            if (GetOptions().Count == 0)
                return -1;
            // If the decision is an elective, don't bother looking at its options
            if (IsElective())
            {
                int count = 0;
                int time = -1;
                while (count < GetPick())
                {
                    time++;
                    count += MaxSubjects[time];
                }
                return time;
            }

            /* This algorithm is similar to "get the smallest number in an array", with some differences:
             * it is trying to get the kth smallest number, so it keeps track of the smallest k numbers. (k = pick)
             * each value in the array needs to be calculated, and the calculation ends early if it will return a value larger than the kth smallest number
             */

            // Prepare an array to store the smallest values
            int[] earliestTimes = new int[GetPick()];
            for (int i = 0; i < earliestTimes.Length; i++)
                earliestTimes[i] = 100;
            // Iterate through the options to find their earliest times
            foreach (Option option in GetOptions())
            {
                int time = option.EarliestCompletionTime(MaxSubjects, cyclesNotAllowed);
                int i = earliestTimes.Length;
                for (i = earliestTimes.Length; i > 0 && earliestTimes[i - 1] > time; i--)
                    if (i != earliestTimes.Length)
                        earliestTimes[i] = earliestTimes[i - 1];
                if (i != earliestTimes.Length)
                    earliestTimes[i] = time;
            }
            // Select the last item from earliestTimes
            return earliestTimes.Last();

            //Get a list of all the option's earliest completion times

            return GetOptions().ConvertAll(option => option.EarliestCompletionTime(MaxSubjects, cyclesNotAllowed))
                .OrderBy(x => x).ElementAt(GetPick() - 1);
        }

        public int RequiredCompletionTime(Plan plan)
        {
            int requiredByPrerequisites = reasonsPrerequisite.Any() ? reasonsPrerequisite.Min(reason => reason.GetChosenTime(plan)) - 1 : 100;
            int requiredByCorequisites = reasonsCorequisite.Any() ? reasonsCorequisite.Min(reason => reason.GetChosenTime(plan)) : 100;
            if (requiredByPrerequisites < requiredByCorequisites)
                return requiredByPrerequisites;
            return requiredByCorequisites;
        }

        public void AddReasons(Decision decision)
        {
            reasonsPrerequisite = reasonsPrerequisite.Union(decision.reasonsPrerequisite).ToList();
            reasonsCorequisite = reasonsCorequisite.Union(decision.reasonsCorequisite).ToList();
        }

        public List<Subject> GetReasonsPrerequisite()
        {
            return reasonsPrerequisite;
        }

        public List<Subject> GetReasonsCorequisite()
        {
            return reasonsCorequisite;
        }

        public IEnumerable<Subject> GetReasons()
        {
            return reasonsPrerequisite.Concat(reasonsCorequisite);
        }
    }

    public enum Selection { AND, OR, CP }
}
