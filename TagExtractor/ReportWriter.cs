// Tag Extractor - Report Writer / Extractor by Jeff Brand - jbrand@adlibsoftware.com
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
using System.Xml;
using System.Text.RegularExpressions;

namespace TagExtractor
{
    class ReportWriter
    {
        private volatile bool _shouldStop;
        private volatile bool _ignorePDFs;

        List<TagDefinition> myDefinitions;



        public void Monitor(bool ignorePDFs)
        {
            _ignorePDFs = ignorePDFs;

            myDefinitions = loadDefinitions();

            while (!_shouldStop)
            {

                    if (!Program.CheckForStopFile(Properties.Settings.Default.TempFolder))
                    {
                        CheckFolder(Properties.Settings.Default.TempFolder);
                        System.Threading.Thread.Sleep(Properties.Settings.Default.ScanInterval);
                    }
                    else
                    {

                    _shouldStop = true;

                    }
                    
            }

        }

        public void CheckFolder(string theFolder)
        {

            //Get the files in the folder
            DirectoryInfo InputDirInfo = new DirectoryInfo(theFolder);

            //Process any .TXT File
            foreach (FileInfo theFile in InputDirInfo.GetFiles("*.txt"))
            {
                if (_shouldStop) { break; }

                Program.TellUser("Found TXT:" + theFile.Name);

                //Rotation is based on last character in file name, default is Horizontal

                string rotationFlag = "Horizontal";
                string OriginalFileName;

                if (theFile.Name.ToLower().EndsWith("+r.pdf.txt"))
                {
                    rotationFlag = "Vertical-Up";
                    OriginalFileName = theFile.Name.Replace("+r.pdf.txt", ".pdf");
                }
                else if (theFile.Name.ToLower().EndsWith("+l.pdf.txt"))
                {
                    rotationFlag = "Vertical-Down";
                    OriginalFileName = theFile.Name.Replace("+l.pdf.txt", ".pdf");
                }
                else
                {
                    OriginalFileName = theFile.Name.Replace(".pdf.txt", ".pdf");
                }
            
                //Load TXT into memory
                string strTextOutput = File.ReadAllText(theFile.FullName);

                //For Each Tag Definition
                foreach (TagDefinition thisDefinition in myDefinitions)
                {
                    //Check for a match
                    var myRegex = new Regex(thisDefinition.tagRegEx);
                    MatchCollection AllMatches = myRegex.Matches(strTextOutput);
                    if (AllMatches.Count > 0)
                    {
                        string PageNumber = "Text";
                        foreach (Match SomeMatch in AllMatches)
                        {
                            string reportLine = OriginalFileName + ",";
                            reportLine += thisDefinition.tagGroupName + ",";
                            reportLine += thisDefinition.tagName + ",";
                            reportLine += rotationFlag + ",";
                            reportLine += PageNumber + ",";
                            reportLine += SomeMatch;
                            reportLine += Environment.NewLine;

                            //Write out to Tag Index Report
                            AddToReport(reportLine);

                        }//Next Match

                    } //Matches? 


                }//Next Definition


                //Archive or delete our Text file?
                try
                {
                    string TxtPath = Properties.Settings.Default.ReportsFolder + @"\TXTFile\";

                    if (Properties.Settings.Default.RetainTextOutput.ToLower() == "true")
                    {
                        if (!Directory.Exists(TxtPath))
                        {
                            Directory.CreateDirectory(TxtPath);
                        }
                        if (File.Exists(TxtPath + theFile.Name))
                        {
                            deleteFile(TxtPath + theFile.Name);
                        }
                        moveFile(theFile.FullName, TxtPath + theFile.Name);
                    }
                    else
                    {
                        if (File.Exists(theFile.FullName))
                        {
                            deleteFile(theFile.FullName);
                        }
                    }

                }
                catch (Exception e)
                {
                    Program.WriteLog("Error cleaning up Text File - " + theFile.FullName);
                    Program.WriteLog(e.Message);
                }
    
            }

            //Process the XML Files (PDFInfo)
            foreach (FileInfo theFile in InputDirInfo.GetFiles("*.xml"))
            {
                if (_shouldStop) { break; }

                Program.TellUser("Found XML:" + theFile.Name);

                //Rotation is based on last character in file name

                string rotationFlag = "Horizontal";
                string OriginalFileName;

                if (theFile.Name.ToLower().EndsWith("+r.pdf.xml"))
                {
                    rotationFlag = "Vertical-Up";
                    OriginalFileName = theFile.Name.Replace("+r.pdf.xml", ".pdf");
                }
                else if (theFile.Name.ToLower().EndsWith("+l.pdf.xml"))
                {
                    rotationFlag = "Vertical-Down";
                    OriginalFileName = theFile.Name.Replace("+l.pdf.xml", ".pdf");
                }
                else
                {
                    OriginalFileName = theFile.Name.Replace(".pdf.xml", ".pdf");
                }

             
                //Load XML Into Memory
                XmlDocument myPDFInfo = new XmlDocument();
                try
                {
                    myPDFInfo.Load(theFile.FullName);
                }
                catch (Exception e)
                {
                    //Something went wrong, let's clean up & report it.

                    Program.TellUser("Error processing - " + theFile.Name + e.ToString());

                    //Move source file to error folder, but only if it's not already there....
                    if (!File.Exists(Properties.Settings.Default.ErrorFolder + @"\" + theFile.Name))
                    {
                        File.Move(theFile.FullName, Properties.Settings.Default.ErrorFolder + @"\" + theFile.Name);
                    }
                    else
                    {
                        File.Delete(theFile.FullName);
                    }

                    string errMsg = "Error processing - " + theFile.Name + e.ToString() + Environment.NewLine;
                    Program.WriteLog(errMsg);
                }

                //For Each Record

                foreach (XmlNode thisRecord in myPDFInfo.GetElementsByTagName("TEXTSTRING"))
                {
                    //For Each Tag Definition
                    foreach (TagDefinition thisDefinition in myDefinitions)
                    {
                        //Check for a match

                        var myRegex = new Regex(thisDefinition.tagRegEx);
                        MatchCollection AllMatches = myRegex.Matches(thisRecord.InnerText);
                        if (AllMatches.Count > 0)
                        {
                            string PageNumber = thisRecord.Attributes.GetNamedItem("PAGE").Value.ToString();
                            foreach (Match SomeMatch in AllMatches)
                            {
                                string reportLine = OriginalFileName + ",";
                                reportLine += thisDefinition.tagGroupName + ",";
                                reportLine += thisDefinition.tagName + ",";
                                reportLine += rotationFlag + ",";
                                reportLine += PageNumber + ",";
                                reportLine += SomeMatch;
                                reportLine += Environment.NewLine;

                                //Write out to Tag Index Report
                                AddToReport(reportLine);

                            }//Next Match

                            //Delete Matching record from XML

                            // - This next line removes the match meaning it won't be found again on subsequent passes
                            //  -   Disabled for now since we were getting subset matches and then leaving partial tags behind.
                            //
                            //  thisRecord.InnerText = "MATCH FOUND - " + thisDefinition.tagName;

                        } //Matches? 


                    }//Next Definition

                }//Next Record

                //Done with this XML, Delete it and it's respective source PDF

                //First, let's define our paths
                string PDFInfoPath = Properties.Settings.Default.ReportsFolder + @"\PDFInfo\";
                string SearchablePDFPath = Properties.Settings.Default.ReportsFolder + @"\SearchablePDF\";
                string OriginalsPath = Properties.Settings.Default.ReportsFolder + @"\Originals\";

                //The original PDF File name is the same as the XML but without .xml
                string thePDFFileName = theFile.Name.Replace(".pdf.xml", ".pdf");

                //The Output / Searchable PDF is same as above but has an extra PDF appended
                string theSearchablePDFFileName = theFile.Name.Replace(".pdf.xml", "pdf.Searchable.pdf");
                string theSearchablePDFFullName = theFile.FullName.Replace(".pdf.xml", ".pdf.Searchable.pdf");

                try
                {

                    //Deal with PDFInfo Files first
                    if (Properties.Settings.Default.RetainPDFInfo.ToLower() == "true" )
                    {
                        if (!Directory.Exists(PDFInfoPath))
                        {
                            Directory.CreateDirectory(PDFInfoPath);
                        }
                        if (File.Exists(PDFInfoPath + theFile.Name))
                        {
                            deleteFile(PDFInfoPath + theFile.Name);
                        }
                       moveFile(theFile.FullName, PDFInfoPath + theFile.Name);
                    }
                    else
                    {
                        if (File.Exists(theFile.FullName))
                        {
                        deleteFile(theFile.FullName);
                        }
                    }

                }
                catch (Exception e)
                {
                    Program.WriteLog("Error cleaning up PDFInfo File - " + theFile.FullName);
                    Program.WriteLog(e.Message);
                }
                if (!_ignorePDFs)
                {
                    try
                    {
                        //Now the Searchable PDF...
                        if (Properties.Settings.Default.RetainSearchablePDF.ToLower() == "true")
                        {
                            //Create the destination directory for the searchable PDF (SearchablePDFPath)
                            if (!Directory.Exists(SearchablePDFPath))
                            {
                                Directory.CreateDirectory(SearchablePDFPath);
                            }
                            //If output already exists at the destination, we'll need to delete it
                            if (File.Exists(SearchablePDFPath + thePDFFileName))
                            {
                                deleteFile(SearchablePDFPath + thePDFFileName);
                            }
                            moveFile(theSearchablePDFFullName, SearchablePDFPath + thePDFFileName);
                        }
                        else
                        {
                            deleteFile(theSearchablePDFFullName);
                        }
                    }
                    catch (Exception e)
                    {
                        Program.WriteLog("Error cleaning up Searchable PDF File - " + theFile.FullName);
                        Program.WriteLog(e.Message);
                    }

                    try
                    {

                        //Now the Original PDF...
                        if (Properties.Settings.Default.RetainOriginalPDF.ToLower() == "true" && File.Exists(theFile.FullName.Replace(".pdf.xml", ".pdf")))
                        {
                            if (!Directory.Exists(OriginalsPath))
                            {
                                Directory.CreateDirectory(OriginalsPath);
                            }
                            if (File.Exists(OriginalsPath + thePDFFileName))
                            {
                                deleteFile(OriginalsPath + thePDFFileName);
                            }
                            moveFile(theFile.FullName.Replace(".pdf.xml", ".pdf"), OriginalsPath + thePDFFileName);
                        }
                        else
                        {
                            if (File.Exists(theFile.FullName.Replace(".pdf.xml", ".pdf")))
                            {
                                deleteFile(theFile.FullName.Replace(".pdf.xml", ".pdf"));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Program.WriteLog("Error cleaning up Original PDF - " + theFile.FullName);
                        Program.WriteLog(e.Message);
                    }
                }

            }

        }
        private bool moveFile(string sourceFileName, string destFileName)
        {
            bool fileMoved = false;
            int tries = 0;
            string errorText = "";

            while (!fileMoved && tries <= 5)
            {
                try
                {
                    File.Move(sourceFileName, destFileName);

                    //We only get here without an error, so...
                    return true;
                }
                catch (Exception e) 
                {

                    //OOps something went wrong.  Might be the file isn't there yet or was locked.  Wait 5 secs and try again.
                    //Up to 5 times, then exit anyways.
                    // - This was added because sometimes Adlib will place the PDFInfo XML file a few moments before the Searchable PDF, so if we're trying to move or delete the
                    //   Searchable PDF, it's possible we won't find it yet.

                    //Wait 5 seconds before we try again.
                    System.Threading.Thread.Sleep(Properties.Settings.Default.CleanupDelayMS);
                    tries += 1;
                    errorText = e.ToString();
                }
            }

            //If we didn't manage to delete the file after 5 tries, write it in the log and onscreen

            if (!fileMoved && File.Exists(sourceFileName))
            {
                Program.WriteLog("Unable to move " + sourceFileName + ", but it exists.");
                Program.WriteLog(errorText);
            }
            
            return false;
        }
        private bool deleteFile(string fileToDelete)
        {
                            bool fileDeleted = false;
                            int tries = 0;

                            while(!fileDeleted && tries <= 5)
                            {
                                try
                                {
                                    File.Delete(fileToDelete);

                                    //We only get here without an error, so...
                                    return true;
                                }
                                catch
                                {

                                    //OOps something went wrong.  Might be the file isn't there yet or was locked.  Wait 5 secs and try again.
                                    //Up to 5 times, then exit anyways.
                                    // - This was added because sometimes Adlib will place the PDFInfo XML file a few moments before the Searchable PDF, so if we're trying to move or delete the
                                    //   Searchable PDF, it's possible we won't find it yet.

                                    //Wait 5 seconds before we try again.
                                    System.Threading.Thread.Sleep(Properties.Settings.Default.CleanupDelayMS);
                                    tries += 1;
                                }
                            }

                            //If we didn't manage to delete the file after 5 tries, write it in the log and onscreen

                            if (!fileDeleted && File.Exists(fileToDelete))
                            {
                                Program.WriteLog("Unable to delete " + fileToDelete + ", and it exists.");
                             }


            return fileDeleted;
        }

        private void AddToReport(string reportLine)
        {
            string reportFileName = Properties.Settings.Default.ReportsFolder + @"\TagIndex.txt";

            //Client requested to add header to each text file.
            if(!File.Exists(reportFileName))
            {
                string HeaderLine = "FileName" + ",";
                HeaderLine += "TagGroupName" + ",";
                HeaderLine += "TagName" + ",";
                HeaderLine += "Orientation" + ",";
                HeaderLine += "PageNumber" + ",";
                HeaderLine += "TagValue";
                HeaderLine += Environment.NewLine;
                File.AppendAllText(reportFileName, HeaderLine);
            }

            File.AppendAllText(reportFileName, reportLine);

            //If the file size is greater than xxx., create a new one.
            long length = new System.IO.FileInfo(reportFileName).Length;

            if (length > (Properties.Settings.Default.ReportSizeLimitMB * 1024 * 1024))
            {
                string newFileName = System.DateTime.Now.ToString().Replace("/",".").Replace(":",".") + " " + "TagIndex.txt";
                try { File.Copy(reportFileName, Properties.Settings.Default.ReportsFolder + @"\" + newFileName); }
                catch (Exception e) 
                { 
                    Program.WriteLog("Error copying Tag Index - " + e.Message);
                }

                try
                { File.Delete(reportFileName); }
                catch (Exception e)
                { 
                    Program.WriteLog("Error deleting Tag Index - " + e.Message);
                }

            }
        }



        public void RequestStop()
        {
            _shouldStop = true;
        }

        public List<TagDefinition> loadDefinitions()
        {
            List<TagDefinition> myDefinitions = new List<TagDefinition>();

            try
            {
                foreach (var line in File.ReadLines(Properties.Settings.Default.TagDefinitionsFile))
                {
                    TagDefinition temp = new TagDefinition();
                    var tempLine = line.Split('\t');
                    temp.tagGroupName = tempLine[0];
                    temp.tagName = tempLine[1];
                    temp.tagRegEx = tempLine[2];
                    myDefinitions.Add(temp);
                }

                return myDefinitions;
            }
            catch (Exception e)
            {
                Program.TellUser("Error loading Tag Definitions - Stopping" + e.Message);
                _shouldStop = true;
                return myDefinitions;
            }
        }


    }

    class TagDefinition
    {
        public string tagName { get; set; }
        public string tagGroupName { get; set; }
        public string tagRegEx { get; set; }

    }



}
