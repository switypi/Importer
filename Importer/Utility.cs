using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer
{
    public static class Utility
    {
        public static string ConvertToCountry(string value)
        {
            switch (value.ToUpper().Trim())
            {
                case "AUSTRALIA":
                    return Country.AUS.ToString();

                case "NEWZEALAND":
                    return Country.NZ.ToString();

                default:
                    if (Enum.GetNames(typeof(Country)).Contains(value))
                        return value;
                    else
                        return Country.NA.ToString();
            }
        }

        public static string ConvertToState(string value)
        {
            switch (value.ToUpper().Trim())
            {
                case "AUSTRALIAN CAPITAL TERRITORY":
                    return State.ACT.ToString();
                case "NEW SOUTH WALES":
                    return State.NSW.ToString();
                case "NORTHERN TERRITORY":
                    return State.NT.ToString();
                case "QUEENSLAND":
                    return State.QLD.ToString();
                case "SOUTH AUSTRALIA":
                    return State.SA.ToString();
                case "TASMANIA":
                    return State.TAS.ToString();
                case "VICTORIA":
                    return State.VIC.ToString();
                case "WESTERN AUSTRALIA":
                    return State.WA.ToString();

                default:
                    if (Enum.GetNames(typeof(State)).Contains(value))
                        return value;
                    else
                        return State.NA.ToString();

            }
        }

        public static string GeneratePassword(int passwordLength)
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
            char[] chars = new char[passwordLength];
            Random rd = new Random();

            for (int i = 0; i < passwordLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }
    }

    public enum Country
    {
        AUS = 1,
        NZ = 2,
        NA = 3
    }

    public enum State
    {
        ACT = 1,
        NSW = 2,
        NT = 3,
        QLD = 4,
        SA = 4,
        TAS = 5,
        VIC = 6,
        WA = 7,
        NA
    }
}
