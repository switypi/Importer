using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer
{
    public static class Utility
    {
        public static Country ConvertToCountry(string value)
        {
            switch (value.ToUpper().Trim())
            {
                case "AUSTRALIA":
                    return Country.AUS;

                case "NEWZEALAND":
                    return Country.NZ;

                default:
                    return Country.NA;
            }
        }

        public static State ConvertToState(string value)
        {
            switch (value.ToUpper().Trim())
            {
                case "AUSTRALIAN CAPITAL TERRITORY":
                    return State.ACT;
                case "NEW SOUTH WALES":
                    return State.NSW;
                case "NORTHERN TERRITORY":
                    return State.NSW;
                case "QUEENSLAND":
                    return State.QLD;
                case "SOUTH AUSTRALIA":
                    return State.SA;
                case "TASMANIA":
                    return State.TAS;
                case "VICTORIA":
                    return State.VIC;
                case "WESTERN AUSTRALIA":
                    return State.WA;

                default:
                     return State.NA;
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
        NA=3
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
