using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Windows.Media.Imaging;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;

namespace Compiler_Parser_Demo_WPF
{
    static class StatechartDiagram
    {
        //Refer to https://csharp.hotexamples.com/examples/QuickGraph.Algorithms.Graphviz/GraphvizAlgorithm/Write/php-graphvizalgorithm-write-method-examples.html
        public static BitmapImage GetDiagram(List<string> VertexList,List<string[]> EdgeList)
        {
            var graph = new AdjacencyGraph<string, TaggedEdge<string, string>>();

            foreach(var item in VertexList)
            {
                graph.AddVertex(item);
            }

            foreach(var item in EdgeList)
            {
                graph.AddEdge(new TaggedEdge<string,string>(item[0],item[1],item[2]));
            }

            var graphViz = new GraphvizAlgorithm<string,TaggedEdge<string,string>>(graph, @".\", QuickGraph.Graphviz.Dot.GraphvizImageType.Png);
            graphViz.GraphFormat.RankDirection = GraphvizRankDirection.LR;
            
            graphViz.FormatVertex += (sender,e) =>
            {
                e.VertexFormatter.Shape = GraphvizVertexShape.Circle;
            };

            graphViz.FormatEdge += (sender,e) =>
            {
                e.EdgeFormatter.Font = new GraphvizFont("Consolas",15.0f);
                e.EdgeFormatter.Dir = GraphvizEdgeDirection.Forward;
                e.EdgeFormatter.Head = new GraphvizEdgeExtremity(true);
                e.EdgeFormatter.Label = new GraphvizEdgeLabel(){Value = e.Edge.Tag};
            };

            var dot = graphViz.Generate();
            var img = Graphviz.RenderImage(dot,"bmp");

            var memstream = new MemoryStream();
            var bmp = new Bitmap(img);
            bmp.Save(memstream,ImageFormat.Bmp);
            var bmpimg = new BitmapImage();
            bmpimg.BeginInit();
            bmpimg.StreamSource = memstream;
            bmpimg.EndInit();
            return bmpimg;
        }
    }
}
