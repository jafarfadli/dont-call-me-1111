using System;

namespace DontCallMe.Data
{
    public enum FactKind
    {
        Phone,
        Account,
        Name,
        Case,
        Url,
        Amount,
        Text,
    }

    /// <summary>
    /// A checkable detail (a number, an account, a name, a web address). Facts show up as
    /// copy chips in the transcript, documents and the notebook's Case tab.
    /// </summary>
    [Serializable]
    public class Fact
    {
        public FactKind kind;
        public string value;
        public string label;

        public Fact() { }

        public Fact(FactKind kind, string value, string label = null)
        {
            this.kind = kind;
            this.value = value;
            this.label = label;
        }

        public string Display => string.IsNullOrEmpty(label) ? value : $"{label}: {value}";
    }

    /// <summary>Phone numbers and account numbers compare without dashes or spaces.</summary>
    public static class FactText
    {
        public static string Digits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            var chars = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (char.IsDigit(c))
                    chars.Append(c);
            return chars.ToString();
        }

        public static bool SameNumber(string a, string b) => Digits(a).Length > 0 && Digits(a) == Digits(b);

        public static string Won(long amount)
        {
            string sign = amount < 0 ? "−" : "";
            return $"{sign}₩{Math.Abs(amount):N0}";
        }
    }
}
