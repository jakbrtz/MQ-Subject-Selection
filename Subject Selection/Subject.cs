﻿using System;
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
        public abstract int EarliestCompletionTime();
    }

    public class Subject : Criteria
    {
        public string ID { get; }
        public int CP { get; }
        public List<string> Times { get; }
        public Prerequisit Prerequisits { get; }
        public string[] NCCWs { get; }

        public Subject(string id, string cp, string times, string prerequisits, string nccws)
        {
            ID = id;
            CP = int.Parse(cp);
            Times = times.Split(' ').ToList();
            Prerequisits = new Prerequisit(this, prerequisits);
            NCCWs = nccws.Split(' ');
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
            //TODO: check whether the order of subjects matter when it comes to NCCWs
            return plan.SelectedSubjects.Exists(subject => subject.NCCWs.Contains(this.ID)) || Prerequisits.HasBeenBanned(plan);
        }

        public override int EarliestCompletionTime()
        {
            return GetPossibleTimes().Where(time => time > Prerequisits.EarliestCompletionTime()).First();
        }

        public List<int> GetPossibleTimes(int end = 10)
        {
            List<int> output = new List<int>();
            for (int year = 0; year < end; year++)
                foreach (int semester in Semesters())
                    if (Prerequisits.EarliestCompletionTime() < 3 * year + semester)
                        output.Add(3 * year + semester);
            return output;
        }

        public List<int> Semesters()
        {
            List<int> output = new List<int>();
            foreach (string time in Times)
                if (time.StartsWith("S"))
                    output.Add(int.Parse(time.Substring(1, 1)));
                //TODO: FY1, FY2, WV
                else if (time.StartsWith("FY"))
                    output.Add(int.Parse(time.Substring(2, 1)));
            return output.Distinct().ToList();
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
        private int pick;
        private string selectionType = "and";
        private int earliestCompletionTime = -1;

        public Prerequisit(Criteria reason, string criteria)
        {
            if (reason is Subject)
                reasons.Add(reason as Subject);
            else if (reason is Prerequisit)
                reasons.AddRange((reason as Prerequisit).reasons);
            this.criteria = criteria;
        }

        public Prerequisit(Criteria reason, List<Criteria> options, int pick, string selectionType, string criteria)
        {
            if (reason is Subject)
                reasons.Add(reason as Subject);
            else if (reason is Prerequisit)
                reasons.AddRange((reason as Prerequisit).reasons);
            this.criteria = criteria;
            this.options = options;
            this.pick = pick;
            this.selectionType = selectionType;
        }

        public List<Criteria> GetOptions()
        {
            if (options != null) return options;

            /* Criterior are imported as text, and need to be parsed. For example:
             * (39CP 100+) AND COMP202 AND (COMP225 OR COMP229) AND (MATH135 OR DMTH137)
             * Anything in brackets is another prereqisit
             * Anything with a dash/plus represents a range of subjects
             * The words CP/AND/OR explain how many criteria must be selected
             * It is assumed that AND and OR are not both selected
             */

            //Start by traversing the text until a space or bracket is reached
            options = new List<Criteria>();
            int i = 0;

            while (i < criteria.Length)
            {
                string currentWord = "";
                for (char c; i < criteria.Length && (c = criteria[i]) != ' ' && c != '('; i++)
                    currentWord += c;
                
                if (i < criteria.Length && criteria[i] == '(')
                {
                    //If a bracket is reached, find it's closing bracket and create another prerequisit
                    i++;
                    int indents = 1;
                    for (char c; !((c = criteria[i]) == ')' && indents == 1); i++)
                    {
                        currentWord += c;
                        if (c == '(')
                            indents++;
                        else if (c == ')')
                            indents--;
                    }
                    options.Add(new Prerequisit(this, currentWord));
                }
                else
                {
                    //If a space is reached, determine what the previous word was
                    if (currentWord == "")
                    {
                        //I can't be bothered to write the parser properly, so sometimes the word ends up being ""
                    }
                    else if (currentWord.EndsWith("CP"))
                    {
                        //If the word was a number, then it is counting how many of the options need to be selected
                        pick = int.Parse(currentWord.Substring(0, currentWord.Length - 2)) / 3;
                        selectionType = "CP";
                    }
                    else if (currentWord == "AND" || currentWord == "OR")
                    {
                        //This means the word described how to pick options
                        selectionType = currentWord;
                        if (currentWord == "OR")
                            pick = 1;
                    }
                    else if (currentWord.Contains('-') || currentWord.Contains('+'))
                    {
                        //This refers to a range of subjects to pick from
                        foreach (string subject in SubjectReader.GetSubjectsFromRange(currentWord, reasons))
                            options.Add(SubjectReader.GetSubject(subject));
                    }
                    else
                    {
                        //otherwise it's just a normal subject
                        Subject subject = SubjectReader.GetSubject(currentWord);
                        if (subject != null)
                            options.Add(subject);
                    }

                }

                i++;
            }

            if (selectionType.ToUpper() == "AND") pick = options.Count;

            return options;
        }

        public int GetPick()
        {
            if (options == null) GetOptions();
            return pick;
        }

        public string GetSelectionType()
        {
            if (options == null) GetOptions();
            return selectionType;
        }

        public override string ToString()
        {
            if (criteria != "" && criteria != null)
                return criteria;
            //This is used by the GetRemainingPrerequisit method.
            if (selectionType == "CP")
            {
                criteria = (GetPick() * 3).ToString() + "CP ";
                criteria += "[range:] "; //TODO: describe range

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
            return GetOptions().Where(criteria => !criteria.HasBeenMet(plan, RequiredCompletionTime(plan)) && !criteria.HasBeenBanned(plan)).ToList();
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
            //This is not accurate at all, however it is my best solution to avoiding stack overflows.
            if (GetPick() != GetOptions().Count)
                return false;
            //This is a simple catch to check for bans without checking recursively
            if (GetRemainingPick(plan) > GetOptions().Count)
                return true;
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
            if (selectionType == "CP") newcriteria = criteria;
            return new Prerequisit(this, remainingOptions, GetRemainingPick(plan), selectionType, newcriteria);
        }

        public override int EarliestCompletionTime()
        {
            if (earliestCompletionTime > -1) return earliestCompletionTime;
            //If there are no prerequisits, then the subject can be done straight away
            if (GetOptions().Count == 0)
                return 0;
            //This makes finding the time based on credit points a lot faster
            if (GetSelectionType() == "CP" && criteria.Contains('+')) return GetPick() / 3 / 4; //TODO: remove magic numbers
            //cache the result
            return earliestCompletionTime = 
                //Get a list of all the option's earliest completion times
                GetOptions().ConvertAll(criteria => criteria.EarliestCompletionTime())
                .OrderBy(x => x).ElementAt(GetPick()-1);
        }

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
    }
}
