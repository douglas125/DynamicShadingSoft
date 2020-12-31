using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCLTemplate;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicShading
{
    /// <summary>Dynamic method to shade pictures</summary>
    public class DynamicShading
    {
        /// <summary>Maximum number of dynamic propagations</summary>
        int MAXPROPAGITERS = int.MaxValue;

        #region Static initializations and code


        static CLCalc.Program.Kernel kernelThreshold;
        static CLCalc.Program.Kernel kernelPropagateDist;
        static CLCalc.Program.Kernel kernelinitWeight;
        static CLCalc.Program.Kernel kernelinitTotalWeight;
        static CLCalc.Program.Kernel kernelAddToTotalWeight;

        static CLCalc.Program.Kernel kernelRestoreBlackPixels;

        static DynamicShading()
        {
            #region OpenCL Source

            #region Thresholding
            string srcThresh = @"
__kernel void Threshold(__constant  int *      cfg,
                        __read_only image2d_t  imgSrc,
                        __global    uchar *    byteInfo,
                        __write_only image2d_t imgThresh)

{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int thresh = cfg[0];

   int2 coord = (int2)(get_global_id(0),get_global_id(1));

   uint4 pix = read_imageui(imgSrc, smp, coord);
   
   int pixBW = (int)pix.x+(int)pix.y+(int)pix.z;
   pixBW = pixBW > 3*thresh ? 255 : 0;
   
   byteInfo[coord.x+get_global_size(0)*coord.y] = pixBW;
   
   pix = (uint4)((uint)pixBW,(uint)pixBW,(uint)pixBW,255);
   write_imageui(imgThresh,coord,pix);
}

__kernel void RestoreBlackPixels(__constant int * cfg,
                                 __read_only image2d_t imgSrc,
                                 __read_only image2d_t imgRender,
                                 __write_only image2d_t dst)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int thresh = cfg[0];

   int2 coord = (int2)(get_global_id(0),get_global_id(1));

   uint4 pix = read_imageui(imgSrc, smp, coord);
   
   int pixBW = (int)pix.x+(int)pix.y+(int)pix.z;
   
   if (pixBW <= 3*thresh) pix = (uint4)(0,0,0,255);
   else pix = read_imageui(imgRender, smp, coord);

   write_imageui(dst,coord,pix);

}

";
            #endregion

            #region Propagate distance to line
            string srcPropag = @"
__kernel void initWeight(__global float * weight)
{
   int2 coord = (int2)(get_global_id(0),get_global_id(1));
   int w = get_global_size(0);
   int idx = coord.x+w*coord.y;
   weight[idx] = 1e20f;
}

__kernel void initTotalWeight(__global float * weight)
{
   int2 coord = (int2)(get_global_id(0),get_global_id(1));
   int w = get_global_size(0);
   int idx = coord.x+w*coord.y;
   weight[idx] = 0.0f;
}

__kernel void AddToTotalWeight(__global       float * totalWeight,
                               __global const float * weight,
                               __constant     float * color,
                               __read_only  image2d_t curImg,
                               __write_only image2d_t dstImg,
                               __global const uchar*  byteInfo)

{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

  int x = get_global_id(0);
  int y = get_global_id(1);
  int w = get_global_size(0);
  
  int idx = x+w*y;
  
  float totWeight = totalWeight[idx];
  float myWeight = 1.0f*native_recip(weight[idx]);

  
    int2 coord = (int2)(x,y);
   
    uint4 pix = read_imageui(curImg, smp, coord);

    float4 curColor = (float4)((float)pix.x,(float)pix.y,(float)pix.z,255.0f);
    float4 newColor = (float4)((float)color[0],(float)color[1],(float)color[2],255.0f);

    if (myWeight > 1E-4f * totWeight)
    {  
        myWeight = powr(myWeight, 1.7f);
        //myWeight = native_exp(-native_recip(myWeight)*1E-15f);  
        newColor = (newColor * myWeight + curColor * totWeight)*native_recip(myWeight+totWeight);
        newColor = clamp(newColor, 0.0f, 255.0f);
        totalWeight[idx] = totWeight + myWeight;
    }
    else newColor = curColor;

    pix = (uint4)((uint)newColor.x,(uint)newColor.y,(uint)newColor.z,255);
    //if (byteInfo[idx] == 0) pix = (uint4)(0,0,0,255);
    write_imageui(dstImg,coord,pix);
}


__kernel void PropagateDist(__global        int * changed,
                            __read_only image2d_t imgStroke,
                            __global const uchar* byteInfo,
                            __global      float * weight,
                            __write_only image2d_t imgDists)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int2 coord = (int2)(get_global_id(0),get_global_id(1));
   int w = get_global_size(0);
   
   uint4 pix = read_imageui(imgStroke, smp, coord);
   
   int val = max((int)pix.z,max((int)pix.x,(int)pix.y));
   
   int idx = coord.x+w*coord.y;

    float curW = 1.0f;
    if (val > 0) curW == 1.0f;
    else if (coord.x == 0 || coord.y == 0 || coord.x == w-1 || coord.y == get_global_size(1)-1 || byteInfo[idx]==0) curW = 1e22f;
    else
    {
        curW = weight[idx-1]+1.0f;
        curW = fmin(curW, weight[idx+1] + 1.0f); 
        curW = fmin(curW, weight[idx+w] + 1.0f); 
        curW = fmin(curW, weight[idx-w] + 1.0f); 

        curW = fmin(curW, weight[idx-w-1] + 1.41421356237f); 
        curW = fmin(curW, weight[idx-w+1] + 1.41421356237f); 
        curW = fmin(curW, weight[idx+w-1] + 1.41421356237f); 
        curW = fmin(curW, weight[idx+w+1] + 1.41421356237f);
      
    }
   
   if (weight[idx] != curW) changed[0] = 1;
   weight[idx] = curW;

   //float pixBW = clamp(curW,0.0f,255.0f);
   //uint4 pix2 = (uint4)((uint)pixBW,(uint)0,255-(uint)pixBW,255);
   //if (byteInfo[idx]==0) pix2 = (uint4)((uint)0,(uint)0,(uint)0,255);
   //write_imageui(imgDists,coord,pix2);
}

";

            #endregion

            #endregion

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();

            CLCalc.Program.Compile(new string[] { srcThresh, srcPropag });

            kernelThreshold = new CLCalc.Program.Kernel("Threshold");
            kernelPropagateDist = new CLCalc.Program.Kernel("PropagateDist");
            kernelinitWeight = new CLCalc.Program.Kernel("initWeight");
            kernelinitTotalWeight = new CLCalc.Program.Kernel("initTotalWeight");
            kernelAddToTotalWeight = new CLCalc.Program.Kernel("AddToTotalWeight");
            kernelRestoreBlackPixels = new CLCalc.Program.Kernel("RestoreBlackPixels");
        }
        #endregion

        #region Constructor and initializations
        /// <summary>Configuration variables: [0] - threshold</summary>
        CLCalc.Program.Variable CLcfgVars;

        /// <summary>Constructor</summary>
        public DynamicShading()
        {
            CLcfgVars = new CLCalc.Program.Variable(new int[] { 100 });
            CLchanged = new CLCalc.Program.Variable(new int[] { 0 });
            CLcolor = new CLCalc.Program.Variable(new float[] { 0, 0, 0 });
        }

        /// <summary>Sets current threshold</summary>
        public int Threshold
        {
            set
            {
                CLcfgVars.WriteToDevice(new int [] { value });
            }
        }

        #endregion

        CLCalc.Program.Image2D CLbmpSrc;
        CLCalc.Program.Image2D CLbmpThresh;
        CLCalc.Program.Image2D CLbmpStroke;
        CLCalc.Program.Image2D CLbmpDists;

        CLCalc.Program.Image2D CLimgFinal1, CLimgFinal2;

        CLCalc.Program.Variable CLTotalPixWeight;
        CLCalc.Program.Variable CLWeight;
        CLCalc.Program.Variable CLthreshInfo;
        CLCalc.Program.Variable CLchanged, CLcolor;

        #region Retrieves various images
        /// <summary>Retrieves thresholded bitmap</summary>
        public Bitmap GetBmpThreshold()
        {
            return CLbmpThresh.ReadBitmap();
        }

        public Bitmap GetBmpStroke()
        {
            return CLbmpStroke.ReadBitmap();
        }

        public Bitmap GetBmpDists()
        {
            return CLbmpDists.ReadBitmap();
        }

        /// <summary>Returns final rendered image</summary>
        /// <returns></returns>
        public Bitmap GetRenderedImage()
        {
            return CLimgFinal1.ReadBitmap();
        }
        #endregion

        #region Manages colors and points
        /// <summary>Colors to use when shading</summary>
        public List<Color> Colors = new List<Color>();
        /// <summary>Points to use when shading</summary>
        public List<List<Point>> Points = new List<List<Point>>();
        /// <summary>Distance maps</summary>
        public List<float[]> DistanceMaps = new List<float[]>();

        /// <summary>Adds a color-point pair without having to re-render image</summary>
        /// <param name="c">New color</param>
        /// <param name="p">New stroke point list</param>
        public void AddColorStrokes(Color c, List<Point> p)
        {
            Colors.Add(c);
            Points.Add(p);
            DistanceMaps.Add(null);
        }

        /// <summary>Removes colors, points and distance maps at index idx</summary>
        /// <param name="idx"></param>
        public void RemoveAt(int idx)
        {
            Colors.RemoveAt(idx);
            Points.RemoveAt(idx);
            DistanceMaps.RemoveAt(idx);
        }

        /// <summary>Clears data</summary>
        public void ClearData()
        {
            Colors.Clear();
            Points.Clear();
            DistanceMaps.Clear();
        }

        #endregion

        /// <summary>Shades a bitmap</summary>
        /// <param name="bmp">Bitmap to shade</param>
        /// <param name="bmp">Actually render?</param>
        /// <returns></returns>
        public void Shade(Bitmap bmp, bool doRender)
        {
            Shade(bmp, null, doRender, null, null, null);
        }

        /// <summary>Shades a bitmap</summary>
        /// <param name="pb">Picturebox to display intermediate results</param>
        /// <param name="bmp">Bitmap to shade</param>
        /// <param name="doRender">Actually render?</param>
        /// <param name="pbCurColor">Current color being rendered</param>
        /// <param name="pbRender">Progress of current render</param>
        /// <param name="lblProg">Label to draw current progress state</param>
        /// <returns></returns>
        public void Shade(Bitmap bmp, PictureBox pb, bool doRender, PictureBox pbCurColor, ProgressBar pbRender, Label lblProg)
        {
            Bitmap bmp2 = new Bitmap(bmp.Width, bmp.Height);
            Graphics g = Graphics.FromImage(bmp2);
            g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

            //draws points onto thresholded bitmap
            for (int i = 0; i < Colors.Count; i++)
            {
                //if (Points[i].Count > 1) g.DrawLines(new Pen(Color.Black, 2), Points[i].ToArray());
            }


            #region Initializations
            if (CLbmpSrc == null || CLbmpSrc.Width != bmp.Width || CLbmpSrc.Height != bmp.Height)
            {
                CLbmpSrc = new CLCalc.Program.Image2D(bmp2);
                CLbmpThresh = new CLCalc.Program.Image2D(bmp);
                CLthreshInfo = new CLCalc.Program.Variable(typeof(byte), bmp.Width * bmp.Height);
                CLbmpStroke = new CLCalc.Program.Image2D(bmp);
                CLbmpDists = new CLCalc.Program.Image2D(bmp);

                CLimgFinal1 = new CLCalc.Program.Image2D(new Bitmap(bmp2.Width, bmp2.Height));
                CLimgFinal2 = new CLCalc.Program.Image2D(new Bitmap(bmp2.Width, bmp2.Height));

                CLTotalPixWeight = new CLCalc.Program.Variable(typeof(float), bmp.Width * bmp.Height);
                lastPixWeight = new float[bmp.Width * bmp.Height];
                CLWeight = new CLCalc.Program.Variable(typeof(float), bmp.Width * bmp.Height);
                lastImage = new Bitmap(bmp.Width, bmp.Height);
            }
            else
            {
                CLbmpSrc.WriteBitmap(bmp2);
                CLimgFinal1.WriteBitmap(new Bitmap(bmp2.Width, bmp2.Height));
                CLimgFinal2.WriteBitmap(new Bitmap(bmp2.Width, bmp2.Height));
                kernelinitTotalWeight.Execute(new CLCalc.Program.MemoryObject[] { CLTotalPixWeight }, new int[] { CLbmpSrc.Width, CLbmpSrc.Height });
            }
            #endregion

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swCL = new System.Diagnostics.Stopwatch();
            sw.Start();

            //computes threshold
            kernelThreshold.Execute(new CLCalc.Program.MemoryObject[] { CLcfgVars, CLbmpSrc, CLthreshInfo, CLbmpThresh }, new int[] { bmp.Width, bmp.Height });

            if (!doRender) return;

            //Reuse last render?
            if (ReUseLastRender)
            {
                CLTotalPixWeight.WriteToDevice(lastPixWeight);
                CLimgFinal1.WriteBitmap(lastImage);
            }
            else lastRenderedIdx = 0;

            //generates images for points
            for (int i = lastRenderedIdx; i < Colors.Count; i++)
            {
                if (pbCurColor != null)
                {
                    pbCurColor.BackColor = Colors[i];
                    pbCurColor.Refresh();

                    lblProg.Text = (100 * i / Colors.Count).ToString() + "%";
                }

                Bitmap bmpStroke = new Bitmap(bmp2.Width, bmp2.Height);

                if (Points[i].Count > 1)
                {
                    if (DistanceMaps[i] == null)
                    {
                        Graphics g2 = Graphics.FromImage(bmpStroke);
                        g2.DrawLines(new Pen(Colors[i], 2), Points[i].ToArray());

                        CLbmpStroke.WriteBitmap(bmpStroke);

                        int[] changed = new int[] { 0 };
                        swCL.Start();
                        changed[0] = 1;
                        kernelinitWeight.Execute(new CLCalc.Program.MemoryObject[] { CLWeight }, new int[] { CLbmpSrc.Width, CLbmpSrc.Height });

                        int n = 0;
                        while (changed[0] > 0 && n < MAXPROPAGITERS)
                        {
                            n++;
                            changed[0] = 0;
                            CLchanged.WriteToDevice(changed);
                            kernelPropagateDist.Execute(new CLCalc.Program.MemoryObject[] { CLchanged, CLbmpStroke, CLthreshInfo, CLWeight, CLbmpDists },
                                new int[] { CLbmpSrc.Width, CLbmpSrc.Height });

                            CLchanged.ReadFromDeviceTo(changed);

                            if (pbRender != null)
                            {
                                int prog = 100*n / Math.Max(CLbmpSrc.Width, CLbmpSrc.Height);
                                if (prog > 100) prog = 100;
                                pbRender.Value = prog;
                                pbRender.Refresh();
                                Application.DoEvents();
                            }
                        }
                        swCL.Stop();

                        DistanceMaps[i] = new float[CLWeight.OriginalVarLength];
                        CLWeight.ReadFromDeviceTo(DistanceMaps[i]);
                    }
                    else CLWeight.WriteToDevice(DistanceMaps[i]);



                    //composes total weight
                    CLcolor.WriteToDevice(new float[] { (float)Colors[i].B, (float)Colors[i].G, (float)Colors[i].R});

                    kernelAddToTotalWeight.Execute(new CLCalc.Program.MemoryObject[] { CLTotalPixWeight, CLWeight, CLcolor, CLimgFinal1, CLimgFinal2, CLthreshInfo }, new int[] { CLbmpSrc.Width, CLbmpSrc.Height });
                    //swaps final images - result is always on CLimgFinal1
                    CLCalc.Program.Image2D temp = CLimgFinal1;
                    CLimgFinal1 = CLimgFinal2;
                    CLimgFinal2 = temp;
                    
                    if (pb != null)
                    {
                        //Bitmap bmpold = (Bitmap)pb.Image;
                        pb.Image = GetRenderedImage();
                        //bmpold.Dispose();
                        pb.Refresh();
                        Application.DoEvents();
                    }
                }
                bmpStroke.Dispose();
            }
            sw.Stop();

            lastRenderedIdx = Colors.Count;
            //saves total pixel weight and final image
            CLTotalPixWeight.ReadFromDeviceTo(lastPixWeight);
            if (lastImage != null) lastImage.Dispose();
            lastImage = CLimgFinal1.ReadBitmap();

            bmp2.Dispose();
        }

        #region Keeps track of last used color
        
        /// <summary>Last rendered index in list</summary>
        private int lastRenderedIdx = 0;

        /// <summary>Reuse last render?</summary>
        public bool ReUseLastRender = false;

        /// <summary>Last pixel weight</summary>
        private float[] lastPixWeight;

        /// <summary>Last rendered image</summary>
        private Bitmap lastImage;
        #endregion

        /// <summary>Restores black pixels to a rendered bitmap</summary>
        /// <param name="bmp">Rendered bitmap to get restored</param>
        /// <param name="origBmp">Original bitmap</param>
        public Bitmap RestoreBlackPixels(Bitmap origBmp, Bitmap bmp)
        {
            CLbmpSrc.WriteBitmap(origBmp);
            CLimgFinal1.WriteBitmap(bmp);
            kernelRestoreBlackPixels.Execute(new CLCalc.Program.MemoryObject[] { CLcfgVars, CLbmpSrc, CLimgFinal1, CLimgFinal2 }, new int[] { bmp.Width, bmp.Height });

            return CLimgFinal2.ReadBitmap();
        }
    }
}
