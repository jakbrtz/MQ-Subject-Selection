using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Subject_Selection
{
    // When I started writing this program, I thought that the algorithms would involve duplicating the plan and experimenting on the duplicates
    // This is why the Plan class is not a static class
    // It is possible that in the future, HasBeenBanned might involve duplicating this class
    public class Plan
    {
        public Dictionary<Time, List<Subject>> SubjectsInOrder { get; } = new Dictionary<Time, List<Subject>>();
        public List<Decision> Decisions { get; } = new List<Decision>();
        public HashSet<Subject> SelectedSubjects { get; } = new HashSet<Subject>();
        public HashSet<Course> SelectedCourses { get; } = new HashSet<Course>();

        readonly Dictionary<Subject, Time> forcedTimes = new Dictionary<Subject, Time>();

        readonly public Dictionary<Time, int> MaxCreditPoints = new Dictionary<Time, int>();
        public Dictionary<Content, List<Content>> BannedContents { get; } = new Dictionary<Content, List<Content>>();
        public HashSet<Edge> ContentRelations { get; } = new HashSet<Edge>();
        public Dictionary<Subject, Time> EarliestCompletionTimes { get; } = new Dictionary<Subject, Time>();
        public Dictionary<Subject, int> CreditPointsRequiredForCompletion { get; } = new Dictionary<Subject, int>();

        public Plan() { }

        public Plan(Plan other)
        {
            foreach (var semester in other.SubjectsInOrder)
                SubjectsInOrder.Add(semester.Key, new List<Subject>(semester.Value));
            Decisions.AddRange(other.Decisions);
            
            SelectedSubjects = new HashSet<Subject>(other.SelectedSubjects);
            SelectedCourses = new HashSet<Course>(other.SelectedCourses);
            foreach (var BannedContent in other.BannedContents)
                BannedContents.Add(BannedContent.Key, new List<Content>(BannedContent.Value));
            EarliestCompletionTimes = new Dictionary<Subject, Time>(other.EarliestCompletionTimes);
            CreditPointsRequiredForCompletion = new Dictionary<Subject, int>(other.CreditPointsRequiredForCompletion);

            forcedTimes = new Dictionary<Subject, Time>(other.forcedTimes);
            MaxCreditPoints = new Dictionary<Time, int>(other.MaxCreditPoints);
            ContentRelations = new HashSet<Edge>(other.ContentRelations);
        }

        public void AddContents(IEnumerable<Content> contents)
        {
            contents = contents.Except(SelectedSubjects);
            if (!contents.Any())
                return;
            foreach (Content content in contents)
                if (content is Subject)
                    SelectedSubjects.Add(content as Subject);
                else
                    SelectedCourses.Add(content as Course);
            RefreshBannedSubjectsList();
            RefreshRelations();
            Order();
        }

        public void RemoveContent(Content content)
        {
            if (content is Subject)
                SelectedSubjects.Remove(content as Subject);
            else
                SelectedCourses.Remove(content as Course);
            RefreshBannedSubjectsList();
            RefreshRelations();
            Order();
        }

        public void AddDecision(Decision decision)
        {
            Decisions.Add(decision);
            RefreshBannedSubjectsList();
        }

        public void RemoveDecision(Decision decision)
        {
            if (Decisions.RemoveAll(match => match == decision) > 0) // I have to use a predicate because I overrode `Decision.Equals`
                RefreshBannedSubjectsList();
        }

        public void ClearDecisions()
        {
            Decisions.Clear();
            RefreshBannedSubjectsList();
        }

        public void AddYear()
        {
            int year = MaxCreditPoints.Any() ? 1 + MaxCreditPoints.Keys.Max(time => time.year) : 1;

            if (year == 1 || year > 20)
            {
                MaxCreditPoints.Add(new Time { year = year, session = Session.S1 }, 40);
                MaxCreditPoints.Add(new Time { year = year, session = Session.WV }, 10);
                MaxCreditPoints.Add(new Time { year = year, session = Session.S2 }, 40);
                MaxCreditPoints.Add(new Time { year = year, session = Session.S3 }, 20);
            }
            else
            {
                MaxCreditPoints.Add(new Time { year = year, session = Session.S1 }, GetMaxCreditPoints(year - 1, Session.S1));
                MaxCreditPoints.Add(new Time { year = year, session = Session.WV }, GetMaxCreditPoints(year - 1, Session.WV));
                MaxCreditPoints.Add(new Time { year = year, session = Session.S2 }, GetMaxCreditPoints(year - 1, Session.S2));
                MaxCreditPoints.Add(new Time { year = year, session = Session.S3 }, GetMaxCreditPoints(year - 1, Session.S3));
            }

            foreach (Time time in MaxCreditPoints.Keys.Where(time => time.year == year))
                SubjectsInOrder[time] = new List<Subject>();

            if (year > 90)
                throw new InvalidOperationException("Unless the user is a fool, this should not happen");
        }

        public IEnumerable<Time> NotableTimes()
        {
            return MaxCreditPoints.Keys.Where(time => time.session == Session.S1 || time.session == Session.S2 || GetSemester(time).Any());
        }

        public List<Subject> GetSemester(Time time)
        {
            if (SubjectsInOrder.ContainsKey(time))
                return SubjectsInOrder[time];
            return new List<Subject>();
        }

        public void ForceTime(Subject subject, Time time)
        {
            forcedTimes[subject] = time;
            Order();
        }

        public void UnForceTime(Subject subject)
        {
            if (forcedTimes.Remove(subject))
                Order();
        }

        public Time? GetForcedTime(Subject subject)
        {
            if (forcedTimes.ContainsKey(subject))
                return forcedTimes[subject];
            return null;
        }

        public int GetMaxCreditPoints(Time time)
        {
            return GetMaxCreditPoints(time.year, time.session);
        }

        public int GetMaxCreditPoints(int year, Session session)
        {
            if (year <= 0)
                throw new ArgumentException("Year must be positive");
            if (MaxCreditPoints.TryGetValue(new Time { year = year, session = session }, out int value))
                return value;
            return GetMaxCreditPoints(year - 1, session);
        }

        public void SetMaxCreditPoints(Time time, int creditPoints)
        {
            // Set the new value
            MaxCreditPoints[time] = creditPoints;
            // Reorder the schedule
            RefreshEarliestTimes(); 
            Order();
            // TODO: do I need to Analyze everything?
        }

        public void SetMaxCreditPoints(Session session, int creditPoints)
        {
            // Set the new values
            foreach (Time key in MaxCreditPoints.Keys.Where(time => time.session == session).ToList())
                MaxCreditPoints[key] = creditPoints;
            // Reorder the schedule
            RefreshEarliestTimes(); 
            Order();
        }

        public void SetMaxCreditPoints(int creditPoints)
        {
            // Set the new values
            foreach (Time key in MaxCreditPoints.Keys.Where(time => time.session == Session.S1 || time.session == Session.S2).ToList())
                MaxCreditPoints[key] = creditPoints;
            // Reorder the schedule
            RefreshEarliestTimes();
            Order();
            // Check how this affects times
        }

        public void RefreshEarliestTimes()
        {
            Stopwatch timer1 = new Stopwatch();
            timer1.Restart();

            // Prepare to analyze everything
            Queue<Subject> subjectQueue = new Queue<Subject>(); // TODO: uniqueQueue
            foreach (Subject subject in Parser.AllSubjects())
                subjectQueue.Enqueue(subject);
            // TODO: prioritize subjects if they're a leaf
            // Iterate through the queue
            while (subjectQueue.Any())
            {
                Subject current = subjectQueue.Dequeue();
                // Find the earliest time that both the prerequisites and the corequisites can be completed in
                Time timePrerequisites = current.Prerequisites.EarliestCompletionTime(this).Next();
                Time timeCorequisites = current.Corequisites.EarliestCompletionTime(this);
                // Pick the later of those times
                Time evaluatedTime = timeCorequisites.IsEarlierThan(timePrerequisites) ? timePrerequisites : timeCorequisites;
                // If this subject has been listed as a banned subject by the plan, return an Impossible time
                if (BannedContents.ContainsKey(current)) evaluatedTime = Time.Impossible;
                // Make sure the time aligns with the subject's semesters
                while (evaluatedTime.year <= current.earliestYear - 2020 || // TODO: get rid of magic number next year
                    !current.Semesters.Any(semester => semester.session == evaluatedTime.session) ||
                    GetMaxCreditPoints(evaluatedTime) < current.CreditPoints())
                {
                    evaluatedTime = evaluatedTime.Next();
                    // Prevent the subject from being later than Impossible (otherwise this leads to infinite loops)
                    if (Time.Impossible.IsEarlierThan(evaluatedTime))
                    {
                        evaluatedTime = Time.Impossible;
                        break;
                    }
                }
                // Check if the value changed
                if (!EarliestCompletionTimes.TryGetValue(current, out Time initialEarliestValue)) initialEarliestValue = Time.Early;
                EarliestCompletionTimes[current] = evaluatedTime;
                if (initialEarliestValue.CompareTo(evaluatedTime) != 0)
                    // If the value changed, re-analyze every subject that depends on this subject
                    foreach (Subject parent in current.Parents)
                        subjectQueue.Enqueue(parent);
            }

            timer1.Stop();
            Console.WriteLine("Getting times:       " + timer1.ElapsedMilliseconds + "ms");
        }

        public void RefreshCreditPointsRequiredForCompletion()
        {
            Stopwatch timer4 = new Stopwatch();
            timer4.Restart();

            // Prepare to analyze everything
            Queue<Subject> subjectQueue = new Queue<Subject>(); // TODO: uniqueQueue
            foreach (Subject subject in Parser.AllSubjects())
                subjectQueue.Enqueue(subject);
            // TODO: prioritize subjects if they're a leaf
            // Iterate through the queue
            while (subjectQueue.Any())
            {
                Subject current = subjectQueue.Dequeue();


                /*

                int evaluatedCreditPoints = 0;
                if (creditPointsAvailable == int.MaxValue)
                    return true;
                if (creditPointsAvailable < 0)
                    return false;
                // If this subject is already part of the plan, then no more credit points are required
                if (HasBeenCompleted(plan, Time.All))
                    return true;
                // Otherwise the credit points from this subject are required
                evaluatedCreditPoints = current.CreditPoints();
                // Also the credit points from the prerequisites and corequisites are required
                if (!current.Prerequisites.EnoughCreditPoints(this, creditPointsAvailable - evaluatedCreditPoints, out int creditPointsRequiredPrerequisites))
                    return false;
                if (!current.Corequisites.EnoughCreditPoints(this, creditPointsAvailable - evaluatedCreditPoints, out int creditPointsRequiredCorequisites))
                    return false;
                // Add the larger of creditPointsRequiredRequisites
                evaluatedCreditPoints += creditPointsRequiredPrerequisites > creditPointsRequiredCorequisites ? creditPointsRequiredPrerequisites : creditPointsRequiredCorequisites;
                // If the number of available credit points is greater than or equal to the required amount, then there are enough credit points
                return evaluatedCreditPoints <= creditPointsAvailable;



                



                // Check if the value changed
                if (!CreditPointsRequiredForCompletion.TryGetValue(current, out int initialCreditPointsRequiredForCompletion)) initialCreditPointsRequiredForCompletion = 0;
                CreditPointsRequiredForCompletion[current] = evaluatedCreditPoints;
                if (initialCreditPointsRequiredForCompletion.CompareTo(evaluatedCreditPoints) != 0)
                    // If the value changed, re-analyze every subject that depends on this subject
                    foreach (Subject parent in current.Parents)
                        subjectQueue.Enqueue(parent);


                */
            }

            timer4.Stop();
            Console.WriteLine("Getting times:       " + timer4.ElapsedMilliseconds + "ms");
        }

        public int RemainingCreditPoints()
        {
            if (!SelectedCourses.Any())
                return int.MaxValue;
            return SelectedCourses.First().CreditPoints() - SelectedSubjects.Sum(subject => subject.CreditPoints());
        }


        public override string ToString()
        {
            string output = "";
            foreach (List<Subject> semester in SubjectsInOrder.Values)
                output += "[" + string.Join(" ", semester) + "] ";
            return output;
        }

        public IEnumerable<Subject> SelectedSubjectsSoFar(Time time)
        {
            return SubjectsInOrder.Where(semester => semester.Key.IsEarlierThanOrAtTheSameTime(time)).SelectMany(kvp => kvp.Value);
        }

        private void RefreshRelations()
        {
            ContentRelations.Clear();
            // Iterate over every selected subject to see what it links to
            foreach (Content content in SelectedSubjects.Cast<Content>().Concat(SelectedCourses))
            {
                // Use a breadth-first search for subjects that this subject rely on
                Queue<(Decision, Importance)> toAnalyze = new Queue<(Decision, Importance)>();
                toAnalyze.Enqueue((content.Prerequisites, Importance.Compulsory));
                toAnalyze.Enqueue((content.Corequisites, Importance.Compulsory));
                while (toAnalyze.Any())
                {
                    (Decision requisite, Importance importance) = toAnalyze.Dequeue();
                    // Ignore electives
                    if (requisite.IsElective()) continue;
                    // Rank the importance according to whether the option is compulsory or not
                    importance = (importance == Importance.Compulsory && requisite.MustPickAll()) ? Importance.Compulsory : Importance.Optional;
                    foreach (Option option in requisite.Options)
                    {
                        // Search the sub-decisions
                        if (option is Decision decision)
                            toAnalyze.Enqueue((decision, importance));
                        // If the option has been selected, create an edge from the subject to this option
                        else if (SelectedSubjects.Contains(option))
                            ContentRelations.Add(new Edge { source = content, importance = importance, dest = option as Content });
                    }
                }
            }
        }

        private void Order()
        {
            Stopwatch timerOrder = new Stopwatch();
            timerOrder.Restart();

            // Check that all forcedTimes are allowed
            foreach (var kvp in forcedTimes)
                if (!kvp.Key.AllowedDuringSemester(kvp.Value, this))
                    forcedTimes.Remove(kvp.Key);

            // This variable works because IEnumerables get evaluated every time a method is called on it. I usually use a local function instead of a variable.
            IEnumerable<Subject> RemainingSubjects = SelectedSubjects.Except(SelectedSubjectsSoFar(Time.All)).OrderBy(subject => subject.GetLevel());

            SubjectsInOrder.Clear();
            for (Time semester = Time.First; MaxCreditPoints.Keys.Contains(semester) || RemainingSubjects.Any(); semester = semester.Next())
            {
                // If neccessary, add another year to the planner
                if (!MaxCreditPoints.Keys.Contains(semester)) AddYear();
                // Create a new semester and add it to SubjectsInOrder (this works because a reference is being added to the Dictionary)
                List<Subject> semesterClasses = new List<Subject>();
                SubjectsInOrder[semester] = semesterClasses;
                // Fill the semester with subjects that can be chosen
                int selectedCredits = 0;
                while (selectedCredits < GetMaxCreditPoints(semester))
                {
                    // Prepare a list of what subjects could be chosen
                    var possibleSubjects = RemainingSubjects;
                    // Do not pick subjects with forced times later than the current session
                    possibleSubjects = possibleSubjects.Where(subject => !(forcedTimes.ContainsKey(subject) && semester.IsEarlierThan(forcedTimes[subject])));
                    // Pick from subjects that are allowed during this semester
                    possibleSubjects = possibleSubjects.Where(subject => subject.AllowedDuringSemester(semester, this));
                    // Pick from subjects which would not result in Credit Overflow
                    possibleSubjects = possibleSubjects.Where(subject => subject.CreditPoints() + semesterClasses.Sum(selected => selected.CreditPoints()) <= GetMaxCreditPoints(semester));
                    // Check if any subjects are forced
                    IEnumerable<Subject> forcedSubjects = possibleSubjects
                        .Where(subject => forcedTimes.ContainsKey(subject) && forcedTimes[subject].IsEarlierThanOrAtTheSameTime(semester));
                    // If any subjects are forced, only consider the forced subjects
                    if (forcedSubjects.Any())
                        possibleSubjects = forcedSubjects.OrderBy(subject => forcedTimes[subject]);
                    // Otherwise, filter subjects according to whether their requisites are completed
                    else
                        possibleSubjects = possibleSubjects.Where(subject => RequisitesHaveBeenSelected(subject, semester));
                    // Favor subjects that have many other subjects relying on them
                    possibleSubjects = possibleSubjects.OrderByDescending(subject => RemainingSubjects.Sum(other => IsAbove(parent: other, child: subject, out int size) ? size : 0))
                        // Favor subjects that cannot fit in many semesters
                        .ThenBy(subject => subject.Semesters.Select(semester => semester.session).Distinct().Count())
                        // Favor lower level subjects
                        .ThenBy(subject => subject.GetLevel())
                        // Favor subjects that don't require many electives as prerequisites
                        .ThenBy(subject => subject.Prerequisites.SizeOfElective() + subject.Corequisites.SizeOfElective())
                        // Favor subjects that are requisites to their Degree (or any course)
                        .ThenBy(subject => ContentRelations.Any(relation => relation.source is Course && relation.dest == subject) ? 0 : 1);
                    // Pick the first item from that list
                    Subject nextSubject = possibleSubjects.FirstOrDefault();
                    // If no subject was chosen, go to the next semester
                    if (nextSubject == null) break;
                    // Add the selected subject to this semester
                    semesterClasses.Add(nextSubject);
                    // Keep track of how many more times this loop can repeat
                    selectedCredits += nextSubject.CreditPoints();
                }
            }

            if (SelectedSubjects.Except(SelectedSubjectsSoFar(Time.All)).Any())
                throw new InvalidOperationException("Not all the subjects were added to the table");

            timerOrder.Stop();
            Console.WriteLine("Ordering Plan:       " + timerOrder.ElapsedMilliseconds + "ms");

            // Helper functions to determine whether a subject can be picked

            bool RequisitesHaveBeenSelected(Subject subject, Time time, Decision requisite = null)
            {
                // This function goes through a subject's requisites to make sure every SelectedSubject that is a requisite to this subject has already been added to the schedule

                // If no requisite has been specified, then this must have been called by outside this function
                if (requisite == null)
                {
                    // Check if this is one of those subjects which don't have any actual requisites (eg no data or "permission by special approval")
                    // If so, assign it an earliest position based on its level
                    if (subject.Prerequisites.HasBeenCompleted() && subject.Corequisites.HasBeenCompleted())
                        return time.AsNumber() / 2 >= subject.GetLevel() - 1;
                    // Call this function again but with the prerequisites and corequisites
                    return RequisitesHaveBeenSelected(subject, time.Previous(), subject.Prerequisites) && RequisitesHaveBeenSelected(subject, time, subject.Corequisites);
                }

                // If the requisit is met by the scheduled selected subjects, return true
                if (requisite.HasBeenCompleted(this, time))
                    return true;
                // If the requisit is an elective, don't process it. Instead, compare the subject's level to the time 
                if (requisite.IsElective())
                    return subject.GetLevel() <= time.Next().year;
                // Iterate over all of the requisite's option
                // The requisite's selectionType doesn't matter, all that matters is whether it HasBeenCompleted.
                foreach (Option option in requisite.Options)
                    // If the option is a subject that needs to be picked, hasn't been picked, and must come before the current subject: return false
                    if (option is Content content && !SelectedSubjectsSoFar(time).Contains(content) && OptionMustComeBeforeSubject(parent: subject, child: content as Subject))
                        return false;
                    // If the option is a decision such that its requisites haven't been selected: return false
                    else if (option is Decision decision && !RequisitesHaveBeenSelected(subject, time, decision))
                        return false;
                // If the code reached this far then all the requisites that need to be scheduled have already been sceduled
                return true;
            }

            bool OptionMustComeBeforeSubject(Subject parent, Subject child)
            {
                // Find the edges between parent and child
                List<Edge> relevantEdges = ContentRelations.Where(edge => edge.source == parent && edge.dest == child).ToList();
                // If no edges are found, return false
                if (!relevantEdges.Any())
                    return false;
                // If a compulsory edge is found
                if (relevantEdges.Any(edge => edge.importance == Importance.Compulsory))
                    // Return true if there is no series of compulsory relations from child to parent, and false otherwise
                    return !IsAbove(parent: child, child: parent, onlyCheckCompulsory: true, size: out _);
                // This means that there are only Optional Relations
                // Only return true if there is no series of relations from child to parent
                return !IsAbove(parent: child, child: parent, size: out _);
            }

            bool IsAbove(Subject parent, Subject child, out int size, bool onlyCheckCompulsory = false)
            {
                // This is a breadth-first search in a cyclic directed graph represented by its edges (ContentRelations).
                // Start at `parent` and search for `child`
                // Also record the size of the path from parent to child
                HashSet<Content> visited = new HashSet<Content>();
                Queue<(Content content, int size)> toAnalyze = new Queue<(Content content, int size)>();
                toAnalyze.Enqueue((parent, 0));
                while (toAnalyze.Any())
                {
                    Content current;
                    (current, size) = toAnalyze.Dequeue();
                    if (visited.Contains(current))
                        continue;
                    visited.Add(current);
                    if (current == child)
                        return true;
                    foreach (Edge edge in ContentRelations.Where(edge => edge.source == current && (edge.importance == Importance.Compulsory || !onlyCheckCompulsory)))
                        toAnalyze.Enqueue((edge.dest, size + 1));
                }
                size = -1;
                return false;
            }
        }

        void RefreshBannedSubjectsList()
        {
            void AddBannedSubject(Content banned, Option reason)
            {
                // Determine whether the reason is a single subject or a decision
                List<Content> reasons;
                if (reason is Decision decision)
                    reasons = decision.GetReasons().ToList(); 
                else // reason is Content
                    reasons = new List<Content>() { reason as Content };
                // Add the reasons to the existing list of reasons, or create a new entry in the dictionary
                if (BannedContents.TryGetValue(banned, out List<Content> originalReasonsList))
                    originalReasonsList.AddRange(reasons);
                else
                    BannedContents.Add(banned, reasons);
            }

            BannedContents.Clear();
            // Get all selected subjects and check them for NCCWs
            foreach (Subject subject in SelectedSubjects)
                foreach (string id in subject.NCCWs)
                    if (Parser.TryGetSubject(id, out Subject nccw))
                        AddBannedSubject(nccw, subject);
            // Get all selected courses and check them for NCCWs
            foreach (Course course in SelectedCourses)
                foreach (string id in course.NCCWs)
                    if (Parser.TryGetContent(id, out Content nccw))
                        AddBannedSubject(nccw, course);
            /* TODO: fix assumptions
             * This code assumes that when subject X is on subject Y's nccw list, then subject Y is on subject X's nccw list
             * I have found 45 exceptions to this assumption. Does that have a special meaning, or is it an incorrect data entry?
             */
            // Check which decisions force a banned subject
            foreach (Decision decision in Decisions)
                foreach (Content subject in decision.ForcedBans())
                    AddBannedSubject(subject, decision);   
        }
    }

    public enum Importance { Optional, Compulsory }
    public struct Edge
    {
        public Content source;
        public Importance importance;
        public Content dest;

        public override string ToString()
        {
            return source.ToString() + " - " + dest.ToString() + " (" + importance + ")";
        }
    }
}
