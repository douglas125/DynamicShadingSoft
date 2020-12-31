using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace DynamicShading
{
    public class DynamicShadingCPU
    {
        /// <summary>Color stroke</summary>
        public class ColorStroke
        {
            /// <summary>Constructor</summary>
            public ColorStroke(List<int[]> coords, Color c)
            {
                StrokeColor = c;
                StrokeCoords = coords;
            }

            /// <summary>Stroke coordinates [x,y]</summary>
            public List<int[]> StrokeCoords;
            /// <summary>Stroke color</summary>
            public Color StrokeColor;
            /// <summary>Pixel distance map</summary>
            public float[] DistanceMap;

            /// <summary>Clones this object</summary>
            public ColorStroke Clone()
            {
                List<int[]> newc = new List<int[]>();
                foreach (int[] k in this.StrokeCoords) newc.Add(new int[] { k[0], k[1] });

                ColorStroke cs = new ColorStroke(newc, this.StrokeColor);

                if (DistanceMap != null) cs.DistanceMap = (float[])this.DistanceMap.Clone();
                return cs;
            }
        }

        /// <summary>Clones this object</summary>
        public DynamicShadingCPU Clone()
        {
            List<int> newEdges = new List<int>();
            foreach (int k in this._edges) newEdges.Add(k);
            DynamicShadingCPU ans = new DynamicShadingCPU(newEdges, ImageWidth, ImageHeight);

            foreach (ColorStroke cs in this._strokes) ans._strokes.Add(cs.Clone());

            return ans;
        }

        /// <summary>Image edges</summary>
        public List<int> _edges;
        /// <summary>Gets Width of this image</summary>
        public int ImageWidth { get; set; }
        /// <summary>Gets height of this image</summary>
        public int ImageHeight { get; set; }

        /// <summary>Strokes used to color the image</summary>
        public List<ColorStroke> _strokes = new List<ColorStroke>();

        /// <summary>Max number of iterations</summary>
        public int MAXITER;

        /// <summary>Create new dynamic shading object for CPU processing</summary>
        /// <param name="edges">Edge map list</param>
        /// <param name="w">Image width</param>
        /// <param name="h">Image height</param>
        public DynamicShadingCPU(List<int> edges, int w, int h)
        {
            ImageWidth = w;
            ImageHeight = h;
            _edges = edges;

            MAXITER = Math.Min(w, h);
        }

        #region Helper functions for the UI

        /// <summary>Retrieves stroke closest to XY position</summary>
        public ColorStroke getClosestStroke(int x, int y, out float dist)
        {
            if (x < 0 || y < 0 || x >= ImageWidth || y >= ImageHeight || _strokes.Count == 0)
            {
                dist = float.PositiveInfinity;
                return null;
            }
            
            ColorStroke closest = _strokes[0];
            float smallestDist = float.MaxValue;

            for (int k = 0; k < _strokes.Count; k++)
            {
                ColorStroke cs = _strokes[k];

                if (cs.DistanceMap != null && smallestDist > cs.DistanceMap[x + ImageWidth * y])
                {
                    smallestDist = cs.DistanceMap[x + ImageWidth * y];
                    closest = cs;
                }
            }
            dist = smallestDist;
            return closest;
        }

        #endregion

        #region Public methods
        /// <summary>Adds a new stroke to this drawing</summary>
        /// <param name="coords">Coordinates of the stroke</param>
        /// <param name="c">Stroke color</param>
        public void AddStroke(List<int[]> coords, Color c)
        {
            ColorStroke cs = new ColorStroke(coords, c);
            _strokes.Add(cs);

        }

        /// <summary>Compute distance maps</summary>
        public void ComputeDistanceMaps()
        {
            //for (int j=0;j<_strokes.Count;j++)
            Parallel.For(0, _strokes.Count, j =>
            {
                ColorStroke cs = _strokes[j];
                if (cs.DistanceMap == null) cs.DistanceMap = GetPixelDistances(cs.StrokeCoords, _edges, ImageWidth, ImageHeight, MAXITER);
            });

        }

        /// <summary>Compose image from current strokes</summary>
        /// <returns></returns>
        public Bitmap ComposeImage()
        {
            Bitmap bmpSource = new Bitmap(ImageWidth, ImageHeight);
            BitmapData bitmapData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadWrite, bmpSource.PixelFormat);


            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpSource.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmpSource.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            //int xx, yy;
            //yy = 0;

            Parallel.For(0, heightInPixels, y =>
            //for (int y = 0; y < heightInPixels; y++)
            {
                int yy = y;

                int currentLine = y * bitmapData.Stride;
                int xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {

                    float rC = 255, gC = 255, bC = 255;
                    if (_edges.BinarySearch(xx + ImageWidth * yy) >= 0)
                    {
                        rC = 0; gC = 0; bC = 0;
                    }

                    float totWeight = 0;
                    for (int j = 0; j < _strokes.Count; j++)
                    {
                        ColorStroke cs = _strokes[j];
                        if (cs.DistanceMap != null)
                        {

                            float myWeight = 1.0f / (1e-5f + cs.DistanceMap[xx + ImageWidth * yy]);

                            float rN = cs.StrokeColor.R;
                            float gN = cs.StrokeColor.G;
                            float bN = cs.StrokeColor.B;

                            if (myWeight > 1E-4f * totWeight)
                            {
                                myWeight = (float)Math.Pow(myWeight, 1.7f);

                                if (myWeight + totWeight > 0)
                                {
                                    float temp = 1.0f / (myWeight + totWeight);

                                    rC = (rN * myWeight + rC * totWeight) * temp;
                                    gC = (gN * myWeight + gC * totWeight) * temp;
                                    bC = (bN * myWeight + bC * totWeight) * temp;

                                    totWeight += myWeight;
                                }

                                if (float.IsNaN(rC))
                                {
                                }
                            }
                        }
                    }

                    pixels[currentLine + x] = (byte)bC;
                    pixels[currentLine + x + 1] = (byte)gC;
                    pixels[currentLine + x + 2] = (byte)rC;
                    pixels[currentLine + x + 3] = 255;


                    xx++;
                }
                yy++;
            });

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bmpSource.UnlockBits(bitmapData);

            return bmpSource;
        }

        #endregion
        
        #region Save/load strokes

        /// <summary>Save entire object in Json</summary>
        /// <param name="file">File to save to</param>
        public void SaveJson(string file)
        {
            string txt = JsonConvert.SerializeObject(this);
            File.WriteAllText(file + ".json", txt);
        }

        /// <summary>Retrieve data from file</summary>
        /// <param name="file">File to load from</param>
        public static DynamicShadingCPU FromJson(string file)
        {
            return JsonConvert.DeserializeObject<DynamicShadingCPU>(File.ReadAllText(file));
        }

        /// <summary>Save strokes to a file</summary>
        /// <param name="curFile">File to save to</param>
        public void SaveStrokes(string curFile)
        {
            if (curFile == "") return;

            using (StreamWriter sw = new StreamWriter(curFile + ".strokes"))
            {
                for (int i = 0; i < _strokes.Count; i++)
                {
                    ColorStroke cs = _strokes[i];

                    sw.WriteLine("Color " + cs.StrokeColor.R.ToString() + " " + cs.StrokeColor.G.ToString() + " " + cs.StrokeColor.B.ToString() + " " + cs.StrokeCoords.Count);
                    foreach (int[] p in cs.StrokeCoords)
                    {
                        sw.WriteLine(p[0] + " " + p[1]);
                    }
                }
            }
        }

        /// <summary>Load strokes from a file</summary>
        /// <param name="file">File to load from</param>
        public void LoadStrokes(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split();
                    Color c = Color.FromArgb(int.Parse(line[1]), int.Parse(line[2]), int.Parse(line[3]));
                    List<int[]> curStrokes = new List<int[]>();
                    int nStrokes = int.Parse(line[4]);
                    for (int k = 0; k < nStrokes; k++)
                    {
                        string[] coords = sr.ReadLine().Split();
                        curStrokes.Add(new int[] { int.Parse(coords[0]), int.Parse(coords[1]) });
                    }

                    _strokes.Add(new ColorStroke(curStrokes, c));
                }
            }
        }

        #endregion

        #region Get list of image edges, bitmap functions in general

        /// <summary>Removes colors from image.</summary>
        /// <param name="bmpSource">Source bitmap</param>
        /// <param name="colorDifThresh">Color difference threshold to consider as not Black and White</param>
        /// <returns></returns>
        public static void RemoveColors(Bitmap bmpSource, int colorDifThresh)
        {
            //Step 1: identify regions in the image that are not B&W

            int W = bmpSource.Width;
            BitmapData bitmapData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadWrite, bmpSource.PixelFormat);
            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpSource.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmpSource.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    int vB = pixels[currentLine + x];
                    int vG = pixels[currentLine + x + 1];
                    int vR = pixels[currentLine + x + 2];

                    if (Math.Abs(vR - vG) > colorDifThresh || Math.Abs(vR - vB) > colorDifThresh || Math.Abs(vB - vG) > colorDifThresh)
                    {
                        pixels[currentLine + x] = 255;
                        pixels[currentLine + x + 1] = 255;
                        pixels[currentLine + x + 2] = 255;
                    }



                    xx++;
                }
                yy++;
            }

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bmpSource.UnlockBits(bitmapData);


        }

        /// <summary>Retrieve color strokes from an image. Remove color strokes from image.</summary>
        /// <param name="bmpSource">Source bitmap</param>
        /// <param name="colorDifThresh">Color difference threshold to consider as not Black and White</param>
        /// <returns></returns>
        public static List<ColorStroke> GetColorStrokes(Bitmap bmpSource, int colorDifThresh)
        {
            //Step 1: identify regions in the image that are not B&W

            //{X, Y, R,G,B} of pixel
            List<int[]> colorPixels = new List<int[]>();

            int W = bmpSource.Width;
            BitmapData bitmapData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadWrite, bmpSource.PixelFormat);
            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpSource.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmpSource.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    int vB = pixels[currentLine + x];
                    int vG = pixels[currentLine + x + 1];
                    int vR = pixels[currentLine + x + 2];

                    if ( Math.Abs(vR-vG) > colorDifThresh || Math.Abs(vR - vB) > colorDifThresh || Math.Abs(vB - vG) > colorDifThresh)
                    {
                        //deserves attention
                        colorPixels.Add(new int[] { xx, yy, vR, vG, vB });

                        pixels[currentLine + x] = 255;
                        pixels[currentLine + x + 1] = 255;
                        pixels[currentLine + x + 2] = 255;
                    }



                    xx++;
                }
                yy++;
            }

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bmpSource.UnlockBits(bitmapData);

            List<ColorStroke> ans = new List<ColorStroke>();

            //Step 2: group regions correctly
            while (colorPixels.Count > 0)
            {
                List<int[]> regionPixels = new List<int[]>();

                regionPixels.Add(colorPixels[colorPixels.Count - 1]);
                colorPixels.RemoveAt(colorPixels.Count - 1);

                bool foundCandidates = true;
                while (foundCandidates)
                {
                    foundCandidates = false;
                    for (int k = 0; k < regionPixels.Count; k++)
                    {
                        List<int[]> regionCandidates = colorPixels.Where(p =>
                            Math.Abs(p[0] - regionPixels[k][0]) < 3 && Math.Abs(p[1] - regionPixels[k][1]) < 3 &&//pixel is near
                            Math.Abs(p[2] - regionPixels[k][2]) + Math.Abs(p[3] - regionPixels[k][3]) + Math.Abs(p[4] - regionPixels[k][4]) < 3*colorDifThresh //color is similar
                            ).ToList<int[]>();

                        if (regionCandidates.Count > 0)
                        {
                            foundCandidates = true;
                            foreach (int[] pt in regionCandidates)
                            {
                                regionPixels.Add(pt);
                                colorPixels.Remove(pt);
                            }
                        }
                        
                    }
                }

                if (regionPixels.Count > 20)
                {
                    //average color
                    int[] avgColor = new int[] { 0, 0, 0 };
                    List<int[]> coords = new List<int[]>();
                    foreach (int[] pt in regionPixels)
                    {
                        for (int k = 0; k < 3; k++) avgColor[k] += pt[k + 2];
                        coords.Add(new int[] { pt[0], pt[1] });
                    }

                    for (int k = 0; k < 3; k++) avgColor[k] /= regionPixels.Count;

                    ColorStroke cs = new ColorStroke(coords, Color.FromArgb(avgColor[0], avgColor[1], avgColor[2]));
                    ans.Add(cs);
                }
            }


            return ans;
        }

        /// <summary>Retrieves a list of impassable pixels from image. Pixels are identified with their index, x+W*y</summary>
        /// <param name="bmpSource">Processed bitmap</param>
        /// <param name="Threshold">Threshold to use when retrieving edge pixels</param>
        public static List<int> RetrieveImageEdges(Bitmap bmpSource, float Threshold)
        {
            int W = bmpSource.Width;
            BitmapData bitmapData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadWrite, bmpSource.PixelFormat);

            List<int> vals = new List<int>();


            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpSource.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmpSource.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            float corrFac = 1.0f / (3.0f * 255.0f);
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    int oldBlue = pixels[currentLine + x];
                    int oldGreen = pixels[currentLine + x + 1];
                    int oldRed = pixels[currentLine + x + 2];

                    //float[] rgb = FcnGetColor(vals[xx + W * yy], min, max);

                    // calculate new pixel value
                    //vals[xx, yy] = (oldBlue + oldGreen + oldRed) * corrFac; //limit range to [0,1];
                    if ((oldBlue + oldGreen + oldRed) * corrFac < Threshold)
                    {
                        vals.Add(xx + W * yy);
                    }

                    if (xx < W-1 && yy > 0)
                    {
                        int bb1 = pixels[currentLine + x + bytesPerPixel];
                        int gg1 = pixels[currentLine + x + bytesPerPixel + 1];
                        int rr1 = pixels[currentLine + x + bytesPerPixel + 2];
                        int bb2 = pixels[currentLine - bitmapData.Stride + x ];
                        int gg2 = pixels[currentLine - bitmapData.Stride + x + 1];
                        int rr2 = pixels[currentLine - bitmapData.Stride + x + 2];
                        if ((bb1 + gg1 + rr1) * corrFac < Threshold && (bb2 + gg2 + rr2) * corrFac < Threshold)
                        {
                            vals.Add(xx + W * yy);
                        }
                    }
                    if (xx > 0 && yy > 0)
                    {
                        int bb1 = pixels[currentLine + x - bytesPerPixel];
                        int gg1 = pixels[currentLine + x - bytesPerPixel + 1];
                        int rr1 = pixels[currentLine + x - bytesPerPixel + 2];
                        int bb2 = pixels[currentLine - bitmapData.Stride + x];
                        int gg2 = pixels[currentLine - bitmapData.Stride + x + 1];
                        int rr2 = pixels[currentLine - bitmapData.Stride + x + 2];
                        if ((bb1 + gg1 + rr1) * corrFac < Threshold && (bb2 + gg2 + rr2) * corrFac < Threshold)
                        {
                            vals.Add(xx + W * yy);
                        }
                    }

                    xx++;
                }
                yy++;
            }

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bmpSource.UnlockBits(bitmapData);

            vals.Sort();

            return vals;
        }

        /// <summary>Retrieves a list of impassable pixels from image. Pixels are identified with their index, x+W*y</summary>
        /// <param name="edges">Edge coordinates - x+W*y</param>
        public static Bitmap RetrieveImageFromEdges(List<int> edges, int W, int H)
        {
            Bitmap processedBitmap = new Bitmap(W, H);
            BitmapData bitmapData = processedBitmap.LockBits(new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite, processedBitmap.PixelFormat);


            int bytesPerPixel = Bitmap.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * processedBitmap.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            //float corrFac = 1.0f / (3.0f * 255.0f);
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    if (edges.BinarySearch(xx + W * yy) < 0)
                    {
                        pixels[currentLine + x] = 255;
                        pixels[currentLine + x + 1] = 255;
                        pixels[currentLine + x + 2] = 255;
                    }
                    else
                    {
                        pixels[currentLine + x] = 0;
                        pixels[currentLine + x + 1] = 0;
                        pixels[currentLine + x + 2] = 0;
                    }
                    pixels[currentLine + x + 3] = 255;

                    xx++;
                }
                yy++;
            }

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            processedBitmap.UnlockBits(bitmapData);


            return processedBitmap;
        }

        /// <summary>Retrieves a bitmap representation of a distance map</summary>
        /// <param name="distMap">Distance map</param>
        /// <param name="W">Image width</param>
        /// <param name="H">Image height</param>
        /// <returns></returns>
        public static Bitmap RetrieveDistanceBitmap(float[] distMap, int W, int H)
        {
            Bitmap processedBitmap = new Bitmap(W, H);
            BitmapData bitmapData = processedBitmap.LockBits(new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite, processedBitmap.PixelFormat);

            float max = 0;
            foreach (float ff in distMap)
            {
                if (ff != float.MaxValue && ff > max) max = ff;
            }

            int bytesPerPixel = Bitmap.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * processedBitmap.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            //float corrFac = 1.0f / (3.0f * 255.0f);
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    byte bb = distMap[xx + W * yy] == float.MaxValue ? (byte)255 : (byte)(255.0f * distMap[xx + W * yy] / max);
                    
                    pixels[currentLine + x] = bb;
                    pixels[currentLine + x + 1] = bb;
                    pixels[currentLine + x + 2] = bb;
                    pixels[currentLine + x + 3] = 255;

                    xx++;
                }
                yy++;
            }

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            processedBitmap.UnlockBits(bitmapData);


            return processedBitmap;
        }
        #endregion

        #region Otsu threshold method

        /// <summary>Retrieves a list of impassable pixels from image. Pixels are identified with their index, x+W*y</summary>
        /// <param name="bmpSource">Processed bitmap</param>
        public static int[] getHistogram(Bitmap bmpSource)
        {
            int W = bmpSource.Width;
            BitmapData bitmapData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadWrite, bmpSource.PixelFormat);

            int[] histogram = new int[256];


            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpSource.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmpSource.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            float corrFac = 1.0f / (3.0f * 255.0f);
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    int oldBlue = pixels[currentLine + x];
                    int oldGreen = pixels[currentLine + x + 1];
                    int oldRed = pixels[currentLine + x + 2];

                    //float[] rgb = FcnGetColor(vals[xx + W * yy], min, max);


                    // calculate new pixel value
                    int v = (int)Math.Round(255 * (oldBlue + oldGreen + oldRed) * corrFac); //limit range to [0,1];
                    if (v < 0) v = 0;
                    if (v > 255) v = 255;

                    histogram[v]++;

                    xx++;
                }
                yy++;
            }

            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bmpSource.UnlockBits(bitmapData);

            return histogram;
        }

        // function is used to compute the q values in the equation
        private static float Px(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += hist[i];

            return (float)sum;
        }

        // function is used to compute the mean values in the equation (mu)
        private static float Mx(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += i * hist[i];

            return (float)sum;
        }

        // finds the maximum element in a vector
        private static int findMax(float[] vec, int n)
        {
            float maxVec = 0;
            int idx = 0;
            int i;

            for (i = 1; i < n - 1; i++)
            {
                if (vec[i] > maxVec)
                {
                    maxVec = vec[i];
                    idx = i;
                }
            }
            return idx;
        }

        // find otsu threshold
        public static float getOtsuThreshold(Bitmap bmp)
        {
            byte t = 0;
            float[] vet = new float[256];
            vet.Initialize();

            float p1, p2, p12;
            int k;

            int[] hist = getHistogram(bmp);

            // loop through all possible t values and maximize between class variance
            for (k = 1; k != 255; k++)
            {
                p1 = Px(0, k, hist);
                p2 = Px(k + 1, 255, hist);
                p12 = p1 * p2;
                if (p12 == 0)
                    p12 = 1;
                float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                vet[k] = (float)diff * diff / p12;
                //vet[k] = (float)Math.Pow((Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1), 2) / p12;
            }

            t = (byte)findMax(vet, 256);

            return (float)t / 255.0f;
        }

        #endregion

        #region Line distance computation

        /// <summary>Retrieves distance map in image of coords. dist[x,y] = dist[x+W*y]</summary>
        /// <param name="coords">Stroke coordinates</param>
        /// <param name="edgePts">Image edges</param>
        /// <param name="W">Image height</param>
        /// <param name="W">Image width</param>
        /// <returns></returns>
        public static float[] GetPixelDistances(List<int[]> coords, List<int> edgePts, int W, int H, int MAXITER)
        {
            int dMapLen = W*H;
            float[] distMap = new float[dMapLen];
            for (int k = 0; k < dMapLen; k++) distMap[k] = float.MaxValue;

            //keep track of visited pixels - the ones I'm sure I dont need to go back anymore
            List<int> visited = new List<int>();

            //block edges
            for (int k = 0; k < edgePts.Count; k++)
            {
                //insert, but keep list sorted
                int id = visited.BinarySearch(edgePts[k]);
                if (id < 0) visited.Insert(~id, edgePts[k]);
            }

            //retrieve zero distance, initialize queue
            List<int[]> queue = new List<int[]>();
            List<float> distances = new List<float>();
            for (int k = 0; k < coords.Count; k++)
            {
                int valIns = coords[k][0] + W * coords[k][1];
                distMap[valIns] = 0;
                queue.Add(new int[] { coords[k][0], coords[k][1] });
                distances.Add(0);

                //insert, but keep list sorted
                int id = visited.BinarySearch(valIns);
                if (id < 0) visited.Insert(~id, valIns);
            }


            int prevK = distances.Count;
            int n = 0;
            while (queue.Count > 0)
            {
                n++;

                int idVisited;

                //pick the pixel in queue closest to the curve
                int curx = queue[queue.Count - 1][0], cury = queue[queue.Count - 1][1];
                float baseDist = distMap[curx + W * cury];


                queue.RemoveAt(queue.Count - 1);
                distances.RemoveAt(distances.Count - 1);
                prevK--;


                #region parallel attempt
                ////waiting lists to add to places
                //List<int[]> waitQueueInsert = new List<int[]>();
                //List<float> waitDistInsert = new List<float>();
                //for (int k = 0; k < 9; k++)
                //{
                //    waitQueueInsert.Add(null);
                //    waitDistInsert.Add(0);
                //}

                //Parallel.For(0, 3, xi =>
                ////for (int xi = 0; xi < 3; xi++)
                //{
                //    int xx = xi - 1;

                //    int curxx = curx + xx;
                //    for (int yy = -1; yy <= 1; yy++)
                //    {
                //        int curyy = cury + yy;

                //        float dist = (float)Math.Sqrt(Math.Abs(xx) + Math.Abs(yy));
                //        if (xx != 0 || yy != 0)
                //        {
                //            int idVisitedd = visited.BinarySearch(curxx + W * curyy);
                //            if (curxx < W && curxx >= 0 && curyy >= 0 && curyy < H && idVisitedd < 0)
                //            {
                //                //compute local distance
                //                float newDist = baseDist + dist;
                //                float curDist = distMap[curxx + W * curyy];

                //                if (newDist < curDist)
                //                {
                //                    distMap[curxx + W * curyy] = newDist;


                //                    //queue.Insert(idQueue, new int[] { curx, cury });
                //                    //distances.Insert(idQueue, -newDist);
                //                    waitQueueInsert[1 + xx + 3 * (1 + yy)] = new int[] { curxx, curyy };
                //                    waitDistInsert[1 + xx + 3 * (1 + yy)] = newDist;

                //                }
                //            }
                //        }
                //    }
                //});

                //for (int k = 0; k < 9; k++)
                //{
                //    if (waitQueueInsert[k] != null)
                //    {
                //        //keep distances sorted descending
                //        int idQueue = distances.BinarySearch(-waitDistInsert[k]);
                //        if (idQueue < 0) idQueue = ~idQueue;

                //        queue.Insert(idQueue, waitQueueInsert[k]);
                //        distances.Insert(idQueue, -waitDistInsert[k]);
                //    }
                //}
                #endregion

                //4 nearest neighbors
                curx++;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (curx < W && idVisited < 0)
                {
                    //compute local distance
                    float newDist = baseDist + 1.0f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                curx -= 2;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (curx >= 0 && idVisited < 0)
                {
                    float newDist = baseDist + 1.0f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                curx++;
                cury++;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (cury < H && idVisited < 0)
                {
                    float newDist = baseDist + 1.0f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                cury -= 2;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (cury >= 0 && idVisited < 0)
                {
                    float newDist = baseDist + 1.0f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }


                //diagonals
                curx++;
                cury += 2;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (curx < W && cury < H && idVisited < 0)
                {
                    float newDist = baseDist + 1.41421f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = baseDist + 1.0f;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                curx -= 2;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (curx >= 0 && cury < H && idVisited < 0)
                {
                    float newDist = baseDist + 1.41421f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                cury -= 2;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (curx >= 0 && cury >= 0 && idVisited < 0)
                {
                    float newDist = baseDist + 1.41421f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                curx += 2;
                idVisited = visited.BinarySearch(curx + W * cury);
                if (curx < W && cury >= 0 && idVisited < 0)
                {
                    float newDist = baseDist + 1.41421f;
                    float curDist = distMap[curx + W * cury];

                    if (newDist < curDist)
                    {
                        distMap[curx + W * cury] = newDist;

                        //keep distances sorted descending
                        int idQueue = distances.BinarySearch(-newDist);
                        if (idQueue < 0) idQueue = ~idQueue;

                        queue.Insert(idQueue, new int[] { curx, cury });
                        distances.Insert(idQueue, -newDist);
                    }
                }

                //pixels whose distance is smaller than baseDist+1 should be marked as visited - their value will not go down anymore
                for (int k = Math.Min(distances.Count - prevK - 1, distances.Count - 1); k >= 0 && -distances[k] <= baseDist + 1; k--)
                {
                    int val = queue[k][0] + W * queue[k][1];
                    idVisited = visited.BinarySearch(val);
                    if (idVisited < 0) visited.Insert(~idVisited, val);
                    prevK++;
                }
            }


            return distMap;
        }

        #endregion

        #region HSL to RGB and vice-versa - change region color

        /// <summary>Equalize RGB histogram in conventional way</summary>
        /// <param name="bmpSource">Bitmap to get equalized. Gets replaced by the equalized version</param>
        public static void EqualizeHistogram(Bitmap bmpSource)
        {
            //Computes histogram
            int N = 1024;
            float[] histLuminance = new float[N];

            int W = bmpSource.Width;
            BitmapData bitmapData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadWrite, bmpSource.PixelFormat);
            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpSource.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmpSource.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            int xx, yy;
            yy = 0;

            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    float B = pixels[currentLine+x];
                    float G = pixels[currentLine + x + 1];
                    float R = pixels[currentLine + x + 2];

                    float[] hsl = RGBtoHSL(R * 0.00392156862745098f, G * 0.00392156862745098f, B * 0.00392156862745098f);

                    histLuminance[(int)((N - 1) * hsl[2])]++;


                    xx++;
                }
                yy++;
            }


            //Compute histogram integrals in-place
            for (int i = 1; i < N; i++) histLuminance[i] += histLuminance[i - 1];

            float scale = 1.0f / histLuminance[N - 1];

            //Scales histograms
            for (int i = 0; i < N; i++) histLuminance[i] *= scale;

            //Transformation: intensity I becomes histX[I]*scaleX
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                xx = 0;
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    float B = pixels[currentLine + x];
                    float G = pixels[currentLine + x + 1];
                    float R = pixels[currentLine + x + 2];

                    float[] hsl = RGBtoHSL(R * 0.00392156862745098f, G * 0.00392156862745098f, B * 0.00392156862745098f);

                    hsl[2] = histLuminance[(int)((N - 1) * hsl[2])];

                    float[] rgb = HSLtoRGB(hsl[0], hsl[1], hsl[2]);

                    if (rgb[0] < 0) rgb[0] = 0;
                    if (rgb[1] < 0) rgb[1] = 0;
                    if (rgb[2] < 0) rgb[2] = 0;
                    if (rgb[0] > 1) rgb[0] = 1;
                    if (rgb[1] > 1) rgb[1] = 1;
                    if (rgb[2] > 1) rgb[2] = 1;


                    rgb[0] *= 255.0f;
                    rgb[1] *= 255.0f;
                    rgb[2] *= 255.0f;


                    pixels[currentLine + x] = (byte)(rgb[2]);
                    pixels[1 + currentLine + x] = (byte)(rgb[1]);
                    pixels[2 + currentLine + x] = (byte)(rgb[0]);


                    xx++;
                }
                yy++;
            }


            // copy modified bytes back
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bmpSource.UnlockBits(bitmapData);
        }

        /// <summary>Change color schema of a region</summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="target">Target color</param>
        public void ChangeRegionColor(int x, int y, Color target)
        {
            //find color strokes that have business in the region
            List<ColorStroke> lstcs = new List<ColorStroke>();

            foreach (ColorStroke cs in _strokes)
                if (cs.DistanceMap != null && cs.DistanceMap[x + ImageWidth * y] != float.MaxValue)
                    lstcs.Add(cs);

            //retrieve HSL of target color
            float temp = 1.0f/255.0f;
            float[] hslTarget = RGBtoHSL(temp * target.R, temp * target.G, temp * target.B);

            foreach (ColorStroke cs in lstcs)
            {
                //retrieve stroke HSL
                float[] hslStroke = RGBtoHSL(temp * cs.StrokeColor.R, temp * cs.StrokeColor.G, temp * cs.StrokeColor.B);

                //map HUE to target
                hslStroke[0] = hslTarget[0];
                //and saturance
                hslStroke[1] = hslTarget[1];

                //retrieve target RGB for region
                float[] rgb = HSLtoRGB(hslStroke[0], hslStroke[1], hslStroke[2]);

                byte newR = (byte)(255.0f * rgb[0]);
                byte newG = (byte)(255.0f * rgb[1]);
                byte newB = (byte)(255.0f * rgb[2]);

                cs.StrokeColor = Color.FromArgb(newR, newG, newB);
            }

        }

        private static float[] RGBtoHSL(float r, float g, float b)
        {
            // r,b and b are assumed to be in the range 0...1
            float luminance = r * 0.299f + g * 0.587f + b * 0.114f;
            float u = -r * 0.1471376975169300226f - g * 0.2888623024830699774f + b * 0.436f;
            float v = r * 0.615f - g * 0.514985734664764622f - b * 0.100014265335235378f;
            float hue = (float)Math.Atan2(v, u);
            float saturation = (float)Math.Sqrt(u * u + v * v);

            return new float[] { hue, saturation, luminance };
        }

        private static float[] HSLtoRGB(float hue, float saturation, float luminance)
        {
            // hue is an angle in radians (-Pi...Pi)
            // for saturation the range 0...1/sqrt(2) equals 0% ... 100%
            // luminance is in the range 0...1
            float u = (float)(Math.Cos(hue) * saturation);
            float v = (float)(Math.Sin(hue) * saturation);
            float r = luminance + 1.139837398373983740f * v;
            float g = luminance - 0.3946517043589703515f * u - 0.5805986066674976801f * v;
            float b = luminance + 2.03211091743119266f * u;

            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
            if (r > 1) r = 1;
            if (g > 1) g = 1;
            if (b > 1) b = 1;

            return new float[] { r, g, b };
        }

        #endregion
    }
}
