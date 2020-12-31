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
    public partial class frmDynShading : Form
    {
        #region Initializations
        public frmDynShading()
        {
            InitializeComponent();
        }
        frmPalette frmpalette = new frmPalette();
        private void frmDynShading_Load(object sender, EventArgs e)
        {
            string[] sLic = lblLicData.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            SoftwareKey.lblNotRegisteredTxt = sLic[1];
            SoftwareKey.lblFindLicenseTxt = sLic[0];

            //check license
            //SoftwareKey.CheckLicense("", false);

            int n = 300;
            Bitmap bmp0 = new Bitmap(n,n);
            Graphics g = Graphics.FromImage(bmp0);
            g.FillRectangle(Brushes.White, 0, 0, n, n);
            ds = new DynamicShadingCPU(new List<int>(), n, n);

            picShadeImg.Image = bmp0;
            frmpalette.Show();
        }
        #endregion

        //current dynamic shading state and previous, for UNDO
        DynamicShadingCPU ds, dsPrev;

        private void doOpenFile( bool tryReadStrokes, bool removeColors)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.bmp;*.png;*.jpeg";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmpSrc = new Bitmap(ofd.FileName);
                curFile = ofd.FileName;

                //DynamicShadingCPU.EqualizeHistogram(bmpSrc);

                #region base64 example
                //MemoryStream stream = new MemoryStream();
                //bmpSrc.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                //byte[] imageBytes = stream.ToArray();

                //// Convert byte[] to Base64 String
                //string base64String = Convert.ToBase64String(imageBytes);

                //byte[] b2 = Convert.FromBase64String(base64String);
                //MemoryStream ms2 = new MemoryStream(b2);
                //bmpSrc = new Bitmap(ms2);
                #endregion

                if (removeColors) DynamicShadingCPU.RemoveColors(bmpSrc,33);

                List<DynamicShadingCPU.ColorStroke> strokes = null;
                if (tryReadStrokes)
                {
                    strokes = DynamicShadingCPU.GetColorStrokes(bmpSrc, 33);
                }

                float thresh = DynamicShadingCPU.getOtsuThreshold(bmpSrc);
                List<int> edges = DynamicShadingCPU.RetrieveImageEdges(bmpSrc, thresh);

                lblStatus.Text = "Otsu threshold: " + thresh.ToString();

                picShadeImg.Image = DynamicShadingCPU.RetrieveImageFromEdges(edges, bmpSrc.Width, bmpSrc.Height);

                ds = new DynamicShadingCPU(edges, bmpSrc.Width, bmpSrc.Height);

                if (File.Exists(ofd.FileName + ".json")) ds = DynamicShadingCPU.FromJson(ofd.FileName + ".json");
                else if (File.Exists(ofd.FileName + ".strokes")) ds.LoadStrokes(ofd.FileName + ".strokes");
                else if (tryReadStrokes) ds._strokes = strokes;

                picShadeImg.Invalidate();
            }
        }

        private void openwithoutColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            doOpenFile(true, true);
        }
        private void importOutlineAndStrokesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            doOpenFile(true, false);
        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            doOpenFile(false, false);
        }
        string curFile;
        private void saveRenderAndStrokesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmpComposed == null) return;

            Bitmap bmp = (Bitmap)bmpComposed.Clone();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG|*.png";

            if (curFile != "")
            {
                FileInfo fi = new FileInfo(curFile);

                sfd.FileName = fi.Name.Split('.')[0] + "strokes.png";

                ds.SaveStrokes(curFile);
                //ds.SaveJson(curFile);
            }

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Graphics g = Graphics.FromImage(bmp);

                bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);

                System.Diagnostics.Process.Start(sfd.FileName);
            }
        }

        #region Draw stroke and pick colors

        bool pickingColor = false;
        bool drawingStroke = false;
        List<int[]> curStroke = new List<int[]>();

        private void picShadeImg_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (closestStroke != null && distToClosestStroke < 15)
                {
                    ctxStroke.Show(picShadeImg, closestStroke.StrokeCoords[0][0], closestStroke.StrokeCoords[0][1]);
                }
                else
                {
                    pickingColor = true;
                    frmpalette.picCurColor.BackColor = ((Bitmap)picShadeImg.Image).GetPixel(e.X, e.Y);
                }
            }
            else
            {
                drawingStroke = true;
                curStroke = new List<int[]>();
                curStroke.Add(new int[] { e.X, e.Y });
            }
        }

        DynamicShadingCPU.ColorStroke closestStroke;
        float distToClosestStroke;
        private void picShadeImg_MouseMove(object sender, MouseEventArgs e)
        {
            if (ds == null) return;
            closestStroke = ds.getClosestStroke(e.X, e.Y, out distToClosestStroke);
            if (closestStroke != null) picShadeImg.Invalidate();

            if (rendered)
            {
                rendered = false;
                picShadeImg.Invalidate();
            }
            if (drawingStroke)
            {
                if (!CurStrokeContainsPt(e.X, e.Y) && (e.X >= 0 && e.Y >= 0 && e.X < ds.ImageWidth && e.Y < ds.ImageHeight)) curStroke.Add(new int[] { e.X, e.Y });
                lblStatus.Text = "Picking [" + e.X.ToString() + ", " + e.Y.ToString() + "]";
                picShadeImg.Invalidate();
            }
            if (pickingColor && e.X >= 0 && e.Y >= 0 && e.X < ds.ImageWidth && e.Y < ds.ImageHeight) frmpalette.picCurColor.BackColor = ((Bitmap)picShadeImg.Image).GetPixel(e.X, e.Y);

        }

        /// <summary>Check if stroke has point</summary>
        private bool CurStrokeContainsPt(int x, int y)
        {
            int[] pt = curStroke.Where(s => s[0] == x && s[1] == y).FirstOrDefault();
            return pt != null;
        }

        private void picShadeImg_MouseUp(object sender, MouseEventArgs e)
        {
            pickingColor = false;

            if (drawingStroke)
            {
                drawingStroke = false;
                dsPrev = ds.Clone();

                ds.AddStroke(curStroke, frmpalette.picCurColor.BackColor);
                picShadeImg.Invalidate();

                //picShadeImg.Image = DynamicShadingCPU.RetrieveDistanceBitmap(ds._strokes[ds._strokes.Count - 1].DistanceMap, ((Bitmap)picShadeImg.Image).Width, ((Bitmap)picShadeImg.Image).Height);
                if (autorenderWhenChangingColorsToolStripMenuItem.Checked) beginRenderToolStripMenuItem_Click(sender, e);
            }

        }
        #endregion

        #region Visual feedback
        private void picShadeImg_Paint(object sender, PaintEventArgs e)
        {
            if (rendered) return;

            if (drawingStroke) DrawStroke(curStroke, frmpalette.picCurColor.BackColor, e.Graphics, false);

            if (ds != null)
            {
                foreach (DynamicShadingCPU.ColorStroke cs in ds._strokes)
                {
                    if (closestStroke == cs && distToClosestStroke < 15) DrawStroke(cs.StrokeCoords, cs.StrokeColor, e.Graphics, true);
                    else DrawStroke(cs.StrokeCoords, cs.StrokeColor, e.Graphics, false);
                }
            }
        }


        private static void DrawStroke(List<int[]> stroke, Color c, Graphics g, bool highLight)
        {
            Brush b = new SolidBrush(c);
            //Draw region being selected
            if (highLight) foreach (int[] p in stroke) g.FillRectangle(Brushes.Red, p[0] - 2, p[1] - 2, 5, 5);
            else
            {
                foreach (int[] p in stroke) g.FillRectangle(Brushes.White, p[0] - 1, p[1] - 2, 3, 5);
                foreach (int[] p in stroke) g.FillRectangle(Brushes.Black, p[0] - 2, p[1] - 1, 5, 3);
            }
            foreach (int[] p in stroke) g.FillEllipse(b, p[0] - 1, p[1] - 1, 3, 3);
        }

        #endregion

        #region Compose image
        System.Diagnostics.Stopwatch swRender = new System.Diagnostics.Stopwatch();
        Bitmap bmpComposed;
        bool rendered = false;
        private void beginRenderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            swRender.Reset();

            lblStatus.Text = "Start rendering at" + DateTime.Now.ToString();
            Application.DoEvents();

            //if (!bgWorker.IsBusy) bgWorker.RunWorkerAsync();

            //bgWorker_DoWork(sender, new DoWorkEventArgs(0));

            //else lblStatus.Text = "Already rendering";

            swRender.Start();

            //compute distance maps if necessary
            ds.ComputeDistanceMaps();

            lblStatus.Text = "Start composing image at" + DateTime.Now.ToString();
            Application.DoEvents();


            bmpComposed = ds.ComposeImage();

            swRender.Stop();
            rendered = true;

            picShadeImg.Image = bmpComposed;
            picShadeImg.Invalidate();
            lblStatus.Text = "Done rendering " + swRender.Elapsed.ToString();


        }
        private void stepbystepRenderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            swRender.Reset();

            lblStatus.Text = "Start rendering at" + DateTime.Now.ToString();
            Application.DoEvents();

            //if (!bgWorker.IsBusy) bgWorker.RunWorkerAsync();

            //bgWorker_DoWork(sender, new DoWorkEventArgs(0));

            //else lblStatus.Text = "Already rendering";

            swRender.Start();

            //compute distance maps if necessary
            for (int j=0;j<ds._strokes.Count;j++)
            {
                lblStatus.Text = "Stroke " + (1 + j).ToString() + " of " + ds._strokes.Count.ToString();
                Application.DoEvents();

                DynamicShadingCPU.ColorStroke cs = ds._strokes[j];
                if (cs.DistanceMap == null) cs.DistanceMap = DynamicShadingCPU.GetPixelDistances(cs.StrokeCoords, ds._edges, ds.ImageWidth, ds.ImageHeight, ds.MAXITER);

                picShadeImg.Image = ds.ComposeImage();
                picShadeImg.Invalidate();
            }


            bmpComposed = ds.ComposeImage();

            swRender.Stop();
            rendered = true;

            picShadeImg.Image = bmpComposed;
            picShadeImg.Invalidate();
            lblStatus.Text = "Done rendering " + swRender.Elapsed.ToString();
        }

        private void revertToOriginalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            picShadeImg.Image = DynamicShadingCPU.RetrieveImageFromEdges(ds._edges, ds.ImageWidth, ds.ImageHeight);
            picShadeImg.Invalidate();
        }

        #endregion




        #region Floating menu
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (closestStroke != null)
            {
                //save for undo
                dsPrev = ds.Clone();

                ds._strokes.Remove(closestStroke);
                picShadeImg.Invalidate();
                if (autorenderWhenChangingColorsToolStripMenuItem.Checked) beginRenderToolStripMenuItem_Click(sender, e);
            }
        }
        private void redefineColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (closestStroke == null) return;

            ColorDialog cd = new ColorDialog();
            cd.Color = closestStroke.StrokeColor;
            cd.FullOpen = true;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                //save for undo
                dsPrev = ds.Clone();

                closestStroke.StrokeColor = cd.Color;
                picShadeImg.Invalidate();

                if (autorenderWhenChangingColorsToolStripMenuItem.Checked) beginRenderToolStripMenuItem_Click(sender, e);
            }
        }
        private void setToPaletteColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (closestStroke == null) return;

            //save for undo
            dsPrev = ds.Clone();

            closestStroke.StrokeColor = frmpalette.picCurColor.BackColor;
            picShadeImg.Invalidate();

            if (autorenderWhenChangingColorsToolStripMenuItem.Checked) beginRenderToolStripMenuItem_Click(sender, e);
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DynamicShadingCPU temp = ds;
            ds = dsPrev;
            dsPrev = temp;

            beginRenderToolStripMenuItem_Click(sender, e);
        }

        private void changeRegionColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (closestStroke == null) return;

            ColorDialog cd = new ColorDialog();
            cd.Color = closestStroke.StrokeColor;
            cd.FullOpen = true;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                //save for undo
                dsPrev = ds.Clone();

                ds.ChangeRegionColor(closestStroke.StrokeCoords[0][0], closestStroke.StrokeCoords[0][1], cd.Color);

                picShadeImg.Invalidate();
            }
            if (autorenderWhenChangingColorsToolStripMenuItem.Checked) beginRenderToolStripMenuItem_Click(sender, e);
        }








        #endregion


    }
}
