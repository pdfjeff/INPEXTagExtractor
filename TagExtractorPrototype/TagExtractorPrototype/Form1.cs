using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;

namespace TagExtractorPrototype
{
    public partial class Form1 : Form
    {

        int FilesProcessed;

        public Form1()

        {
            InitializeComponent();
            iTextSharp.text.pdf.PdfReader.unethicalreading = true;
         
        }

        private void RotatePDF(string inputFile, string outputFile, int desiredRot)
        {


                PdfReader reader = new PdfReader(inputFile);
                FileStream outStream = new FileStream(outputFile, FileMode.Create);

                int numPages = reader.NumberOfPages;

                PdfDictionary page;

                for (int pageNum = 0; pageNum < numPages; )
                {
                    ++pageNum;

                     page = reader.GetPageN(pageNum);

                     PdfNumber rotate = page.GetAsNumber(PdfName.ROTATE);

                   
                    desiredRot += rotate.IntValue;
                    desiredRot %= 360; // must be 0, 90, 180, or 270

                    page.Put(PdfName.ROTATE, new PdfNumber(desiredRot));


                }
                PdfStamper stamper = new iTextSharp.text.pdf.PdfStamper(reader, outStream);
                stamper.Close();
                reader.Close();
                outStream.Close();

            
        }


        private void Form1_Close(object sender, EventArgs e)
        {

        }


        private void processFolder()
        {


            string thePath = "\\\\psf\\Dropbox\\Adlib\\Clients\\JCG\\Inpex Tag Extractor\\Samples";

            System.IO.DirectoryInfo theDirectoryInfo = new System.IO.DirectoryInfo(thePath);

            System.IO.FileInfo[] thetxtFiles = theDirectoryInfo.GetFiles("*.txt");

            foreach (System.IO.FileInfo thetxtFile in thetxtFiles)
            {

               // textBox2.Text += DateTime.Now.ToShortTimeString() + " Opening " + thetxtFile.Name + "\r\n";
               // Application.DoEvents();

                string txtFile = System.IO.File.ReadAllText(thetxtFile.FullName);

                //Find Matches

                var myRegex = new Regex(textBox1.Text);
                MatchCollection AllMatches = myRegex.Matches(txtFile);
                if (AllMatches.Count > 0)
                {

                    string matchText = "";

                    foreach (Match SomeMatch in AllMatches)
                    {

                        matchText += thetxtFile.Name.Replace(".txt", "") + "\t" + SomeMatch.Value + "\r\n";
                    }


                    System.IO.File.AppendAllText("Tags_txt.txt", matchText);

                    FilesProcessed += 1;
                    label1.Text = FilesProcessed + " Files Processed.";
                    Application.DoEvents(); 
                }

                //Insert Doc & Matches

            }

            textBox2.Text += DateTime.Now.ToShortTimeString() + " Done.\r\n";
            textBox2.ScrollToCaret();
            Application.DoEvents();  
        }


        private void button1_Click(object sender, EventArgs e)
        {
            FilesProcessed = 0;
            processFolder();
            }
        



        private void button2_Click(object sender, EventArgs e)
        {

            FilesProcessed = 0;
            //Do the 15 files 11,000 times to simulate 165K files.
            for (int i = 1; i <= 11000; i++)
            {
                processFolder();
            }



        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

 

 private void button4_Click(object sender, EventArgs e)
        {
       string thePath = @"\\psf\Dropbox\Adlib\Clients\JCG\Inpex Tag Extractor\Samples Provided";

            System.IO.DirectoryInfo theDirectoryInfo = new System.IO.DirectoryInfo(thePath);

            System.IO.FileInfo[] thePDFFiles = theDirectoryInfo.GetFiles("*.pdf");

            foreach (System.IO.FileInfo thePDFFile in thePDFFiles)
            {
                RotatePDF(thePDFFile.FullName, (thePath + @"\Input\" +thePDFFile.Name.Replace(".pdf", "+l.pdf")), 270);
                RotatePDF(thePDFFile.FullName, (thePath + @"\Input\" + thePDFFile.Name.Replace(".pdf", "+r.pdf")), 90);
                File.Copy(thePDFFile.FullName, (thePath + @"\Input\" + thePDFFile.Name));
            }
        }
    }
}
