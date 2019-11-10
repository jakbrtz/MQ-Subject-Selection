using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    abstract public class Criteria
    {
        //I have made this superclass to allow prerequisits so be made of other prerequisits
        public abstract bool HasBeenMet(Plan plan, int time);
        public abstract bool HasBeenBanned(Plan plan);
        public abstract int EarliestCompletionTime(List<int> MaxSubjects);
    }

    public class Subject : Criteria
    {
        public string ID { get; }
        public List<int> Semesters { get; }
        public Prerequisit Prerequisits { get; }
        public string[] NCCWs { get; }

        public Subject(string id, string times, string prerequisits, string nccws)
        {
            ID = id;

            Semesters = new List<int>();
            foreach (string time in times.Split('\n'))
                if (time.StartsWith("S"))
                    Semesters.Add(int.Parse(time.Substring(1, 1)) - 1);
            Semesters = Semesters.Distinct().ToList();

            Prerequisits = new Prerequisit(this, prerequisits);
            NCCWs = nccws.Split(',');
        }

        public override string ToString()
        {
            return ID;
        }

        public override bool HasBeenMet(Plan plan, int time)
        {
            if (!plan.SelectedSubjects.Contains(this)) return false;
            return plan.SelectedSubjectsSoFar(time).Contains(this);
        }

        public override bool HasBeenBanned(Plan plan)
        {
            return plan.SelectedSubjects.Exists(subject => subject.NCCWs.Contains(this.ID)) || Prerequisits.HasBeenBanned(plan);
            //MATH123 has an extra detail about NCCW, which would require this to be completely remade
            //Check whether other subjects have those conditions
        }

        public override int EarliestCompletionTime(List<int> MaxSubjects)
        {
            //Find the first time after the prerequisits has been satisfied which also allows for the semester
            int time = Prerequisits.EarliestCompletionTime(MaxSubjects) + 1;
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
            return plan.SubjectsInOrder.FindIndex(semester => semester.Contains(this));
        }
        
    }

    public class Prerequisit : Criteria
    {
        List<Subject> reasons = new List<Subject>();
        private string criteria;
        private List<Criteria> options;
        private int pick = 1;
        private Selection selectionType = Selection.OR;
        private int earliestCompletionTime = -1;

        public Prerequisit(Criteria reason, string criteria)
        {
            if (reason is Subject)
                reasons.Add(reason as Subject);
            else if (reason is Prerequisit)
                reasons.AddRange((reason as Prerequisit).reasons);
            this.criteria = criteria;

            if (criteria == "") ToString();
        }

        public Prerequisit(Criteria reason, List<Criteria> options, int pick, Selection selectionType, string criteria)
        {
            if (reason is Subject)
                reasons.Add(reason as Subject);
            else if (reason is Prerequisit)
                reasons.AddRange((reason as Prerequisit).reasons);
            this.criteria = criteria;
            this.options = options;
            this.pick = pick;
            this.selectionType = selectionType;

            if (criteria == "") ToString();
        }

        public List<Criteria> GetOptions()
        {
            // Load the cached result
            if (options != null) return options;

            // Create a list of options and prepare to translate the text description
            options = new List<Criteria>();

            criteria = SubjectReader.DealWithBrackets(criteria);

            // Get rid of words that make this difficult
            criteria = criteria.Replace("or above", "orabove").Replace("and above", "andabove").Replace(" only", "");

            // Check if the criteria is a single subject
            if (SubjectReader.TryGetSubject(criteria, out Subject subject))
            {
                options.Add(subject);
                selectionType = Selection.AND;
                return options;
            }

            // Splits the criteria accounting for brackets, and putting the output in `tokens`
            List<string> tokens;
            bool TrySplit(string search, string without = "", bool firstWord = false)
            {
                return SubjectReader.SplitAvoidingBrackets(criteria, search, out tokens, without, firstWord);
            }

            // Check if the criteria contains specific key words

            if (TrySplit(" INCLUDING "))
            {
                selectionType = Selection.AND;
                options.Add(new Prerequisit(this, tokens[0]));
                options.Add(new Prerequisit(this, tokens[1]));
            }

            else if (TrySplit("POST HSC"))
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisit(this, remaining));
            }

            else if (TrySplit("HSC"))
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisit(this, remaining));
            }

            else if (TrySplit(" AND "))
            {
                selectionType = Selection.AND;
                foreach (string token in tokens)
                    options.Add(new Prerequisit(this, token));
            }

            else if (TrySplit("ADMISSION TO"))
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisit(this, remaining));
            }

            else if (TrySplit("PERMISSION"))
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisit(this, remaining));
            }

            else if (TrySplit("A GPA OF"))
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisit(this, remaining));
            }

            else if (TrySplit("CP", without: "CP OR", firstWord: true)) //Includes `CP AT`, 'CP IN`, and `CP FROM`
            {
                selectionType = Selection.CP;
                pick = int.Parse(tokens[0]) / 10; // 10 is a magic number equal to the amount of credit points per subject
                options = SubjectReader.GetSubjectsFromQuery(tokens[1]);
            }

            else if (TrySplit(" OR "))
            {
                selectionType = Selection.OR;
                pick = 1;
                foreach (string token in tokens)
                    options.Add(new Prerequisit(this, token));
            }

            else if (criteria == "")
            {
                selectionType = Selection.AND;
                pick = 0;
            }

            // Unknown edge cases
            else if (
                !(criteria.Split('(')[0].Length <  8 && int.TryParse(criteria.Split('(')[0].Substring(criteria.Split('(')[0].Length-3), out int idk)) &&
                !(criteria.Split('(')[0].Length == 8 && int.TryParse(criteria.Split('(')[0].Substring(4), out int wut)))
            {
                Console.WriteLine(reasons[0]);
                throw new Exception("idk how to parse this:\n" + criteria);
            }

            // If the selection type is AND then everything must be picked
            if (selectionType == Selection.AND) pick = options.Count;

            return options;
        }

        public int GetPick()
        {
            if (options == null) GetOptions();
            return pick;
        }

        public Selection GetSelectionType()
        {
            if (options == null) GetOptions();
            return selectionType;
        }

        public override string ToString()
        {
            if (criteria != "" && criteria != null)
                return criteria;
            //This is used by the GetRemainingDecision method.
            if (selectionType == Selection.CP)
            {
                criteria = (GetPick() * 10).ToString() + "cp from ";
                criteria += string.Join(" or ", GetOptions());
            }
            else
            {
                string separator = " " + selectionType + " ";
                criteria = string.Join(separator, GetOptions());
            }
            return criteria;
        }

        public List<Criteria> GetRemainingOptions(Plan plan)
        {
            return GetOptions().Where(criteria => !criteria.HasBeenMet(plan, RequiredCompletionTime(plan)) && 
                !criteria.HasBeenBanned(plan)).ToList();
        }

        public int GetRemainingPick(Plan plan)
        {
            return GetPick() - GetOptions().Count(criteria => criteria.HasBeenMet(plan, RequiredCompletionTime(plan)));
        }

        public override bool HasBeenMet(Plan plan, int time)
        {
            //Start by checking the study plan for the earliest subject that requires this decision
            if (time == -1) time = plan.SubjectsInOrder.FindIndex(semester => semester.Intersect(reasons).Any());
            //Recursively count the number of options that have been met
            return GetPick() <= GetOptions().Count(criteria => criteria.HasBeenMet(plan, time));
        }

        public override bool HasBeenBanned(Plan plan)
        {
            // Severly speed up calculation time
            if (IsElective()) return false;
            // This is a simple catch to check for bans without checking recursively
            if (GetRemainingPick(plan) > GetOptions().Count)
                return true;
            // TODO: remove this
            return false;
            //This is the most accurate, however it leads to infinite loops
            return GetRemainingPick(plan) > GetRemainingOptions(plan).Count;
        }

        public List<Subject> GetSubjects()
        {
            List<Subject> output = new List<Subject>();
            foreach (Criteria option in GetOptions())
                if (option is Subject)
                    output.Add(option as Subject);
                else if (option is Prerequisit)
                    output.AddRange((option as Prerequisit).GetSubjects());
            return output;
        }

        public List<Subject> GetRemainingSubjects(Plan plan)
        {
            List<Subject> output = new List<Subject>();
            foreach (Criteria option in GetRemainingOptions(plan))
                if (option is Subject)
                    output.Add(option as Subject);
                else if (option is Prerequisit)
                    output.AddRange((option as Prerequisit).GetRemainingSubjects(plan));
            return output;
        }

        public bool MustPickAllRemaining(Plan plan)
        {
            return GetRemainingPick(plan) == GetRemainingOptions(plan).Count;
        }

        public Prerequisit GetRemainingDecision(Plan plan)
        {
            //If the prerequisit is met then there should be nothing to return
            if (HasBeenMet(plan, RequiredCompletionTime(plan))) return new Prerequisit(this, "");
            //If there is only one option to pick from then pick it
            if (GetRemainingOptions(plan).Count == 1)
            {
                Criteria lastOption = GetRemainingOptions(plan)[0];
                if (lastOption is Prerequisit)
                    return (lastOption as Prerequisit).GetRemainingDecision(plan);
            }
            //Create a new list to store the remaining prerequisits
            List<Criteria> remainingOptions = new List<Criteria>();
            foreach (Criteria option in GetRemainingOptions(plan))
                if (option is Subject)
                    remainingOptions.Add(option);
                else if (option is Prerequisit && this.GetRemainingPick(plan) == 1 && (option as Prerequisit).GetRemainingPick(plan) == 1)
                    remainingOptions.AddRange((option as Prerequisit).GetRemainingDecision(plan).GetOptions());
                else if (option is Prerequisit)
                    remainingOptions.Add((option as Prerequisit).GetRemainingDecision(plan));
            string newcriteria = "";
            if (IsElective())
            {
                newcriteria = (GetRemainingPick(plan) * 10) + "cp";
                if (criteria.Contains("cp "))
                    newcriteria += " " + criteria.Substring(criteria.IndexOf(' ') + 1);
            }
            return new Prerequisit(this, remainingOptions, GetRemainingPick(plan), selectionType, newcriteria);
        }

        public override int EarliestCompletionTime(List<int> MaxSubjects)
        {
            if (earliestCompletionTime > -1) return earliestCompletionTime;
            //If there are no prerequisits, then the subject can be done straight away
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
            //cache the result // todo: use lazy values to cache results
            return earliestCompletionTime =
                //Get a list of all the option's earliest completion times
                GetOptions().ConvertAll(criteria => criteria.EarliestCompletionTime(MaxSubjects))
                .OrderBy(x => x).ElementAt(GetPick() - 1);
        }

        //TODO: cache result
        public int RequiredCompletionTime(Plan plan)
        {
            return reasons.Min(reason => reason.GetChosenTime(plan));
        }

        public void AddReasons(Prerequisit prerequisit)
        {
            reasons = reasons.Union(prerequisit.reasons).ToList();
        }

        public List<Subject> GetReasons()
        {
            return reasons;
        }

        public bool IsElective()
        {
            return selectionType == Selection.CP && ((!criteria.Contains("units") && !criteria.Contains("from")) || GetSubjects().Intersect(GetReasons()).Any());
        }

        public bool HasElectivePrerequisit()
        {
            return IsElective() || GetOptions().Exists(criteria => criteria is Prerequisit && (criteria as Prerequisit).HasElectivePrerequisit());
        }
    }

    public enum Selection { AND, OR, CP }
}
