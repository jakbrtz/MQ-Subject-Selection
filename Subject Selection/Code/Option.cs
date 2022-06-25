using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Subject_Selection
{
    /// <summary>
    /// A component of a decision
    /// </summary>
    abstract public class Option
    {
        /// <summary> Has this Option been satisfied by a plan </summary>
        public abstract bool HasBeenCompleted(Plan plan, Time requiredCompletionTime);

        /// <summary> Is it impossible for this option to be satisfied in this plan? </summary>
        public abstract bool HasBeenBanned(Plan plan);

        /// <summary> How many credit points are required to complete this option? </summary>
        public abstract int CreditPoints();

        /// <summary> What is the earliest time that this option can be satisfied by </summary>
        public abstract Time EarliestCompletionTime(Plan plan);

        /// <summary> Which subjects are banned due to this option? </summary>
        public abstract List<Content> ForcedBans();

        /// <summary> Is this option practically the same as another option </summary>
        public abstract bool HasSameOptions(Option other);

        /// <summary> Does this option force another option to be satisfied? </summary>
        public abstract bool Covers(Option maybeRedundant);

        /// <summary> What does this option look like when another subject has been selected/deleted </summary>
        public abstract Option WithoutContent(Content content, bool countTowardsDecision);

        /// <summary> Is there enough credit points remaining for this option to be satisfied? </summary>
        public abstract bool EnoughCreditPoints(Plan plan, int creditPointsAvailable, out int creditPointsRequired);
    }

    public abstract class Content : Option
    {
        /// <summary> The short code </summary>
        public string ID { get; }

        /// <summary> The full name </summary>
        public string Name { get; }

        /// <summary> What other subjects need to be completed before this can be completed? </summary>
        public Decision Prerequisites => _prerequisites;
        private Decision _prerequisites;

        /// <summary> What other subjects need to be completed before (or at the same time) as this can be completed? </summary>
        public Decision Corequisites => _corequisites;
        private Decision _corequisites;

        /// <summary> What other subjects are not allowed to be done if the human selects this content? </summary>
        public List<Content> NCCWs => _nccws;
        private List<Content> _nccws;

        protected Content(string id, string name) 
        {
            this.ID = id;
            this.Name = name;
        }

        public override string ToString()
        {
            return ID;
        }

        internal void PostLoad(Decision prerequisites, Decision corequisites, List<Content> nccws = null)
        {
            Debug.Assert(Prerequisites == null, "The prerequisites should not be set at this point");
            Debug.Assert(Corequisites == null, "The corequisites should not be set at this point");
            Debug.Assert(NCCWs == null, "The nccws should not be set at this point");
            _prerequisites = prerequisites;
            _corequisites = corequisites;
            _nccws = nccws ?? new List<Content>();
        }

        public override List<Content> ForcedBans()
        {
            return NCCWs;
        }

        public override bool HasSameOptions(Option other)
        {
            if (other is Decision decision)
                return decision.Options.Count == 1 && decision.Options.First() == this;
            return this.Equals(other);
        }

        public override bool Covers(Option maybeRedundant)
        {
            if (maybeRedundant is Decision maybeRedundantDecision)
                return maybeRedundantDecision.OnlyPickOne() && maybeRedundantDecision.Options.Any(option => this.Covers(option));
            return this == maybeRedundant;
        }

        public override Option WithoutContent(Content content, bool countTowardsDecision)
        {
            if (this == content)
                if (countTowardsDecision)
                    return new CompletedDecision(this);
                else
                    return new ImpossibleDecision(this);
            else
                return this;
        }
    }

    public class Subject : Content
    {
        /// <summary> How many credit points is this worth </summary>
        private readonly int CP;
        /// <summary> Is this a pace unit? </summary>
        public readonly bool pace = false;
        /// <summary> What is the earliest year that this can be taken? </summary>
        public readonly int earliestYear = 2020;

        /// <summary> A list of subjects where their requisites include this subject </summary>
        public List<Subject> Parents { get; } = new List<Subject>(); //TODO: better name

        /// <summary> A list of times where this subject is offered </summary>
        public List<OfferTime> Semesters { get; }

        public Subject(string id, string name, int cp, bool pace, List<OfferTime> semesters, int earliestYear) : base (id, name)
        {
            CP = cp;
            this.pace = pace;
            this.Semesters = semesters;
            this.earliestYear = earliestYear;
        }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            if (!plan.SelectedSubjects.Contains(this)) return false;
            return plan.SelectedSubjectsSoFar(requiredCompletionTime).Contains(this);
        }

        public override bool HasBeenBanned(Plan plan)
        {
            return Time.Impossible.IsEarlierThanOrAtTheSameTime(EarliestCompletionTime(plan));
        }

        public override int CreditPoints()
        {
            return CP;
        }

        /// <summary> Get the time that this subject was taken according to the plan </summary>
        public Time GetChosenTime(Plan plan)
        {
            return plan.AssignedTimes[this];
        }

        public override Time EarliestCompletionTime(Plan plan)
        {
            // Get the value from the dictionary
            if (plan.EarliestCompletionTimes.TryGetValue(this, out Time earliestTimeForOption))
                return earliestTimeForOption;
            // If no value was found, then assume that this can be completed straight away (this should only occur when populating the dictionary)
            return Time.Early;
        }

        /// <summary> Return true if the earliest time is earliest than the semester, and the subject allows the session </summary>
        public bool AllowedDuringSemester(Time semester, Plan plan)
        {
            return plan.EarliestCompletionTimes[this].IsEarlierThanOrAtTheSameTime(semester) && Semesters.Any(offerTime => offerTime.session == semester.session);
        }

        /// <summary> Check if this option is recommended to the human, according to the other subjects they have selected </summary>
        public bool IsRecommended(List<Subject> otherSelectedSubjects, List<Course> otherSelectedCourses, out List<Content> reasons)
        {
            reasons = MasterList.ReasonsForRecommendation(this, otherSelectedSubjects.Concat<Content>(otherSelectedCourses));
            return reasons.Any();
        }

        /// <summary> Work out the Parents of this subject </summary>
        public void FindChildren()
        {
            Prerequisites.FindChildren(this);
            Corequisites.FindChildren(this);
        }

        private bool _flag_running_EnoughCreditPoints = false;
        public override bool EnoughCreditPoints(Plan plan, int creditPointsAvailable, out int creditPointsRequired)
        {
            /* This function does not produce correct results
             * The purpose of the algorithm is to answer the question "There are X credit points remaining, can this subject and its requisites be selected?"
             * This algorithm is tougher than I expected. One issue is Requisites such as "20cp in STAT units at 3000 level" where options could have overlapping requisites
             * Another issue is the LAWS subjects take a long time to analyze
             * It is better to incorrectly return true than to incorrectly return false, so sometimes this results in letting the user pick a subject that cannot fit on their schedule
             */

            creditPointsRequired = 0;
            // If the amount of credit Points available is huge then we can assume there are enough points
            if (creditPointsAvailable == int.MaxValue)
                return true;
            // If there are no credit points available then there are not enough points
            if (creditPointsAvailable < 0)
                return false;
            // If this subject is already part of the plan then no more credit points are required
            if (HasBeenCompleted(plan, Time.All))
                return true;
            // Keep track of how many points are required for the prerequisites and corequisites
            int creditPointsRequiredPrerequisites;
            int creditPointsRequiredCorequisites;
            // This flag is used to avoid infinites loops in this recursive algorithm
            if (_flag_running_EnoughCreditPoints)
                return false;
            try
            {
                _flag_running_EnoughCreditPoints = true;
                // The credit points from this subject are required
                creditPointsRequired = CreditPoints();
                // The credit points from the prerequisites and corequisites are required
                if (!Prerequisites.EnoughCreditPoints(plan, creditPointsAvailable - creditPointsRequired, out creditPointsRequiredPrerequisites))
                    return false;
                if (!Corequisites.EnoughCreditPoints(plan, creditPointsAvailable - creditPointsRequired, out creditPointsRequiredCorequisites))
                    return false;
            }
            finally
            {
                _flag_running_EnoughCreditPoints = false;
            }
            // It is possible that there is overlap between Prerequisites and Corequisites, so add the larger of creditPointsRequiredRequisites
            creditPointsRequired += creditPointsRequiredPrerequisites > creditPointsRequiredCorequisites ? creditPointsRequiredPrerequisites : creditPointsRequiredCorequisites;
            // If the number of available credit points is greater than or equal to the required amount, then there are enough credit points
            return creditPointsRequired <= creditPointsAvailable;
        }
    }

    /// <summary>
    /// A group of subjects that the human needs to do to get a degree
    /// </summary>
    public class Course : Content
    {
        public Course(string code, string name) : base (code, name) { }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            return plan.SelectedCourses.Contains(this);
        }

        public override bool HasBeenBanned(Plan plan)
        {
            // If a degree has been chosen, and this is not it, then this is banned
            if (plan.SelectedCourses.Any() && IsDegree() && !plan.SelectedCourses.Contains(this))
                return true;
            return false;
        }

        public override int CreditPoints()
        {
            return Prerequisites.CreditPoints();
        }

        public override Time EarliestCompletionTime(Plan plan)
        {
            if (HasBeenBanned(plan))
                return Time.Impossible;
            else
                return Time.Early;
        }

        public override bool EnoughCreditPoints(Plan plan, int creditPointsAvailable, out int creditPointsRequired)
        {
            creditPointsRequired = 0;
            if (plan.SelectedCourses.Any() && IsDegree())
                return plan.SelectedCourses.Contains(this);
            if (creditPointsAvailable == int.MaxValue)
                return true;
            if (creditPointsAvailable < 0)
                return false;
            // Make sure that there are enough credit points to satisfy the prerequisites
            return Prerequisites.EnoughCreditPoints(plan, creditPointsAvailable, out creditPointsRequired);
        }

        /// <summary> Is this a degree </summary>
        bool IsDegree()
        {
            // TODO: make a separate class
            return CreditPoints() > 210 || CreditPoints() == 0;
        }
    }

    /// <summary>
    /// The human needs to decide which subjects they must complete in order to satisfy requisites
    /// </summary>
    public partial class Decision : Option
    {
        /// <summary> What subjects have this as a prerequisite? </summary>
        List<Content> reasonsPrerequisite = new List<Content>();
        /// <summary> What subjects have this as a corequisite? </summary>
        List<Content> reasonsCorequisite = new List<Content>();
        /// <summary> A human-friendly string to represent this decision </summary>
        string description;
        /// <summary> A domain of options for the human to choose from </summary>
        List<Option> options;
        /// <summary> The amount of credit points that need to be completed to satisfy this decision </summary>
        int? creditPoints;
        /// <summary> What type of decision is this? </summary>
        Selection selectionType;

        public Decision(Option reason, string description = "", List<Option> options = null, Selection selectionType = Selection.OR, int? creditPoints = null, bool reasonIsCorequisite = false)
        {
            // Validate that creditPoints are only used for CP decisions
            if (creditPoints != null && selectionType != Selection.CP)
                throw new ArgumentException("creditPoints are only allowed for Credit Point (CP) selections");
            if (creditPoints == null && selectionType == Selection.CP)
                throw new ArgumentException("creditPoints are required for Credit Point (CP) selections");

            // Add the reason to the list of reasons
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

            // Copy values from the parameters
            this.description = description;
            this.options = options;
            this.creditPoints = creditPoints;
            this.selectionType = selectionType;

            // If necessary, copy the description from the reason
            if (selectionType == Selection.CP && description == "" && reason is Decision && options.All(option => !(option is Decision decision) || decision.HasBeenCompleted()))
                CopyDescription(reason.ToString());
        }

        /// <summary> A domain of options for the human to choose from </summary>
        public List<Option> Options
        {
            get
            {
                if (options == null)
                    LoadFromDescription();
                return options;
            }
        }

        /// <summary> What type of decision is this? </summary>
        public Selection SelectionType
        {
            get
            {
                if (options == null)
                    LoadFromDescription();
                return selectionType;
            }
        }

        /// <summary> The amount of credit points that need to be completed to satisfy this decision </summary>
        public override int CreditPoints()
        {
            if (options == null)
                LoadFromDescription();
            if (selectionType == Selection.CP)
                return creditPoints.Value;
            if (SelectionType == Selection.AND)
                return Options.Sum(option => option.CreditPoints());
            if (SelectionType == Selection.OR && Options.All(option => option.CreditPoints() == Options.First().CreditPoints()))
                return Options.First().CreditPoints();
            throw new ArgumentException("CreditPoints cannot be evaluated for this SelectionType");
        }

        /// <summary> When courses have overlapping units, the same unit cannot count towards both decisions. This makes a decision "Unique" </summary>
        public bool Unique()
        {
            return GetReasonsPrerequisite().Any(content => content is Course);
        }

        public virtual bool HasBeenCompleted()
        {
            return SelectionType switch
            {
                Selection.OR  => Options.Any(option => option is Decision decision && decision.HasBeenCompleted()),
                Selection.AND => Options.All(option => option is Decision decision && decision.HasBeenCompleted()),
                Selection.CP  => CreditPoints() <= 0,
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        public override bool HasBeenCompleted(Plan plan, Time requiredCompletionTime)
        {
            /* This method does not take into account that Unique decisions cannot have overlapping selected subjects
             * There is no place in the code that calls this function when the decision is Unique, so I guess that means I don't need to write code to account for it.
             * I don't trust myself so I wrote this to stop me from breaking that rule:
             */
            if (Unique()) throw new ArgumentException("Unique decisions are not allowed to use this function");
            // Check if the decision is completed under normal circumstances
            if (HasBeenCompleted()) return true;
            // Recursively count the number of options that have been completed
            return SelectionType switch
            {
                Selection.OR  => Options.Any(option => option.HasBeenCompleted(plan, requiredCompletionTime)),
                Selection.AND => Options.All(option => option.HasBeenCompleted(plan, requiredCompletionTime)),
                Selection.CP  => Options.SumOfCreditPointsIsGreaterThanOrEqualTo(option => option.HasBeenCompleted(plan, requiredCompletionTime), CreditPoints()),
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        /// <summary> If you ignore the "Elective" component of this decision, has it been satisfied </summary>
        public bool HasBeenCompletedIgnoringElectives(Plan plan, Time requiredCompletionTime)
        {
            // Rather than evaluating elective decisions, assume that it is true
            if (IsElective()) return true;
            // This local function handles the recursive call. I did not want to make a abstract function
            bool Completed(Option option)
            {
                if (option is Decision decision)
                    return decision.HasBeenCompletedIgnoringElectives(plan, requiredCompletionTime);
                return option.HasBeenCompleted(plan, requiredCompletionTime);
            }
            // This is pretty much a copy of HasBeenCompleted
            return SelectionType switch
            {
                Selection.OR  => Options.Any(option => Completed(option)),
                Selection.AND => Options.All(option => Completed(option)),
                Selection.CP  => Options.SumOfCreditPointsIsGreaterThanOrEqualTo(option => Completed(option), CreditPoints()),
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        /// <summary> Should only one option be selected? </summary>
        public bool OnlyPickOne()
        {
            return SelectionType switch
            {
                Selection.OR  => true,
                Selection.AND => Options.Count == 1,
                Selection.CP  => CreditPoints() != 0 && Options.All(option => option.CreditPoints() >= CreditPoints()), // In most cases, this line is the same as `CreditPoints() == 10`
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        /// <summary> Will all options need to be selected? </summary>
        public bool MustPickAll()
        {
            return SelectionType switch
            {
                Selection.OR  => Options.Count == 1,
                Selection.AND => true,
                Selection.CP  => CreditPoints() == Options.Sum(option => option.CreditPoints()),
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        public virtual bool HasBeenBanned()
        {
            return SelectionType switch
            {
                Selection.OR  => Options.All(option => option is Decision decision && decision.HasBeenBanned()),
                Selection.AND => Options.Any(option => option is Decision decision && decision.HasBeenBanned()),
                Selection.CP  => Options.SumOfCreditPointsIsLessThan(option => !(option is Decision decision && decision.HasBeenBanned()), CreditPoints()),
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        public override bool HasBeenBanned(Plan plan)
        {
            // Assume electives cannot be banned
            if (IsElective()) return false;
            // Check if the decision is banned under normal circumstances
            if (HasBeenBanned()) return true;
            // Recursively count the number of options that have not been banned
            return SelectionType switch
            {
                Selection.OR  => Options.All(option => option.HasBeenBanned(plan)),
                Selection.AND => Options.Any(option => option.HasBeenBanned(plan)),
                Selection.CP  => Options.SumOfCreditPointsIsLessThan(option => !option.HasBeenBanned(plan), CreditPoints()),
                _             => throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType"),
            };
        }

        public override List<Content> ForcedBans()
        {
            // Assume electives don't force anything
            if (IsElective())
                return new List<Content>();

            // For each option, check if it forces a subject to be banned
            // Count how often a subject is banned by an option
            // If it gets banned by too many options, then it gets banned by the entire decision

            switch (SelectionType)
            {
                case Selection.OR:
                    // Intersection of all Options.ForcedBans()
                    IEnumerable<Content> bans = Options.First().ForcedBans();
                    foreach (Option option in Options)
                        bans = bans.Intersect(option.ForcedBans());
                    return bans.ToList();
                case Selection.AND:
                    // Union of all Options.ForcedBans()
                    return Options.SelectMany(option => option.ForcedBans()).Distinct().ToList();
                case Selection.CP:
                    Dictionary<Content, int> counts = new Dictionary<Content, int>();
                    foreach (Option option in Options)
                    {
                        foreach (Content content in option.ForcedBans())
                        {
                            if (!counts.ContainsKey(content))
                                counts[content] = 0;
                            counts[content] += content.CreditPoints();
                        }
                    }
                    return counts.Where(kvp => Options.Sum(option => option.CreditPoints()) - kvp.Value < CreditPoints()).Select(kvp => kvp.Key).ToList();
                default:
                    throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType");
            }
        }

        /// <summary> What decision does the user still need to make, when account for what's already been selected </summary>
        public Decision GetRemainingDecision(Plan plan)
        {
            // Figure out when this decision must be completed
            Time requiredCompletionTime = RequiredCompletionTime(plan);
            // Create a list containing all the remaining options
            List<Option> remainingOptions = new List<Option>();

            switch (SelectionType)
            {
                case Selection.OR:
                    // Populate remainingOptions from Options
                    foreach (Option option in Options)
                    {
                        if (option is Content content)
                        {
                            // If an option has been completed then this decision is complete
                            if (content.HasBeenCompleted(plan, requiredCompletionTime))
                                return new CompletedDecision(this);
                            // If an option has been banned then ignore it
                            if (content.HasBeenBanned(plan))
                                continue;
                            // Add the option to remainingOptions
                            remainingOptions.Add(content);
                        }
                        else if (option is Decision decision)
                        {
                            // Reduce the decision before adding it to remainingOptions
                            remainingOptions.Add(decision.GetRemainingDecision(plan));
                        }
                    }
                    // Create a new Decision using remainingOptions
                    return new Decision(this, options: remainingOptions, selectionType: Selection.OR);
                case Selection.AND:
                    // Populate remainingOptions from Options
                    foreach (Option option in Options)
                    {
                        if (option is Content content)
                        {
                            // If the option has been completed then ignore it
                            if (content.HasBeenCompleted(plan, requiredCompletionTime))
                                continue;
                            // If the option has been banned then this decision is impossible
                            if (content.HasBeenBanned(plan))
                                return new ImpossibleDecision(this);
                            // Add the option to remainingOptions
                            remainingOptions.Add(content);
                        }
                        else if (option is Decision decision)
                        {
                            // Reduce the decision
                            Decision reducedSubDecision = decision.GetRemainingDecision(plan);
                            // Check that it hasn't been banned
                            if (reducedSubDecision.HasBeenBanned())
                                return new ImpossibleDecision(this);
                            // Add the reduced decision to remainingOptions
                            remainingOptions.Add(reducedSubDecision);
                        }
                    }
                    // Create a new Decision using remainingOptions
                    return new Decision(this, options: remainingOptions, selectionType: Selection.AND);
                case Selection.CP:
                    Decision reduced = this;
                    // For each selection Option in the plan, find a way to select it from this decision
                    foreach (Content content in plan.SelectedSubjects.Where(subject => subject.HasBeenCompleted(plan, requiredCompletionTime)).Cast<Content>().Concat(plan.SelectedCourses))
                    {
                        Decision withSelected = (Decision)reduced.WithoutContent(content, true);
                        Decision simplified = withSelected.GetSimplifiedDecision();
                        reduced = simplified;
                    }
                    // For each banned Subject, remove it
                    return reduced.RemoveBannedSubjects(plan);
                default:
                    throw new ArgumentException(SelectionType.ToString() + " is not a know SelectionType");
            }
        }

        /// <summary> Create a simple version of this decision </summary>
        public Decision GetSimplifiedDecision()
        {
            // If the decision doesn't require anything, then return a blank decision
            if (HasBeenCompleted()) return new CompletedDecision(this);
            // Create a new list to store the remaining options
            List<Option> simpleOptions = new List<Option>();
            foreach (Option option in Options)
            {
                if (option is Content)
                {
                    // When a subject is worth 0cp and the decision is to reach a number of credit points (eg ENGG4099 in electives) ignore the option
                    if (SelectionType == Selection.CP && option.CreditPoints() == 0)
                        continue;
                    // Otherwise, just add the content normally
                    simpleOptions.Add(option);
                }
                else if (option is Decision decision)
                {
                    // Simplify decisions
                    Decision simplifiedOption = decision.GetSimplifiedDecision();
                    // If the decision is impossible, either skip it or return an impossible deicision
                    if (simplifiedOption.HasBeenBanned())
                    {
                        if (SelectionType == Selection.AND)
                            return new ImpossibleDecision(this);
                        continue;
                    }
                    // If the decision is blank, skip the decision
                    if (simplifiedOption.HasBeenCompleted())
                        continue;
                    // Otherwise, just add the decision normally
                    simpleOptions.Add(simplifiedOption);
                }
            }
            // TODO: check if this has been banned
            // If there is only one remaining option to pick from then return it
            if (simpleOptions.Count == 1 && simpleOptions.First() is Decision onlyDecision)
                return onlyDecision;
            // Figure out what the selection type is
            Selection simpleSelectionType = SelectionType;
            // Special case: if only one option needs to be picked, and some of the options are decisions which also only require picking one option, then combine the decisions
            if (simpleSelectionType == Selection.OR || (simpleSelectionType == Selection.CP && Options.All(option => option.CreditPoints() >= CreditPoints())))
            {
                bool rearranged = false;
                // For each subdecision where only one needs to be picked, copy all of the subdecision's options to this decision's options
                foreach (Decision decision in new List<Option>(simpleOptions).Where(option => option is Decision decision && decision.OnlyPickOne()))
                {
                    if (decision.IsElective())
                    {
                        simpleSelectionType = Selection.CP;
                        
                    }

                    simpleOptions.Remove(decision);
                    simpleOptions.AddRange(decision.Options);
                    rearranged = true;
                }
                // Regardless of whether this decision was rearranged, this next line should run
                // The reason I put a condition was as a time saver. I don't want it to run "10cp at 1000 level or above".Distinct()
                if (rearranged)
                    simpleOptions = simpleOptions.Distinct().ToList();
            }
            // Special case: if every option needs to be picked, and some of the options are decisions which also require picking all options, then combine the decisions
            if (simpleSelectionType == Selection.AND || (simpleSelectionType==Selection.CP && Options.Sum(option => option.CreditPoints()) == CreditPoints())) // This is the same as MustPickAll() // TODO: optimize this line
            {
                foreach (Decision decision in new List<Option>(simpleOptions).Where(option => option is Decision decision && (decision.SelectionType == simpleSelectionType || decision.OnlyPickOne()) && decision.MustPickAll()))
                {
                    simpleOptions.Remove(decision);
                    simpleOptions.AddRange(decision.Options);
                }
            }
            // Special case: try to rearrange it so it's nicer for the user
            if ((simpleSelectionType == Selection.OR || (simpleSelectionType==Selection.CP && simpleOptions.All(option => option.CreditPoints() >= CreditPoints()))) // This is the same as OnlyPickOne()
                && simpleOptions.Any() && simpleOptions.All(option => option is Decision decision))  // All options in this list must be Decisions
            {
                /* For example, if the decision is "(A and B) or (A and C)" turn it into "A and (B or C)"
                 * Another example is "(2 from A and 1 from B) or (1 from A and 2 from B)" turns into "1 from A and 1 from B and 1 from union(A, B)"
                 * Finally, "(2 from A) or (1 from A and 1 from B)" turns into "1 from A and 1 from union (A, B)". This is different to the previous example because the first condition isn't MustPickAll
                 * TODO: figure out how this interacts with non-Unique decisions
                 * TODO: A or (A and B)
                 * It is most useful for cleaning up the mess made by Decision.WithoutContent
                 */

                List<Decision> simpleDecisions = simpleOptions
                    .Cast<Decision>()
                    .Select(decision => decision.MustPickAll() ? decision : new Decision(decision, options: new List<Option>() { decision }, selectionType: Selection.AND))
                    .ToList();

                // Compile a list of options that are forced by the decision
                List<Option> commonOptions = simpleDecisions.First().Options
                    .Where(option => simpleDecisions.All(otherDecision => otherDecision.Options.Any(foo => foo.HasSameOptions(option))))
                    .ToList();

                // Skip if nothing is found
                if (commonOptions.Any())
                {
                    // Prepare a list for the new simplified decision. It will contain all the common options + a decision containing the remaining part that does not include the common options 
                    List<Option> newSimpleOptions = new List<Option>();
                    foreach (Option commonOption in commonOptions)
                    {
                        if (commonOption is Decision commonDecision)
                        {
                            // If the common option is a decision, work out what kind of decision it is (start by assuming it is CP)
                            Selection subDecisionSelectionType = Selection.CP;
                            // Achieve that by finding the set of all simpleDecision's selection types
                            HashSet<Selection> selectionsFound = new HashSet<Selection>();
                            // During that, keep track of the smallest value of CreditPoints
                            int? minCreditPoints = null;
                            foreach (Decision simpleOption in simpleDecisions)
                            {
                                // Find the subOption which matches with the commonDecision
                                Decision found = simpleOption.Options.Find(subOption => subOption.HasSameOptions(commonDecision)) as Decision;
                                // Record its selection type
                                selectionsFound.Add(found.SelectionType);
                                // If it is a CP selection, update minCreditPoints
                                if (found.SelectionType == Selection.CP && (minCreditPoints > found.CreditPoints() || !minCreditPoints.HasValue))
                                    minCreditPoints = found.CreditPoints();
                            }
                            // If only one selectionType was found, choose it. If a CP and an OR was found, then choose CP. If AND was found, there was a mistake
                            if (selectionsFound.Contains(Selection.AND))
                                throw new ArgumentException("If the commonDecision is AND then the previous special case didn't work properly");
                            if (selectionsFound.Contains(Selection.OR))
                            {
                                if (selectionsFound.Contains(Selection.CP))
                                {
                                    minCreditPoints = commonDecision.Options.Min(option => option.CreditPoints());
                                    if (minCreditPoints == 0)
                                        throw new InvalidOperationException("OR decision could not be converted into a CP decision");
                                }
                                else
                                {
                                    subDecisionSelectionType = Selection.OR;
                                }
                            }

                            newSimpleOptions.Add(new Decision(commonDecision, options: commonDecision.Options, selectionType: subDecisionSelectionType, creditPoints: minCreditPoints));
                        }
                        else
                        {
                            newSimpleOptions.Add(commonOption);
                        }
                    }
                    // Replace commonOptions with all the updated common options (with the smallest value of CreditPoints)
                    commonOptions = new List<Option>(newSimpleOptions);
                    // Prepare a list of each decision with the common options removed
                    List<Option> decisionsExcludingCommonOptions = new List<Option>();
                    foreach (Decision decision in simpleDecisions)
                    {
                        // Make a list of options in the decision that are not in commonOptions
                        List<Option> excludedOptions = decision.Options.Where(option => !commonOptions.Any(commonOption => option.HasSameOptions(commonOption))).ToList();
                        // For each common option, check if it should still be part of the excluded options (see the second example)
                        foreach (Option commonOption in commonOptions)
                        {
                            // The option can only be part of the excluded options if it is a decision
                            if (commonOption is Decision commonDecision)
                            {
                                // Find the sub-decision from the decision that matches with the commonDecision
                                Decision relevantOption = decision.Options.Find(subOption => subOption.HasSameOptions(commonDecision)) as Decision;
                                // A quick check to make sure I'm not breaking my own rules
                                if (relevantOption.SelectionType == Selection.AND)
                                    throw new ArgumentException("If the commonDecision is AND then the previous special case didn't work properly");
                                // The relevantOption would only need to be added back into the decision if it's a CP decision (see second example)
                                if (relevantOption.SelectionType == Selection.CP)
                                {
                                    // Another quick check to make sure I'm not breaking my own rules
                                    if (commonDecision.SelectionType != Selection.CP)
                                        throw new ArgumentException("If the commonDecision is not CP then it must be OR, but in that case relevantOption would also have to be OR");
                                    // Find the new value for CreditPoints
                                    int excludedOptionCreditPoints = relevantOption.CreditPoints() - commonDecision.CreditPoints();
                                    if (excludedOptionCreditPoints <= 0)
                                        continue;
                                    // Recreate relevantOption with a new value of CreditPoints
                                    Decision excludedOption = new Decision(this, options: commonDecision.Options, selectionType: Selection.CP, creditPoints: excludedOptionCreditPoints);
                                    // Add the excludedOption to the list of excludedOptions
                                    excludedOptions.Add(excludedOption);
                                }
                            }
                        }
                        // Put the excludedOptions list in a decision, and add that decision to the list
                        decisionsExcludingCommonOptions.Add(new Decision(this, options: excludedOptions, selectionType: Selection.AND));
                    }
                    // Create a decision based on all the decisions but with the common options excluded
                    Decision decisionExcludingCommonOptions = new Decision(this, 
                        options: decisionsExcludingCommonOptions, 
                        selectionType: simpleSelectionType, 
                        creditPoints: simpleSelectionType == Selection.CP ? decisionsExcludingCommonOptions.First().CreditPoints() : (int?)null
                        ).GetSimplifiedDecision();
                    newSimpleOptions.Add(decisionExcludingCommonOptions);
                    simpleOptions = newSimpleOptions;
                    if (simpleSelectionType == Selection.OR)
                        simpleSelectionType = Selection.AND;
                }
            }
            // Special case: a selection of subjects where you only pick 1 of them, and some subjects are prerequisites of other subjects (eg 10cp in BIOL units)
            if (simpleOptions.All(option => option is Subject) && !Unique() && !IsElective() && // TODO: process electives quickly
                (simpleSelectionType == Selection.OR || (simpleSelectionType == Selection.CP && simpleOptions.All(option => option.CreditPoints() >= CreditPoints())))) // This is the same as OnlyPickOne() 
            {
                Decision simpleDecisionSoFar = new Decision(this, options: simpleOptions, selectionType: simpleSelectionType, creditPoints: simpleSelectionType == Selection.CP ? CreditPoints() : (int?)null);
                IEnumerable<Option> newSimpleOptions = simpleOptions.Where(option => 
                    !(option as Subject).Prerequisites.Covers(simpleDecisionSoFar) && 
                    !(option as Subject).Corequisites.Covers(simpleDecisionSoFar));
                simpleOptions = newSimpleOptions.ToList();
            }
            // Return the remaining decision
            return new Decision(this, options: simpleOptions, selectionType: simpleSelectionType, creditPoints: simpleSelectionType == Selection.CP ? CreditPoints() : (int?)null);
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                Decision other => 
                    this.MustPickAll() == other.MustPickAll() && 
                    this.OnlyPickOne() == other.OnlyPickOne() && 
                    (this.SelectionType != Selection.CP || other.SelectionType != Selection.CP || this.CreditPoints() == other.CreditPoints()) && 
                    this.HasSameOptions(other),
                Content content => 
                    Options.Count == 1 && Options.First() == content,
                _ => 
                    base.Equals(obj),
            };
        }

        public override int GetHashCode()
        {
            var hashCode = 352033288;
            hashCode = hashCode * -1521134295 + OnlyPickOne().GetHashCode();
            hashCode = hashCode * -1521134295 + MustPickAll().GetHashCode();
            if (!OnlyPickOne() && !MustPickAll())
                hashCode = hashCode * -1521134295 + CreditPoints().GetHashCode();
            foreach (Option option in Options)
                hashCode = hashCode * -1521134295 + option.GetHashCode();
            return hashCode;
        }

        public override bool HasSameOptions(Option other)
        {
            // This assumes that there are no sub-decisions that are [complete] or [impossible], and that none of the options are repeated

            if (other is Decision decision)
            {
                // Make sure that there are the same number of elements in both lists
                if (this.Options.Count != decision.Options.Count)
                    return false;
                // For efficiency, check if the lists have the same options in the same order
                if (this.Options.SequenceEqual(decision.Options))
                    return true;
                // Make sure that 
                return this.Options.All(option => decision.Options.Any(otherOption => option.Equals(otherOption)));
            }
            else
            {
                return Options.Count == 1 && Options.First() == other;
            }
        }

        public override bool Covers(Option maybeRedundant)
        {
            // This isn't a thorough check, because otherwise it would be possible for simple decisions to be CoveredBy very complicated decisions
            // Also, I do not want to think about how NCCWs would interact with this function

            if (maybeRedundant is Decision maybeRedundantDecision)
            {
                // If the maybeRedundant decision is complete by default, then it is technically covered
                if (maybeRedundantDecision.HasBeenCompleted())
                    return true;
                // This once was a very elegant function which was only a few lines long
                // Now that I'm using SelectionType instead of Pick, I need to write code that compares every possible pairs of SelectionTypes
                if (OnlyPickOne())
                {
                    if (Options.All(option => option.Covers(maybeRedundantDecision)))
                        return true;
                }

                if (MustPickAll())
                {
                    if (Options.Any(option => option.Covers(maybeRedundantDecision)))
                        return true;
                }

                if (!OnlyPickOne() && !MustPickAll())
                {
                    // This is only possible if SelectionType == Selection.CP
                    // Or maybe there's some new Selection that I haven't programmed yet
                    switch (maybeRedundantDecision.SelectionType)
                    {
                        // These tests were written when CP selections could only contain subjects
                        // TODO: rewrite these tests to account for the fact that CP selections could be made of other selections

                        case Selection.OR:
                            // Count how many options are in this decision but not in the maybeRedundantDecision, then compare that to the number of CreditPoints that need to be selected
                            if (CreditPoints() > 0 && Options.SumOfCreditPointsIsLessThan(option => !maybeRedundantDecision.Options.Contains(option), CreditPoints()))
                                return true;
                            break;
                        case Selection.AND:
                            // This condition is covered in a different switch statement
                            break;
                        case Selection.CP:
                            // This condition is redundant, but it makes the program run a bit faster
                            if (CreditPoints() >= maybeRedundantDecision.CreditPoints())
                            {
                                // Another redundant condition that makes the program run a bit faster
                                if (this.HasSameOptions(maybeRedundantDecision))
                                    return true;

                                // Use the pigeonhole principle to compare the CreditPoints from both decisions
                                // TODO: account for when two elective decisions don't have the same options because they are at different semesters
                                if (Options.SumOfCreditPointsIsLessThanOrEqualTo(option => !maybeRedundantDecision.Options.Contains(option), CreditPoints() - maybeRedundantDecision.CreditPoints()))
                                    return true;
                            }
                            break;
                        default:
                            throw new ArgumentException(maybeRedundantDecision.SelectionType.ToString() + " is not a know SelectionType");
                    }
                }

                // Recursively check if the the options in maybeRedundantOptions are covered
                switch (maybeRedundantDecision.SelectionType)
                {
                    case Selection.OR:
                        if (maybeRedundantDecision.Options.Any(option => this.Covers(option)))
                            return true;
                        break;
                    case Selection.AND:
                        if (maybeRedundantDecision.Options.All(option => this.Covers(option)))
                            return true;
                        break;
                    case Selection.CP:
                        if (this.IsElective())
                            break;
                        if (maybeRedundantDecision.Options.SumOfCreditPointsIsGreaterThanOrEqualTo(option => this.Covers(option), maybeRedundantDecision.CreditPoints()))
                            return true;
                        break;
                    default:
                        throw new ArgumentException(maybeRedundantDecision.SelectionType.ToString() + " is not a know SelectionType");
                }

                return false;
            }
            // Otherwise the option is a Content, and can only be covered if this decision is "pick all of {list containing that content}"
            return MustPickAll() && Options.Contains(maybeRedundant);
        }

        public override Time EarliestCompletionTime(Plan plan)
        {
            // TODO: consider how NCCWs affect this

            // It is assumed that this method is only used to calculate the earliest time a subject can be selected
            if (GetReasons().Any(reason => reason is Course))
                throw new ArgumentException("This method should only be used for Subject's Requisites");
            // Some prerequisites have been parsed incorrectly so they are automatically banned
            if (HasBeenBanned())
                return Time.Impossible;
            // If the decision is an elective, pick the lower bound
            if (IsElective())
            {
                int countSubjectsCreditPoints = 0;
                Time lowerBound = Time.Early;
                // Assume that all semesters can hold units
                while (countSubjectsCreditPoints < CreditPoints())
                {
                    lowerBound = lowerBound.Next();
                    countSubjectsCreditPoints += plan.GetMaxCreditPoints(lowerBound);
                }
                return lowerBound;
            }

            switch (SelectionType)
            {
                case Selection.OR:
                    // Pick the earliest option
                    return Options.Min(option => option.EarliestCompletionTime(plan));
                case Selection.AND:
                    // Pick the latest option
                    if (Options.Any())
                        return Options.Max(option => option.EarliestCompletionTime(plan));
                    else
                        return Time.Early;
                case Selection.CP:
                    /* This algorithm finds the EarliestCompletionTime of every Option.
                     * It selects the earliest options (enough to satisfy CreditPoints),
                     *   then it returns the latest EarliestCompletionTime out of those options.
                     */
                    SortedDictionary<Time, int> TimeDistribution = new SortedDictionary<Time, int>();
                    foreach (Option option in Options)
                    {
                        // Find the option's EarliestCompletionTime
                        Time time = option.EarliestCompletionTime(plan);
                        // Add this option's CP to the distribution
                        if (!TimeDistribution.ContainsKey(time))
                            TimeDistribution[time] = 0;
                        TimeDistribution[time] += option.CreditPoints();
                        // TODO: account for MaxCreditPoints
                    }
                    // Find the earliest time that the decision can be satisfied by adding the CreditPoints in TimeDistribution
                    int creditPointsSoFar = 0;
                    foreach ((Time time, int Credits) in TimeDistribution)
                    {
                        creditPointsSoFar += Credits;
                        if (creditPointsSoFar >= CreditPoints())
                            return time;
                    }
                    // The code should have returned by now
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary> When must the options be completed by? </summary>
        public Time RequiredCompletionTime(Plan plan)
        {
            Time requiredByPrerequisites = reasonsPrerequisite.Any(reason => reason is Subject)
                ? reasonsPrerequisite.OfType<Subject>().Min(reason => reason.GetChosenTime(plan)).Previous()
                : Time.All;
            Time requiredByCorequisites = reasonsCorequisite.Any(reason => reason is Subject)
                ? reasonsCorequisite.OfType<Subject>().Min(reason => reason.GetChosenTime(plan))
                : Time.All;
            // Return the earlier out of those two values
            if (requiredByPrerequisites.IsEarlierThan(requiredByCorequisites))
                return requiredByPrerequisites;
            return requiredByCorequisites;
        }

        /// <summary> Combine the reasons from another decision into this decision's reasons </summary>
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

        /// <summary> Check if an option exists in this decision </summary>
        public bool Contains(Option value)
        {
            return Options.Any(option => option == value || (option is Decision decision && decision.Contains(value)));
        }

        public override Option WithoutContent(Content selectedContent, bool designated)
        {
            // If this decision does not contain the content, return the decision without any changes
            if (!Contains(selectedContent))
                return this;
            // Make sure that this is the correct type of decision
            if (SelectionType != Selection.CP)
                throw new ArgumentException("This method is only suitable for CP selections. Use GetRemainingDecision instead.");
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
                // Check each relevant option to see if it could be a designated option. Achieve this by checking:
                // - Are any options subjects?
                // - Are any options decisions that contain courses?
                // - Are any options decisions that are subsets to other options (everything is a subset of regular electives)
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
                            // Don't compare an option to itself
                            if (relevantOption == otherOption)
                                continue;

                            if (otherOption is Decision otherDecision)
                            {
                                // Check if the other decision is a subset of this decision (eg this decision is an elective)
                                if (otherDecision.Options.All(option => relevantDecision.Options.Contains(option)))
                                    keep = false;
                                // Check if the other decision contains courses and this decision does not
                                // (this is only to sort out the weird behaviour when picking Bachelor of Arts and ARTS2000
                                if (!relevantDecision.Options.Any(option => option is Course) && otherDecision.Options.Any(option => option is Course))
                                    keep = false;
                            }
                            else
                                // If the other option is a content, then that is the only option that can be the designated option
                                keep = false;
                        }
                    }
                    // If the flag is still true, then this option could be a designated option
                    if (keep) possibleDesignations.Add(relevantOption);
                }

                foreach (Option designatedOption in possibleDesignations)
                {
                    // Remove the subject from each of the other decisions
                    List<Option> allOptions = Options.Where(option => option != designatedOption).Select(option => option.WithoutContent(selectedContent, false)).ToList<Option>();
                    // Remove the subject from the designated decision, but also reduce the Pick of that decision
                    allOptions.Add(designatedOption.WithoutContent(selectedContent, true));
                    // Create a new decision which is a combination of all decisions when the subject is removed
                    possibleResults.Add(new Decision(this, options: allOptions, selectionType: Selection.CP, creditPoints: CreditPoints() - selectedContent.CreditPoints())); // TODO: check that this works when this causes a decision to have a negative Pick
                    
                }
            }
            // If none of the options can be designated, remove the selected subject from the desicion
            if (!designated || !possibleResults.Any())
            {
                // Remove the subject from each of the decisions
                List<Option> allOptions = Options.Where(option => option != selectedContent).Select(option => option.WithoutContent(selectedContent, false)).ToList();
                // Create a new decision which is a combination of all decisions when the subject is removed
                possibleResults.Add(new Decision(this, options: allOptions, selectionType: Selection.CP, creditPoints: CreditPoints()));
            }
            // Create a new decision that is a combination of all possible decisions, and simplify it
            return new Decision(this, options: possibleResults.ToList<Option>(), selectionType: Selection.CP, creditPoints: possibleResults.First().CreditPoints()).GetSimplifiedDecision(); // TODO: make sure that creditPoints is consistent
        }

        /// <summary> Remove the subjects that have been banned </summary>
        Decision RemoveBannedSubjects(Plan plan)
        {
            // Iterate recusively through Options
            List<Option> remainingOptions = new List<Option>();
            foreach (Option option in Options)
            {
                // Only include contents that are not banned and are not in the list of reasons (eg STAT3199 requires STAT units at 3000 level)
                if (option is Content content && !content.HasBeenBanned(plan) && !GetReasons().Contains(content))
                    remainingOptions.Add(content);
                // Include decisions that have had their decisions removed
                if (option is Decision decision)
                    remainingOptions.Add(decision.RemoveBannedSubjects(plan));
            }
            // AND decisions that have any banned options are impossible
            if (SelectionType == Selection.AND && remainingOptions.Count < Options.Count)
                return new ImpossibleDecision(this);
            // Return a new decision with the remaining options
            return new Decision(this, options: remainingOptions, selectionType: SelectionType, creditPoints: SelectionType == Selection.CP ? CreditPoints() : (int?)null);
        }

        /// <summary> If this decision is an elective, how many credit point are part of that elective </summary>
        public int SizeOfElective()
        {
            if (IsElective())
                return CreditPoints();
            // Take the sum of each SubDecision's SizeOfElective()
            return Options.Sum(option => option is Decision decision ? decision.SizeOfElective() : 0);
        }

        /// <summary>
        /// Find all options in this decisision and set their parent to a particular subject
        /// </summary>
        /// <param name="parent"></param>
        public void FindChildren(Subject parent)
        {
            // Skip electives
            if (IsElective()) return;
            // Iterate over options
            foreach (Option option in Options)
            {
                // If the option is a decision, recursively call this function
                if (option is Decision decision)
                    decision.FindChildren(parent);
                // Create an edge from the main subject to the option here
                if (option is Subject subject)
                    subject.Parents.Add(parent);
            }
        }

        public override bool EnoughCreditPoints(Plan plan, int creditPointsAvailable, out int creditPointsRequired)
        {
            creditPointsRequired = 0;
            if (creditPointsAvailable == int.MaxValue)
                return true;
            if (creditPointsAvailable < 0)
                return false;

            // If the number of available creditpoints is negative, return false
            if (creditPointsAvailable < 0)
                return false;

            // If this is an Elective, just assume that it is allowed
            if (IsElective())
                return true;

            
            switch (SelectionType)
            {
                case Selection.OR:
                    // Pick the smallest option
                    int? smallest = null;
                    foreach (Option option in Options)
                    {
                        int limit = smallest ?? creditPointsAvailable;
                        if (option.EnoughCreditPoints(plan, limit, out int result))
                            if (!smallest.HasValue || result < smallest.Value)
                                smallest = result;
                        if (smallest.HasValue && smallest.Value == 0)
                            break;
                    }
                    creditPointsRequired = smallest ?? int.MaxValue;
                    break;
                case Selection.AND:
                    // One possible answer is the sum of creditPoints of all subject options
                    creditPointsRequired = Options.Where(option => option is Subject subject && !subject.HasBeenCompleted(plan, Time.All)).Sum(option => option.CreditPoints());
                    // The other ansewr is the largest option's result from EnoughCreditPoints
                    foreach (Option option in Options)
                    {
                        option.EnoughCreditPoints(plan, creditPointsAvailable, out int result);
                        if (result > creditPointsRequired)
                            creditPointsRequired = result;
                    }
                    break;
                case Selection.CP:
                    // One possible answer is the number of credit points required to solve this. For example, this is "80cp from laws" at 2 laws subjects are already picked
                    creditPointsRequired = CreditPoints() - Options.Where(option => option is Subject subject && subject.HasBeenCompleted(plan, Time.All)).Sum(option => option.CreditPoints());
                    /* This algorithm finds the creditPointsRequired of every Option.
                     * It selects the earliest options (enough to satisfy CreditPoints),
                     *   then it returns the latest EarliestCompletionTime out of those options.
                     * It also continuously uses this result to make recursive calls faster
                     */
                    SortedDictionary<int, List<Option>> RequiredCreditPointsDistribution = new SortedDictionary<int, List<Option>>();
                    int? answerSoFar = null;
                    foreach (Option option in Options)
                    {
                        // Find the option's creditPointsRequired
                        int limit = answerSoFar ?? creditPointsAvailable;
                        if (option.EnoughCreditPoints(plan, limit, out int result))
                        {
                            // Add this option to the distribution
                            if (!RequiredCreditPointsDistribution.ContainsKey(result))
                                RequiredCreditPointsDistribution[result] = new List<Option>();
                            RequiredCreditPointsDistribution[result].Add(option);
                            // Calculate answerSoFar
                            int usedCP = 0;
                            answerSoFar = 0;
                            foreach (var kvp in RequiredCreditPointsDistribution)
                            {
                                answerSoFar = kvp.Key;
                                foreach (Option recordedOption in kvp.Value)
                                {
                                    usedCP += recordedOption.CreditPoints();
                                    if (usedCP >= CreditPoints())
                                        break;
                                }
                                if (usedCP >= CreditPoints())
                                    break;
                            }
                            // If not enough optoins were selected for this decision, an answer cannot yet be evaluated
                            if (usedCP < CreditPoints())
                                answerSoFar = null;
                            // If the answer so far is less than the answer calculated at the start of this case (before the big comment), give up
                            else if (answerSoFar < creditPointsRequired)
                                break;
                        }
                    }
                    // Choose the bigger of the two calculated answers
                    if (answerSoFar.HasValue && answerSoFar > creditPointsRequired)
                        creditPointsRequired = answerSoFar.Value;
                    break;
                default:
                    throw new ArgumentException();
            }

            // Return true if there are enough credit points available for the credit points that are required
            return creditPointsRequired <= creditPointsAvailable;


            /*
            // I haven't worked out how to properly do this. So long as I write something that produces a value less than or equal to the correct value, it's a good enough lower bound
            // The reason this is such a difficult task is because the structure of prerequisites does not make a tree, and the actual answer could be a weird combination of overlapping subjects
            // Even if a simple recursive function gave the right answer, there would be the problem that LAWS units takes too long to process
            if (IsElective()) return 0; 
            return Options
                .Where(option => !(option is Decision decision && decision.IsElective()))
                .Select(option => option.CreditPointsRequired(plan, count))
                .DefaultIfEmpty().Min();
            */
        }
    }

    /// <summary>
    /// A decision that is hard-coded to be impossible
    /// </summary>
    class ImpossibleDecision : Decision
    {
        public ImpossibleDecision(Option reason) : base (reason, options: new List<Option>(), selectionType: Selection.OR) { }

        public override bool HasBeenBanned()
        {
            return true;
        }
        public override bool HasBeenCompleted()
        {
            return false;
        }

        public override string ToString()
        {
            return "[impossible]";
        }
    }

    /// <summary>
    /// A decision that is hard-coded to pass
    /// </summary>
    class CompletedDecision : Decision 
    {
        public CompletedDecision(Option reason) : base(reason, options: new List<Option>(), selectionType: Selection.CP, creditPoints: 0) { }

        public override bool HasBeenCompleted()
        {
            return true;
        }

        public override bool HasBeenBanned()
        {
            return false;
        }

        public override string ToString()
        {
            return "[complete]";
        }
    }

    public enum Selection { OR, CP, AND }

    public static class OptionListExtentions
    {
        public static bool SumOfCreditPointsIsGreaterThanOrEqualTo(this IEnumerable<Option> options, Func<Option, bool> predicate, int minimum)
        {
            // This function is just an optimized version of this line of code:
            // return list.Where(predicate).Sum(option => option.CreditPoints()) >= minimum;

            if (minimum <= 0) return true;
            int sum = 0;
            foreach (Option option in options.Where(predicate))
            {
                sum += option.CreditPoints();
                if (sum >= minimum)
                    return true;
            }
            return false;
        }

        public static bool SumOfCreditPointsIsGreaterThan(this IEnumerable<Option> options, Func<Option, bool> predicate, int minimum)
        {
            return SumOfCreditPointsIsGreaterThanOrEqualTo(options, predicate, minimum + 1);
        }

        public static bool SumOfCreditPointsIsLessThan(this IEnumerable<Option> options, Func<Option, bool> predicate, int maximum)
        {
            return !SumOfCreditPointsIsGreaterThanOrEqualTo(options, predicate, maximum);
        }

        public static bool SumOfCreditPointsIsLessThanOrEqualTo(this IEnumerable<Option> options, Func<Option, bool> predicate, int maximum)
        {
            return !SumOfCreditPointsIsGreaterThan(options, predicate, maximum);
        }
    }
}
