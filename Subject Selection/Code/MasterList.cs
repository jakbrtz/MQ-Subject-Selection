using System.Collections.Generic;
using System.Linq;

namespace Subject_Selection
{
    public static class MasterList
    {
        private static readonly Dictionary<string, Subject> subjects = new Dictionary<string, Subject>();
        private static readonly Dictionary<string, Course> minors = new Dictionary<string, Course>();
        private static readonly Dictionary<string, Course> majors = new Dictionary<string, Course>();
        private static readonly Dictionary<string, Course> specialisations = new Dictionary<string, Course>();
        private static readonly Dictionary<string, Course> courses = new Dictionary<string, Course>();
        private static readonly Dictionary<string, Course> awards = new Dictionary<string, Course>();
        private static readonly List<(Content reason, Subject recommendation)> Recommendations = new List<(Content reason, Subject recommendation)>();

        public static void AddSubject(Subject subject, string code = null) => subjects[code ?? subject.ID] = subject;
        public static IEnumerable<Subject> AllSubjects => subjects.Values;

        public static void AddMinor(Course minor, string code = null) => minors[code ?? minor.ID] = minor;
        public static IEnumerable<Course> AllMinors => minors.Values;

        public static void AddMajor(Course major, string code = null) => majors[code ?? major.ID] = major;
        public static IEnumerable<Course> AllMajors => majors.Values;

        public static void AddSpecialisation(Course specialisation, string code = null) => specialisations[code ?? specialisation.ID] = specialisation;
        public static IEnumerable<Course> AllSpecialisations => specialisations.Values;

        public static void AddCourse(Course course, string code = null) => courses[code ?? course.ID] = course;
        public static IEnumerable<Course> AllCourses => courses.Values;

        public static void AddAward(Course award, string code = null) => awards[code ?? award.ID] = award;
        public static IEnumerable<Course> AllAwards => awards.Values;

        public static void AddRecommendation(Content reason, Subject recommendation) => Recommendations.Add((reason, recommendation));
        public static List<Content> ReasonsForRecommendation(Subject recommendation, IEnumerable<Content> otherSelectedContent) => Recommendations
            .Where(tuple => tuple.recommendation == recommendation && otherSelectedContent.Contains(tuple.reason))
            .Select(tuple => tuple.reason).ToList();

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

        public static Course GetMinor(string id)
        {
            if (minors.TryGetValue(id, out Course minor))
                return minor;
            return null;
        }

        public static Course GetMajor(string id)
        {
            if (majors.TryGetValue(id, out Course major))
                return major;
            return null;
        }

        public static Course GetSpecialisation(string id)
        {
            if (specialisations.TryGetValue(id, out Course specialisation))
                return specialisation;
            return null;
        }

        public static Course GetCourse(string id)
        {
            if (courses.TryGetValue(id, out Course course))
                return course;
            id = id.Replace("(0-12)", "").Replace("(0-5)", "");
            if (awards.TryGetValue(id, out course))
                return course;
            return null;
        }

        public static bool TryGetContent(string id, out Content content)
        {
            content = GetSubject(id);
            if (content != null)
                return true;
            content = GetMinor(id);
            if (content != null)
                return true;
            content = GetMajor(id);
            if (content != null)
                return true;
            content = GetSpecialisation(id);
            if (content != null)
                return true;
            content = GetCourse(id);
            if (content != null)
                return true;
            return false;
        }
    }
}
