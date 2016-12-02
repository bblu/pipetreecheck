using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace PipeAuto
{
    public partial class FormMain : Form
    {
        int intColCount = 0;
        string strpath="";
        DataTable mydt = new DataTable("pipe");
        Util util = new Util();
        PipeNet net = null;
        string[] color={"",""};
        StreamWriter log;
        public FormMain()
        {
            InitializeComponent();
            FileStream logFile = File.OpenWrite("./zw_cvs.txt");
            log = new StreamWriter(logFile);
            net = new PipeNet(ref log);              
        }

        private void toolStripOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            DialogResult dr = dlg.ShowDialog(this);
            dlg.RestoreDirectory = true;
            dlg.Filter = "pipe csv files(*.csv)|*.csv";
            if (dr == DialogResult.OK)
            {
                if (strpath.Length > 5)
                {
                    net.clear();
                    txtBox.Clear();
                    mydt.Rows.Clear();
                    mydt.Columns.Clear();
                }
                //checkedBoxFiles.Items.Clear();
                strpath = dlg.FileName;
                readCsv(strpath);
                loadEdges();
                log.Flush();                
            }
        }
        private void loadEdges()
        {
            //mydt.Columns
            int counter=0;
            int c=0;
            color[0] = mydt.Rows[0]["颜色"].ToString();
            color[1] = "";
            foreach (DataRow row in mydt.Rows)
            {
                counter++;
                int oldc = c;
                float x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                string str = row["起点坐标"].ToString().Trim();
                if (!util.pointValid(counter, str, ref x1, ref y1))
                {
                    c++;
                    txtBox.AppendText(string.Format("*erro: 第[{0:0000}]行-问题坐标->{1}\n", counter, str));
                }
                str = row["终点坐标"].ToString().Trim();
                if (!util.pointValid(counter, str, ref x2, ref y2))
                {
                    c++;
                    txtBox.AppendText(string.Format("*erro: 第[{0:0000}]行-问题坐标->{1}\n", counter, str));
                }
                int w = 0;
                int.TryParse(row["管径"].ToString().Trim(), out w);
                int gshs = (row["颜色"].ToString() == color[0]) ? 0 : 1;                
                net.appendEdge(counter, gshs, x1, y1, x2, y2, w);

                if (gshs == 1 && color[1].Length == 0)
                    color[1] = row["颜色"].ToString();
            }
            net.parserNet();
            foreach(Vertex v in net.getErrSnap())
            {
                txtBox.AppendText(string.Format("*erro: 第[{0:0000}]行-未捕捉到点->[53{1},464{2}]\n",v.id/2,v.pos.x,v.pos.y));
            }
            txtBox.AppendText("======================================================\n");
            txtBox.AppendText("+info: 供水回水出站管线未接上级网属于正常现象无需修改\n");             
            for (int i = 0; i < 2; i++)
            {
                txtBox.AppendText(string.Format("颜色[{0}]管线-共[{1}]条未接入:\n", color[i],net.getsubTree(i).Count));
                foreach (Vertex v in net.getsubTree(i))
                {
                    txtBox.AppendText(string.Format("*erro: 第[{0:0000}]行-[53{1},464{2}]->未接入网\n", v.id / 2, v.pos.x, v.pos.y));
                }
                txtBox.AppendText("------------------------------------------------------\n");
            }
        }
        private void readCsv(string strpath)
        {
            string strline;
            string[] aryline;
            DataColumn mydc;
            DataRow mydr;

            System.IO.StreamReader mysr = new System.IO.StreamReader(strpath, Encoding.Default);
            strline = mysr.ReadLine();
            aryline = strline.Split(new char[] { ',' });
            intColCount = aryline.Length;
            for (int i = 0; i < aryline.Length; i++)
            {
                mydc = new DataColumn(aryline[i]);                
                mydt.Columns.Add(mydc);
            }
            intColCount = 9;
            while ((strline = mysr.ReadLine()) != null)
            {
                aryline = strline.Split(new char[] { ',' });
                mydr = mydt.NewRow();
                for (int i = 0; i < intColCount; i++)
                {
                    if (i == 2)
                    {
                        mydr[i] = aryline[i].TrimStart('\"') + "," + aryline[i+1].TrimEnd('\"');
                    }
                    else if (i == 3)
                    {
                        mydr[i] = aryline[++i].TrimStart('\"') + "," + aryline[++i].TrimEnd('\"');
                    }
                    else if (i > 4)
                    {
                        mydr[i-2] = aryline[i];
                    }
                    else
                    {
                        mydr[i] = aryline[i];
                    }
                }
                mydt.Rows.Add(mydr);
            }
            mysr.Close();
            dataGridView1.DataSource = mydt;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (strpath.Length > 5)
            {
                net.clear();
                txtBox.Clear();
                mydt.Rows.Clear();
                mydt.Columns.Clear();
                readCsv(strpath);
                loadEdges();
            }
        }
        private void dataGridView1_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
        {
            e.Row.HeaderCell.Value = string.Format("{0}", e.Row.Index + 1);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            log.Close();
            File.Delete("./zw_cvs.txt");
        }


    }
}
