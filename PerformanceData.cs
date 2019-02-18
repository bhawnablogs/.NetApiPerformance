using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ParserPerformance
{
   public class PerformanceData
    {
       public enum DataType
       {
          ASCIIString
       }

        public static System.Collections.Generic.Dictionary<int, TimeTakenForData[]> GraphData { get; set; }

        public static string[] Addresses { get; set; }

        public static int[] DataRanges { get; set; }

        public class TimeTakenForData
        {
            public long TimeTaken { get; set; }
            public string Address { get; set; }
        }

        private static Stopwatch s_stopWatch = new Stopwatch();
        public static void FetchPerformanceData(DataType dataType)
        {
            //string[] addresses = new string[] { "TCPIP0::mastbgtest::inst0::INSTR", "TCPIP0::mastbgtest::hislip0::INSTR" };// args[0];
            string[] addresses = new string[] { "<VISAAddress1>", "<VISAAddress2>" };// args[0];
            if (addresses.Length == 0)
            {
                Console.WriteLine("Unexpected command line: should have 1 argument.");
                Console.WriteLine(string.Empty);
                ShowUsage();
                return;
            }
            Addresses = addresses;
            string command = "";
            switch (dataType)
            {
                case DataType.ASCIIString:
                    command = "STRnode";
                    break;
                
            }
            GraphData = new System.Collections.Generic.Dictionary<int, TimeTakenForData[]>();
            DataRanges = new int[] { 16000,32000, 48000, 64000,80000 };
            //DataRanges = new int[] { 16000};

            for (int index = 0; index < DataRanges.Length; index++)
            {
                int Size = DataRanges[index];
                TimeTakenForData[] tymTaken = new TimeTakenForData[addresses.Length];
                for (int iIndex = 0; iIndex < addresses.Length; iIndex++)
                {
                    var address = addresses[iIndex];
                    var io = new IO();
                    io.Open(address);

                    string dataSent = GetData(Size, dataType);

                    s_stopWatch.Reset();
                    s_stopWatch.Start();
                    io.Write(command + " " + dataSent);
                    s_stopWatch.Stop();

                    long timetakentoWrite = s_stopWatch.ElapsedMilliseconds;
                    Console.WriteLine("Time Taken to Write : {0}", timetakentoWrite);

                    s_stopWatch.Reset();
                    s_stopWatch.Start();
                    io.Write(command + "?");
                    string result = io.Read();
                    s_stopWatch.Stop();

                    long timetakentoRead = s_stopWatch.ElapsedMilliseconds;
                    Console.WriteLine("Time Taken to Read : {0}", timetakentoRead);
                    TimeTakenForData tm = new TimeTakenForData();
                    tm.Address = address;
                    tm.TimeTaken = timetakentoRead + timetakentoWrite;
                    Console.WriteLine("Time Taken For StringParameter {0}", tm.TimeTaken.ToString());
                    tymTaken[iIndex] = tm;
                    io.Close();
                }
                GraphData.Add(Size, tymTaken);
            }
           
        }
       private static string GetData(int Size,DataType dataType)
        {
            string data = "";
            if ((dataType == DataType.ASCIIString) )
            {
                data = GetASCIIData(Size, dataType);
            }
            else
            {
                Console.WriteLine("InCorrecr DataType!! Something wrong");
            }
            return data;
        }

       private static string GetASCIIData(int Size, DataType dataType)
       {
           int size = Size;
           StringBuilder builder = new StringBuilder();
           switch (dataType)
           {
               case DataType.ASCIIString:
                   {
                       string[] dataArray = new string[size];
                       builder = new StringBuilder();
                       for (int i = 0; i < dataArray.Length; i++)
                       {
                           builder.Append("'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'");
                           if (1 != dataArray.Length - 1)
                           {
                               builder.Append(",");
                           }
                       }
                       break;
                   }
           }

           var data = builder.ToString();
           // Remove Last Comma
           data = data.Remove(data.Length - 1);
           return data;
       }
        private static void ShowUsage()
        {
            Console.WriteLine("        TestParserPerformance Usage Help");
            Console.WriteLine();
            Console.WriteLine("Usage: TestParserPerformance");
            Console.WriteLine("       TestParserPerformance <VisaAddressString>");
            Console.WriteLine("Where:");
            Console.WriteLine("  <VisaAddressString> is used for the requested connection type.");
            Console.WriteLine("  Running TestParserPerformance without parameters prints this usage help.");
            Console.WriteLine();
        }
    }
}
