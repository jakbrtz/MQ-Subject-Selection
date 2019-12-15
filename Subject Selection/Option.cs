using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    abstract public class Option
    {
        // This superclass allows decisions to be made of other decisions
        public abstract bool HasBeenCompleted(Plan plan, Time requiredCompletionTime);
        public abstract bool HasBeenBanned(Plan plan, int countPrerequisites = 0);
        public abstract Time EarliestCompletionTime(Dictionary<Time, int> MaxSubjects, int countPrerequisites = 0);
    }

    public abstract class Content : Option
    {
        // This superclass is used to represent both Subjects and Courses
        public string ID { get; }
        public string Name { get; }
        public Decision Prerequisites { get; }
        public Decision Corequisites { get; }
        public string[] NCCWs { get; }

        protected Content(string id, string name, string prerequisites, string corequisites, string nccws)
        {
            ID = id;
            Name = name;

            Prerequisites = new Decision(this, prerequisites);
            Corequisites = new Decision(this, corequisites, reasonIsCorequisite: true);
            NCCWs = nccws.Split(new string[] { ", " }, StringSplitOptions.None);
        }

        protected Content(string document)
        {
            Prerequisites = new Decision(this);
            Prerequisites.LoadFromDocument(document, out string name, out string code);
            Corequisites = new Decision(this);
            ID = code;
            Name = name;
            NCCWs = new string[] { "" };
        }

        public override string ToString()
        {
            return ID;
        }

        private int _countPrerequisites_HasBeenBanned = -1;
        public override bool HasBeenBanned(Plan plan, int countPrerequisites)
        {
            // If this subject has been listed as a banned subject by the plan, return true
            if (plan.BannedContents.Contains(this))
                return true;
            /* `_countPrerequisites` acts as a flag to avoid infinite loops (looking at you, BIOL2220 and BIOL2230)
             * When there is a cylce containing prerequisites this should return true
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
    }

    public class Subject : Content
    {
        public int CP { get; }
        public List<OfferTime> Semesters { get; }

        public Subject(string id, string name, string cp, string times, string prerequisites, string corequisites, string nccws) :
            base(id, name, prerequisites, corequisites, nccws)
        {
            CP = int.Parse(cp);

            Semesters = new List<OfferTime>();
            foreach (string time in times.Split('\n'))
                if (OfferTime.TryParse(time, out OfferTime result))
                    Semesters.Add(result);

            // If no semesters are found, add both semesters
            if (!Semesters.Any())
            {
                if (OfferTime.TryParse("S1 Day", out OfferTime result1))
                    Semesters.Add(result1);
                if (OfferTime.TryParse("S1 Day", out OfferTime result2))
                    Semesters.Add(result2);
            }
        }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            if (!plan.SelectedSubjects.Contains(this)) return false;
            return plan.SelectedSubjectsSoFar(requiredCompletionTime).Contains(this);
        }

        public Time GetChosenTime(Plan plan)
        {
            return plan.SubjectsInOrder.First(semester => semester.Value.Contains(this)).Key;
        }

        int _countPrerequisites_EarliestCompletionTime = -1;
        public override Time EarliestCompletionTime(Dictionary<Time, int> MaxSubjects, int countPrerequisites = 0)
        {
            /* `_countPrerequisites` acts as a flag to avoid infinite loops (looking at you, EDTE4040 and EDTE4560)
             * When there is a cylce containing prerequisites this should return 100
             * This flag needs to be an integer (not a bool) because sometimes there is a cycle of corequisites that are also a prequisite to another subject
             */
            if (_countPrerequisites_EarliestCompletionTime > -1)
                return countPrerequisites > _countPrerequisites_EarliestCompletionTime ? Time.Impossible : Time.Early;
            // Set the flag
            _countPrerequisites_EarliestCompletionTime = countPrerequisites;
            // Find the first time after the prerequisites has been satisfied
            Time timePrerequisites = Prerequisites.EarliestCompletionTime(MaxSubjects, countPrerequisites + 1).Next();
            // Find the first time that the corequisites has been satisfied
            Time timeCorequisites = Corequisites.EarliestCompletionTime(MaxSubjects, countPrerequisites);
            // Pick the later of these times
            Time time = timeCorequisites.IsEarlierThan(timePrerequisites) ? timePrerequisites : timeCorequisites;
            // Make sure this value is not negative
            if (time.year == 0) time = time.Next();
            // Find a semester that this subject could happen in
            while (!Semesters.Any(semester => semester.session == time.session)) time = time.Next();
            // Unset the flag
            _countPrerequisites_EarliestCompletionTime = -1;
            return time;
        }

        private List<Time> possibleTimes;
        public List<Time> GetPossibleTimes(Dictionary<Time, int> MaxSubjects)
        {
            if (possibleTimes != null) return possibleTimes;
            possibleTimes = new List<Time>();
            for (Time time = EarliestCompletionTime(MaxSubjects); time.year <= MaxSubjects.Count/6; time = time.Next())
                foreach (OfferTime offerTime in Semesters.Where(offerTime => offerTime.session == time.session))
                    possibleTimes.Add(new Time { year = time.year, session = offerTime.session });
            return possibleTimes;
        }
    }

    public class Course : Content
    {
        public Course(string document) :
            base(document)
        {

        }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            return plan.SelectedCourses.Contains(this);
        }

        public override Time EarliestCompletionTime(Dictionary<Time, int> MaxSubjects, int countPrerequisites = 0)
        {
            return Time.Impossible;
        }
    }

    public partial class Decision : Option
    {
        List<Content> reasonsPrerequisite = new List<Content>();
        List<Content> reasonsCorequisite = new List<Content>();
        string description;
        List<Option> options;
        int pick;
        Selection selectionType;

        public Decision(Option reason, string description = "", List<Option> options = null, int pick = 1, Selection selectionType = Selection.OR, bool reasonIsCorequisite = false)
        {
            if (reason is Content content)
            {
                if (reasonIsCorequisite)
                    reasonsCorequisite.Add(content);
                else
                    reasonsPrerequisite.Add(content);
            }
            else if (reason is Decision decision)
            {
                reasonsPrerequisite.AddRange(decision.GetReasonsPrerequisite());
                reasonsCorequisite.AddRange(decision.GetReasonsCorequisite());
            }
            this.description = description;
            this.options = options;
            this.pick = pick;
            this.selectionType = selectionType;

            ToString();
        }

        public List<Option> Options
        {
            get
            {
                if (options == null)
                    LoadFromDescription();
                return options;
            }
        }

        public int Pick
        {
            get
            {
                if (options == null)
                    LoadFromDescription();
                return pick;
            }
        }

        public Selection SelectionType
        {
            get
            {
                if (options == null)
                    LoadFromDescription();
                return selectionType;
            }
        }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            // An "empty decision" (pick 0) is automatically met
            if (Pick == 0)
                return true;
            // Check the study plan for the earliest subject that requires this decision
            if (requiredCompletionTime.year == 0) requiredCompletionTime = RequiredCompletionTime(plan);
            // Recursively count the number of options that have been met, compare it to the number of options that need to be met
            // This could be done in one line of LINQ, but this version of the code excecutes faster
            // return Pick <= Options.Count(option => option.HasBeenMet(plan, time));
            int countMetOptions = 0;
            foreach (Option option in Options)
            {
                if (option.HasBeenCompleted(plan, requiredCompletionTime))
                {
                    countMetOptions++;
                    if (countMetOptions >= Pick)
                        return true;
                }
            }
            return false;
        }

        public override bool HasBeenBanned(Plan plan, int countPrerequisites)
        {
            // This function could be determined in a single line, but it would be inefficient:
            // return Options.Count(option => !option.HasBeenBanned(plan, cyclesNotAllowed)) < GetPick(plan);

            // This is a simple catch to check for bans without checking recursively
            if (Pick > Options.Count)
                return true;
            // Assume electives cannot be banned
            if (IsElective()) return false;
            // If there is nothing to pick from, it cannot be banned
            if (Pick <= 0)
                return false;
            // This compares the number of options that can be picked with the number of options that need to be picked
            int countRemainingOptions = 0;
            foreach (Option option in Options)
            {
                if (!option.HasBeenBanned(plan, countPrerequisites))
                {
                    countRemainingOptions++;
                    if (countRemainingOptions == Pick)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<Content> ForcedBans()
        {
            // For each option, check if it forces a subject to be banned
            // Count how often a subject is banned by an option
            // If it gets banned by too many options, then it gets banned by the entire decision

            Dictionary<Content, int> counts = new Dictionary<Content, int>();
            foreach (Option option in Options)
            {
                if (option is Content)
                {
                    foreach (string ID in (option as Content).NCCWs)
                    {
                        Content content = Parser.GetSubject(ID);
                        if (content == null) continue;
                        if (!counts.ContainsKey(content))
                            counts[content] = 0;
                        counts[content]++;
                    }
                }
                else if (option is Decision)
                {
                    foreach (Content subject in (option as Decision).ForcedBans())
                    {
                        if (!counts.ContainsKey(subject))
                            counts[subject] = 0;
                        counts[subject]++;
                    }
                }
            }

            return counts.Where(kvp => Options.Count - Pick < kvp.Value).Select(kvp => kvp.Key).ToList();
        }

        public Decision GetRemainingDecision(Plan plan)
        {
            // Figure out when this decision must be completed
            Time requiredCompletionTime = RequiredCompletionTime(plan);
            // If the decision is met then there should be nothing to return
            if (HasBeenCompleted(plan, requiredCompletionTime)) return new Decision(this);
            // Only select the options that can be picked
            List<Option> remainingOptions = Options.Where(option => !option.HasBeenCompleted(plan, requiredCompletionTime) && !option.HasBeenBanned(plan)).ToList();
            //If there is only one remaining option to pick from then pick it
            if (remainingOptions.Count == 1)
            {
                Option lastOption = remainingOptions.First();
                if (lastOption is Decision lastDecision)
                    return lastDecision.GetRemainingDecision(plan);
            }
            // Figure out how many options still need to be picked
            int remainingPick = Pick - Options.Count(option => option.HasBeenCompleted(plan, requiredCompletionTime));
            // Create a new list to store the remaining options
            List<Option> optionBuilder = new List<Option>();
            foreach (Option option in remainingOptions)
            {
                if (option is Content)
                    optionBuilder.Add(option);
                else if (option is Decision decision)
                {
                    Decision remainingDecision = decision.GetRemainingDecision(plan);
                    if (remainingPick == 1 && remainingDecision.Pick == 1)
                        optionBuilder.AddRange(remainingDecision.Options);
                    else
                        optionBuilder.Add(remainingDecision);
                }
            }
            string newDescription = "";
            if (selectionType == Selection.CP)
                newDescription = CopyDescription(remainingPick);
            return new Decision(this, newDescription, optionBuilder, remainingPick, selectionType);
        }

        public override Time EarliestCompletionTime(Dictionary<Time, int> MaxSubjects, int countPrerequisites)
        {
            // If it weren't for cyclic prerequisites and electives, this function could be done in a single line:
            // return Options.ConvertAll(option => option.EarliestCompletionTime(MaxSubjects)).OrderBy(x => x).ElementAt(Pick - 1);

            // It is assumed that this method is only called by a Subject referring to it's requisites, or a Decision referring to it's options

            // Some prerequisites have been parsed incorrectly so they are automatically banned
            if (Options.Count < Pick)
                return Time.Impossible;
            // If there are no options, then the subject can be done straight away
            if (Options.Count == 0)
                return Time.Early;

            // Find the lowest useful result of this function
            Time lowerBound = Time.Early;
            // If this is a corequisite, then the lower bound can increase by 1
            if (!GetReasonsPrerequisite().Any())
                lowerBound = lowerBound.Next();
            // Increase the lower bound according to the level of the subjects that is the reason
            for (int i = 1; i < GetReasons().Max(reason => reason.GetLevel()); i++)
                lowerBound = lowerBound.Next();
            if (Options.All(option => option is Subject))
            {
                // If every option is a decision, then the EarliestCompletionTime cannot be negative
                if (lowerBound.year == 0)
                    lowerBound = lowerBound.Next(); ;
                // Find the lower bound by filling up the plan with random subjects (useful for electives)
                int countSubjects = 0;
                Time otherLowerBound = Time.Early;
                while (countSubjects < Pick)
                {
                    otherLowerBound = otherLowerBound.Next();
                    countSubjects += MaxSubjects[otherLowerBound];
                }
                // Pick the larger of these lower bounds
                if (lowerBound.IsEarlierThan(otherLowerBound))
                    lowerBound = otherLowerBound;
            }
            // If the decision is an elective, don't bother looking at its options
            if (IsElective())
                return lowerBound;

            /* This algorithm is similar to "get the smallest number in an array", with some differences:
             * it is trying to get the kth smallest number, so it keeps track of the smallest k numbers, where k equals Pick
             * each value in the array needs to be calculated (this is a recursive function)
             * this checks if the recursive call is more complex than it should be
             * it is known that the smallest possible answer is -1 or 0, so the calculation ends early when that answer has been found
             */

            // Prepare an array to store the smallest values
            Time[] earliestTimes = new Time[Pick];
            for (int i = 0; i < earliestTimes.Length; i++)
                earliestTimes[i] = Time.Impossible;
            // Iterate through the options to find their earliest times
            foreach (Option option in Options)
            {
                Time time = Time.Impossible;
                // If the option is a subject who's prerequisite is more complex then the current decision, then don't bother evaluating it
                if (!(option is Content content && content.Prerequisites.Covers(this)))
                    time = option.EarliestCompletionTime(MaxSubjects, countPrerequisites);
                // Insert the time into the array
                int i = earliestTimes.Length;
                for (i = earliestTimes.Length; i > 0 && time.IsEarlierThan(earliestTimes[i - 1]); i--)
                    if (i != earliestTimes.Length)
                        earliestTimes[i] = earliestTimes[i - 1];
                if (i != earliestTimes.Length)
                    earliestTimes[i] = time;
                // If the calculated solution so far is the lower bound, stop looking for a solution
                if (earliestTimes.Last().IsEarlierThanOrAtTheSameTime(lowerBound))
                    break;
            }
            // Select the last item from earliestTimes
            return earliestTimes.Last();
        }

        public Time RequiredCompletionTime(Plan plan)
        {
            Time requiredByPrerequisites = reasonsPrerequisite.Any(reason => reason is Subject) ? reasonsPrerequisite.OfType<Subject>().Min(reason => reason.GetChosenTime(plan)).Previous() : Time.Impossible;
            Time requiredByCorequisites = reasonsCorequisite.Any(reason => reason is Subject) ? reasonsCorequisite.OfType<Subject>().Min(reason => reason.GetChosenTime(plan)) : Time.Impossible;
            // Return the earlier out of those two values
            if (requiredByPrerequisites.IsEarlierThan(requiredByCorequisites))
                return requiredByPrerequisites;
            return requiredByCorequisites;
        }

        public void AddReasons(Decision decision)
        {
            reasonsPrerequisite = reasonsPrerequisite.Union(decision.reasonsPrerequisite).ToList();
            reasonsCorequisite = reasonsCorequisite.Union(decision.reasonsCorequisite).ToList();
        }

        public List<Content> GetReasonsPrerequisite()
        {
            return reasonsPrerequisite;
        }

        public List<Content> GetReasonsCorequisite()
        {
            return reasonsCorequisite;
        }

        public IEnumerable<Content> GetReasons()
        {
            return reasonsPrerequisite.Concat(reasonsCorequisite);
        }
    }

    public enum Selection { AND, OR, CP }
}
