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
    public static class SubjectReader
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
                    Subject subject = new Subject(r.code, r.time, r.prerequisites, r.nccw);
                    if (subject.Semesters.Any()) // Some subjects aren't offered yet
                        subjects[r.code] = subject; // Sometimes a subject appears twice in the csv.
                }
            }

            foreach (string line in Properties.Resources.Majors.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                string[] properties = line.Split('|');
                Subject major = new Subject(properties[0], "S1D S2D S3D", properties[1], ""); //TODO, treat majors differently
                majors.Add(major.ID, major);
            }
        }

        static readonly Dictionary<string, Subject> majors = new Dictionary<string, Subject>();
        static readonly Dictionary<string, Subject> subjects = new Dictionary<string, Subject>();

        public static Subject GetSubject(string id)
        {
            id = id.Split('(')[0];
            if (subjects.TryGetValue(id, out Subject subject))
                return subject;
            if (majors.TryGetValue(id, out Subject major))
                return major;
            return null;
        }

        public static bool TryGetSubject(string id, out Subject subject)
        {
            subject = GetSubject(id);
            return subject != null;
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
                if (query.Contains("from"))
                    textFilters = FindText("from ", " units").Split(new string[] { " or " }, StringSplitOptions.None);

                // Specify number part
                if (query.Contains("at"))
                {
                    lower = int.Parse(FindText("at ", " level").Substring(0, 4));
                    upper = lower + 999;

                    if (query.Contains("ABOVE"))
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

    public partial class Prerequisit
    {
        public List<Criteria> GetOptions()
        {
            // Load the cached result
            if (options != null) return options;

            // Create a list of options and prepare to translate the text description
            options = new List<Criteria>();

            DealWithBrackets(ref criteria);

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
                return SplitAvoidingBrackets(criteria, search, out tokens, without, firstWord);
            }

            void AddAllTokens()
            {
                foreach (string token in tokens)
                {
                    if (SubjectReader.CouldBeCode(token))
                    {
                        if (SubjectReader.TryGetSubject(token, out subject))
                            options.Add(subject);
                    }
                    else
                        options.Add(new Prerequisit(this, token));
                }
            }

            void AddFirstTokenOnly()
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" or "))
                    remaining = remaining.Substring(0, remaining.Length - 4);
                options.Add(new Prerequisit(this, remaining));
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
                options = SubjectReader.GetSubjectsFromQuery(tokens[1]);
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
                !(criteria.Split('(')[0].Length < 8 && int.TryParse(criteria.Split('(')[0].Substring(criteria.Split('(')[0].Length - 3), out int idk)) &&
                !(criteria.Split('(')[0].Length == 8 && int.TryParse(criteria.Split('(')[0].Substring(4), out int wut)))
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
    }


    public class SubjectRecord
    {
        [Name("Name")]
        public string name { get; set; }

        [Name("Code")]
        public string code { get; set; }

        [Name("Unit Type")]
        public string pace { get; set; }

        [Name("Prerequisites")]
        public string prerequisites { get; set; }

        [Name("Corequisites")]
        public string corequisites { get; set; }

        [Name("NCCW")]
        public string nccw { get; set; }

        [Name("When Offered")]
        public string time { get; set; }
    }
}