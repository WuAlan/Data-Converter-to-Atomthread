using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace DataConverter
{
    public partial class Converter : Form
    {
        private SerialPort comm = new SerialPort();
        private StringBuilder builder = new StringBuilder();
        private long received_count = 0;
        private List<List<String>> MetaData = new List<List<String>>();
        private List<String> FormalData = new List<String>();
        Dictionary<string,int> Tasks=new Dictionary<string,int>();
        private int TaskNumbers=0;
        private int MutexNumbers = 0;
        public Converter()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            ComboName.Items.AddRange(ports);
            ComboName.SelectedIndex = 0;
            ComboBaud.SelectedIndex = 5;

        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {

            comm.NewLine = "/n";
            comm.RtsEnable = false;
            comm.DataReceived += comm_DataReceived;
            if (!comm.IsOpen)
            {
                comm.PortName = ComboName.Text;
                comm.BaudRate = Convert.ToInt32(ComboBaud.Text);
                try
                {
                    comm.Open();
                    TextGet.AppendText("Serial Opened\n");
                }
                catch (Exception)
                {
                    TextGet.AppendText("Open Failed\n");
                }
            }
            else
            {
                TextGet.AppendText("Serial Open Failed\n");
            }

        }

        void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int n = comm.BytesToRead;
            byte[] buf = new byte[n];
            received_count += n;
            comm.Read(buf, 0, n);
            builder.Clear();
            this.Invoke((EventHandler)(delegate
            {
                builder.Append(Encoding.ASCII.GetString(buf));
                this.TextGet.AppendText(builder.ToString());
            }));

        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            if (comm.IsOpen)
            {
                comm.Close();
                TextGet.AppendText("Serial Closed\n");
            }
        }

        private void button_Click(object sender, EventArgs e)
        {
            Tasks.Clear();
            TaskNumbers = 0;
            MetaData.Clear();
            FormalData.Clear();
            String[] RemoveInformations ={"Serial Opened",
                                          "Open Failed",
                                          "Serial Closed",
                                          "Serial Open Failed",
                                          "Some Exception Ignored in EventConvert.",
                                         };

            String TotalString = TextGet.Text;
            String[] SplitedString = TotalString.Split('\n');
            List<String> SplitedList = SplitedString.ToList<String>();
            foreach (String s in RemoveInformations)
            {
                SplitedList.Remove(s);
            }


            ConvertFormat(SplitedList);
            WriteToFile();
            


        }


        private void ConvertFormat(List<String> Strings)
        {
            //split the string by space
            for (int i = 0; i < Strings.Count; i++)
            {
                MetaData.Insert(i, Strings[i].Split(' ').ToList<String>());
            }
            //find the new task event and put at the head in the result buffer
            for (int i = 0; i < MetaData.Count; i++)
            {
                string s= NewTaskConvertByPosition(i);
                string s2 = NewMutexCovertByPostition(i);
                if(s.Contains("newTask"))
                {
                    FormalData.Add(s);
                }
                if (s2.Contains("newMutex"))
                {
                    FormalData.Add(s2);
                }
            }
            int TaskCount = TaskNumbers;
            int[] CurJobID=new int[TaskCount];
            MetaData.RemoveAll(NotEvent);
            MetaData.RemoveAt(MetaData.Count - 1);
            MetaData.Sort(new StringListComparer());
            for (int i = 0; i < MetaData.Count; i++)
            {
                string s = EventConvert(i, CurJobID);
                if (s.Contains("plot"))
                {
                    FormalData.Add(s);
                }
            }
        }

        private string NewMutexCovertByPostition(int position)
        {
            StringBuilder Result = new StringBuilder();
            //New Mutex
            for (int i = 0; i < MetaData[position].Count; i++)
            {
                if (MetaData[position][i] == "nM")
                {
                    Result.Append("newMutex");
                    string MutexName=GetNameByAddress(MetaData[position][i + 1],1);
                    //Mutex Id

                    Result.Append(" " + MutexName);
                    if (MutexNumbers % 2 == 0)
                    {
                        Result.Append(" -name "+MutexName+" -color red -pattern lines_up ");
                    }
                    else
                    {
                        Result.Append(" -name "+MutexName+" -color blue -pattern lines_down ");
                    }
                    Result.Append("\n");
                    MutexNumbers++;
                }
            }
            return Result.ToString();
        }
        /// <summary>
        /// Get name by address
        /// </summary>
        /// <param name="Address"> address</param>
        /// <param name="type">0: Task, 1: Mutex</param>
        /// <returns></returns>
        private string GetNameByAddress(string Address, int type)
        {
            string Name = "";
            int start=0,end=0;
            StreamReader read = File.OpenText("F:/Avr/AtomTrace/Test/atomclock/default/atomclock.map");
            string content = read.ReadToEnd();
            read.Close();
            string FormatAddress="0x";
            if (type == 0)
            {
                for (int i = 0; i < 8 - Address.Length; i++)
                {
                    FormatAddress += "0";

                }
                FormatAddress += Address;
                start = content.LastIndexOf(FormatAddress);
            }
            else
            {
                start = content.LastIndexOf(Address);
            }

            char[] contents = content.ToCharArray();
            string substring = "";
            if (start != -1)
            {
                for (int i = start; i < contents.Length; i++)
                {
                    if (contents[i] == '\r')
                    {
                        end = i;
                        break;
                    }
                }
                if (end != 0)
                {
                    substring = content.Substring(start, end - start);
                    if (substring != "")
                    {
                        int NameStart = substring.LastIndexOf(" ");
                        substring = substring.Substring(NameStart);
                        Name = substring;
                    }
                }
            }

            //          content.
            if (Name != "")
            {
                return Name;
            }
            else
            {
                return "Idle";
            }
        }

        private static bool Empty(string L)
        {
            if (L== "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void WriteToFile()
        {
            saveFileDialog1.FileName = "TraceData";
            saveFileDialog1.Filter = "Text File(*.txt)|*.txt";
            saveFileDialog1.AddExtension = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileStream File = new FileStream(saveFileDialog1.FileName,FileMode.Create);
                foreach(string s in FormalData)
                {
                    Byte[] data = new UTF8Encoding().GetBytes(s);
                    File.Write(data, 0, data.Length);
                    
                }
                File.Flush();
                File.Close();
            }
        }

        private String AdjustTime(String s)
        {
            UInt32 OriginalTime = Convert.ToUInt32(s);
            double NewTime = OriginalTime/1000.0;
            return NewTime.ToString();
        }

        private String EventConvert(int i, int[] CurJobID)
        {

            StringBuilder Result = new StringBuilder(); ;
            //clear the event buffer
            Result.Clear();
            try
            {
                switch (MetaData[i][2])
                {
                    case "JA":
                        {
                            string TaskName =GetNameByAddress(MetaData[i][3],0);
                            int CurJob = Tasks[MetaData[i][3]];
                            CurJobID[CurJob]++;
                            string SJobID = MetaData[i][3] + "." + CurJobID[CurJob].ToString();
                            Result.Append("plot");
                            Result.Append(" " + AdjustTime(MetaData[i][1]));
                            Result.Append(" jobArrived " + SJobID);
                            Result.Append(" " + TaskName);
                            Result.Append("\n");


                        } break;
                    case "JR":
                        {
                            string TaskAddress = MetaData[i][3];
                            int CurJob = Tasks[TaskAddress];
                            string SJobID = MetaData[i][3] + "." + CurJobID[CurJob].ToString();
                            Result.Append("plot");
                            Result.Append(" " + AdjustTime(MetaData[i][1]));
                            Result.Append(" jobResumed " + SJobID);
                            Result.Append("\n");
                        } break;
                    case "JC":
                        {
                            string TaskAddress = MetaData[i][3];
                            int CurJob = Tasks[TaskAddress];
                            string SJobID = MetaData[i][3] + "." + CurJobID[CurJob].ToString();
                            Result.Append("plot");
                            Result.Append(" " + AdjustTime(MetaData[i][1]));
                            Result.Append(" jobCompleted " + SJobID);
                            Result.Append("\n");
                        } break;
                    case "JP":
                        {
                            string TaskAddress = MetaData[i][3];
                            int CurJob = Tasks[TaskAddress];
                            string SJobID = MetaData[i][3] + "." + CurJobID[CurJob].ToString();
                            Result.Append("plot");
                            Result.Append(" " + AdjustTime(MetaData[i][1]));
                            Result.Append(" jobPreempted " + SJobID);
                            Result.Append(" -target ");
                            string TaskAddress2 = MetaData[i][5];
                            int CurJob2 = Tasks[TaskAddress2];
                            string SJobID2 = MetaData[i][5] + "." + CurJobID[CurJob2].ToString();
                            Result.Append(SJobID2);
                            Result.Append("\n");
                        } break;
                    case "AM":
                        {
                            string MutexName =GetNameByAddress(MetaData[i][3],1);
                            string TaskAddress = MetaData[i][4];
                            int CurJob = Tasks[TaskAddress];
                            string SJobID = MetaData[i][4] + "." + CurJobID[CurJob].ToString();
                            Result.Append("plot");
                            Result.Append(" " + AdjustTime(MetaData[i][1]));
                            Result.Append(" jobAcquiredMutex " + SJobID);
                            Result.Append(" "+MutexName);
                            Result.Append("\n");
                        }break;
                    case "RM":
                        {
                            string MutexName = GetNameByAddress(MetaData[i][3], 1);
                            string TaskAddress = MetaData[i][4];
                            int CurJob = Tasks[TaskAddress];
                            string SJobID = MetaData[i][4] + "." + CurJobID[CurJob].ToString();
                            Result.Append("plot");
                            Result.Append(" " + AdjustTime(MetaData[i][1]));
                            Result.Append(" jobReleasedMutex " + SJobID);
                            Result.Append(" " + MutexName);
                            Result.Append("\n");
                        } break;
                    default:
                        {
                        } break;

                }
            }
            catch (Exception)
            {
                TextGet.AppendText("Some Exception Ignored in EventConvert.\n");
            }

            return Result.ToString();
        }

    

        private static bool NotEvent(List<String> L)
        {
            if (L[0] !="p")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private String NewTaskConvertByPosition(int position)
        {
            StringBuilder Result = new StringBuilder();
            for (int i = 0; i < MetaData[position].Count; i++)
            {


                //New Task
                if (MetaData[position][i] == "nT")
                {
                    //KeyWords for Create Task
                    Result.Append("newTask");
                    //Task Id
                    string TaskName=GetNameByAddress(MetaData[position][i + 1],0);
                    Result.Append(" " + TaskName);
                    Tasks.Add(MetaData[position][i + 1],TaskNumbers);
                    TaskNumbers++;
                }
                else if (MetaData[position][i] == "-p")
                {
                    Result.Append(" -priority");
                    Result.Append(" " + MetaData[position][i + 1]);
                }
                else if (MetaData[position][i] == "-n")
                {
                    Result.Append(" -name");
                    Result.Append(" " + MetaData[position][i + 1]);
                }






                //New Line
                if (i == MetaData[position].Count-1)
                {
                    Result.Append("\r\n");
                }

            }

            return Result.ToString();
        }



    }
}




