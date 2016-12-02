using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PipeAuto
{
    class Util
    {
        public string station;
        string colsWell = ",编号,所属换热站,所属小区,名称,坐标,相对位置,类型,规格,井盖型号,范围,设计单位,施工单位,竣工日期,备注,";
        string colsPipe = ",编号,所属换热站,所属小区,类别,等级,名称,起止点,起点坐标,终点坐标,相对位置,管径DN,壁厚(mm),长度(m),敷设方式,敷设深度(m),竣工日期,保温材料,保温厚度(mm),外护壳类型,外护壳厚度(mm),生产厂家,设计单位,施工单位,维修日期,维修原因,备注,";
        string colsNode = ",编号,所属换热站,所属小区,所属阀门井,类别,名称,坐标,连接点,相对位置,管径DN,壁厚(mm),流量(t),开启程度(T),生产厂家,备注,";
        string colsOute = ",编号,所属换热站,所属阀门井,安装位置,种类,数量,规格,型号,流量(t),出厂日期,生产编号,开启程度,生产厂家,更换时间,更换原因,备注,";
        string[] columsTemp = { "", "", "", "", "" };
        int[] columNums = { 0, 14, 26, 20, 16 };
        public List<string> wellNames;
        public List<string> nodeNames;
        List<string> valveNames;
        public Dictionary<string, string> linkNames;
        HashSet<string> linkWells;
        HashSet<string> linkNodes;

        Dictionary<string, int> deviceCounter;
        public Util()
        {
            columsTemp[0] = "";
            columsTemp[1] = colsWell;
            columsTemp[2] = colsPipe;
            columsTemp[3] = colsNode;
            columsTemp[4] = colsOute;
            deviceCounter = new Dictionary<string, int>();
            linkNames = new Dictionary<string, string>();
            wellNames = new List<string>();
            nodeNames = new List<string>();
            valveNames = new List<string>();
            linkWells = new HashSet<string>();
            linkNodes = new HashSet<string>();
        }
        public bool preparedDevice(string station)
        {
            return deviceCounter[station] == 8;
        }
        public int checkFileName(string fName)
        {
            string[] spt = fName.Split('-');
            if (spt.Count() == 3)
            {
                if ((spt[0].EndsWith("所") && spt[1].EndsWith("站")))
                {
                    station = spt[1];
                    if (!deviceCounter.ContainsKey(station))
                        deviceCounter.Add(station, 0);
                    deviceCounter[station]++;
                    //System.Console.WriteLine("{0}:{1}", station, deviceCounter[station]);
                    if (spt[2].StartsWith("阀门井"))
                        return 1;
                    if (spt[2].StartsWith("管线"))
                        return 2;
                    if (spt[2].StartsWith("节点"))
                        return 3;
                    if (spt[2].StartsWith("站外设备"))
                        return 4;
                }
                else if (spt[0].EndsWith("市") && spt[1].EndsWith("网"))
                {
                    station = spt[0];
                    if (!deviceCounter.ContainsKey(station))
                        deviceCounter.Add(station, 0);
                    deviceCounter[station]++;
                    //System.Console.WriteLine("{0}:{1}", station, deviceCounter[station]);
                    if (spt[2].StartsWith("阀门井"))
                        return 1;
                    if (spt[2].StartsWith("管线"))
                        return 2;
                    if (spt[2].StartsWith("节点"))
                        return 3;
                    if (spt[2].StartsWith("站外设备"))
                        return 4;
                }
            }
            station = "";
            return 0;
        }
        public int columNum(int type)
        {
            return columNums[type];
        }
        public bool checkColums(int type, string col)
        {
            return columsTemp[type].IndexOf(col) < 0;
        }
        public void appendName(int type, string name)
        {
            if (type == 1)
                wellNames.Add(name);
            else if (type == 3)
                nodeNames.Add(name);
        }
        public void appendLink(int row, string b, string e)
        {
            if (linkNames.Keys.Contains(b))
                linkNames[b] = linkNames[b] + string.Format(",{0:000}:{1}", row, e);
            else
                linkNames.Add(b, string.Format("{0:000}:{1}", row, e));
        }
        public void appendValve(string link)
        {
            valveNames.Add(link);
        }

        public bool checkName(string name)
        {
            if (name.EndsWith("点"))
            {
                linkWells.Add(name);
                return nodeNames.Contains(name);
            }
            else if (name.EndsWith("井"))
            {
                linkWells.Add(name);
                return wellNames.Contains(name);
            }
            else if (name.EndsWith("站"))
            {
                return deviceCounter.Keys.Contains(name);
            }
            return false;
        }
        public bool wellLinked(string name)
        {
            return linkWells.Contains(name);
        }

        public bool nodeLinked(string name)
        {
            return linkNodes.Contains(name);
        }

        public bool pointUnvalid(int row, string str)
        {
            if (str.Length < 26)
                return false;
            if (str.StartsWith("[53"))
            {
                str = str.Substring(3);
                string[] yx = str.Split(',');
                if (yx.Count() == 2 && str.IndexOf('.') == 4 && yx[1].StartsWith("464") && yx[1].IndexOf('.') == 7)
                    return false;
            }
            else if (str.StartsWith("[464"))
            {
                str = str.Substring(4);
                str = str.TrimEnd(']');
                string[] yx = str.Split(',');
                if (yx.Count() == 2 && str.IndexOf('.') == 4 && yx[1].StartsWith("51") && yx[1].IndexOf('.') == 6)
                    return false;
            }
            return true;
        }
        public bool pointValid(int row, string str, ref float x, ref float y)
        {
            if (str.Length < 26)
                return false;
            str = str.TrimEnd(']');
            if (str.StartsWith("[53"))
            {
                str = str.Substring(3);
                string[] xy = str.Split(',');
                if (xy.Count() == 2 && str.IndexOf('.') == 4 && xy[1].StartsWith("464") && xy[1].IndexOf('.') == 7)
                {
                    xy[1] = xy[1].Substring(3);
                    if (float.TryParse(xy[0], out x) && float.TryParse(xy[1], out y))
                    {
                        return true;
                    }
                }
            }
            else if (str.StartsWith("[464"))
            {
                str = str.Substring(4);
                string[] yx = str.Split(',');
                if (yx.Count() == 2 && str.IndexOf('.') == 4 && yx[1].StartsWith("51") && yx[1].IndexOf('.') == 6)
                {
                    yx[1] = yx[1].Substring(2);
                    if (float.TryParse(yx[0], out y) && float.TryParse(yx[1], out x))
                    {
                        return true;
                    }
                }
            }


            return false;
        }
    }
}
