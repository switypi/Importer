using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer
{
    public static class LogWriter
    {
        public static void WriteLog(string path, string Mesg)
        {
            StreamWriter sm = new StreamWriter(path, true);
            sm.WriteLine(Mesg);
            sm.Flush();
            sm.Close();
        }
    }

}
