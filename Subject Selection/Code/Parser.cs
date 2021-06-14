using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Diagnostics;

namespace Subject_Selection
{
    // This class deals with everything related to parsing from databases
    public static class Parser
    {
        public static void LoadData()
        {
            List<SubjectRecord> subjectRecords = new List<SubjectRecord>();

            // Open csv to get subjects
            using (var reader = new StringReader(Properties.Resources.ScheduleOfUndergraduateUnits))
            using (var csv = new CsvReader(reader))
            {
                var record = new SubjectRecord();
                var records = csv.EnumerateRecords(record);
                foreach (var r in records)
                {
                    var subjectRecord = (SubjectRecord)r.Clone();
                    subjectRecord.InitialSetup();
                    // Make sure the subject is being offered
                    if (subjectRecord.Actual.Semesters.Any())
                    {
                        // subject codes should be unique - if this code already exists then replace it
                        bool found = false;
                        for (int i = 0; i < subjectRecords.Count; i++)
                            if (subjectRecords[i].Code == subjectRecord.Code)
                            {
                                found = true;
                                subjectRecords[i] = subjectRecord;
                                break;
                            }
                        if (!found)
                            subjectRecords.Add(subjectRecord);
                        Debug.Assert(subjectRecords.Count(s => s.Code == subjectRecord.Code) == 1, "the code should appear exactly once");
                    }
                }
            }

            foreach (SubjectRecord subject in subjectRecords)
                MasterList.AddSubject(subject.Actual);

            Debug.WriteLine("loaded subjects");

            StringBuilder descriptionBuilder = new StringBuilder();

            // Load minors

            static void MakeMinor(string description)
            {
                if (TryReadDocument(description, out Course minor))
                    MasterList.AddMinor(minor);
            }

            descriptionBuilder.Clear();

            foreach (string line in Properties.Resources._2020_ScheduleOfMinors.Replace("\r\n","\n").Split('\n', StringSplitOptions.None))
            {
                if (line.Contains("T000") || line.Contains("P000"))
                {
                    MakeMinor(descriptionBuilder.ToString());
                    descriptionBuilder.Clear();
                }

                descriptionBuilder.Append(line + "\n");
            }
            MakeMinor(descriptionBuilder.ToString());

            Debug.WriteLine("loaded minor");

            // Load majors

            static void MakeMajor(string description)
            {
                if (TryReadDocument(description, out Course major))
                    MasterList.AddMajor(major);
            }

            descriptionBuilder.Clear();
            foreach (string line in Properties.Resources._2020_ScheduleOfMajors.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None))
            {
                if (line.Contains("N000"))
                {
                    MakeMajor(descriptionBuilder.ToString());
                    descriptionBuilder.Clear();
                }

                descriptionBuilder.Append(line + "\n");
            }
            MakeMajor(descriptionBuilder.ToString());

            Debug.WriteLine("loaded majors");

            // Make NCCW links between matching majors and minors
            foreach (Course major in MasterList.AllMajors)
            {
                Course nccw = MasterList.AllMinors.FirstOrDefault(minor => minor.Name == major.Name);
                if (nccw != null)
                    major.NCCWs.Add(nccw);
            }
            foreach (Course minor in MasterList.AllMinors)
            {
                Course nccw = MasterList.AllMajors.FirstOrDefault(major => major.Name == minor.Name);
                if (nccw != null)
                    minor.NCCWs.Add(nccw);
            }

            // Load specialisations

            static void MakeSpecialisation(string description)
            {
                if (TryReadDocument(description, out Course specialisation))
                    MasterList.AddSpecialisation(specialisation);
            }

            descriptionBuilder.Clear();
            foreach (string line in Properties.Resources._2020_ScheduleOfUGSpecialisations.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None))
            {
                if (line.Contains("Q000"))
                {
                    MakeSpecialisation(descriptionBuilder.ToString());
                    descriptionBuilder.Clear();
                }

                descriptionBuilder.Append(line + "\n");
            }
            MakeSpecialisation(descriptionBuilder.ToString());

            Debug.WriteLine("loaded specialisations");

            // Load courses

            foreach (string description in Properties.Resources._2020_ScheduleOfCoursesUG.Replace("\r\n","\n").Split(new string[] { "\nBachelor of" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryReadDocument("\nBachelor of" + description, out Course course))
                    MasterList.AddCourse(course);
            }

            Debug.WriteLine("loaded courses");

            // Postload subjects

            foreach (SubjectRecord subject in subjectRecords)
                subject.PostLoad();
            

            Debug.WriteLine("postloaded subjects");

            // Read recommendations

            foreach (string line in Properties.Resources.Recommender.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None))
            {
                string reasonStr = line.Split()[0];
                string recommendationStr = line.Split()[1];
                if (MasterList.TryGetContent(reasonStr, out Content reason) && MasterList.TryGetSubject(recommendationStr, out Subject recommendation))
                    MasterList.AddRecommendation(reason, recommendation);
            }

            Debug.WriteLine("loaded recommendations");
        }

        public static List<Option> GetSubjectsFromQuery(string query)
        {
            /* Aight now here me out
             * This doesn't properly parse a query, but it succeeds in parsing every query at Macquarie
             * I haven't tested every query, but I'm sure it works
             * okay maybe not
             * This works by splitting the query at spaces, then assuming the meaning of every word that isn't a keyword
             * It also builds a list of conditions to filter the full list of subjects by: the smallest number, largest number, and list of possible Letters that it can start with
             
             * Some inputs include:
             * " in COMP or ISYS or ACCG or STAT or BUS or BBA or MGMT units at 2000 level"
             * " from BIOL or ENVS or GEOS or ANTH1051 or ANTH151 or AHIS190"
             * " from FOSE1005 or MATH1000 or MATH1010-MATH1025 or MATH111-MATH339"
             * ""
             * " from N000054 or N000055 or N000087 or N000086 or N000062 or N000063"
             * " from COMP units at 2000 level or MATH units at 2000 level or STAT units at 2000 level"
             * That last one has the assumption that each group will have the same number
             */

            List<Option> output = new List<Option>();

            // Clear brackets and remove spaces around dashes
            query = query.Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(" -", "-").Replace("- ", "-");

            // Split at spaces
            List<string> words = query.Split(' ').ToList();

            // Remove keywords
            string[] keywords = new[] { "from", "of", "at", "in", "or", "OR", "units", "level", "either" , "" };
            words.RemoveAll(word => keywords.Contains(word));

            // If there are no useful words, then return every subject
            if (!words.Any())
                return MasterList.AllSubjects.Cast<Option>().ToList();

            // Prepare filter conditions
            int lower = 1000;
            int upper = 9999;
            List<string> textFilters = null;
            bool filter = false;

            // Iterate over each word
            foreach (string word in words)
            {

                // Any subject gets added straight to the list of outputs.
                if (CouldBeSubjectCode(word))
                {
                    if (MasterList.TryGetContent(word, out Content c))
                        output.Add(c);
                    continue;
                }

                // Get short ranges of subjects
                if (word.Contains('-'))
                {
                    string left = word.Split('-')[0].Trim();
                    string right = word.Split('-')[1].Trim();

                    // Get the last 4 character from left. Make sure they're digits
                    if (!(left.Length == 8 && int.TryParse(left[4..], out int localLower)))
                    {
                        // There was an error getting the digits from left. Maybe it's using the old codes?
                        if (CouldBeSubjectCode(left))
                            continue;
                        // If the old codes weren't used then something is seriously wrong
                        throw new FormatException("left is not a subject");
                    }

                    // Get the last 4 character from right. Make sure they're digits
                    if (!((right.Length == 8 && int.TryParse(right[4..], out int localUpper)) || (right.Length == 4 && int.TryParse(right, out localUpper))))
                    {
                        // There was an error getting the digits from right. Maybe it's using the old codes?
                        if (CouldBeSubjectCode(right) || right.Length == 3)
                            continue;
                        // If the old codes weren't used then something is seriously wrong
                        throw new FormatException("right is not a subject");
                    }

                    string unit = left.Substring(0, 4);

                    if (right.Length == 8 && right.Substring(0, 4) != unit)
                        throw new FormatException("subjects don't match");

                    output.AddRange(MasterList.AllSubjects.Where(subject =>
                        subject.ID.StartsWith(unit) &&
                        localLower <= subject.GetNumber() &&
                        subject.GetNumber() <= localUpper)
                        .Cast<Option>());

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
                if (word == "above")
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
                throw new FormatException("wtf is this: " + word);
            }

            // Add the subjects that match the filters
            if (filter)
                output.AddRange(MasterList.AllSubjects.Where(subject =>
                        (textFilters == null || textFilters.Any(unit => subject.ID.StartsWith(unit))) &&
                        lower <= subject.GetNumber() &&
                        subject.GetNumber() <= upper)
                        .Cast<Option>().ToList());

            return output;
        }

        public static bool TryReadDocument(string document, out Course course)
        {
            string Name = null;
            string Code = null;

            course = null;

            if (!document.Contains("0")) return false;

            List<Option> mainOptions = new List<Option>();

            int maxAt1000Level = int.MaxValue;

            string decisionBuilder = "";
            string previousFirstCell = "";
            string previousLastCell = "";

            foreach (string line in document.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None))
            {
                if (line.Contains("Concentration")) break; // TODO: parse concentrations in Specialisations

                string[] cells = line.Split(new string[] { "\t" }, StringSplitOptions.None);

                if (decisionBuilder != "" && (line.Trim() == "" || (cells[0] != "" && previousFirstCell != "UG Specialisations" && previousFirstCell != "Majors" && previousFirstCell != "Minors")))
                {
                    // Conclude previous Option set
                    if (decisionBuilder.EndsWith(" or "))
                        decisionBuilder = decisionBuilder[0..^4];
                    // Finish the speghetti from later in this code (the thing about [arts minor])
                    decisionBuilder = decisionBuilder.Replace("[arts minor]", Properties.Resources.arts_minor);
                    // Make sure that previousLastCell is a number. If it is not, work out what number is it by looking at the first word in decisionBuilder
                    if (!int.TryParse(previousLastCell, out _))
                        previousLastCell = MasterList.TryGetContent(decisionBuilder.Split(' ').First(), out Content firstOption) 
                            ? firstOption.CreditPoints().ToString() 
                            : throw new FormatException("The first word should be an option");
                    // Create a decision from the decisionBuilder, and add it to the list of stuff to do
                    Debug.Assert(course != null);
                    mainOptions.Add(new Decision(course, "(" + previousLastCell + "cp from " + decisionBuilder + ")"));
                    // Reset the builder
                    decisionBuilder = "";
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                if (Code == null)
                {
                    Name = cells[0];
                    Code = cells[1];
                    course = new Course(Code, Name);
                    continue;
                }

                if (line.StartsWith("Minors"))
                {
                    previousFirstCell = "Minors";
                    previousLastCell = "40";
                    continue;
                }

                if (previousFirstCell == "Minors")
                {
                    if (cells[0].StartsWith("P000") || cells[0].StartsWith("T000"))
                        decisionBuilder += cells[0] + " or ";
                    continue;
                }

                if (line.StartsWith("Majors"))
                {
                    previousFirstCell = "Majors";
                    previousLastCell = "80";
                    continue;
                }

                if (previousFirstCell == "Majors")
                {
                    if (cells[0].StartsWith("N000"))
                        decisionBuilder += cells[0] + " or ";
                    continue;
                }

                if (line.StartsWith("UG Specialisations"))
                {
                    previousFirstCell = "UG Specialisations";
                    previousLastCell = "";
                    continue;
                }

                if (previousFirstCell == "UG Specialisations")
                {
                    if (cells[0].StartsWith("Q000"))
                        decisionBuilder += cells[0] + " or ";
                    continue;
                }

                if (cells[0] == "") cells[0] = previousFirstCell;

                switch (cells[0])
                {
                    case "Essential":
                    case "Option set":
                        // Remember the spaghetti from a few lines earlier? It continues here
                        if (cells[1] == "or")
                            decisionBuilder += "[arts minor] or ";
                        else if (cells[2] != "")
                            decisionBuilder += cells[2] + " or ";
                        break;
                    case "Electives":
                        //decisionBuilder += " or ";
                        Debug.Assert(course != null);
                        mainOptions.Add(new Decision(course, description: cells.Last() + "cp"));
                        break;
                    case "Note:":
                    case "Note: ":
                        var potentialSentence = cells[1].Split("Students may count a maximum of ", StringSplitOptions.None);
                        if (potentialSentence.Length == 2)
                            maxAt1000Level = int.Parse(potentialSentence[1].Split("cp")[0]);
                        break;
                    case "Award:":
                        Debug.Assert(course != null);
                        string award = cells.Last();
                        string awardCode = award.Split(" (").Last()[0..^1];
                        MasterList.AddAward(course, awardCode);
                        break;
                    // I might need these later
                    case "Owner:":
                    case "Core Zone":
                    case "Capstone unit:":
                    case "Elective units":
                    case "Total Required for Core Zone":
                    case "Flexible Zone":
                    case "TOTAL CREDIT POINTS REQUIRED FOR THIS COURSE":
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
                if (cells.First() != "") previousFirstCell = cells.First();
                if (cells.Last() != "") previousLastCell = cells.Last();
            }

            Decision mainDecision = new Decision(course, options: mainOptions, selectionType: Selection.CP, creditPoints: mainOptions.Sum(option => option.CreditPoints()));
            int minAt200LevelOrAbove = mainDecision.CreditPoints() - maxAt1000Level;
            if (minAt200LevelOrAbove < 0) minAt200LevelOrAbove = 0;
            Decision levelConditions = new Decision(course, description: minAt200LevelOrAbove + "cp at 2000 level or above", reasonIsCorequisite: true);

            course.PostLoad(mainDecision, levelConditions);
            return true;
        }

        public static bool CouldBeSubjectCode(string id)
        {
            if (id.EndsWith("(P)") || id.EndsWith("(Cr)") || id.EndsWith("(D)") || id.EndsWith("(HD)"))
                id = id.Split('(')[0];
            return id.Length >= 6 && id.Length <= 8 && !id.Contains(' ') && int.TryParse(id[^3..], out _);
        }

        public static int GetNumber(this Subject subject)
        {
            //Assumes all IDs are made of 4 letters then 4 digits
            return int.Parse(subject.ID[4..]);
        }

        public static int GetLevel(this Option option)
        {
            if (option is Subject subject)
                return subject.GetNumber() / 1000;
            else if (option is Decision decision && decision.Options.Any())
                return decision.Options.First().GetLevel();
            else
                return 1000;
        }

        public static string ListContents(List<Content> contents)
        {
            string output = "";
            for (int i = 0; i < contents.Count; i++)
            {
                Content content = contents[i];

                if (content is Subject)
                    output += content.ID;
                else
                    output += content.Name.Replace(",","");

                if (i < contents.Count - 1)
                    output += ", ";
            }
            return output;
        }
    }

    public partial class Decision
    {
        public bool IsElective()
        {
            return SelectionType == Selection.CP && (!Description.Contains(" ") || Description.Contains("000 level")) && (!Description.Contains("units") || Description.Contains("level units"));
        }

        void CopyDescription(string previousDescription)
        {
            if (previousDescription.Contains("cp ") && !previousDescription.Contains("units") && !previousDescription.Contains("level"))
                return;
            if (previousDescription.Contains("("))
                return;
            description = creditPoints + "cp";
            if (previousDescription.Contains("cp "))
                description += " " + previousDescription[(previousDescription.IndexOf(' ') + 1)..];
        }

        public string Description
        {
            get
            {
                if (description == "" || description == null)
                {
                    if (selectionType == Selection.CP)
                    {
                        description = CreditPoints() + "cp from ";
                        description += string.Join(" or ", Options.Select(option => option is Decision ? "(" + option.ToString() + ")" : option.ToString()));
                    }
                    else
                    {
                        string separator = " " + selectionType + " ";
                        description = string.Join(separator, Options.Select(option => option is Decision ? "(" + option.ToString() + ")" : option.ToString()));
                    }
                }
                return description;
            }
        }

        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// Give the user information about their cuurent action
        /// </summary>
        /// <returns>A description of the Decision in the form of an instruction</returns>
        public string Instruction()
        {
            if (Options.Any(option => option is Course))
            {
                return Options.First().CreditPoints() switch
                {
                    40 => "Select a Minor",
                    80 => "Select a Major",
                    120 => "Select a Specialisation",
                    160 => "Select a Specialisation",
                    210 => "Select a Specialisation",
                    _ => "Select a Degree",
                };
            }
            if (SelectionType == Selection.OR) return "Choose 1:";
            if (MustPickAll()) return "You need to satisfy all of these. Pick one to focus on:";
            string output = "Select ";
            if (Options.All(option => option.CreditPoints() == Options.First().CreditPoints()))
                output += CreditPoints() / Options.First().CreditPoints();
            else
                output += CreditPoints() + " Credit Points";
            if (IsElective())
            {
                output += " of Electives";
                int minLevel = Options.Min(option => (option as Subject).GetLevel());
                bool orAbove = Options.Any(option => (option as Subject).GetLevel() > minLevel);
                if (!(minLevel == 1 && orAbove))
                {
                    output += " at " + minLevel + "000 level";
                    if (orAbove)
                        output += " or above";
                }
            }
            else if (Options.All(option => option is Subject subject && subject.pace))
            {
                output += " pace unit";
                if (!OnlyPickOne())
                    output += "s";
            }
            else
            {
                List<string> unitCodes = Options.Select(option => (option as Subject).ID[0..4].ToUpper()).Distinct().ToList();
                if (unitCodes.Count < 3)
                {
                    output += " from " + string.Join(" or ", unitCodes) + " unit";
                    if (!OnlyPickOne())
                        output += "s";
                }
            }
            output += ":";
            return output;
        }

        public string ListItem()
        {
            // Check for complicated stuff
            if (Options.Any(option => option is Decision))
                return Description;
            // Check for courses
            if (Options.Any(option => option is Course))
            {
                return CreditPoints() switch
                {
                    40 => "Minor",
                    80 => "Major",
                    120 => "Specialisation",
                    160 => "Specialisation",
                    210 => "Specialisation",
                    _ => throw new ArgumentException("Unknown number of credit points")
                };
            }
            // Check if it's a PACE unit
            if (Options.All(option => option is Subject subject && subject.pace))
                return "PACE unit";
            // Small set of subjects
            if (Options.Count < 5)
            {
                if (OnlyPickOne())
                    return string.Join(" or ", Options);
                return Description;
            }
            // Larger set of subjects
            string output = "";
            if (IsElective())
            {
                output += CreditPoints() + " Credit Points";
                if (Unique())
                    output += " of Electives";
            }
            else
            {
                if (SelectionType == Selection.OR)
                {
                    output += "1 ";
                }
                else // SelectionType == Selection.CP
                {
                    int unitCreditPoints = Options.First().CreditPoints();
                    if (Options.All(option => option.CreditPoints() == unitCreditPoints))
                        output += Math.Ceiling((decimal)CreditPoints() / unitCreditPoints) + " ";
                }

                List<string> unitCodes = Options.Select(option => (option as Subject).ID[0..4].ToUpper()).Distinct().ToList();
                if (unitCodes.Count < 3)
                    output += string.Join(" or ", unitCodes) + " ";
                output += "unit";
                if (!OnlyPickOne())
                    output += "s";
            }
            int minLevel = Options.Min(option => (option as Subject).GetLevel());
            bool orAbove = Options.Any(option => (option as Subject).GetLevel() > minLevel);
            if (!(minLevel == 1 && orAbove))
            {
                output += " at " + minLevel + "000 level";
                if (orAbove)
                    output += " or above";
            }
            return output;
        }

        public string ReasonDescription()
        {
            string output = "";

            List<Content> reasonsCourse = GetReasons().Where(reason => reason is Course).ToList();
            List<Content> reasonsPre = GetReasonsPrerequisite().Where(reason => reason is Subject).ToList();
            List<Content> reasonsCo = GetReasonsCorequisite().Where(reason => reason is Subject).ToList();

            if (reasonsCourse.Any())
            {
                if (output != "")
                    output += " & ";
                output += "Requisite to " + Parser.ListContents(reasonsCourse);
            }
            if (reasonsPre.Any())
            {
                if (output != "")
                    output += " & ";
                output += "Prerequisite to " + Parser.ListContents(reasonsPre);
            }
            if (reasonsCo.Any())
            {
                if (output != "")
                    output += " & ";
                output += "Corequisite to " + Parser.ListContents(reasonsCo);
            }

            return output;
        }

        public string ComponentItem()
        {
            static string Display(Option option)
            {
                if (option is Decision decision)
                    return "(" + decision.ComponentItem() + ")";
                return option.ToString();
            }

            if (OnlyPickOne())
                return string.Join(" or ", Options.Select(option => Display(option)));
            if (MustPickAll())
                return string.Join(" and ", Options.Select(option => Display(option)));
            return Description;
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
                        source = source[1..^1];
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
                if (brackets == 0 && source.ToUpper()[i..].StartsWith(search) && (without == "" || !source.ToUpper()[i..].StartsWith(without)))
                {
                    result.Add(source[startOfSubstring..i]);
                    startOfSubstring = i + search.Length;
                }
                // If the search is known to be in the first word, and a space has been reached, leave
                if (firstWord && source[i] == ' ')
                    break;
            }
            result.Add(source[startOfSubstring..]);
            return result.Count > 1;
        }

        public void LoadFromDescription()
        {
            // Create a list of options and prepare to translate the text description
            options = new List<Option>();

            DealWithBrackets(ref description);

            // Get rid of words that make this difficult
            description = description.Replace("(P)", "").Replace(" at 1000 level or above", "").Replace(" only", "").Replace("Admission to ", "").Replace("admission to ", "");

            // Check if the option is a single subject
            if (MasterList.TryGetContent(description, out Content content))
            {
                options.Add(content);
                selectionType = Selection.AND;
                return;
            }

            // Splits the option accounting for brackets, and putting the output in `tokens`
            List<string> tokens;
            bool TrySplit(string search, string without = "", bool firstWord = false)
            {
                return SplitAvoidingBrackets(description, search, out tokens, without, firstWord);
            }

            void AddAllTokens()
            {
                foreach (string token in tokens)
                {
                    if (Parser.CouldBeSubjectCode(token))
                        if (MasterList.TryGetContent(token, out content))
                            options.Add(content);
                        else
                            options.Add(new ImpossibleDecision(this));
                    else
                        options.Add(new Decision(this, token));
                }
            }

            void AddFirstTokenOnly()
            {
                selectionType = Selection.AND;
                string remaining = tokens[0];
                if (remaining.EndsWith(" "))
                    remaining = remaining[0..^1];
                if (remaining.EndsWith(" or"))
                    remaining = remaining[0..^3];
                options.Add(new Decision(this, remaining));
            }

            // Check if the description contains specific key words

            if (TrySplit(" INCLUDING "))
            {
                selectionType = Selection.AND;
                AddAllTokens();
            }

            else if (TrySplit("POST HSC "))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit("HSC ", firstWord: true))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit(" HSC "))
            {
                AddFirstTokenOnly();
            }

            else if (TrySplit(" AND ", without: " AND ABOVE"))
            {
                selectionType = Selection.AND;
                AddAllTokens();
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
                creditPoints = int.Parse(tokens[0]);
                options = Parser.GetSubjectsFromQuery(tokens[1]);
            }

            else if (TrySplit(" OR ", without: " OR ABOVE"))
            {
                selectionType = Selection.OR;
                AddAllTokens();
            }

            else if (description == "")
            {
                selectionType = Selection.AND;
            }

            // Unknown edge cases
            else if (
                !(description.Split('(')[0].Length < 8 && int.TryParse(description.Split('(')[0][^3..], out _)) &&
                !(description.Split('(')[0].Length == 8 && int.TryParse(description.Split('(')[0][4..], out _)))
            {
                Debug.WriteLine(GetReasons().First());
                throw new FormatException("idk how to parse this:\n" + description);
            }
        }
    }

    public class SubjectRecord : ICloneable
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

        [Name("Credit\nPoints")]
        public string CP { get; set; }

        internal Subject Actual { get; private set; }

        internal void InitialSetup()
        {
            int.TryParse(CP, out int creditpoints);
            Debug.Assert(creditpoints >= 0, "Credit Points cannot be negative");
            var semesters = new List<OfferTime>();
            int earliestYear = 0;
            foreach (string time in Time.Split('\n'))
                if (OfferTime.TryParse(time, out OfferTime result))
                    semesters.Add(result);
                else if (int.TryParse(time, out int result2))
                    earliestYear = result2;
                else
                    Debug.Assert(time == "" || time == "TBD", $"Unknown semester: {time}");

            this.Actual = new Subject(Code, Name, creditpoints, Pace == "PACE", semesters, earliestYear);
        }

        internal void PostLoad()
        {
            var prerequisitesActual = new Decision(Actual, description: Prerequisites);
            var corequisitesActual = new Decision(Actual, description: Corequisites, reasonIsCorequisite: true);
            var nccwsActual = new List<Content>();
            foreach (var ID in NCCW.Split(new string[] { ", " }, StringSplitOptions.None))
            {
                Content content = MasterList.GetSubject(ID);
                if (content == null) continue;
                nccwsActual.Add(content);
            }
            Actual.PostLoad(prerequisitesActual, corequisitesActual, nccwsActual);
        }

        public override string ToString() => $"{Name} ({Code})";

        public object Clone() => this.MemberwiseClone();
    }
}