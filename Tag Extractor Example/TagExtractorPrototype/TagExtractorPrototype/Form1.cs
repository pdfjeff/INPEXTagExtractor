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



// using System.Data.SQLite;
// The database approach was terribly slow, it seems this implementation of SQLite is not efficient.
// No worries, we'll use text output instead, it's MUCH faster.

namespace TagExtractorPrototype
{
    public partial class Form1 : Form
    {

       // SQLiteConnection m_dbConnection;
        int FilesProcessed;

        public Form1()

        {
            InitializeComponent();

            iTextSharp.text.pdf.PdfReader.unethicalreading = true;

           // m_dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3");
           // m_dbConnection.Open();
            //SQLiteCommand cmd = new SQLiteCommand("PRAGMA journal_mode = WAL", m_dbConnection);
            //cmd.ExecuteNonQuery();
            //cmd.CommandText = "PRAGMA synchronous = NORMAL";
            //cmd.ExecuteNonQuery();


            try
            {
                //string sql = "CREATE TABLE tags (FileName VARCHAR(255), Tag VARCHAR(255))";
                //SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                //command.ExecuteNonQuery();
            }
            catch
            {

                //Error?  Guess we already had the table.
            }


         
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

                    //iTextSharp.text.pdf.PdfNumber rotation = pageDict.GetAsNumber(iTextSharp.text.pdf.PdfName.ROTATE);


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
            // m_dbConnection.Close();
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

                    ////We use a transaction to make these record inserts faster.
                    // SQLiteCommand command = new SQLiteCommand("begin", m_dbConnection);
                    // command.ExecuteNonQuery();

                    string matchText = "";

                    //string sql = "";
                    foreach (Match SomeMatch in AllMatches)
                    {

                        ////Persist this record to the database

                       // sql += "insert into tags (FileName, Tag) VALUES ('";
                       // sql += thetxtFile.Name.Replace(".txt", "") + "', '";
                       // sql += SomeMatch.Value + "');\r\n";

                        matchText += thetxtFile.Name.Replace(".txt", "") + "\t" + SomeMatch.Value + "\r\n";
                    }


                    //command.CommandText = sql;
                    //command.ExecuteNonQuery();

                    System.IO.File.AppendAllText("Tags_txt.txt", matchText);

                    //We use a transaction to make these record inserts faster.

                    //command.CommandText = "END";
                    //command.ExecuteNonQuery();

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
            //string sql = "DELETE FROM Tags";
            //SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            //command.ExecuteNonQuery();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //MessageBox.Show("Form1 Load!");
        }

        private void btnToFile_Click(object sender, EventArgs e)
        {
            //string sql = "select * from tags";
            //string TagReport = "";

            //SQLiteCommand commandCount = new SQLiteCommand("select count(*) from tags", m_dbConnection);
            //var RecordCount = commandCount.ExecuteScalar();
            //SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            //SQLiteDataReader reader = command.ExecuteReader();
            //FilesProcessed = 0;
            


            //while (reader.Read())
            //{
            //    TagReport += reader["FileName"] + "\t" + reader["Tag"] + "\r\n";
            //    FilesProcessed += 1;
            //    label1.Text = "Retrieved " + FilesProcessed + " records of " + RecordCount + ".";
            //    Application.DoEvents();

            //}
            //File.WriteAllText("Tags_DB.txt", TagReport);
            //label1.Text = "Wrote out " + FilesProcessed + " records.";
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
