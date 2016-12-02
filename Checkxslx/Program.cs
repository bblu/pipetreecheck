using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.IO;
using System.Data;

namespace PipeAuto
{

    class Program
    {
        static void Main(string[] args)
        {
            System.Console.Write("检查前请关闭目录下所有打开的Excel文件，否则程序会报错！\n");
            string dir = System.Environment.CurrentDirectory;
            FileStream logFile = File.OpenWrite("./检查结果.txt");
            StreamWriter log = new StreamWriter(logFile);
            PipeNet net = new PipeNet(ref log);
            log.WriteLine("####################################################");
            log.WriteLine("#版本: V0.4 功能：检查字段|坐标|连接拓扑存在性问题 #");
            log.WriteLine("#功能: 增加对起止点连接点字段和格式进行规范化检查！#");
            log.WriteLine("####################################################");
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            FileInfo[] excels = dirInfo.GetFiles("*.xls");
            List<string> excelFiles = new List<string>(excels.Count());
            Util util = new Util();
            int counter = 1;
            int c = 0;
            foreach (FileInfo fi in excels)
            {
                excelFiles.Add(fi.FullName);
                if (fi.FullName.EndsWith(".xls"))
                {
                    System.Console.Write(string.Format("*erro: [{0}] 文件格式需要更新到.xlsx\n", Path.GetFileName(fi.FullName)));
                    log.WriteLine(string.Format("*erro: [{0}] 文件格式需要更新到.xlsx", Path.GetFileName(fi.FullName)));
                }
            }
            excelFiles.Sort();
            foreach (string FullName in excelFiles)
            {
                log.WriteLine("====================================================");
                string fname = Path.GetFileName(FullName);
                if (fname.StartsWith("~$"))
                    continue;
                System.Console.Write("#info: 开始检查:[{0}]坐标...\n", fname);
                log.WriteLine("#info: 检查[" + fname + "] 规范化问题");
                log.WriteLine("----------------------------------------------------");
                
                int type = util.checkFileName(fname);
                if (type > 0)
                {
                    string connStr = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + FullName + ";" + ";Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"";
                    OleDbConnection conn = new OleDbConnection(connStr);
                    conn.Open();
                    System.Data.DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                    string sheetName = "Sheet";    
                    string sheetTrim = "Sheet";    
                    foreach (DataRow row in dtSheet.Rows)
                    {
                        sheetName = (string)row["TABLE_NAME"];
                        sheetTrim = sheetName.Trim('\'');
                        if (!sheetTrim.StartsWith("Sheet") && sheetTrim.EndsWith("$"))
                            break;//_xlnm#_FilterDatabase
                    }                    
                    System.Data.DataTable sheet = null;
                    if (util.checkFileName(sheetTrim) == 0)
                    {
                        log.WriteLine(string.Format("*erro: sheet名称[{0}]和文件名不一致!", sheetTrim.TrimEnd('$')));
                    }                    
                    if (type > 0)
                    {
                        c = 0;
                        OleDbDataAdapter da = new OleDbDataAdapter(String.Format("select * from [{0}]", sheetName), conn);
                        sheet = new System.Data.DataTable();
                        da.Fill(sheet);
                        counter = 1;
                        int colNums = util.columNum(type);
                        bool hasCoord = false;
                        bool hadBEpoint = false;
                        foreach (DataColumn col in sheet.Columns)
                        {
                            string str = col.ColumnName.Trim();
                            if (str.EndsWith("）") || str.IndexOf('（') > 1)
                            {
                                log.WriteLine(string.Format("+warn: 列名[{0}]包含中文括号", str));
                                str = str.Replace('（', '(');
                                str = str.Replace('）', ')');
                                c++;
                            }
                            else if (util.checkColums(type, "," + str + ","))
                            {
                                log.WriteLine(string.Format("*erro: 列名[{0}]与元数据定义不符", str));
                                c++;
                            }
                            if (!hasCoord)
                            {
                                hasCoord = str == "坐标";
                            }
                            if (type == 2 && !hadBEpoint)
                            {
                                hadBEpoint = str == "起止点";
                            }
                            if (counter == colNums)
                                break;
                            counter++;
                        }

                        log.WriteLine("----------------------------------------------------");

                        //log.WriteLine(string.Format("#---{0}问题列表---\n", sheetName));
                        counter = 1;
                        c = 0;
                        log.WriteLine("#info: -------检查[坐标-和-起止点]问题 ------------#");
                        log.WriteLine("----------------------------------------------------");
                        if (type == 2)
                        {
                            if (hadBEpoint)
                            {
                                foreach (DataRow row in sheet.Rows)
                                {
                                    counter++;
                                    string[] names = row["名称"].ToString().Trim().Split('-');
                                    string[] links = row["起止点"].ToString().Trim().Split(',');
                                    if (names.Count() != 2 || links.Count() != 2)
                                    {
                                        log.WriteLine(string.Format("*erro: 第[{0:000}]行-起止点或名称->格式不正确", counter));
                                        c++;
                                    }
                                    else
                                    {
                                        if (links[0].EndsWith("井") )
                                        {
                                            c++;
                                            log.WriteLine(string.Format("*erro: 第[{0:000}]行-连接起点->[{1}]不能作为节点设备", counter, links[0]));
                                        }
                                        if (links[1].EndsWith("井"))
                                        {
                                            c++;
                                            log.WriteLine(string.Format("*erro: 第[{0:000}]行-连接终点->[{1}]不能作为节点设备", counter, links[1]));
                                        }
                                        if (names[0] != links[0])
                                        {
                                            c++;
                                            log.WriteLine(string.Format("*erro: 第[{0:000}]行-连接起点->[{1}]和名称[{2}]不相符", counter, links[0], names[0]));
                                        }
                                        if (names[1] != links[1])
                                        {
                                            c++;
                                            log.WriteLine(string.Format("*erro: 第[{0:000}]行-连接终点->[{1}]和名称[{2}]不相符", counter, links[1], names[1]));
                                        }
                                        if (links.Count() == 2)
                                            util.appendLink(counter, links[0], links[1]);
                                    }
                                    int oldc = c;
                                    float x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                                    string str = row["起点坐标"].ToString().Trim();
                                    if (!util.pointValid(counter, str, ref x1, ref y1))
                                    {
                                        c++;
                                        log.WriteLine(string.Format("*erro: 第[{0:000}]行-问题坐标->{1}", counter, str));
                                    }
                                    str = row["终点坐标"].ToString().Trim();
                                    if (!util.pointValid(counter, str, ref x2, ref y2))
                                    {
                                        c++;
                                        log.WriteLine(string.Format("*erro: 第[{0:000}]行-问题坐标->{1}", counter, str));
                                    }
                                    float len = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
                                    if (oldc == c && len > 0.1)
                                    {
                                        int w = 0;
                                        int.TryParse(row["管径DN"].ToString().Trim(), out w);
                                        int gshs = row["类别"].ToString().Trim().EndsWith("供水") ? 0 : 1;
                                        net.appendEdge(counter, gshs, x1, y1, x2, y2, w);
                                        len = (float)Math.Sqrt(len);
                                        string ml = row["长度(m)"].ToString().Trim();
                                        float rl;
                                        if (!float.TryParse(ml, out rl))
                                        {
                                            log.WriteLine(string.Format("*error: 第[{0:000}]行-长度字段->[值={1:0.0}]", ml));
                                        }
                                        float p = Math.Abs(rl - len) / rl;
                                        if (p > 0.5 && len + rl > 5)
                                        {
                                            c++;
                                            if (p > 9.999)
                                            {
                                                p = 9.999f;
                                                log.WriteLine(string.Format("*erro: 第[{0:000}]行-长度异常->[错误{1:000.0}%]-[{2:0.0} <> {3}]", counter, p * 100, len, ml));

                                            }
                                            else
                                            {
                                                log.WriteLine(string.Format("+warn: 第[{0:000}]行-长度警告->[误差{1:000.0}%]-[{2:0.0} != {3}]", counter, p * 100, len, ml));
                                            }
                                        }
                                    }
                                    str = row["所属换热站"].ToString().Trim();
                                    if (str != util.station)
                                    {
                                        c++;
                                        log.WriteLine(string.Format("*erro: 第[{0:000}]行-所属换热站->{1}有误", counter, str));
                                    }
                                }
                            }
                            else
                            {
                                log.WriteLine("*erro: 管线表列名与元数据定义不符：缺少--[起止点]->字段");
                            }
                            net.parserNet();
                        }
                        else if (type == 1 || type == 3)
                        {
                            if (!hasCoord)
                            {
                                continue;
                            }
                            foreach (DataRow row in sheet.Rows)
                            {
                                counter++;
                                string str = row["坐标"].ToString().Trim();
                                if (util.pointUnvalid(counter, str)) {
                                    c++;
                                    log.WriteLine(string.Format("*erro: 第[{0:000}]行-问题坐标->{1}", counter, str));
                                }
                                str = row["所属换热站"].ToString().Trim();
                                if (str != util.station)
                                {
                                    c++;
                                    log.WriteLine(string.Format("*erro: 第[{0:000}]行-所属换热站->[{1}]和文件名不一致", counter, str));
                                }
                                str = row["名称"].ToString().Trim();
                                util.appendName(type, str);
                            }
                        }
                        else if (type == 4)
                        {
                            log.WriteLine(string.Format("*info: 该设备表无坐标"));
                        }
                        if (c > 0)
                        {
                            log.WriteLine("----------------------------------------------------");
                            log.WriteLine(string.Format("#info: 检查[坐标-和-连接点] 总计有[{0:000}]个问题 -----#", c));
                            log.WriteLine("----------------------------------------------------");
                        }
                    }
                    conn.Close();
                   

                }
                else
                {
                    log.WriteLine(string.Format("*erro: 文件[{0}]名称不合法", fname));
                }
            }
            log.Flush();
            c = 0;
            //check logic
            log.WriteLine("####################################################");
            log.WriteLine("###info: 检查[管线-起止点] 连接关系节点是否存在 ####");
            log.WriteLine("----------------------------------------------------");
            foreach (KeyValuePair<string, string> links in util.linkNames)
            {

                if (!util.checkName(links.Key))
                {
                    c++;
                    log.WriteLine(string.Format("*erro: 第[{0:000}]行-起点缺失->[{1}]", counter + 1, links.Key));
                }
                string[] values = links.Value.Split(',');
                foreach (string val in values)
                {
                    counter++;
                    string[] v = val.Split(':');
                    if (!util.checkName(v[1]))
                    {
                        c++;
                        log.WriteLine(string.Format("*erro: 第[{0:000}]行-终点缺失->[{1}]", v[0], v[1]));
                    }
                }
            }
            log.WriteLine("----------------------------------------------------");
            log.WriteLine(string.Format("#info: 检查[管线-起止点]结果：总计[{0}]个连接无对应节点#", c));

            log.WriteLine("====================================================");
            log.WriteLine("########## info: 检查[节点设备] 是否被连接 #########");
            log.WriteLine("----------------------------------------------------");
            counter = 1;
            c = 0;
            //util.nodeNames.Sort();
            foreach (string name in util.nodeNames)
            {
                counter++;
                if (!util.nodeLinked(name))
                {
                    c++;
                    log.WriteLine(string.Format("*erro: 第[{0:000}]行-节点悬空->[{1}]", counter, name));
                }
            }
            log.WriteLine("----------------------------------------------------");
            log.WriteLine(string.Format("#info: 检查[节点]表结果：总计[{0}]个节点未被连接#", c));
            log.WriteLine("====================================================");

            log.Close();
            logFile.Close();
            System.Console.Write("#info: 检查完毕5s后退出，查看检查结果...\n");
            System.Threading.Thread.Sleep(5000);
            //int chr = System.Console.Read();
        }
    }
       
    
}