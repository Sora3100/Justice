using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace Justice
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

    private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            vitaCheckBox.Enabled = false;

            richTextBox1.Clear();

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Freedom Wars EAF (*.eaf)|*.eaf";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                EAFHeader head;
                head.eafMagic = new byte[4];

                System.Buffer.BlockCopy(br.ReadBytes(0x4), 0, head.eafMagic, 0, 0x4);
                //head.eafVersion = BitConverter.ToInt32(br.ReadBytes(0x4).Reverse().ToArray(), 0);
                head.eafUnk1 = BitConverter.ToInt32(br.ReadBytes(0x4), 0);
                head.eafSize = BitConverter.ToInt64(br.ReadBytes(0x8), 0);
                head.eafFileNum = BitConverter.ToInt32(br.ReadBytes(0x4), 0);
                head.eafUnk2 = BitConverter.ToInt32(br.ReadBytes(0x4), 0);
                //richTextBox1.Text += "DEBUG: (before) " + br.BaseStream.Position + "\r\n";
                //richTextBox1.Update();
                br.BaseStream.Position += 0x58; // skip reserved space unless it becomes used one day
                //richTextBox1.Text += "DEBUG: (after) " + br.BaseStream.Position + "\r\n";
                //richTextBox1.Update();

                string magic = Encoding.ASCII.GetString(head.eafMagic);

                if (magic == "#EAF")
                {
                    richTextBox1.Text += "> Looks like a valid EAF magic!\r\n";

                    richTextBox1.Text += "> Unknown value 1: " + head.eafUnk1 + "\r\n";

                    if (head.eafSize != br.BaseStream.Length)
                        richTextBox1.Text += "> WARNING: EAF size discrepancy?\r\n";

                    richTextBox1.Text += "> Files in this EAF: " + head.eafFileNum + "\r\n";
                    richTextBox1.Text += "> Unknown value 2: " + head.eafUnk2 + "\r\n";

                    // Read File Table
                    // note: the first file seems to not include its offset and size

                    EAFFileInfo[] eafFileInfo = new EAFFileInfo[head.eafFileNum];
                    for (int i = 0; i < head.eafFileNum; i++)
                    {
                        eafFileInfo[i].beforeNamePadding = br.ReadBytes(0x10);
                        eafFileInfo[i].eafFileName = Encoding.ASCII.GetString(br.ReadBytes(0x100)).Split('\0')[0];
                        Console.WriteLine(eafFileInfo[i].eafFileName);
                        eafFileInfo[i].eafFileOffset = BitConverter.ToInt64(br.ReadBytes(0x8), 0);
                        eafFileInfo[i].eafFileSize = BitConverter.ToInt64(br.ReadBytes(0x8), 0);
                    }

                    // Visit Files & Extract
                    string path;
                    for (int i = 0; i < head.eafFileNum; i++)
                    {
                        richTextBox1.Text += "> Extracting "  + eafFileInfo[i].eafFileName + "... " + " ("+ eafFileInfo[i].eafFileSize + " bytes)\r\n";
                        richTextBox1.Update();

                        br.BaseStream.Position = eafFileInfo[i].eafFileOffset;

                        path = Path.GetDirectoryName(openFileDialog1.FileName);
                        path = Path.Combine(path, Path.GetFileNameWithoutExtension(openFileDialog1.FileName));
                        path = Path.Combine(path, Path.GetDirectoryName(eafFileInfo[i].eafFileName));

                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        if (eafFileInfo[i].eafFileSize > 0)
                            File.WriteAllBytes(path + '/' + Path.GetFileName(eafFileInfo[i].eafFileName), br.ReadBytes(Convert.ToInt32(eafFileInfo[i].eafFileSize)));
                        else
                            File.WriteAllText(path + '/' + Path.GetFileName(eafFileInfo[i].eafFileName), String.Empty);

                        backgroundWorker1.ReportProgress(((i + 1) * 100) / head.eafFileNum);
                        label1.Text = "Extracted file " + (i + 1) + " of " + head.eafFileNum;
                        label1.Update();
                    }
                    
                    richTextBox1.Text += "> Done!\r\n";
                    backgroundWorker1.ReportProgress(0);
                    label1.Text = "Nothing to process...";
                }
                else
                {
                    richTextBox1.Text += "> Invalid EAF!\r\n";
                }

                br.Close();
                fs.Close();
            }
            vitaCheckBox.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            vitaCheckBox.Enabled = false;
            richTextBox1.Clear();

            FolderSelect.FolderSelectDialog fsd = new FolderSelect.FolderSelectDialog();
            fsd.Title = "Please select a folder to repack.";
            if (fsd.ShowDialog(IntPtr.Zero))
            {
                byte[] eafHead = new byte[0x70];
                eafHead[0x00] = 0x23;
                eafHead[0x01] = 0x45;
                eafHead[0x02] = 0x41;
                eafHead[0x03] = 0x46;

                if (vitaCheckBox.Checked)
                {
                    eafHead[0x06] = 0x01; //Vita
                } else
                {
                    eafHead[0x05] = 0x93; //PC
                }

                eafHead[0x14] = 0x01;
                

                File.WriteAllBytes(Path.GetDirectoryName(fsd.FileName) + "/" + "eafHead.bin", eafHead);
                File.WriteAllText(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin", String.Empty);
                File.WriteAllText(Path.GetDirectoryName(fsd.FileName) + "/" + "eafBigFile.bin", String.Empty);

                richTextBox1.Text += "> Writing table...\r\n";
                richTextBox1.Update();

                int fileCount = 0;
                ProcessTable(fsd.FileName, Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin", fsd.FileName.Length + 1, ref fileCount);

                Int64 headerSize = eafHead.Length + fileCount * 0x120;
                Int64 PaddingToWrite = (byte)(headerSize & 0xFF); //get smallest byte
                PaddingToWrite = 0x100 - PaddingToWrite;
                if (PaddingToWrite != 0)
                {
                    FileStream paddingFS = new FileStream(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin", FileMode.Append, FileAccess.Write);
                    BinaryWriter paddingBW = new BinaryWriter(paddingFS);

                    paddingBW.BaseStream.Position = fileCount * 0x120;
                    //paddingBW.Write(0xDEADBEEF);
                    for (int i = 0; i < PaddingToWrite; i++)
                    {
                        paddingBW.Write((byte)0x0);
                    }

                    paddingBW.Close();
                    paddingFS.Close();
                }                

                richTextBox1.Text += "> Finished writing table...\r\n";
                richTextBox1.Text += "> Packing file(s)...\r\n";
                richTextBox1.Update();

                FileStream fs = new FileStream(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin", FileMode.Open);
                BinaryWriter bw = new BinaryWriter(fs);
                BinaryReader br = new BinaryReader(fs);
                bw.BaseStream.Position = 0x110;

                int bigFileCount = 0;
                ProcessBigPack(fsd.FileName, Path.GetDirectoryName(fsd.FileName) + "/" + "eafBigFile.bin", ref bw, ref fileCount, ref bigFileCount);

                richTextBox1.Text += "> File(s) packed...\r\n";
                richTextBox1.Update();

                //bw.BaseStream.Position = 0;
                //byte[] file0 = br.ReadBytes(0x10);

                //bw.BaseStream.Position = 0;
                //byte[] file0_Infos = new byte[0x10];
                //bw.Write(file0_Infos, 0, 0x10);

                br.Close();
                bw.Close();
                fs.Close();
                
                backgroundWorker1.ReportProgress(0);
                label1.Text = "Building EAF...";
                label1.Update();

                //using (FileStream stream = new FileStream(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin", FileMode.Append))
                //{
                //    stream.Write(file0, 0, file0.Length);
                //}

                File.WriteAllText(Path.GetDirectoryName(fsd.FileName) + "/" + Path.GetFileName(fsd.FileName) + "_new.eaf", String.Empty);

                byte[] head = File.ReadAllBytes(Path.GetDirectoryName(fsd.FileName) + "/" + "eafHead.bin");
                byte[] table = File.ReadAllBytes(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin");

                const int chunksize = 1024 * 1024 * 1024;
                byte[] buffer = new byte[chunksize];

                string bfilePath = Path.GetDirectoryName(fsd.FileName) + "/" + "eafBigFile.bin";

                long eafSize = 0;

                using (FileStream tempfs = new FileStream(bfilePath, FileMode.Open, FileAccess.Read))
                using (FileStream stream = new FileStream(Path.GetDirectoryName(fsd.FileName) + "/" + Path.GetFileName(fsd.FileName) + "_new.eaf", FileMode.Append))
                {
                    stream.Write(head, 0, head.Length);
                    stream.Write(table, 0, table.Length);

                    int bytesRead;
                    while ((bytesRead = tempfs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }

                    //stream.Write(bfile, 0, bfile.Length);
                    eafSize = stream.Length;
                }

                FileStream fs2 = new FileStream(Path.GetDirectoryName(fsd.FileName) + "/" + Path.GetFileName(fsd.FileName) + "_new.eaf", FileMode.Open);
                BinaryWriter bw2 = new BinaryWriter(fs2);

                bw2.BaseStream.Position = 0x8;
                bw2.Write(BitConverter.GetBytes(eafSize), 0, 8);
                bw2.Write(BitConverter.GetBytes(fileCount), 0, 4);

                backgroundWorker1.ReportProgress(100);
                label1.Text = "EAF Built...";
                label1.Update();

                bw2.Close();
                fs2.Close();

                if (File.Exists(Path.GetDirectoryName(fsd.FileName) + "/" + "eafHead.bin"))
                    File.Delete(Path.GetDirectoryName(fsd.FileName) + "/" + "eafHead.bin");

                if (File.Exists(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin"))
                    File.Delete(Path.GetDirectoryName(fsd.FileName) + "/" + "eafTable.bin");

                if (File.Exists(Path.GetDirectoryName(fsd.FileName) + "/" + "eafBigFile.bin"))
                    File.Delete(Path.GetDirectoryName(fsd.FileName) + "/" + "eafBigFile.bin");

                richTextBox1.Text += "> Done!\r\n";
                backgroundWorker1.ReportProgress(0);
                label1.Text = "Nothing to process...";
            }

            vitaCheckBox.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
        }

        public void ProcessTable(string sourceDirectory, string baseDirectory, int sourceDirLength, ref int fileCount)
        {
            string[] fileEntries = Directory.GetFiles(sourceDirectory);
            foreach (string fileName in fileEntries)
            {
                //totalFileSize += fileName.Length;
                ProcessTableFile(fileName, baseDirectory, sourceDirLength);
                fileCount++;
            }

            string[] subdirectoryEntries = Directory.GetDirectories(sourceDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessTable(subdirectory, baseDirectory, sourceDirLength, ref fileCount);
        }

        public void ProcessTableFile(string path, string baseDirectory, int sourceDirLength)
        {
            backgroundWorker1.ReportProgress(0);

            FileInfo info = new FileInfo(path);
            byte[] fileInfo = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }; //padding
            byte[] fileName = Encoding.ASCII.GetBytes(path.Substring(sourceDirLength));
            for(int i = 0; i < fileName.Length; i++)
            {
                if (fileName[i] == 0x5C)
                    fileName[i] = 0x2F;
            }

            fileInfo = fileInfo.Concat(fileName).ToArray(); //name
            fileInfo = fileInfo.Concat(new byte[0x100 - fileName.Length]).ToArray(); //padding until info
            fileInfo = fileInfo.Concat(new byte[0x8]).ToArray(); //data
            //fileInfo = fileInfo.Concat(new byte[0x10]).ToArray(); //padding
            //fileInfo = fileInfo.Concat(BitConverter.GetBytes(totalFileSize)).ToArray(); 
            fileInfo = fileInfo.Concat(BitConverter.GetBytes(info.Length)).ToArray(); //info of curr file

            using (FileStream stream = new FileStream(baseDirectory, FileMode.Append))
            {
                stream.Write(fileInfo, 0, fileInfo.Length);
            }

            backgroundWorker1.ReportProgress(100);

            label1.Text = "Created entry in file table for " + path.Substring(sourceDirLength);
            label1.Update();
        }

        public void ProcessBigPack(string sourceDirectory, string bigFile, ref BinaryWriter bw, ref int fileCount, ref int bigFileCount)
        {
            string[] fileEntries = Directory.GetFiles(sourceDirectory);
            foreach (string fileName in fileEntries)
            {
                ProcessBigPackFiles(fileName, bigFile, ref bw, ref fileCount, ref bigFileCount);
                bw.BaseStream.Position += 0x118; // skip rest of current file's info in the table
                bigFileCount++;
            }

            string[] subdirectoryEntries = Directory.GetDirectories(sourceDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessBigPack(subdirectory, bigFile, ref bw, ref fileCount, ref bigFileCount);
        }

        public void ProcessBigPackFiles(string path, string bigFile, ref BinaryWriter bw, ref int fileCount, ref int bigFileCount)
        {
            backgroundWorker1.ReportProgress(0);

            byte[] baseFile = File.ReadAllBytes(path);

            long prevBigFileLen = 0;

            //using (FileStream prevstream = new FileStream(bigFile, FileMode.Open, FileAccess.Write))
            //{
            //    prevBigFileLen = prevstream.Length;
            //    prevstream.Seek(-0x10, SeekOrigin.End);
            //    prevstream.Write(baseFile, 0, baseFile.Length);
            //}

            using (FileStream stream = new FileStream(bigFile, FileMode.Append))
            {
                prevBigFileLen = stream.Length;
                stream.Write(baseFile, 0, baseFile.Length); //writes to the end of file stuff

                int PaddingToWrite = baseFile.Length;
                PaddingToWrite = PaddingToWrite & 0xFF;
                PaddingToWrite = 0x100 - PaddingToWrite;
                if (fileCount-1 > bigFileCount)
                {
                    for (int j = 0; j < PaddingToWrite; j++)
                    {
                        stream.WriteByte(0x0); //padding to align file
                    }
                }

                //stream.Write(baseFile, 8, BitConverter.ToInt64(stream.Length).ToArray());
                //stream.Write(baseFile, 8, 0);
            }

            //bw.Write(bw.BaseStream.Length);
            //bw.BaseStream.Position += 0x110; //go to offset
            bw.Write(0x60 + bw.BaseStream.Length + 0x10 + prevBigFileLen); // header + table + additional table size + where ever the big file left off at
            //bw.BaseStream.Position = 0x118; //go to file size

            backgroundWorker1.ReportProgress(100);
            label1.Text = "Packed " + Path.GetFileName(path);
            label1.Update();
        }

        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
        }

        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = Math.Min(e.ProgressPercentage, 100);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            vitaCheckBox.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;

            About box = new About();
            box.ShowDialog();

            vitaCheckBox.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
