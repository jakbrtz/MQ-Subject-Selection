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
        public static void Load()
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

            void MakeMajor(string description)
            {
                if (description == "") return;
                Subject major = new Subject(description);
                if (major.ID == null) return;
                majors[major.ID] = major;
            }

            string descriptionBuilder = "";
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
            query = query.Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "");

            // A blank query returns everything
            if (query == "")
                return subjects.Values.Cast<Criteria>().ToList();

            if (query.Contains("units") || query.Contains("level"))
            {
                // A query with words

                // Figure out how to filter the data
                int lower = 1000;
                int upper = 9999;
                string[] textFilters = null;

                string FindText(string left, string right)
                {
                    int leftIndex = query.IndexOf(left) + left.Length;
                    int rightIndex = query.IndexOf(right);
                    if (rightIndex == -1)
                        rightIndex = query.Length;
                    return query.Substring(leftIndex, rightIndex - leftIndex);
                }

                // Specify text part
                if (query.Contains("in"))
                    textFilters = FindText("in ", " units").Split(new string[] { " or " }, StringSplitOptions.None);
                if (query.Contains("of"))
                    textFilters = FindText("of ", " units").Split(new string[] { " or " }, StringSplitOptions.None);
                if (query.Contains("from") && !query.Contains("from units"))
                    textFilters = FindText("from ", " units").Split(new string[] { " or " }, StringSplitOptions.None);

                // Specify number part
                if (query.Contains("at"))
                {
                    lower = int.Parse(FindText("at ", " level").Substring(0, 4));
                    upper = lower + 999;

                    if (query.Contains("above"))
                    {
                        upper = 9999;
                    }
                }

                return subjects.Values.ToList().FindAll(subject =>
                    (textFilters == null || textFilters.Any(unit => subject.ID.StartsWith(unit))) &&
                    lower <= subject.GetNumber() &&
                    subject.GetNumber() <= upper)
                    .Cast<Criteria>().ToList();
            }
            else
            {
                string[] queriedSubjects = query.Replace("from", "").Replace("in", "").Split(new string[] { "or", "OR" }, StringSplitOptions.RemoveEmptyEntries);

                Criteria[] translateQuery(string subQuery)
                {
                    if (subQuery.Contains('-'))
                    {
                        string left = subQuery.Split('-')[0].Trim();
                        string right = subQuery.Split('-')[1].Trim();

                        if (!(left.Length == 8 && int.TryParse(left.Substring(4), out int lower)))
                        {
                            // Maybe this subject is from the old codes? In that case we should neatly return nothing instead of yeeting an exception
                            if (CouldBeCode(left))
                                return new Criteria[0];
                            throw new Exception("left does not match");
                        }

                        if (!((right.Length == 8 && int.TryParse(right.Substring(4), out int upper)) || (right.Length == 4 && int.TryParse(right, out upper))))
                        {
                            // Who the fuck says "yeet an exception"?
                            if (CouldBeCode(right) || right.Length == 3)
                                return new Criteria[0];
                            throw new Exception("right does not match");
                        }

                        string unit = left.Substring(0, 4);

                        if (right.Length == 8 && right.Substring(0, 4) != unit)
                            throw new Exception("units don't match");

                        return subjects.Values.ToList().FindAll(subject =>
                            subject.ID.StartsWith(unit) &&
                            lower <= subject.GetNumber() &&
                            subject.GetNumber() <= upper)
                            .Cast<Criteria>().ToArray();
                    }
                    else
                    {
                        // Return a single subject
                        if (TryGetSubject(subQuery, out Subject subject))
                            return new Criteria[] { GetSubject(subQuery) };
                        // Return all subjects with a name
                        if (subQuery.Length == 4)
                            return subjects.Values.ToList().FindAll(s => s.ID.StartsWith(subQuery)).Cast<Criteria>().ToArray();
                        // Old codes
                        if (CouldBeCode(subQuery))
                            return new Criteria[0];
                        throw new Exception("\'" + subQuery + "\' is not a subject");
                    }
                }

                return queriedSubjects.SelectMany(q => translateQuery(q.Trim())).ToList();
            }
        }

        public static bool CouldBeCode(string id)
        {
            return id.Length <= 8 && !id.Contains(' ') && int.TryParse(id.Substring(id.Length - 3), out _);
        }

        public static int GetNumber(this Subject subject)
        {
            //Assumes all IDs are made of 4 letters then 4 digits
            if(int.TryParse(subject.ID.Substring(4), out int value)) //TODO: remove this by no longer treating COURSES and majors as subjects
                return value;
            return 1000;
        }

        public static int GetLevel(this Subject subject)
        {
            return subject.GetNumber() / 1000;
        }
    }

    public partial class Prerequisite
    {
        public List<Criteria> GetOptions()
        {
            /* The process of translating criteria into options can only happen once `subjects` has been fully populated
             * That is why I put the translation process in this function
             */
            
            // Load the cached result
            if (options != null) return options;

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
                return options;
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
                Console.WriteLine(reasons[0]);
                throw new Exception("idk how to parse this:\n" + criteria);
            }

            // If the selection type is AND then everything must be picked
            if (selectionType == Selection.AND) pick = options.Count;

            return options;
        }

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

                // If the entire text is in backets, remove the brackets
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