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
        public abstract bool HasSameOptions(Option other);
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

        public override bool HasSameOptions(Option other)
        {
            if (other is Decision decision)
                return decision.Options.Count == 1 && decision.Options.First() == this;
            return this.Equals(other);
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

        public bool Unique()
        {
            // When courses have overlapping units, the same unit cannot count towards both decisions. This makes a decision "Unique"
            // Also, the results process faster and are easier to understand if electives aren't counted as unique
            return GetReasons().Any(content => content is Course) && !IsElective();
        }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            /* This method does not take into account that Unique decisions cannot have overlapping selected subjects
             * This is not a problem, because the function gets called for Unique decisions, all selected subjects should have already been removed
             * Here's a little check to make sure I follow that rule:
             */

            if (Unique() && Options.Any(option => option.HasBeenCompleted(plan, requiredCompletionTime)) && Options.All(option => !(option is Course)))
                throw new FormatException("You're not allowed to run this function on this object");

            // An "empty decision" (pick 0) is automatically met
            if (Pick == 0)
                return true;
            // Recursively count the number of options that have been met, compare it to the number of options that need to be met
            // This could be done in one line of LINQ, but this version of the code excecutes faster
            // return Pick <= Options.Count(option => option.HasBeenCompleted(plan, requiredCompletionTime));
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
            // Create a list containing all the remaining options
            List<Option> remainingOptions = new List<Option>();
            // Keep track of how many options need to be picked
            int remainingPick = Pick;
            // Iterate through all the options
            foreach (Option option in Options)
            {
                if (option is Content)
                {
                    if (option.HasBeenCompleted(plan, requiredCompletionTime))
                    {
                        remainingPick--;
                        if (remainingPick <= 0)
                            return new Decision(this);
                    }
                    else if (!option.HasBeenBanned(plan))
                        remainingOptions.Add(option);
                }
                else if (option is Decision)
                    remainingOptions.Add((option as Decision).GetRemainingDecision(plan));
            }

            string newDescription = selectionType == Selection.CP ? CopyDescription(Pick) : "";
            return new Decision(this, newDescription, remainingOptions, remainingPick, selectionType);
        }

        public Decision GetSimplifiedDecision()
        {
            // If the decision doesn't require anything, then return a blank decision
            if (Pick <= 0) return new Decision(this);
            // Figure out how many options still need to be picked when the blank decisions are removed
            int simplePick = Pick;
            // Create a new list to store the remaining options
            List<Option> simpleOptions = new List<Option>();
            foreach (Option option in Options)
            {
                // Add all content
                if (option is Content)
                    simpleOptions.Add(option);
                else if (option is Decision decision)
                {
                    // Simplify decisions
                    Decision simplifiedOption = decision.GetSimplifiedDecision();
                    // If the decision is impossible, skip it
                    if (simplifiedOption.Pick > simplifiedOption.Options.Count)
                        continue;
                    // If the decision is blank, reduce simplePick, and skip the decision
                    if (simplifiedOption.Pick <= 0)
                        simplePick--;
                    // Otherwise, just add the decision normally
                    else
                        simpleOptions.Add(simplifiedOption);
                }
            }
            // If there is only one remaining option to pick from then return it
            if (simplePick == 1 && simpleOptions.Count == 1 && simpleOptions.First() is Decision onlyDecision)
                return onlyDecision;
            // Figure out what the selection type is
            Selection simpleSelectionType = SelectionType;
            // If only one option needs to be picked, and some of the options are decisions which also only require picking one option, then combine the decisions
            if (simplePick == 1)
            {
                foreach (Decision decision in new List<Option>(simpleOptions).Where(option => option is Decision decision && decision.Pick == 1))
                {
                    simpleOptions.Remove(decision);
                    simpleOptions.AddRange(decision.Options);
                    simpleSelectionType = Selection.OR;
                }
            }
            // If every option needs to be picked, and some of the options are decisions which also require picking all options, then combine the decisions
            if (simplePick == simpleOptions.Count)
            {
                
                foreach (Decision decision in new List<Option>(simpleOptions).Where(option => option is Decision decision && decision.Pick == decision.Options.Count))
                {
                    simpleOptions.Remove(decision);
                    simpleOptions.AddRange(decision.Options);
                    simplePick = simpleOptions.Count;
                    simpleSelectionType = Selection.AND;
                }
            }
            // Special case: try to rearrange it so it's nicer for the user
            if (simplePick == 1 && simpleOptions.Any() && simpleOptions.All(option => option is Decision decision && decision.Pick == decision.Options.Count))
            {
                /* For example, if the decision is "(A and B) or (A and C)" turn it into "A and (B or C)"
                 * Another example is "(2 from A and 1 from B) or (1 from A and 2 from B)" turns into "1 from A and 1 from B and 1 from union(A, B)"
                 * TODO: figure out how this interacts with regular decisions
                 * It is most useful for simplifying megaDecisions
                 */

                // Compile a list of options that are forced by the decision
                List<Option> commonOptions = (simpleOptions.First() as Decision).Options
                    .Where(option => simpleOptions.All(otherOption => (otherOption as Decision).Options.Any(foo => foo.HasSameOptions(option))))
                    .ToList();

                // Skip if nothing is found
                if (commonOptions.Any())
                {
                    // Prepare a list of each decision with the common options removed
                    List<Option> decisionsExcludingCommonOptions = new List<Option>();
                    foreach (Decision decision in simpleOptions)
                    {
                        // Make a list of options in the decision that are not in commonOptions
                        List<Option> excludedOptions = decision.Options.Where(option => !commonOptions.Any(commonOption => option.HasSameOptions(commonOption))).ToList();
                        // For each common option, check if it should still be part of the excluded options (see the second example)
                        foreach (Option commonOption in commonOptions)
                        {
                            // The option can only be part of the excluded options if it is a decision
                            if (commonOption is Decision commonDecision)
                            {
                                // Compare the pick of the decision with the pick of commonDecision
                                int minPick = simpleOptions.Min(simpleOption => ((simpleOption as Decision).Options.Find(subOption => subOption.HasSameOptions(commonDecision)) as Decision).Pick);
                                // Find the sub-decision in the decision that matches with the commonDecision
                                Decision relevantOption = decision.Options.Find(subOption => subOption.HasSameOptions(commonDecision)) as Decision;
                                // Find the new value for Pick
                                int excludedOptionPick = relevantOption.Pick - minPick;
                                if (excludedOptionPick <= 0)
                                    continue;
                                // Recreate relevantOption with a new value of pick
                                Selection excludedOptionSelectionType = relevantOption.SelectionType;
                                string excludedOptionDescription = excludedOptionSelectionType == Selection.CP ? relevantOption.CopyDescription(excludedOptionPick) : "";
                                Decision excludedOption = new Decision(this, excludedOptionDescription, commonDecision.Options, excludedOptionPick, excludedOptionSelectionType);
                                // Add the excludedOption to the list of excludedOptions
                                excludedOptions.Add(excludedOption);
                            }
                        }
                        // Put the excludedOptions list in a decision, and add that decision to the list
                        decisionsExcludingCommonOptions.Add(new Decision(this, options: excludedOptions, pick: excludedOptions.Count, selectionType: Selection.AND));
                    }
                    // Create a decision based on all the decisions but with the common options excluded
                    Decision decisionExcludingCommonOptions = new Decision(this, options: decisionsExcludingCommonOptions).GetSimplifiedDecision();
                    // Prepare a list for the new simplified decision. It will contain all the common options + decisionExcludingCommonOptions 
                    List<Option> newSimpleOptions = new List<Option> { decisionExcludingCommonOptions };
                    // For each common option, work out the Pick
                    foreach (Option commonOption in commonOptions)
                    {
                        if (commonOption is Decision commonDecision)
                        {
                            int minPick = simpleOptions.Min(simpleOption => ((simpleOption as Decision).Options.Find(subOption => subOption.HasSameOptions(commonDecision)) as Decision).Pick);
                            Selection subDecisionSelectionType = commonDecision.SelectionType;
                            string subDecisionDescription = subDecisionSelectionType == Selection.CP ? commonDecision.CopyDescription(minPick) : "";
                            newSimpleOptions.Add(new Decision(this, subDecisionDescription, commonDecision.Options, minPick, subDecisionSelectionType));
                        }
                        else
                        {
                            newSimpleOptions.Add(commonOption);
                        }
                    }
                    // Move these values to simpleOptions and simplePick
                    simpleOptions = newSimpleOptions;
                    simplePick = simpleOptions.Count;
                }
            }
            // If this is one of those decisions which are like "10CP from 2 options" then just turn it into an OR decision
            if (simpleSelectionType == Selection.CP && simplePick == 1 && simpleOptions.Count < 3)
                simpleSelectionType = Selection.OR;
            // If the selection is one of those vague descriptions then figure it out now. Otherwise let the program figure out what it is later
            string simpleDescription = simpleSelectionType == Selection.CP ? CopyDescription(simplePick) : "";
            // Return the remaining decision
            return new Decision(this, simpleDescription, simpleOptions, simplePick, simpleSelectionType);
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Decision other:
                    return this.Pick == other.Pick && this.HasSameOptions(other);
                case Content content:
                    return Pick == 1 && Options.Count == 1 && Options.First() == content;
                default:
                    return base.Equals(obj);
            }
        }

        public override bool HasSameOptions(Option other)
        {
            if (other is Decision decision)
                return this.Options.Count == decision.Options.Count && this.Options.All(option => decision.Options.Any(otherOption => option.Equals(otherOption)));
            else
                return Options.Count == 1 && Options.First() == other;
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
            Time requiredByPrerequisites = reasonsPrerequisite.Any(reason => reason is Subject) 
                ? reasonsPrerequisite.OfType<Subject>().Min(reason => reason.GetChosenTime(plan)).Previous() 
                : Time.Impossible;
            Time requiredByCorequisites = reasonsCorequisite.Any(reason => reason is Subject) 
                ? reasonsCorequisite.OfType<Subject>().Min(reason => reason.GetChosenTime(plan)) 
                : Time.Impossible;
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

        public bool Contains(Option value)
        {
            return Options.Any(option => option == value || (option is Decision decision && decision.Contains(value)));
        }

        public Decision WithSelectedContent(Content selectedContent, bool designated)
        {
            // Local function that allow any option to call WithSelectedContent 
            //TODO: create an abstract function to replace this local function
            Option OptionWithoutSelectedContent(Option option, bool selected)
            {
                if (option is Decision decision)
                    // Recursive call
                    return decision.WithSelectedContent(selectedContent, selected);
                if (option == selectedContent)
                    if (selected)
                        // Blank decision
                        return new Decision(this);
                    else
                        // Impossible decision
                        return new Decision(this, options: new List<Option>());
                else
                    // Content as decision (possible but optional)
                    return option;
            }
             
            // If this decision does not contain the content, return the decision without any changes
            if (!Contains(selectedContent))
                return this;
            // Prepare a list of every way this combination of decisions could end up in
            List<Decision> possibleResults = new List<Decision>();
            // The subject can only belong to one designated decision, so check what happens when each relevant option gets designated
            if (designated)
            {
                /* Find all decisions that involve this subject, but ignore decisions that are made of vague options
                 * For example, if one of the decisions contains the options {A, B} and another decision contains {A, B, C, D} then don't include the second option, regardless of their Pick
                 * If one of the options is the subject, then it should be the only possible designated option
                 */

                // Find options that contain selectedContent or are selectedContent
                List<Option> relevantOptions = Options.Where(option => option == selectedContent || (option is Decision decision && decision.Contains(selectedContent))).ToList();
                // Prepare a list of options that could be the designated option
                List<Option> possibleDesignations = new List<Option>();
                // Check each relevant option to see if it could be a designated option
                foreach (Option relevantOption in relevantOptions)
                {
                    // `keep` is a flag that stores whether this option can be a designated option
                    bool keep = true;
                    // If this option is a content then it should be the only designated option
                    if (relevantOption is Decision relevantDecision)
                    {
                        // Compare this option to every other option
                        foreach (Option otherOption in relevantOptions)
                        {
                            if (relevantOption == otherOption)
                                continue;

                            // Check if the other option is a decision that's Options is a subset of the relevant decision's Options
                            if (otherOption is Decision otherDecision)
                            {
                                if (otherDecision.Options.All(option => relevantDecision.Options.Contains(option)))
                                    keep = false;
                            }
                            // If the other option is a content, then that is the only option that can be the designated option
                            else
                                keep = false;
                        }
                    }
                    // If the flag is still true, then this option could be a designated option
                    if (keep) possibleDesignations.Add(relevantOption);
                }

                foreach (Option designatedOption in possibleDesignations)
                {
                    // Remove the subject from each of the other decisions
                    List<Option> allOptions = Options.Where(option => option != designatedOption).Select(option => OptionWithoutSelectedContent(option, false)).ToList<Option>();
                    // Remove the subject from the designated decision, but also reduce the Pick of that decision
                    allOptions.Add(OptionWithoutSelectedContent(designatedOption, true));
                    // Create a new decision which is a combination of all decisions when the subject is removed
                    string possibleDescription = SelectionType == Selection.CP ? CopyDescription(Pick) : "";
                    possibleResults.Add(new Decision(this, possibleDescription, allOptions, Pick, SelectionType));
                }
            }
            // If none of the options can be designated, remove the selected subject from the desicion
            if (!designated || !possibleResults.Any())
            {
                // Remove the subject from each of the decisions
                List<Option> allOptions = Options.Select(option => OptionWithoutSelectedContent(option, false)).ToList();
                // Create a new decision which is a combination of all decisions when the subject is removed
                string possibleDescription = SelectionType == Selection.CP ? CopyDescription(Pick) : "";
                possibleResults.Add(new Decision(this, possibleDescription, allOptions, Pick, SelectionType));
            }
            // Create a new decision that is a combination of all possible decisions, and simplify it
            return new Decision(this, options: possibleResults.ToList<Option>()).GetSimplifiedDecision();
        }
    }

    public enum Selection { AND, OR, CP }
}
