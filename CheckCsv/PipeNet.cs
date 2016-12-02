using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
namespace PipeAuto
{
    struct Point
    {
        public Point(float x=0,float y=0)
        {
            this.x = x;
            this.y = y;
        }
        public bool equals(float x,float y)
        {
            return (Math.Abs(this.x - x) < 0.001 && Math.Abs(this.y - y) < 0.001);
        }
        public float xydis(float x,float y)
        {
            return Math.Abs(this.x - x) + Math.Abs(this.y - y);
        }
        public float x, y;
    };

    struct Edge
    {
        public Edge(int index, float x0, float y0, float x1, float y1, int width)
        {
            i = index;
            v0 = new Point(x0, y0);
            v1 = new Point(x1, y1);
            w = width;
        }
        int i;
        Point v0;
        Point v1;
        int w;
    };

    //data struct
    class Vertex
    {
        public Vertex(Vertex p,float x,float y)
        {
            v0 = p;
            if (p != null)
            {                
                if (p.i == 1)
                    p.v1 = this;
                else if (p.i == 2)
                {
                    p.v2 = this;
                }
                p.inc();
            }
            pos.x = x;
            pos.y = y;
            v1 = null;
            v2 = null;
            i = 1;
        }
        public int id;
        public Point pos = new Point();

        public void inc() { i++; }

        public Vertex v0;   //parent
        public Vertex v1;   //next

        public Vertex v2;   //branch
        public int w;
        public int i;
    };
    class PipeNet
    {
        List<Edge>[] edges = new List<Edge>[2] { new List<Edge>(1000), new List<Edge>(1000) };
        List<Vertex>[] vtxLists = new List<Vertex>[2] { new List<Vertex>(1000), new List<Vertex>(1000) };       
        List<Vertex>[] subTrees = new List<Vertex>[2] { new List<Vertex>(1000), new List<Vertex>(1000) };

        Vertex[] root = new Vertex[2]{null,null};
        StreamWriter log;

        //错误信息
        //分支未捕捉到节点
        List<Vertex> errSnap;
        public List<string> errMsg; 

        public PipeNet(ref StreamWriter sw)
        {
            this.log = sw;
            errSnap = new List<Vertex>(10);
            errMsg = new List<string>(10);
        }

        public List<Vertex> getErrSnap() { return errSnap; }
        public List<Vertex> getsubTree(int type) { return subTrees[type]; }

        public void clear()
        {
            errMsg.Clear();
            errSnap.Clear();
            subTrees[0].Clear();
            subTrees[1].Clear();
            edges[0].Clear();
            edges[1].Clear();
            vtxLists[0].Clear();
            vtxLists[1].Clear();
        }

        public void parserNet()
        {
            for (int i = 0; i < 2;i++ )
            {
                List<Vertex> branches = vtxLists[i];
                //log.WriteLine(string.Format("brances[{0}] count={1}",i, branches.Count));
                Queue < Vertex >  queue = new Queue<Vertex>(branches.Count);
                foreach (Vertex s in branches)
                {
                    //printSub(s);
                    queue.Enqueue(s);
                }
                while (queue.Count > 0)
                {
                    Vertex sub = queue.Dequeue();
             
                    Vertex p = findInList(branches,sub.id, sub.pos.x, sub.pos.y);
                    if (p == null || p.id == sub.id)
                    {
                        subTrees[i].Add(sub);                        
                        Console.WriteLine(string.Format("sub index[{0}] edge[{1}] not find Parent ",sub.id,sub.id/2));
                        log.WriteLine(string.Format("*erro: 第[{0:000}]行-[53{1},464{2}]->未接入网", sub.id / 2, sub.pos.x, sub.pos.y));
                        errMsg.Add(string.Format("*erro: 第[{0:000}]行-[53{1},464{2}]->未接入网", sub.id / 2, sub.pos.x, sub.pos.y));
                    }
                    else
                    {
                        sub.v0 = p;
                        if (p.i == 1)
                        {
                            p.v1 = sub;
                            //Console.WriteLine(string.Format("P index[{0}] find next v[{1}]"),p.id,sub.id);
                        }
                        else if (p.i == 2)
                        {
                            p.v2 = sub;
                            //Console.WriteLine(string.Format("P index[{0}] find branch v[{1}]"), p.id, sub.id);
                        }
                    }                   
                }
            }


        }
        public void printSub(Vertex sub)
        {
            if (sub == null)
                log.WriteLine("sub over!");
            else
            {
                printVertex(sub);
                printSub(sub.v1);
                printSub(sub.v2);
            }
        }
        public void printVertex(Vertex v)
        {
            if (v == null)
                log.WriteLine("vtx over!");
            else
                log.WriteLine(string.Format("id:{0},rep:{1},dn:{2},x:{3}",v.id,v.i,v.w,v.pos.x));
        }
        //type=0 供水，type=1 回水
        int counter = 0;
        int tmpc = 0;
        public void appendEdge(int index, int type,float x0, float y0, float x1, float y1, int width)
        {
            if (index == 327)
            {
               // log.WriteLine("139");
            }
            Edge e = new Edge(index, x0, y0, x1, y1, width);
            edges[type].Add(e);
            if (root[type] == null)
            {
                root[type] = new Vertex(null, x0, y0);
                root[type].w = width;
                root[type].id = index * 2;
                Vertex end = new Vertex(root[type], x1, y1);       
                end.w = width;
                end.id = root[type].id + 1;
                vtxLists[type].Add(root[type]);
            }
            else
            {
                counter = index * 2;
                tmpc = 0;
                Vertex b = findInList(vtxLists[type],counter, x0, y0);
                if (b == null)
                {
                    //TODO:
                    //log.WriteLine(string.Format("+info {0} parent is null",index));
                    //Console.WriteLine(string.Format("+info {0} parent is null", index));
                    b = new Vertex(null, x0, y0);
                    b.w = width;
                    b.id = counter;
                    Vertex end = new Vertex(b, x1, y1);
                    end.w = width;
                    end.id = b.id + 1;
                    vtxLists[type].Add(b);
                }
                else 
                {
                    counter++;
                    Vertex end = new Vertex(b, x1, y1);
                    end.w = width;
                    end.id = counter;
                    Console.WriteLine(string.Format("+info index[{0}] find pos in [{1}]",end.id-1,b.id));
                }
            }           

        }
        Vertex findInList(List<Vertex> list,int i, float x, float y)
        {
            Vertex n = null;
            foreach (Vertex v in list)
            {
                n = findVertex(v,i, x, y);
                if (n != null)
                    return n;
            }
            return n;
        }

        Vertex findVertex(Vertex node,int i, float x, float y)
        {
            if (node == null)
                return null;
            if (tmpc++ > counter)
            {
                //errMsg.Add(string.Format("*erro: 第[{0:000}]行-未连接点->[53{1},464{2}]", i/2, x,y));
                return null;
            }
            if (node.id != i)
            {
                float dis = node.pos.xydis(x, y);
                if(dis<0.001)
                    return node;
                else if (dis < 0.3)
                {
                    Vertex v = new Vertex(null, x, y);
                    v.id = i;
                    errSnap.Add(v);
                    log.WriteLine(string.Format("*erro: 第[{0:000}]行-未捕捉点->[53{1},464{2}]", i/2, x,y));
                    return node;
                }
            }
            if (node.v1 != null)
            {
                Vertex n = findVertex(node.v1,i, x, y);
                if (n != null)
                    return n;
                else if (tmpc > counter)
                {
                    //Console.WriteLine(string.Format("Find[{0}]-[53{1},464{2}]-in-root index[{3}]", i / 2, x, y, node.v1.id / 2));
                    //return null;
                }
            }
            if (node.v2 != null)
            {
                Vertex n = findVertex(node.v2,i, x, y);
                if (n != null)
                    return n;
                else if (tmpc > counter)
                {
                    //Console.WriteLine(string.Format("Find[{0}]-[53{1},464{2}]-in-root index[{3}]", i / 2, x, y, node.v1.id / 2));
                    //return null;
                }
            }

            return null;
        }
    };
}
