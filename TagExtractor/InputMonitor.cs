// Tag Extractor - Input Monitor by Jeff Brand - jbrand@adlibsoftware.com
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
using iTextSharp.text.pdf;

namespace TagExtractor
{
    class InputMonitor
    {
        private volatile bool _shouldStop;

        public void Monitor()
        {
            iTextSharp.text.pdf.PdfReader.unethicalreading = true;
            while (!_shouldStop)
            {
                //Program.TellUser("Scanning.");

                if (!Program.CheckForStopFile(Properties.Settings.Default.FolderToMonitor))
                {
                    CheckFolder(Properties.Settings.Default.FolderToMonitor);
                }
                else
                {
                    break;
                }

                System.Threading.Thread.Sleep(Properties.Settings.Default.ScanInterval);
            }
        }

        public void CheckFolder(string theFolder)
        {
            //Get the files in the folder
            DirectoryInfo InputDirInfo = new DirectoryInfo(theFolder);
            foreach (FileInfo theFile in InputDirInfo.GetFiles("*.pdf"))
            {
                if (_shouldStop) { break; }

                //Only process PDF Files
                if (theFile.Extension.ToString().ToLower() == ".pdf")
                {
                    Program.TellUser("Found PDF:" + theFile.Name);

                    string tempPath = Properties.Settings.Default.TempFolder;

                    if (!IsFileLocked(theFile))
                    {
                        
                        try
                        {

                            //Place original & Rotated files in temp location

                            string rFilePath = tempPath + @"\" + theFile.Name.Replace(".pdf", "+l.pdf");
                            string lFilePath = tempPath + @"\" + theFile.Name.Replace(".pdf", "+r.pdf");
                            string oFilePath = tempPath + @"\" + theFile.Name;

                            //Delete any files from the temp folder with the same name
                            if (File.Exists(rFilePath))
                            {
                                File.Delete(rFilePath);
                            }
                            if (File.Exists(lFilePath))
                            {
                                File.Delete(lFilePath);
                            }
                            if (File.Exists(oFilePath))
                            {
                                File.Delete(oFilePath);
                            }

                            //Rotate and save
                            RotatePDF(theFile.FullName, rFilePath, 270);
                            RotatePDF(theFile.FullName, lFilePath, 90);
                            File.Copy(theFile.FullName, oFilePath);

                            //Create our 3 XML Job Tickets, one for each rotation:

                            CreateXMLJobTicket(theFile.Name);
                            CreateXMLJobTicket(theFile.Name.Replace(".pdf", "+l.pdf"));
                            CreateXMLJobTicket(theFile.Name.Replace(".pdf", "+r.pdf"));
                            
                            //We got this far, let's delete the source file from the input folder
                            File.Delete(theFile.FullName);

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
                    }
                    else //The file was locked
                    {
                        Program.TellUser("Skipping Locked File - " + theFile.Name);
                    }
                }
            }

        }


        private void CreateXMLJobTicket(string DocInput)
        {

            //Load our template
            string xmlJobTicket = File.ReadAllText("JTTemplate.xml");

            //Update textmode and output sections
            xmlJobTicket = xmlJobTicket.Replace("&[TextMode]", Properties.Settings.Default.TextMode);
            xmlJobTicket = xmlJobTicket.Replace("&[OutputPath]", Properties.Settings.Default.TempFolder);
            xmlJobTicket = xmlJobTicket.Replace("&[OutputFileName]", DocInput.Replace("&", "&amp;"));

            //Update DOCINPUT section
            xmlJobTicket = xmlJobTicket.Replace("&[DocInputs]", @"<JOB:DOCINPUT FOLDER='" + Properties.Settings.Default.TempFolder + @"' FILENAME='" + DocInput.Replace("&","&amp;") + @"'/>");

            File.WriteAllText(Properties.Settings.Default.AdlibInputFolder + @"\" + DocInput + ".xml", xmlJobTicket);
            if (Properties.Settings.Default.RetainXMLJobTicket.ToLower() == "true")
            {
                if (!Directory.Exists(Properties.Settings.Default.ReportsFolder + @"\XML Job Tickets\"))
                {
                    Directory.CreateDirectory(Properties.Settings.Default.ReportsFolder + @"\XML Job Tickets\");
                }
                File.WriteAllText(Properties.Settings.Default.ReportsFolder + @"\XML Job Tickets\" + DocInput + ".xml", xmlJobTicket);
            }
        }

        private void RotatePDF(string inputFile, string outputFile, int desiredRot)
        {


            PdfReader reader = new PdfReader(inputFile);
            FileStream outStream = new FileStream(outputFile, FileMode.Create);

            int numPages = reader.NumberOfPages;

            PdfDictionary page;
            PdfNumber rotate;

            for (int pageNum = 0; pageNum < numPages; )
            {
                ++pageNum;
                page = reader.GetPageN(pageNum);
                rotate = page.GetAsNumber(PdfName.ROTATE);

                if (rotate == null)
                {
                    page.Put(PdfName.ROTATE, new PdfNumber(desiredRot));
                }
                else
                {

                    desiredRot += rotate.IntValue;
                    desiredRot %= 360; // must be 0, 90, 180, or 270
                    page.Put(PdfName.ROTATE, new PdfNumber(desiredRot));
                }


            }
            PdfStamper stamper = new iTextSharp.text.pdf.PdfStamper(reader, outStream);
            stamper.Close();
            reader.Close();
            outStream.Close();


        }

        public void RequestStop()
        {
            _shouldStop = true;
        }





        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
       


    }
}
