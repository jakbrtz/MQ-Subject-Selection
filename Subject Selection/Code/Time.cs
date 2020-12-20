using System;

namespace Subject_Selection
{
    public struct Time : IComparable<Time>
    {
        public int year;
        public Session session;

        public override string ToString()
        {
            return "Year " + year.ToString() + " " + session.ToString();
        }

        public Time Next()
        {
            bool nextYear = session == Session.S3;
            Time next = this;
            if (nextYear)
            {
                next.session = Session.S1;
                next.year++;
            }
            else
            {
                next.session = session + 1;
            }
            return next;
        }

        public Time Previous()
        {
            Time previous = this;
            bool previousYear = session == Session.S1;
            if (previousYear)
            {
                previous.session = Session.S3;
                previous.year--;
            }
            else
            {
                previous.session = session - 1;
            }
            return previous;
        }

        public int AsNumber()
        {
            return (year - 1) * 4 + (int)session;
        }

        public bool IsEarlierThan(Time other)
        {
            if (year < other.year)
                return true;
            if (year > other.year)
                return false;
            return session < other.session;
        }

        public bool IsEarlierThanOrAtTheSameTime(Time other)
        {
            return !other.IsEarlierThan(this);
        }

        public int CompareTo(Time other)
        {
            if (year < other.year)
                return -1;
            if (year > other.year)
                return 1;
            if (session < other.session)
                return -1;
            if (session > other.session)
                return 1;
            return 0;
        }

        public static readonly Time First = new Time { year = 1, session = Session.S1 };
        public static readonly Time Early = First.Previous();
        public static readonly Time Impossible = new Time { year = 100 };
        public static readonly Time All = Impossible;
    }
    public enum Session { S1, WV, S2, S3 }
    public enum Method { Day, Block, Fieldwork, External, Online, Placement, Evening }

    public struct OfferTime
    {
        public Session session;
        public bool fullYear;
        public Method method;

        public static bool TryParse(string str, out OfferTime result)
        {
            result = new OfferTime();

            var words = str.Split(' ');
            if (words.Length != 2)
                return false;

            switch (words [0])
            {
                case "S1":
                case "FY1":
                    result.session = Session.S1;
                    break;
                case "WV":
                    result.session = Session.WV;
                    break;
                case "S2":
                case "FY2":
                    result.session = Session.S2;
                    break;
                case "S3":
                    result.session = Session.S3;
                    break;
                default:
                    return false;
            }

            result.fullYear = words[0].StartsWith("FY");

            if (!Method.TryParse(words[1], out result.method))
                return false;

            return true;
        }

        public override string ToString()
        {
            return session.ToString() + (fullYear ? "(FY)" : "") + " " + method.ToString();
        }
    }

    public static class SessionExtention
    {
        public static string FullName(this Session session)
        {
            return session switch
            {
                Session.S1 => "Session 1",
                Session.WV => "Winter Vacation",
                Session.S2 => "Session 2",
                Session.S3 => "Session 3",
                _          => throw new ArgumentException(session.ToString() + " is not a know Session")
            };
        }
    }
}
