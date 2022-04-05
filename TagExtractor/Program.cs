// Tag Extractor by Jeff Brand - jbrand@adlibsoftware.com
//
// This application helps identify and extract / index tags within drawings
// Tags are identified using Regular Expressions configured in a user-defined file
// Source documents are rotated 90 degrees left and right to ensure all vertically-oriented text is extracted
// Tags are placed into an index file, a new index file is created once the file size exceeds a user-defined limit.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace TagExtractor
{
    class Program
    {



        static void Main(string[] args)
        {

            bool noMonitor = false;
            bool noReporting = false;

            foreach (string theArg in Environment.GetCommandLineArgs())
            {

                if(theArg.ToLower().Contains("-noinput")){noMonitor=true;}
                if(theArg.ToLower().Contains("-noextract")){noReporting=true;}
            }

            ValidateSettings(noMonitor, noReporting);

            InputMonitor myInputMonitor = new InputMonitor();
            ReportWriter myReportWriter = new ReportWriter();
            Thread myIMThread = new Thread(myInputMonitor.Monitor);
            Thread myRWThread = new Thread(() => myReportWriter.Monitor(noMonitor));

            if (!noMonitor)
            {     
                myIMThread.Start();
                while (!myIMThread.IsAlive) ;
                TellUser("Input monitor started for folder - " + Properties.Settings.Default.FolderToMonitor);

            }

            if (!noReporting)
            {
                myRWThread.Start();
                while (!myRWThread.IsAlive) ;
                TellUser("Tag Extractor started for folder - " + Properties.Settings.Default.TempFolder);
            }


            TellUser("Press Any Key to Stop Monitoring.");
            TellUser("Or place a file named stop in the Input Folder.");

            while (((myIMThread.IsAlive && !noMonitor) | noMonitor) && ((myRWThread.IsAlive && !noReporting) | noReporting) && !Console.KeyAvailable)
            {
                Thread.Sleep(500);
            }

            TellUser("Stopping...");

            //Ask nicely for Input Monitor to stop
            myInputMonitor.RequestStop();

            //Ask nicely for Report Writer to stop
            myReportWriter.RequestStop();
            try
            {
                //Wait for Input Monitor to stop
                myIMThread.Join();
            }
            catch
            {
            }

            try
            {
                //Wait for Report Writer to stop
                myRWThread.Join();
            }
            catch
            {
            }


        }

        public static void ExitOnError(string errMsg)
        {
            TellUser(errMsg);
            TellUser("Press Any Key to End.", true);
            Environment.Exit(0);
        }

        public static void TellUser(string msg, bool waitKey = false)
        {
            msg = System.DateTime.Now.ToString() + "\t" + msg;
            Console.WriteLine(msg);
            if (waitKey == true) Console.ReadKey();
        }

        public static void WriteLog(string msg)
        {
            try 
            { 
                File.AppendAllText(Properties.Settings.Default.ErrorFolder + @"\log.txt", System.DateTime.Now.ToString() + "\t" + msg);
                TellUser(msg);
            }
            catch 
            {
                ExitOnError("Unable to write to error log!" + Properties.Settings.Default.ErrorFolder + @"\log.txt");
            }
        }

        static void ValidateSettings(bool noMonitor, bool noReporting)
        {
            //Validate Settings - Do Folders Exist? TODO: Do we have write access?  Do we have an XML JT?

            //Folder to Monitor (RW)
            if (!noMonitor && !CheckDirectoryAccess(Properties.Settings.Default.FolderToMonitor))
            {
                ExitOnError("Input Folder does not exist, or we don't have write access - " + Properties.Settings.Default.FolderToMonitor);
            }

            //Adlib Input Folder (RW)
            if (!noMonitor && !CheckDirectoryAccess(Properties.Settings.Default.AdlibInputFolder))
            {
                ExitOnError("Adlib Input Folder does not exist, or we don't have write access - " + Properties.Settings.Default.AdlibInputFolder);
            }

            //Temp Folder (RW)
            if (!CheckDirectoryAccess(Properties.Settings.Default.TempFolder))
            {
                ExitOnError("Temp does not exist, or we don't have write access - " + Properties.Settings.Default.TempFolder);
            }

            //Tag Definitions File (R not Zero Bytes)
            if (!noReporting && !System.IO.File.Exists(Properties.Settings.Default.TagDefinitionsFile))
            {
                ExitOnError("Tag Definitions File does not exist or we don't have access - " + Properties.Settings.Default.TagDefinitionsFile);
            }

            //Reports Folder (RW)
            if (!noReporting && !CheckDirectoryAccess(Properties.Settings.Default.ReportsFolder))
            {
                ExitOnError("Reports Folder does not exist, or we don't have write access - " + Properties.Settings.Default.ReportsFolder);
            }

            //Errors Folder (RW)
            if (!CheckDirectoryAccess(Properties.Settings.Default.ErrorFolder))
            {
                ExitOnError("Errors Folder does not exist, or we don't have write access - " + Properties.Settings.Default.ErrorFolder);
            }

        }

        public static bool CheckForStopFile(string folderToCheck)
        {
            bool shouldStop = false;
            DirectoryInfo InputDirInfo = new DirectoryInfo(folderToCheck);

            //Quickly run through the files to make sure there's no stopfile.
            foreach (FileInfo theFile in InputDirInfo.GetFiles())
            {
                if (theFile.Name.ToString().ToLower() == "stop")
                {
                    shouldStop = true;
                    try { theFile.Delete(); }
                    catch { }
                }
            }
            return shouldStop;
        }

        private static bool CheckDirectoryAccess(string directory)
        {

            bool success = false;

            string fullPath = directory + "testfile.tmp";

            if (Directory.Exists(directory))
            {
                try
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fs.WriteByte(0xff);
                    }
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;

                }
            }
            return success;
        }
    }
}
