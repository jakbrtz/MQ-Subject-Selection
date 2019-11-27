using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace Subject_Selection
{
    // This class deals with everything related to parsing from databases
    public static class Parser
    {
        public static void LoadData()
        {
            using (var reader = new StringReader(Properties.Resources.ScheduleOfUndergraduateUnits))
            using (var csv = new CsvReader(reader))
            {
                var record = new SubjectRecord();
                var records = csv.EnumerateRecords(record);
                foreach (var r in records)
                {
                    Subject subject = new Subject(r.Code, r.Name, r.Time, r.Prerequisites, r.NCCW);
                    if (subject.Semesters.Any()) // Some subjects aren't offered yet
                        subjects[r.Code] = subject; // Sometimes a subject appears twice in the csv.
                }
            }

            foreach (Subject subject in subjects.Values)
                subject.Prerequisites.LoadFromCriteria();

            string descriptionBuilder;

            void MakeMinor(string description)
            {
                if (description == "") return;
                Subject minor = new Subject(description);
                if (minor.ID == null) return;
                minors[minor.ID] = minor;
            }

            descriptionBuilder = "";
            foreach (string line in Properties.Resources._2020_ScheduleOfMinors.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                if (line.Contains("T000") || line.Contains("P000"))
                {
                    MakeMinor(descriptionBuilder);
                    descriptionBuilder = "";
                }

                descriptionBuilder += line + "\r\n";
            }
            MakeMinor(descriptionBuilder);

            void MakeMajor(string description)
            {
                if (description == "") return;
                Subject major = new Subject(description);
                if (major.ID == null) return;
                majors[major.ID] = major;
            }

            descriptionBuilder = "";
            foreach (string line in Properties.Resources._2020_ScheduleOfMajors.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                if (line.Contains("N000"))
                {
                    MakeMajor(descriptionBuilder);
                    descriptionBuilder = "";
                }

                descriptionBuilder += line + "\r\n";
            }
            MakeMajor(descriptionBuilder);

            void MakeSpecialisation(string description)
            {
                if (description == "") return;
                Subject specialisation = new Subject(description);
                if (specialisation.ID == null) return;
                specialisations[specialisation.ID] = specialisation;
            }

            descriptionBuilder = "";
            foreach (string line in Properties.Resources._2020_ScheduleOfUGSpecialisations.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                if (line.Contains("Q000"))
                {
                    MakeSpecialisation(descriptionBuilder);
                    descriptionBuilder = "";
                }

                descriptionBuilder += line + "\r\n";
            }
            MakeSpecialisation(descriptionBuilder);

            foreach (string description in Properties.Resources._2020_ScheduleOfCoursesUG.Split(new string[] { "\r\nBachelor of" }, StringSplitOptions.RemoveEmptyEntries))
            {
                Subject course = new Subject("\r\nBachelor of" + description);
                courses[course.ID] = course;
            }
        }

        static readonly Dictionary<string, Subject> subjects = new Dictionary<string, Subject>();
        static readonly Dictionary<string, Subject> minors = new Dictionary<string, Subject>();
        static readonly Dictionary<string, Subject> majors = new Dictionary<string, Subject>();
        static readonly Dictionary<string, Subject> specialisations = new Dictionary<string, Subject>();
        static readonly Dictionary<string, Subject> courses = new Dictionary<string, Subject>();

        public static List<Subject> AllCourses()
        {
            return courses.Values.ToList();
        }

        public static Subject GetSubject(string id)
        {
            if (id.Contains(' '))
                return null;
            id = id.Split('(')[0];
            if (subjects.TryGetValue(id, out Subject subject))
                return subject;
            return null;
        }

        public static bool TryGetSubject(string id, out Subject subject)
        {
            subject = GetSubject(id);
            return subject != null;
        }

        public static Subject GetMinor(string id)
        {
            if (minors.TryGetValue(id, out Subject minor))
                return minor;
            return null;
        }

        public static Subject GetMajor(string id)
        {
            if (majors.TryGetValue(id, out Subject major))
                return major;
            return null;
        }

        public static Subject GetSpecialisation(string id)
        {
            if (specialisations.TryGetValue(id, out Subject specialisation))
                return specialisation;
            return null;
        }

        public static Subject GetCourse(string id)
        {
            if (courses.TryGetValue(id, out Subject course))
                return course;
            return null;
        }

        public static bool TryGetCriteria(string id, out Criteria criteria)
        {
            criteria = GetSubject(id);
            if (criteria != null)
                return true;
            criteria = GetMinor(id);
            if (criteria != null)
                return true;
            criteria = GetMajor(id);
            if (criteria != null)
                return true;
            criteria = GetSpecialisation(id);
            if (criteria != null)
                return true;
            criteria = GetCourse(id);
            if (criteria != null)
                return true;
            return false;
        }

        public static List<Criteria> GetSubjectsFromQuery(string query)
        {
            /* Aight now here me out
             * This doesn't properly parse a query, but it succeeds in parsing every query at Macquarie
             * I haven't tested every query, but I'm sure it works
             * This works by splitting the query at spaces, then assuming the meaning of every word that isn't already a keyword
             * It also builds a list of conditions to filter the full list of subjects by: the smallest number, largest number, and list of possible Letters that it can start with
             
             * Some inputs include:
             * " in COMP or ISYS or ACCG or STAT or BUS or BBA or MGMT units at 2000 level"
             * " from BIOL or ENVS or GEOS or ANTH1051 or ANTH151 or AHIS190"
             * " from FOSE1005 or MATH1000 or MATH1010-MATH1025 or MATH111-MATH339"
             * ""
             * " from COMP units at 2000 level or MATH units at 2000 level or STAT units at 2000 level"
             * That last one has the assumption that each group will have the same number
             */

            List<Criteria> output = new List<Criteria>();

            // Clear brackets and remove spaces around dashes
            query = query.Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(" -", "-").Replace("- ", "-");

            // A blank query returns everything
            if (query == "")
                return subjects.Values.Cast<Criteria>().ToList();

            // Split at spaces
            string[] words = query.Split(' ');

            // Prepare filter conditions
            int lower = 1000;
            int upper = 9999;
            List<string> textFilters = null;
            bool filter = false;

            // Iterate over each word
            foreach (string word in words)
            {
                // If the word is a keyword, don't process it
                if (word == "from" || word == "of" || word == "at" || word == "in" || word == "or" || word == "OR" || word == "units" || word == "level" || word == "either" || word == "")
                    continue;

                // Any subject gets added straight to the list of outputs.
                if (CouldBeCode(word))
                {
                    if (TryGetSubject(word, out Subject s))
                    {
                        output.Add(s);
                    }
                    continue;
                }

                // Get short ranges of subjects
                if (word.Contains('-'))
                {
                    string left = word.Split('-')[0].Trim();
                    string right = word.Split('-')[1].Trim();

                    if (!(left.Length == 8 && int.TryParse(left.Substring(4), out int localLower)))
                    {
                        // Maybe this subject is from the old codes? In that case we should neatly return nothing instead of yeeting an exception
                        if (CouldBeCode(left))
                            continue;
                        throw new Exception("left does not match");
                    }

                    if (!((right.Length == 8 && int.TryParse(right.Substring(4), out int localUpper)) || (right.Length == 4 && int.TryParse(right, out localUpper))))
                    {
                        // Who the fuck says "yeet an exception"?
                        if (CouldBeCode(right) || right.Length == 3)
                            continue;
                        throw new Exception("right does not match");
                    }

                    string unit = left.Substring(0, 4);

                    if (right.Length == 8 && right.Substring(0, 4) != unit)
                        throw new Exception("units don't match");

                    output.AddRange(subjects.Values.ToList().FindAll(subject =>
                        subject.ID.StartsWith(unit) &&
                        localLower <= subject.GetNumber() &&
                        subject.GetNumber() <= localUpper)
                        .Cast<Criteria>().ToList());

                    continue;
                }

                // Check if the word is a number
                if (int.TryParse(word, out int number))
                {
                    filter = true;
                    lower = number;
                    upper = number + 999;
                    continue;
                }

                // Check if there is a limit for the number
                if (word == "orabove")
                {
                    upper = 9999;
                    continue;
                }

                // Check if it is a subject without a number
                if (word.Length == 4)
                {
                    filter = true;
                    if (textFilters == null) textFilters = new List<string>();
                    textFilters.Add(word);
                    continue;
                }

                // Check if it is an old subject without a number
                if (word.Length == 3)
                {
                    // ACCG3020 asks about `40cp in LAW units at 2000 level`, so this needs to be impossible to meet
                    filter = true;
                    if (textFilters == null) textFilters = new List<string>();
                    continue;
                }

                // Sometimes the course specifies a mark for the subject
                if (word == "P" || word == "Cr" || word == "D" || word == "HD")
                    continue;

                // Something unusual is found.
                throw new Exception("wtf is this: " + word);
            }

            // Add the subjects that match the filters
            if (filter)
                output.AddRange(subjects.Values.ToList().FindAll(subject =>
                        (textFilters == null || textFilters.Any(unit => subject.ID.StartsWith(unit))) &&
                        lower <= subject.GetNumber() &&
                        subject.GetNumber() <= upper)
                        .Cast<Criteria>().ToList());

            return output;
        }

        public static bool CouldBeCode(string id)
        {
            if (id.EndsWith("(P)") || id.EndsWith("(Cr)") || id.EndsWith("(D)") || id.EndsWith("(HD)"))
                id = id.Split('(')[0];
            return id.Length >= 6 && id.Length <= 8 && !id.Contains(' ') && int.TryParse(id.Substring(id.Length - 3), out _);
        }

        public static int GetNumber(this Subject subject)
        {
            if (!subject.IsSubject)
                return 1000;
            //Assumes all IDs are made of 4 letters then 4 digits
            return int.Parse(subject.ID.Substring(4));
        }

        public static int GetLevel(this Subject subject)
        {
            return subject.GetNumber() / 1000;
        }
    }

    public partial class Prerequisite
    {
        public bool IsElective()
        {
            return selectionType == Selection.CP && (!criteria.Contains("units") || GetSubjects().Intersect(GetReasons()).Any());
        }

        string CopyCriteria(int remainingPick)
        {
            if (criteria.Contains("cp ") && !criteria.Contains("units") && !criteria.Contains("level"))
                return "";

            string output = (remainingPick * 10) + "cp";
            if (criteria.Contains("cp "))
                output += " " + criteria.Substring(criteria.IndexOf(' ') + 1);
            return output;
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

        private static string DealWithBrackets(ref string source)
        {
            // Remove leading/trailing whitespace
            source = source.Trim();

            if (source.Length > 1)
            {
                // If the description contains a closing bracket without an opening bracket, add an opening bracket (looking at you, ACCG3040)
                int brackets = 0;
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] == '(' || source[i] == '[')
                        brackets++;
                    if (source[i] == ')' || source[i] == ']')
                        brackets--;
                    if (brackets == -1)
                    {
                        source = (source[i] == ')' ? '(' : '[') + source;
                        i++;
                        brackets = 0;
                    }
                }

                // If the description contains an opening bracket without a closing bracket, add a closing bracket (looking at you, ANTH2003)
                while (brackets > 0)
                {
                    source += ')';
                    brackets--;
                }

                // If the entire text is in brackets, remove the brackets
                bool shouldRemoveBrackets = true;
                while (shouldRemoveBrackets)
                {
                    brackets = 0;
                    for (int i = 0; i < source.Length - 1; i++)
                    {
                        if (source[i] == '(' || source[i] == '[')
                            brackets++;
                        if (source[i] == ')' || source[i] == ']')
                            brackets--;
                        if (brackets == 0)
                        {
                            shouldRemoveBrackets = false;
                            break;
                        }
                    }
                    if (shouldRemoveBrackets)
                        source = source.Substring(1, source.Length - 2);
                }
            }

            return source;
        }

        private static bool SplitAvoidingBrackets(string source, string search, out List<string> result, string without = "", bool firstWord = false)
        {
            // Prepare a list for the output
            result = new List<string>();
            // Count whether there are brackets
            int brackets = 0;
            int startOfSubstring = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '(' || source[i] == '[')
                    brackets++;
                if (source[i] == ')' || source[i] == ']')
                    brackets--;
                // If there are no brackets and the text has been found, add that to the output
                if (brackets == 0 && source.ToUpper().Substring(i).StartsWith(search) && (without == "" || !source.ToUpper().Substring(i).StartsWith(without)))
                {
                    result.Add(source.Substring(startOfSubstring, i - startOfSubstring));
                    startOfSubstring = i + search.Length;
                }
                // If the search is known to be in the first word, and a space has been reached, leave
                if (firstWord && source[i] == ' ')
                    break;
            }
            result.Add(source.Substring(startOfSubstring, source.Length - startOfSubstring));
            return result.Count > 1;
        }

        public void LoadFromCriteria()
        {
            // Create a list of options and prepare to translate the text description
            options = new List<Criteria>();

            DealWithBrackets(ref criteria);

            // Get rid of words that make this difficult
            criteria = criteria.Replace("or above", "orabove").Replace("and above", "andabove").Replace(" only", "");

            // Check if the criteria is a single subject
            if (Parser.TryGetCriteria(criteria, out Criteria subject))
            {
                options.Add(subject);
                selectionType = Selection.AND;
                return;
            }

            // Splits the criteria accounting for brackets, and putting the output in `tokens`
            List<string> tokens;
            bool TrySplit(string search, string without = "", bool firstWord = false)
            {
                return SplitAvoidingBrackets(criteria, search, out tokens, without, firstWord);
            }

            void AddAllTokens()
            {
                foreach (string token in tokens)
                {
                    if (Parser.CouldBeCode(token))
                    {
                        if (Parser.TryGetCriteria(token, out subject))
                            options.Add(subject);
                    }
                    else
                        options.Add(new Prerequisite(this, token));
                }
            }

            void AddFirstTokenOnly()
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisite(this, remaining));
            }

            // Check if the criteria contains specific key words

            if (TrySplit(" INCLUDING "))
            {
                selectionType = Selection.AND;
                AddAllTokens();
            }

            else if (TrySplit("POST HSC"))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit("HSC"))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit(" AND "))
            {
                selectionType = Selection.AND;
                AddAllTokens();
            }

            else if (TrySplit("ADMISSION TO"))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit("PERMISSION"))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit("A GPA OF"))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit("CP", without: "CP OR", firstWord: true)) //Includes `CP AT`, 'CP IN`, and `CP FROM`
            {
                selectionType = Selection.CP;
                pick = int.Parse(tokens[0]) / 10; // 10 is a magic number equal to the amount of credit points per subject
                options = Parser.GetSubjectsFromQuery(tokens[1]);
            }

            else if (TrySplit(" OR "))
            {
                selectionType = Selection.OR;
                pick = 1;
                AddAllTokens();
            }

            else if (criteria == "")
            {
                selectionType = Selection.AND;
                pick = 0;
            }

            // Unknown edge cases
            else if (
                !(criteria.Split('(')[0].Length < 8 && int.TryParse(criteria.Split('(')[0].Substring(criteria.Split('(')[0].Length - 3), out _)) &&
                !(criteria.Split('(')[0].Length == 8 && int.TryParse(criteria.Split('(')[0].Substring(4), out _)))
            {
                Console.WriteLine(GetReasons().First());
                throw new Exception("idk how to parse this:\n" + criteria);
            }

            // If the selection type is AND then everything must be picked
            if (selectionType == Selection.AND) pick = options.Count;
        }

        public void LoadFromDocument(string document, out string Name, out string Code)
        {
            Name = null;
            Code = null;

            if (!document.Contains("0")) return;

            options = new List<Criteria>();

            string criteriaBuilder = "";
            string previousFirstCell = "";

            foreach (string line in document.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                if (line.Trim() == "" || line.Trim() == "Major")
                {
                    // If there is nothing to add, do nothing
                    if (criteriaBuilder == "") continue;
                    // Conclude previous Option set
                    if (criteriaBuilder.EndsWith(" or "))
                        criteriaBuilder = criteriaBuilder.Substring(0, criteriaBuilder.Length - 4) + ") and ";
                    // Remove " and " from the end of the option
                    criteriaBuilder = criteriaBuilder.Substring(0, criteriaBuilder.Length - 5);
                    // Create a prerequisite using the criteria
                    // Add that criteria to the list of stuff to do
                    options.Add(new Prerequisite(this, criteriaBuilder));
                    // Reset the builder
                    criteriaBuilder = "";
                    previousFirstCell = "";
                    continue;
                }

                string[] cells = line.Split(new string[] { "\t" }, StringSplitOptions.None);

                if (Code == null)
                {
                    Name = cells[0];
                    Code = cells[1];
                    continue;
                }

                if (line.StartsWith("Minors"))
                {
                    previousFirstCell = "Minors";
                    criteriaBuilder = "(";
                    continue;
                }

                if (previousFirstCell == "Minors")
                {
                    if (cells[0].StartsWith("P000") || cells[0].StartsWith("T000"))
                        criteriaBuilder += cells[0] + " or ";
                    continue;
                }

                if (line.StartsWith("Majors"))
                {
                    previousFirstCell = "Majors";
                    criteriaBuilder = "(";
                    continue;
                }

                if (previousFirstCell == "Majors")
                {
                    if (cells[0].StartsWith("N000"))
                        criteriaBuilder += cells[0] + " or ";
                    continue;
                }

                if (line.StartsWith("UG Specialisations"))
                {
                    previousFirstCell = "UG Specialisations";
                    criteriaBuilder = "(";
                    continue;
                }

                if (previousFirstCell == "UG Specialisations")
                {
                    if (cells[0].StartsWith("Q000"))
                        criteriaBuilder += cells[0] + " or ";
                    continue;
                }

                switch (cells[0])
                {
                    case "Essential":
                        criteriaBuilder += cells[2] + " and ";
                        break;
                    case "Option set":
                        // Conclude previous Option set
                        if (criteriaBuilder.EndsWith(" or "))
                            criteriaBuilder = criteriaBuilder.Substring(0, criteriaBuilder.Length - 4) + ") and ";
                        // TODO: confirm that cells[1] always ends in a space
                        criteriaBuilder += "(" + cells[1];
                        criteriaBuilder += cells[2] + " or ";
                        break;
                    case "":
                        if (previousFirstCell == "Option set")
                            criteriaBuilder += cells[2] + " or ";
                        break;
                    case "TOTAL CREDIT POINTS REQUIRED FOR THIS COURSE":
                        options.Add(new Prerequisite(this, cells[1] + "cp"));
                        break;
                    case "Note:":
                        // TODO: "Students may count a maximum of 100cp at 1000 level towards their course requirements."
                        break;
                    // I might need these later
                    case "Owner:":
                    case "Award:":
                    case "Core Zone":
                    case "Capstone unit:":
                    case "Elective units":
                    case "Total Required for Core Zone":
                    case "Flexible Zone":
                    case "Electives":
                    case "Total Required for Flexible Zone":
                    case "Faculty:":
                    case "Department:":
                    case "This major must be completed as part of an award. The general requirements for the award must be satisfied in order to graduate.":
                    case "Requirements:":
                    case "Essential units":
                    case "TOTAL CREDIT POINTS REQUIRED TO SATISFY THIS MAJOR":
                    case "This minor must be completed as part of an award. The general requirements for the award must be satisfied in order to graduate.":
                    case "TOTAL CREDIT POINTS REQUIRED TO SATISFY THIS MINOR":
                        break;
                    default:
                        break; //throw new NotImplementedException();
                }
                if (cells[0] != "") previousFirstCell = cells[0];
            }

            selectionType = Selection.AND;
            pick = options.Count;
        }
    }

    public class SubjectRecord
    {
        [Name("Name")]
        public string Name { get; set; }

        [Name("Code")]
        public string Code { get; set; }

        [Name("Unit Type")]
        public string Pace { get; set; }

        [Name("Prerequisites")]
        public string Prerequisites { get; set; }

        [Name("Corequisites")]
        public string Corequisites { get; set; }

        [Name("NCCW")]
        public string NCCW { get; set; }

        [Name("When Offered")]
        public string Time { get; set; }
    }
}