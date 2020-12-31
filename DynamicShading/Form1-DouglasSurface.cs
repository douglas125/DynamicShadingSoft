using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DynamicShading
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //TODO: juntar cores iguais para processar junto
            //TODO: Preview
            //TODO: Zoom
            //OK: Manter último render
            //OK: Save/load
            //OK: Destacar linha proxima
        }

        Bitmap bmpSource, bmpThresh;
        Bitmap waterMark;
        DynamicShading ds;
        private void Form1_Load(object sender, EventArgs e)
        {

            waterMark = (Bitmap)pbWaterMark.Image;//new Bitmap(Application.StartupPath + "\\waterMark.png");

            bmpSource = new Bitmap(200, 300);
            Graphics g = Graphics.FromImage(bmpSource);

            g.FillRectangle(new SolidBrush(Color.LightGray), 0, 0, bmpSource.Width, bmpSource.Height);

            ds = new DynamicShading();
            ds.Threshold = (int)numThresh.Value;
            doProcessing(bmpSource);

        }

        private void doProcessing(Bitmap bmp)
        {
            ds.Shade(bmp, picShaded, false);
            bmpThresh = ds.GetBmpThreshold();
            picOutline.Image = bmpThresh;
            picThumb.Image = bmpThresh;
            picShaded.Image = bmpThresh;
        }
        string curFile = "";
        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.bmp;*.png";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ds.ClearData();

                ds.ReUseLastRender = false;

                flowStrokes.Controls.Clear();
                _pbs.Clear();

                Bitmap bmp = new Bitmap(ofd.FileName);


                bmpSource = new Bitmap(bmp.Width, bmp.Height);
                Graphics g = Graphics.FromImage(bmpSource);
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

                g.DrawImage(waterMark, 0, 0);

                doProcessing(bmpSource);

                curFile = ofd.FileName;

                if (File.Exists(curFile + ".strokes")) LoadStrokes(curFile + ".strokes");

                tabControl1.SelectedIndex = 1;
            }
        }

        private void numThresh_ValueChanged(object sender, EventArgs e)
        {
            ds.Threshold = (int)numThresh.Value;
            ds.Shade(bmpSource, picShaded, false);
            bmpThresh = ds.GetBmpThreshold();
            picOutline.Image = bmpThresh;
            picThumb.Image = bmpThresh;
            picShaded.Image = bmpThresh;
        }

        private void picOutline_Click(object sender, EventArgs e)
        {

        }
        #region Draw onto image
        bool clicado = false;
        private void picOutline_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                clicado = true;
                curPt = new Point();
                lastPt = new Point();
            }
        }

        Point curPt, lastPt;
        private void picOutline_MouseMove(object sender, MouseEventArgs e)
        {
            if (clicado && (lastPt.X != 0 || lastPt.Y !=0))
            {
                if (bmpThresh != null)
                {
                    Graphics g = Graphics.FromImage(bmpThresh);
                    g.DrawLine(Pens.Black, curPt, lastPt);
                    g.DrawLine(Pens.Black, curPt.X, curPt.Y - 1, lastPt.X, lastPt.Y - 1);
                    g.DrawLine(Pens.Black, curPt.X, curPt.Y + 1, lastPt.X, lastPt.Y + 1);
                    
                }
                picThumb.Image = bmpThresh;
                picOutline.Image = bmpThresh;
            }
            lastPt.X = curPt.X;
            lastPt.Y = curPt.Y;
            curPt.X = e.X;
            curPt.Y = e.Y;
        }
        private void picOutline_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) clicado = false;
        }
        #endregion

        #region Shading strokes

        bool clickShade = false;
        bool clickRightColor = false;
        Color curColor = Color.Black;
        List<PictureBox> _pbs = new List<PictureBox>();
        List<Point> curPts = new List<Point>();
        private void picShaded_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                clickShade = true;
                curColor = picStrokeColor.BackColor;
                curPts = new List<Point>();

                ds.AddColorStrokes(curColor, curPts);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right) clickRightColor = true;
        }
        private void picShaded_MouseMove(object sender, MouseEventArgs e)
        {
            if (clickShade)
            {
                curPts.Add(new Point(e.X, e.Y));
                picShaded.Refresh();
            }
            else if (clickRightColor) picStrokeColor.BackColor =((Bitmap)picShaded.Image).GetPixel(e.X,e.Y);
        }



        private void picShaded_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (curPts.Count > 2 && ds.Points.Contains(curPts))
                {
                    clickShade = false;

                    picShaded.Refresh();

                    CreatePicBox(curColor);
                }
                else if (ds.Points.Contains(curPts))
                {
                    ds.RemoveAt(ds.Colors.Count - 1);
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right) clickRightColor = false;
        }

        #region Create reference color pictureboxes
        private void CreatePicBox(Color c)
        {
            PictureBox pb = new PictureBox();
            pb.Height = pb.Width = 18;
            pb.BackColor = c;
            flowStrokes.Controls.Add(pb);
            _pbs.Add(pb);
            pb.MouseDown += new MouseEventHandler(pb_MouseDown);

            pb.MouseLeave += new EventHandler(pb_MouseLeave);
            pb.MouseMove += new MouseEventHandler(pb_MouseMove);
        }
        PictureBox selPb;

        void pb_MouseMove(object sender, MouseEventArgs e)
        {
            foreach (PictureBox pbb in _pbs) if (pbb != sender) pbb.BorderStyle = BorderStyle.None;
            selPb = (PictureBox)sender;
            selPb.BorderStyle = BorderStyle.FixedSingle;
            picShaded.Refresh();
        }


        void pb_MouseLeave(object sender, EventArgs e)
        {
            selPb = (PictureBox)sender;
            selPb.BorderStyle = BorderStyle.None;
            selPb = null;
            picShaded.Refresh();
        }

        void pb_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            int idx = _pbs.IndexOf(pb);

            //Change color
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {

                ColorDialog cd = new ColorDialog();
                cd.Color = pb.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    pb.BackColor = cd.Color;
                    ds.Colors[idx] = cd.Color;
                    ds.ReUseLastRender = false;
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                //delete
                ds.RemoveAt(idx);
                _pbs.RemoveAt(idx);
                flowStrokes.Controls.Remove(pb);
                ds.ReUseLastRender = false;
            }
            picShaded.Refresh();
        }
        #endregion

        private void picShaded_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;


            for (int i = 0; i < ds.Colors.Count; i++)
            {
                if (ds.Points[i].Count > 1) g.DrawLines(new Pen(ds.Colors[i],2), ds.Points[i].ToArray());
            }
            //g.DrawImage(waterMark, 0, 0);


            //Show selected color
            int selIdx = -1;
            if (selPb != null)
            {
                selIdx = _pbs.IndexOf(selPb);
                if (selIdx >=0 && selIdx < ds.Points.Count && ds.Points[selIdx].Count > 1)
                {
                    Pen pW = new Pen(Color.White, 2);
                    Pen pB = new Pen(Color.Black, 2);
                    g.TranslateTransform(0, 2);
                    g.DrawLines(pW, ds.Points[selIdx].ToArray());
                    g.TranslateTransform(0, -4);
                    g.DrawLines(pB, ds.Points[selIdx].ToArray());
                    g.TranslateTransform(0, 2);
                }
            }
        }
        #endregion

        private void picStrokeColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if (cd.ShowDialog() == DialogResult.OK)
            {
                picStrokeColor.BackColor = cd.Color;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            picShaded.Image = bmpThresh;
        }

        private void btnRender_Click(object sender, EventArgs e)
        {
            SaveStrokes();
            
            ds.Shade(bmpThresh, picShaded, true);

            //picShaded.Image = ds.GetBmpDists();
            picShaded.Image = ds.GetRenderedImage();
            ds.ReUseLastRender = true;
        }

        #region Save/load strokes and save image
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap bmp = (Bitmap)picShaded.Image;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG|*.png";
            sfd.FileName = "desenho.png";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Graphics g = Graphics.FromImage(bmp);

                for (int i = 0; i < ds.Colors.Count; i++)
                {
                    if (ds.Points[i].Count > 1) g.DrawLines(new Pen(ds.Colors[i],2), ds.Points[i].ToArray());
                }
                bmp = ds.RestoreBlackPixels(bmpSource, bmp);

                bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);

                System.Diagnostics.Process.Start(sfd.FileName);
            }
        }

        private void SaveStrokes()
        {
            if (curFile == "") return;

            using (StreamWriter sw = new StreamWriter(curFile + ".strokes"))
            {
                for (int i = 0; i < ds.Colors.Count; i++)
                {
                    sw.WriteLine("Color " + ds.Colors[i].R.ToString() + " " + ds.Colors[i].G.ToString() + " " + ds.Colors[i].B.ToString() + " " + ds.Points[i].Count);
                    foreach (Point p in ds.Points[i])
                    {
                        sw.WriteLine(p.X + " " + p.Y);
                    }
                }
            }
        }

        private void LoadStrokes(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split();
                    Color c = Color.FromArgb(int.Parse(line[1]), int.Parse(line[2]), int.Parse(line[3]));
                    List<Point> curStrokes = new List<Point>();
                    int nStrokes = int.Parse(line[4]);
                    for (int k = 0; k < nStrokes; k++)
                    {
                        string[] coords = sr.ReadLine().Split();
                        curStrokes.Add(new Point(int.Parse(coords[0]), int.Parse(coords[1])));
                    }
                    ds.AddColorStrokes(c, curStrokes);


                    CreatePicBox(c);
                }
            }
        }

        #endregion

    }
}
