using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subject_Selection
{
    public static class SubjectReader
    {
        static readonly Dictionary<string, Subject> majors = new Dictionary<string, Subject>();
        static readonly Dictionary<string, Subject> subjects = new Dictionary<string, Subject>();

        public static Subject GetSubject(string id)
        {
            id = id.Split('[')[0];
            if (subjects.TryGetValue(id, out Subject subject))
                return subject;
            if (majors.TryGetValue(id, out Subject major))
                return major;
            return null;
        }

        public static void Load()
        {
            foreach (string line in Properties.Resources.Subjects.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                string[] properties = line.Split('|');
                Subject subject = new Subject(properties[0], properties[1], properties[2], properties[3], properties[4]);
                subjects.Add(subject.ID, subject);
            }

            /* TODO:
             * admission to BAdvSc
             * (HSC Mathematics Band 2 or Extension 1 or Extension 2)
             * GetOptions() assumes that all subjects are always 3cp
             * consider the fact the offer times change (and some subjects aren't offered)
             */

            foreach (string line in Properties.Resources.Majors.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                string[] properties = line.Split('|');
                Subject major = new Subject(properties[0], "0", "S1D S2D S3D", properties[1], ""); //TODO, treat majors differently
                majors.Add(major.ID, major);
            }
        }

        public static List<string> GetSubjectsFromRange(string query)
        {
            /*
             * Queries are made by using a dash to represent a range
             * Examples:
             *     200-999          represents any unit that is 200 level or higher
             *     COMP100-199      represents a COMP unit that is 100 level
             * This assumes that there are always 3 digits and the end of the subject ID (this is the case at MQ)
             */

            string unit = "";
            int lower = 100;
            int upper = 999;

            if (query.Contains('-'))
            {
                unit = query.Substring(0, query.Length - 7);
                query = query.Substring(query.Length - 7);

                lower = int.Parse(query.Split('-')[0]);
                upper = int.Parse(query.Split('-')[1]);
            }
            else if (query.Contains('+'))
            {
                unit = query.Substring(0, query.Length - 4);
                query = query.Substring(query.Length - 4);
                
                lower = int.Parse(query.Split('+')[0]);
            }

            if (unit == "*") unit = "";

            return subjects.Keys.ToList().FindAll(subject =>
                subject.StartsWith(unit) &&
                lower <= int.Parse(subject.Substring(subject.Length - 3)) &&
                int.Parse(subject.Substring(subject.Length - 3)) <= upper)
                .ToList();
        }

        public static int GetNumber(this Subject subject)
        {
            //Assumes all IDs have 3 digits at end
            if(int.TryParse(subject.ID.Substring(subject.ID.Length - 3), out int value)) //TODO: remove this by no longer treating COURSES and majors as subjects
                return value;
            return 1000;
        }

        public static int GetLevel(this Subject subject)
        {
            return subject.GetNumber() / 100;
        }
    }
}
