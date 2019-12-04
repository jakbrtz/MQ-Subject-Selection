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
        public abstract bool HasBeenBanned(Plan plan, int countPrerequisites = 0);
        public abstract int EarliestCompletionTime(List<int> MaxSubjects, int countPrerequisites = 0);
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

        private int _countPrerequisites_HasBeenBanned = -1;
        public override bool HasBeenBanned(Plan plan, int countPrerequisites)
        {
            // If this subject has been listed as a banned subject by the plan, return true
            if (plan.BannedSubjects.Contains(this))
                return true;
            /* `_countPrerequisites` acts as a flag to avoid infinite loops (looking at you, BIOL2220 and BIOL2230)
             * When there are cyclic prerequisites then this should return true
             * When there are cyclic corequisites then this should return false
             * When there is a cylce containing both prerequisites and corequisites this should return true
             * This flag needs to be an integer (not a bool) because sometimes there is a cycle of corequisites that are also a prequisite to another subject
             */
            if (_countPrerequisites_HasBeenBanned > -1)
                return countPrerequisites > _countPrerequisites_HasBeenBanned;
            // Set the flag
            _countPrerequisites_HasBeenBanned = countPrerequisites;
            // A subject is banned if it's prerequisites or corequisites are banned
            bool result = Prerequisites.HasBeenBanned(plan, countPrerequisites + 1) || Corequisites.HasBeenBanned(plan, countPrerequisites);
            // Unset the flag
            _countPrerequisites_HasBeenBanned = -1;
            return result;
        }

        int _countPrerequisites_EarliestCompletionTime = -1;
        public override int EarliestCompletionTime(List<int> MaxSubjects, int countPrerequisites = 0)
        {
            /* `_countPrerequisites` acts as a flag to avoid infinite loops (looking at you, EDTE4040 and EDTE4560)
             * When there are cyclic prerequisites then this should return 100
             * When there are cyclic corequisites then this should return -1
             * When there is a cylce containing both prerequisites and corequisites this should return 100
             * This flag needs to be an integer (not a bool) because sometimes there is a cycle of corequisites that are also a prequisite to another subject
             */
            if (_countPrerequisites_EarliestCompletionTime > -1)
                return countPrerequisites > _countPrerequisites_EarliestCompletionTime ? 100 : -1;
            // Set the flag
            _countPrerequisites_EarliestCompletionTime = countPrerequisites;
            // Find the first time after the prerequisites has been satisfied
            int timePrerequisites = Prerequisites.EarliestCompletionTime(MaxSubjects, countPrerequisites + 1) + 1;
            // Find the first time that the corequisites has been satisfied
            int timeCorequisites = Corequisites.EarliestCompletionTime(MaxSubjects, countPrerequisites);
            // Pick the later of these times
            int time = timePrerequisites > timeCorequisites ? timePrerequisites : timeCorequisites;
            // Make sure this value is not negative
            if (time < 0) time = 0;
            // Find a semester that this subject could happen in
            while (!Semesters.Contains(time % 3)) time++; //TODO %6 (3 new semesters)
            // Unset the flag
            _countPrerequisites_EarliestCompletionTime = -1;
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

        public override bool HasBeenBanned(Plan plan, int countPrerequisites)
        {
            // This function could be determined in a single line, but it would be inefficient:
            // return GetOptions().Count(option => !option.HasBeenBanned(plan, cyclesNotAllowed)) < GetPick(plan);

            // This is a simple catch to check for bans without checking recursively
            if (GetPick() > GetOptions().Count)
                return true;
            // Assume electives cannot be banned
            if (IsElective()) return false;
            // If there is nothing to pick from, it cannot be banned
            if (GetPick() <= 0)
                return false;
            // This compares the number of options that can be picked with the number of options that need to be picked
            int countRemainingOptions = 0;
            foreach (Option option in GetOptions())
            {
                if (!option.HasBeenBanned(plan, countPrerequisites))
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
                if (option is Subject && (option as Subject).IsSubject)
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

        public override int EarliestCompletionTime(List<int> MaxSubjects, int countPrerequisites)
        {
            // It is assumed that this method is only called by a Subject referring to it's requisites, or a Decision referring to it's options

            // Some prerequisites have been parsed incorrectly so they are automatically banned
            if (GetOptions().Count < GetPick())
                return 100;
            // If there are no options, then the subject can be done straight away
            if (GetOptions().Count == 0)
                return -1;

            // Find the lowest useful result of this function
            int lowerBound = -1;
            // If this is a corequisite, then the lower bound can increase by 1
            if (!GetReasonsPrerequisite().Any())
                lowerBound++;
            // Increase the lower bound according to the level of the subjects that is the reason
            lowerBound += GetReasons().Max(reason => reason.GetLevel()) - 1;
            if (GetOptions().All(option => option is Subject))
            {
                // If every option is a decision, then the EarliestCompletionTime cannot be negative
                if (lowerBound < 0)
                    lowerBound = 0;
                // Find the lower bound by filling up the plan with random subjects (useful for electives)
                int countSubjects = 0;
                int otherLowerBound = -1;
                while (countSubjects < GetPick())
                {
                    otherLowerBound++;
                    countSubjects += MaxSubjects[otherLowerBound];
                }
                // Pick the larger of these lower bounds
                if (lowerBound < otherLowerBound)
                    lowerBound = otherLowerBound;
            }
            // If the decision is an elective, don't bother looking at its options
            if (IsElective())
                return lowerBound;

            /* This algorithm is similar to "get the smallest number in an array", with some differences:
             * it is trying to get the kth smallest number, so it keeps track of the smallest k numbers, where k equals GetPick()
             * each value in the array needs to be calculated (this is a recursive function)
             * this checks if the recursive call is more complex than it should be
             * it is known that the smallest possible answer is -1 or 0, so the calculation ends early when that answer has been found
             */

            // Prepare an array to store the smallest values
            int[] earliestTimes = new int[GetPick()];
            for (int i = 0; i < earliestTimes.Length; i++)
                earliestTimes[i] = 100;
            // Iterate through the options to find their earliest times
            foreach (Option option in GetOptions())
            {
                int time = 100;
                // If the option is a subject who's prerequisite is more complex then the current decision, then don't bother evaluating it
                if (!(option is Subject && (option as Subject).Prerequisites.Covers(this)))
                    time = option.EarliestCompletionTime(MaxSubjects, countPrerequisites);
                // Insert the time into the array
                int i = earliestTimes.Length;
                for (i = earliestTimes.Length; i > 0 && earliestTimes[i - 1] > time; i--)
                    if (i != earliestTimes.Length)
                        earliestTimes[i] = earliestTimes[i - 1];
                if (i != earliestTimes.Length)
                    earliestTimes[i] = time;
                // If the calculated solution so far is the lower bound, stop looking for a solution
                if (earliestTimes.Last() <= lowerBound)
                    break;
            }
            // Select the last item from earliestTimes
            return earliestTimes.Last();
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
