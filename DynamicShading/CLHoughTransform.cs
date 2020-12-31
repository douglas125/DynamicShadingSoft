using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

using OpenCLTemplate;

namespace OpenCLTemplate.ImageProcessing
{
    /// <summary>Implements OpenCL accelerated generalized Hough transform</summary>
    public class CLHoughTransform
    {
        #region References

        /*
         * 
         * [1] BALLARD, D.H., "Generalizing the Hough Transform to Detect Arbitrary Shapes", Pattern Recognition, Vol.13, No.2, p.111-122, 1981
         * 
         * [2] BISWAS, P.K., "Digital Image Processing", NPTel Online Course, IIT Kharagpur, available at http://nptel.iitm.ac.in/index.php accessed in March - 2011.
         * 
         * [3] HOUGH V., PAUL C., Method and means for recognizing complex patterns. United States Patent 3069654. 

         */

        #endregion

        #region Hough transform for a particular image size

        #region Configuration Properties
        /// <summary>Gets or sets if this Hough transform will use Median Filter to process images. Can be slow.</summary>
        public bool UseMedianFilter { get; set; }

        /// <summary>Shrink image by 3 before analyzing?</summary>
        private bool ShrinkImageBy3 = false;

        /// <summary>Use image thinning?</summary>
        public bool UseThinning { get; set; }

        /// <summary>Apply close and opening morphologic operations to image?</summary>
        public bool UseCloseOpen { get; set; }

        /// <summary>Gets or sets Sobel edge detector threshold</summary>
        public byte SobelThreshold { get; set; }
        /// <summary>OpenCL block size. BLOCKSIZE² should be less than MAX_LOCALWORKGROUPSIZE</summary>
        public int OpenCLBLOCKSIZE { get; set; }
        #endregion

        #region Private information and OpenCL variables
        /// <summary>Image dimensions</summary>
        private int[] bmpDim;
        /// <summary>Bitmap after sobel operation</summary>
        private byte[] SobelImg;
        /// <summary>Thresholded points</summary>
        private int[] ThresholdedPoints;
        /// <summary>Hough transform data</summary>
        private int[] HoughTransform;


        /// <summary>Bitmap in OpenCL memory before shrink</summary>
        private CLCalc.Program.Image2D CLbmpBeforeShrink;        

        /// <summary>Bitmap in OpenCL memory</summary>
        private CLCalc.Program.Image2D CLbmp;        
        /// <summary>Bitmap dimensions in OpenCL memory</summary>
        private CLCalc.Program.Variable CLbmpDim;
        /// <summary>Bitmap in OpenCL memory after Median filter or original if no median filter is to be used</summary>
        private CLCalc.Program.Image2D CLbmpMedian;
        /// <summary>Binary bitmap after sobel operation in Device memory</summary>
        private CLCalc.Program.Variable CLSobelImg;
        /// <summary>Binary bitmap to use for thinning after sobel operation in Device memory</summary>
        private CLCalc.Program.Variable CLSobelImgThinning;

        /// <summary>Sobel threshold in OpenCL memory</summary>
        private CLCalc.Program.Variable CLSobelThresh;
        /// <summary>Thresholded points in Device memory</summary>
        private CLCalc.Program.Variable CLThresholdedPoints;

        /// <summary>Hough transform data in Device memory</summary>
        private CLCalc.Program.Variable CLHoughTransformData;


        /// <summary>Geometry data in Device memory</summary>
        private CLCalc.Program.Variable CLGeom;
        /// <summary>Number of points of Geometry in Device memory</summary>
        private CLCalc.Program.Variable CLGeometryCount;

        /// <summary>Maximum number of points of interest</summary>
        public static int MAXNUMPTSOFINTEREST = 75000; //10000

        /// <summary>Number of points of interest</summary>
        private CLCalc.Program.Variable CLNumPtsOfInterest;
        /// <summary>Points of interest in OpenCL memory</summary>
        private CLCalc.Program.Variable CLPtsOfInterest;
        /// <summary>Weights of points of interest in OpenCL memory</summary>
        private CLCalc.Program.Variable CLPtsOfInterestWeights;
        /// <summary>Points of interest threshold</summary>
        private CLCalc.Program.Variable CLPtsOfInterestThreshold;

        /// <summary>Open/Close morphological operations window sizes</summary>
        private CLCalc.Program.Variable CLOpenCloseSize;

        /// <summary>Hough line transform data</summary>
        private int[] HoughLineTransformData;
        /// <summary>Hough Line transform data in OpenCL memory</summary>
        private CLCalc.Program.Variable CLHoughLineTransform;
        /// <summary>Dimensions of line Hough transform</summary>
        private int _nTheta, _nRho;
        /// <summary>Precomputed values in device memory</summary>
        private CLCalc.Program.Variable CLTheta, CLRho, CLcosTheta, CLsinTheta;
        /// <summary>Precomputed values</summary>
        float[] cosTheta, sinTheta;

        #endregion

        #region Constructors and initialization
        /// <summary>Constructor. Enables OpenCL and compiles kernels if necessary.
        /// Specializes at one particular image size.</summary>
        /// <param name="Width">Image width</param>
        /// <param name="Height">Image height</param>
        /// <param name="ShrinkBy3">Shrink image by 3 before processing?</param>
        public CLHoughTransform(int Width, int Height, bool ShrinkBy3)
        {
            this.ShrinkImageBy3 = ShrinkBy3;
            if (!ShrinkBy3) InitHoughTransformVars(new Bitmap(Width, Height));
            else
            {
                InitHoughTransformVars(new Bitmap(Width / 3, Height / 3));
                CLbmpBeforeShrink = new CLCalc.Program.Image2D(new Bitmap(Width, Height));
            }
        }

        /// <summary>Constructor. Enables OpenCL and compiles kernels if necessary.
        /// Specializes at one particular image size.</summary>
        /// <param name="ShrinkBy3">Shrink image by 3 before processing?</param>
        public CLHoughTransform(Bitmap bmp, bool ShrinkBy3)
        {
            this.ShrinkImageBy3 = ShrinkBy3;
            if (!ShrinkBy3) InitHoughTransformVars(bmp);
            else
            {
                InitHoughTransformVars(new Bitmap(bmp.Width / 3, bmp.Height / 3));
                CLbmpBeforeShrink = new CLCalc.Program.Image2D(bmp);
            }
        }

        /// <summary>Initializes this instance's variables</summary>
        private void InitHoughTransformVars(Bitmap bmp)
        {
            InitCL();

            SobelThreshold = 140;
            UseMedianFilter = true;
            UseThinning = true;
            UseCloseOpen = false;

            bmpDim = new int[] { bmp.Width, bmp.Height };
            CLbmpDim = new CLCalc.Program.Variable(bmpDim);

            CLbmp = new CLCalc.Program.Image2D(bmp);
            CLbmpMedian = new CLCalc.Program.Image2D(bmp);

            SobelImg = new byte[bmpDim[0] * bmpDim[1]];
            for (int i = 0; i < SobelImg.Length; i++) SobelImg[i] = 255;
            CLSobelImg = new CLCalc.Program.Variable(SobelImg);
            CLSobelImgThinning = new CLCalc.Program.Variable(SobelImg);

            CLSobelThresh = new CLCalc.Program.Variable(new int[] { this.SobelThreshold });

            OpenCLBLOCKSIZE = 16;

            HoughTransform = new int[bmpDim[0] * bmpDim[1]];
            CLHoughTransformData = new CLCalc.Program.Variable(HoughTransform);

            //Geometry
            CLGeom = new CLCalc.Program.Variable(new int[800]);
            CLGeometryCount = new CLCalc.Program.Variable(new int[1]);

            //Points of interes
            CLNumPtsOfInterest = new CLCalc.Program.Variable(new int[1]);
            CLPtsOfInterestThreshold = new CLCalc.Program.Variable(new int[1]);
            CLPtsOfInterest = new CLCalc.Program.Variable(new int[MAXNUMPTSOFINTEREST << 1]);
            CLPtsOfInterestWeights = new CLCalc.Program.Variable(new int[MAXNUMPTSOFINTEREST]);
            
            //Morphological operations window sizes
            CLOpenCloseSize = new CLCalc.Program.Variable(new int[1]);
        }
        #endregion

        #region Image filtering and edge detection
        /// <summary>Reads bitmap and pre-processes it to identify shapes.</summary>
        /// <param name="bmp">Bitmap to process</param>
        public void ReadBitmap(Bitmap bmp)
        {
            CLCalc.Program.MemoryObject[] args;

            if (this.ShrinkImageBy3)
            {
                CLbmpBeforeShrink.WriteBitmap(bmp);
                if (UseMedianFilter) args = new CLCalc.Program.MemoryObject[] { CLbmpBeforeShrink, CLbmp };
                else args = new CLCalc.Program.MemoryObject[] { CLbmpBeforeShrink, CLbmpMedian };

                kernelShrinkBmpBy3.Execute(args, new int[] { bmpDim[0], bmpDim[1] });
            }

            if (this.UseMedianFilter)
            {
                if (!ShrinkImageBy3) CLbmp.WriteBitmap(bmp);

                int groupSizeX = (bmp.Width - 4) / BLOCK_SIZE;
                int groupSizeY = (bmp.Height - 4) / BLOCK_SIZE;
                args = new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpMedian };
                kernelmedianFilter.Execute(args, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
            }
            else
            {
                //No filter, just writes to Median bitmap
                if (!ShrinkImageBy3) CLbmpMedian.WriteBitmap(bmp);
            }

            //Sobel edge detection
            CLSobelThresh.WriteToDevice(new int[] { this.SobelThreshold });

            args = new CLCalc.Program.MemoryObject[] { CLbmpMedian, CLSobelImg, CLSobelThresh };
            kernelSobelBytes.Execute(args, new int[] { bmpDim[0] - 2, bmpDim[1] - 2 });

            if (UseCloseOpen)
            {
                //Closes with 3x3 and Opens with 2x2

                //Uses the thinning buffer as temporary
                CLCalc.Program.MemoryObject[] args1 = new CLCalc.Program.MemoryObject[] { CLSobelImg, CLSobelImgThinning, CLOpenCloseSize };
                CLCalc.Program.MemoryObject[] args2 = new CLCalc.Program.MemoryObject[] { CLSobelImgThinning, CLSobelImg, CLOpenCloseSize };

                //Closing
                CLOpenCloseSize.WriteToDevice(new int[] { 3 });
                kernelDilateByte.Execute(args1, new int[] { bmpDim[0] - 1, bmpDim[1] - 1 });
                kernelErodeByte.Execute(args2, new int[] { bmpDim[0] - 1, bmpDim[1] - 1 });

                //Opening
                CLOpenCloseSize.WriteToDevice(new int[] { 2 });
                kernelErodeByte.Execute(args1, new int[] { bmpDim[0] - 1, bmpDim[1] - 1 });
                kernelDilateByte.Execute(args2, new int[] { bmpDim[0] - 1, bmpDim[1] - 1 });

            }

            //Thinning operation - this is very important
            if (UseThinning)
            {
                int groupSizeX = (bmp.Width - 2) / BLOCK_SIZE;
                int groupSizeY = (bmp.Height - 2) / BLOCK_SIZE;

                CLCalc.Program.Variable[] args0 = new CLCalc.Program.Variable[] { CLSobelImg, CLSobelImgThinning, CLbmpDim };
                CLCalc.Program.Variable[] args1 = new CLCalc.Program.Variable[] { CLSobelImgThinning, CLSobelImg, CLbmpDim };

                kernelImageThinningByte.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                kernelImageThinningByte.Execute(args1, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                kernelImageThinningByte.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                kernelImageThinningByte.Execute(args1, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                kernelImageThinningByte.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                kernelImageThinningByte.Execute(args1, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                kernelImageThinningByte.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });

                //Needs to retrieve thresholded points
                CLSobelImgThinning.ReadFromDeviceTo(SobelImg);
            }
            else
            {
                //Needs to retrieve thresholded points
                CLSobelImg.ReadFromDeviceTo(SobelImg);
            }
            

            List<int> LstThreshPts = new List<int>();

            for (int x = 1; x < bmpDim[0] - 1; x++)
            {
                int yInd = bmpDim[0];
                for (int y = 1; y < bmpDim[1] - 1; y++)
                {
                    if (SobelImg[x + yInd] == 0)
                    {
                        LstThreshPts.Add(x);
                        LstThreshPts.Add(y);
                    }
                    yInd += bmpDim[0];
                }
            }

            if (LstThreshPts.Count > 0)
            {
                ThresholdedPoints = LstThreshPts.ToArray();
                CLThresholdedPoints = new CLCalc.Program.Variable(ThresholdedPoints);
            }

        }


        #endregion

        #region Geometry matching

        /// <summary>Definition of parameters for geometry search</summary>
        public class GeometryFindParameters
        {
            /// <summary>Is this geometry symmetric in X axis?</summary>
            public bool IsSymmetric { get; set; }

            /// <summary>Scale X and Y together (faster) or one at a time?</summary>
            public bool ScaleXYTogether { get; set; }

            /// <summary>What values of angles should be tested?</summary>
            public float[] AnglesToSearch;
            /// <summary>What values of X scales should be tested?</summary>
            public float[] XScales;
            /// <summary>What values of Y scales should be tested?</summary>
            public float[] YScales;

            /// <summary>Number of votes, as % of total geometry vectors, that a point has to have to be considered relevant</summary>
            public float PercentToBeRelevant = 0.43f;

            /// <summary>Creates a new instance of GeometryFindParameters</summary>
            /// <param name="Symmetric">Is the geometry symmetric in X axis?</param>                                                                                                                        
            /// <param name="ScaleXYTogether">Use a global scale factor for geometry (faster) or scale X and Y separatedly?</param>                                                                                                                        
            /// <param name="Scales">Scales to use. Set to {1.0f} to only use the geometry as it is</param>                                                                                                                        
            public GeometryFindParameters(bool Symmetric, bool ScaleXYTogether, float[] Scales)
            {
                this.IsSymmetric = Symmetric;
                this.ScaleXYTogether = ScaleXYTogether;

                float theta0 = 0;
                float thetaf = Symmetric ? 3.14159265358979f : 6.28318530717958f;
                int n = Symmetric ? 360 : 720;

                AnglesToSearch = new float[n];
                float invN = 1/(float)n;
                for (int i = 0; i < n; i++)
                {
                    float val = theta0 + (thetaf - theta0) * (float)i * invN;
                    AnglesToSearch[i] = val;
                }

                XScales = new float[Scales.Length];
                YScales = new float[Scales.Length];
                for (int i = 0; i < Scales.Length; i++)
                {
                    XScales[i] = Scales[i];
                    YScales[i] = Scales[i];
                }
            }
        }

        /// <summary>Finds geometry in currently loaded bitmap. Each element is a float[] containing {centerX centerY weight angle scaleX scaleY numberOfTimesFound}</summary>
        /// <param name="BaseGeometry">Base geometry which generated geometries to find</param>
        /// <param name="Geometries">Geometries to find</param>
        /// <param name="GeomParameters">Geometry modification parameters {angle, XScale, YScale}</param>
        /// <param name="Parameters">Configuration parameters for geometry search</param>
        /// <param name="GeometryDimensions">Geometry dimensions {Width, Height} to use when merging relevant center candidates</param>
        /// <param name="ExistingCenters">Merges existing centers with newfound ones</param>
        public List<float[]> FindGeometry(List<int> BaseGeometry, List<List<int>> Geometries, List<float[]> GeomParameters, GeometryFindParameters Parameters, int[] GeometryDimensions, List<float[]> ExistingCenters)
        {
            //No points to analyze. Just return empty
            if (this.ThresholdedPoints == null || this.ThresholdedPoints.Length == 0) return new List<float[]>();

            //Defines geometry properties
            if (BaseGeometry.Count > CLGeom.OriginalVarLength)
            {
                CLGeom = new CLCalc.Program.Variable(new int[BaseGeometry.Count]);
            }
            int[] geom = new int[CLGeom.OriginalVarLength];


            //Allocates space to store points of interest
            int[] PtsOfInterest = new int[MAXNUMPTSOFINTEREST<<1];
            int[] Weights = new int[MAXNUMPTSOFINTEREST];
            int[] QtdPtsOfInterest = new int[1];

            //Keeps all centers found
            //CLCalc.Program.Event.Clear();
            List<List<float[]>> AllCenters = new List<List<float[]>>();

            for (int indGeom = 0; indGeom < Geometries.Count; indGeom++)
            {
                List<int> curGeom = Geometries[indGeom];
                float[] GeomParams = GeomParameters[indGeom];
                List<float[]> Centers = GetCurGeomCenters(curGeom, Parameters, GeometryDimensions, geom, QtdPtsOfInterest, PtsOfInterest, Weights, GeomParams);
                if (Centers.Count > 0)
                    AllCenters.Add(Centers);
            }


            //Merges all centers  to keep only relevant ones
            List<float[]> MostRelevantCenters = new List<float[]>();
            List<bool> increasedCenterCounter = new List<bool>();

            if (ExistingCenters != null)
            {
                for (int i = 0; i < ExistingCenters.Count; i++)
                {
                    MostRelevantCenters.Add(ExistingCenters[i]);
                    increasedCenterCounter.Add(false);
                }
            }

            foreach (List<float[]> Centers in AllCenters)
            {
                foreach (float[] c in Centers)
                {
                    int ind = -1;
                    for (int i = 0; i < MostRelevantCenters.Count; i++)
                    {
                        float minScaleX = Math.Min(c[4], MostRelevantCenters[i][4])*0.8f;
                        float minScaleY = Math.Min(c[5], MostRelevantCenters[i][5])*0.8f;
                        if (Math.Abs(c[0] - MostRelevantCenters[i][0]) < GeometryDimensions[0] * minScaleX && Math.Abs(c[1] - MostRelevantCenters[i][1]) < GeometryDimensions[1] * minScaleY)
                        {
                            //Found a more relevant center point
                            if (c[2] > MostRelevantCenters[i][2])
                            {
                                for (int k = 0; k < c.Length; k++) MostRelevantCenters[i][k] = c[k];
                            }
                            //If point is less relevant just do nothing

                            //Adds to the number of times found counter if center already existed
                            if (ExistingCenters != null && i < ExistingCenters.Count && !increasedCenterCounter[i])
                            {
                                increasedCenterCounter[i] = true;
                                MostRelevantCenters[i][6]++;
                            }

                            ind = i;
                            i = MostRelevantCenters.Count;
                        }
                    }

                    //Center not found, add it
                    if (ind < 0)
                    {
                        float[] newC = new float[c.Length];
                        for (int k = 0; k < c.Length; k++) newC[k] = c[k];

                        MostRelevantCenters.Add(newC);
                    }
                }
            }

            return MostRelevantCenters;
        }

        /// <summary>Retrieves current geometry centers</summary>
        private List<float[]> GetCurGeomCenters(List<int> curGeom, GeometryFindParameters Parameters, int[] GeometryDimensions, int[] geom, int[] QtdPtsOfInterest, int[] PtsOfInterest, int[] Weights, float[] GeomParameters)
        {
            ////System.Diagnostics.Stopwatch swTot = new System.Diagnostics.Stopwatch();
            ////System.Diagnostics.Stopwatch swInspec = new System.Diagnostics.Stopwatch();
            ////swTot.Start();

            //hough transform arguments
            CLCalc.Program.Variable[] HoughArgs = new CLCalc.Program.Variable[] { CLHoughTransformData, CLbmpDim, CLThresholdedPoints, CLGeom, CLGeometryCount };

            //Writes geometry point count to Device memory
            CLGeometryCount.WriteToDevice(new int[] { curGeom.Count });

            //return new List<float[]>();

            //Copies data to geometry
            for (int i = 0; i < curGeom.Count; i += 2)
            {
                geom[i] = curGeom[i]; geom[i + 1] = curGeom[i + 1];
            }
            //Writes data to Device
            CLGeom.WriteToDevice(geom);

            //swInspec.Start();
            //Clears Hough transform data
            kernelClearHough.Execute(new CLCalc.Program.MemoryObject[] { CLHoughTransformData }, new int[] { this.bmpDim[0], this.bmpDim[1] });

            //Strangely, the __constant didnt help speed up the process
            //Runs Hough transform kernel
            //if (ThresholdedPoints.Length >= (CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Device.MaxConstantBufferSize>>2)) //__constant space limit exceeded, can't load to __constant Device memory
            kernelGeneralizedHough.Execute(HoughArgs, this.ThresholdedPoints.Length >> 1);
            //else 
                //kernelGeneralizedHoughConstTP.Execute(HoughArgs, this.ThresholdedPoints.Length >> 1);
            //CLCalc.Program.Sync(); swInspec.Stop();

            //Retrieves points of interest
            RetrievePtsOfInterestFromHT(CLHoughTransformData, (int)((float)(curGeom.Count >> 1) * Parameters.PercentToBeRelevant), QtdPtsOfInterest, PtsOfInterest, Weights, bmpDim);

            //Retrieves centers
            List<float[]> Centers = new List<float[]>();
            //Point global weight should take into account merges
            List<float> GlobalWeights = new List<float>();

            float xx, yy, weight;
            int indd;

            int MergeIndex;
            for (int i = 0; i < QtdPtsOfInterest[0]; i++)
            {
                indd = i << 1;
                xx = (float)PtsOfInterest[indd];
                yy = (float)PtsOfInterest[indd + 1];
                weight = (float)Weights[i];

                MergeIndex = -1;
                for (int j = Centers.Count - 1; j >= 0; j--)
                {
                    //Scales original geometry dimension with scaleX and scaleY
                    if (Math.Abs(xx - Centers[j][0]) < GeometryDimensions[0] * GeomParameters[1] && Math.Abs(yy - Centers[j][1]) < GeometryDimensions[1] * GeomParameters[2])
                    {
                        ////computes weighted average
                        //float totWeight = weight + Centers[j][2];
                        //float invTotWeight = 1.0f / totWeight;

                        //Centers[j][0] = (Centers[j][0] * Centers[j][2] + xx * weight) * invTotWeight;
                        //Centers[j][1] = (Centers[j][1] * Centers[j][2] + yy * weight) * invTotWeight;

                        //Centers[j][2] = totWeight;


                        //Mode 2: simply keep best point, but saves global weight of point
                        GlobalWeights[j] = Centers[j][2] + weight;

                        if (weight > Centers[j][2])
                        {
                            Centers[j][0] = xx;
                            Centers[j][1] = yy;
                            Centers[j][2] = weight;
                        }

                        MergeIndex = j;
                        j = -1;
                    }
                }

                if (MergeIndex < 0)
                {
                    //Includes geometry modification parameters
                    Centers.Add(new float[] { xx, yy, weight, GeomParameters[0], GeomParameters[1], GeomParameters[2], 1 });
                    
                    //List of global weights
                    GlobalWeights.Add(weight);
                }

            }

            //Scales weights. Saves global weights.
            float temp = 1 / (float)(curGeom.Count >> 1);

            for (int j = 0; j < Centers.Count; j++)
            {
                Centers[j][2] = GlobalWeights[j] * temp;
            }

            //swTot.Stop();

            return Centers;
        }

        /// <summary>Retrieves points of interest from Hough transform</summary>
        private void RetrievePtsOfInterestFromHT(CLCalc.Program.Variable CLHough, int Threshold, int[] QtdPtsOfInterest, int[] PtsOfInterest, int[] Weights, int[] GlobalSize)
        {
            CLCalc.Program.Variable[] PtsOfInterestArgs = new CLCalc.Program.Variable[] { CLHough, CLNumPtsOfInterest, CLPtsOfInterest, CLPtsOfInterestWeights, CLPtsOfInterestThreshold };

            //Retrieves points of interest
            CLNumPtsOfInterest.WriteToDevice(new int[] { 0 });
            
            //Threshold = (int)((float)(curGeom.Count >> 1) * Parameters.PercentToBeRelevant);

            CLPtsOfInterestThreshold.WriteToDevice(new int[] { Threshold });
            kernelRetrievePointsOfInterest.Execute(PtsOfInterestArgs, GlobalSize);

            CLNumPtsOfInterest.ReadFromDeviceTo(QtdPtsOfInterest);
            if (QtdPtsOfInterest[0] > 0)
            {
                //Reads only the needed amount of points. Need to use Cloo for that
                unsafe
                {
                    List<Cloo.ComputeEventBase> ev = new List<Cloo.ComputeEventBase>();
                    fixed (void* ponteiro = PtsOfInterest)
                    {
                        CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Read<int>((Cloo.ComputeBuffer<int>)CLPtsOfInterest.VarPointer, true, 0, QtdPtsOfInterest[0] << 1,
                            (IntPtr)ponteiro, ev);
                    }

                    fixed (void* ponteiro = Weights)
                    {
                        CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Read<int>((Cloo.ComputeBuffer<int>)CLPtsOfInterestWeights.VarPointer, true, 0, QtdPtsOfInterest[0],
                            (IntPtr)ponteiro, ev);
                    }
                }
            }
        }

        #endregion

        #region Finding polygons

        /// <summary>Finds polygons within current Bitmap by locating connected edges.
        /// Each float[] f = {x0 y0 x1 y1 x2 y2 ...} contains the coordinates of the edges of the polygon.
        /// For example, if f.Length = 4, the polygon has four sides</summary>
        public List<float[]> FindPolygons()
        {
            int n = 1;
            int d = (int)Math.Sqrt((float)bmpDim[0] * (float)bmpDim[0] + (float)bmpDim[1] * (float)bmpDim[1]);
            return FindPolygons(n*360, n*d, Math.Min(bmpDim[0], bmpDim[1]) / 8, 0.06f, 9, true, 0.4f);
        }

        /// <summary>Finds polygons within current Bitmap by locating connected edges.
        /// Each float[] f = {x0 y0 x1 y1 x2 y2 ...} contains the coordinates of the edges of the polygon.
        /// For example, if f.Length = 4, the polygon has four sides</summary>
        /// <param name="nPtsTheta">Number of divisions in theta</param>
        /// <param name="nPtsRho">Number of divisions in rho</param>
        /// <param name="MinLineLength">Minimum line length to take into account a line segment</param>
        /// <param name="MaxLineAngles">Maximum allowable angle between polygon lines in radians</param>
        /// <param name="edgeWindowSize">Window size to use when determining if an edge has points nearby. Also used to check if a line segment is valid</param>
        /// <param name="removeLonelyEdges">Remove edges which aren't close to any border?</param>
        /// <param name="RequiredSegmentCompleteness">Required segment completeness to consider line segment as valid</param>
        public List<float[]> FindPolygons(int nPtsTheta, int nPtsRho, int MinLineLength, float MaxLineAngles, int edgeWindowSize, 
            bool removeLonelyEdges, float RequiredSegmentCompleteness)
        {
            //Allocates space to store points of interest
            int[] PtsOfInterest = new int[MAXNUMPTSOFINTEREST << 1];
            int[] Weights = new int[MAXNUMPTSOFINTEREST];
            int[] QtdPtsOfInterest = new int[1];

            float d = (float)Math.Sqrt((float)bmpDim[0] * (float)bmpDim[0] + (float)bmpDim[1] * (float)bmpDim[1]);
            float[] theta = new float[] { -1.5707963267949f + 3.14159265358979f / (float)(nPtsTheta), 1.5707963267949f, nPtsTheta };
            float[] rho = new float[] { -d, d, nPtsRho };

            float invPtsTheta = 1.0f / (nPtsTheta - 1);
            if (HoughLineTransformData == null || _nRho != nPtsRho || _nTheta != nPtsTheta)
            {
                _nRho = nPtsRho; _nTheta = nPtsTheta;

                if (HoughLineTransformData == null)
                {
                    CLTheta = new CLCalc.Program.Variable(new float[3]);
                    CLRho = new CLCalc.Program.Variable(new float[3]);
                }

                HoughLineTransformData = new int[nPtsTheta * nPtsRho];
                CLHoughLineTransform = new CLCalc.Program.Variable(HoughLineTransformData);

                //Precomputes cos and sin
                cosTheta = new float[nPtsTheta];
                sinTheta = new float[nPtsTheta];
                float _curTheta;
                for (int k = 0; k < nPtsTheta; k++)
                {
                    _curTheta = theta[0] + (theta[1] - theta[0]) * (float)k * invPtsTheta;

                    cosTheta[k] = (float)Math.Cos(_curTheta);
                    sinTheta[k] = (float)Math.Sin(_curTheta);
                }

                CLcosTheta = new CLCalc.Program.Variable(cosTheta);
                CLsinTheta = new CLCalc.Program.Variable(sinTheta);
            }

            System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
            sw1.Start();

            CLRho.WriteToDevice(rho);
            CLTheta.WriteToDevice(theta);

            //Computes Hough transform
            //Clears Hough transform data
            kernelClearHough.Execute(new CLCalc.Program.MemoryObject[] { CLHoughLineTransform }, new int[] { nPtsTheta, nPtsRho });

            //Hough transform arguments
            CLCalc.Program.Variable[] HoughArgs = new CLCalc.Program.Variable[] { CLHoughLineTransform, CLThresholdedPoints, CLsinTheta, CLcosTheta, CLTheta, CLRho };
            kernelLineHoughTransform.Execute(HoughArgs, ThresholdedPoints.Length >> 1);

            //Retrieves points of interest
            RetrievePtsOfInterestFromHT(CLHoughLineTransform, MinLineLength, QtdPtsOfInterest, PtsOfInterest, Weights, new int[] { nPtsTheta, nPtsRho });

            sw1.Stop();
            
            //Rho index factor: i = (numPointsRho-1)*(rho[i]-rho0)/(rhof-rho0)
            float rFactor = (rho[1] - rho[0]) / (nPtsRho - 1.0f);



            //Merges line values which are close. int[] { theta, rho, relevance }
            List<int[]> Lines = new List<int[]>();

            //Saves line information cosTheta, sinTheta, rho
            List<float[]> LinesInfo = new List<float[]>();

            //Maximum weight found
            int MaxWFound = 2 * MinLineLength; 

            //Copies thresholded points
            List<int> RemainingThreshPts = new List<int>();
            for (int i = 0; i < ThresholdedPoints.Length; i++) RemainingThreshPts.Add(ThresholdedPoints[i]);

            //Retrieves lines 1 by 1
            while (QtdPtsOfInterest[0] > 0)
            {
                MaxWFound = 0;
                int indMaxLen = -1;
                
                for (int i = 0; i < QtdPtsOfInterest[0]; i++)
                {
                    if (Weights[i] > MaxWFound)
                    {
                        MaxWFound = Weights[i];
                        indMaxLen = i;
                    }
                }

                //theta and rho information
                float cosThetaMax = cosTheta[PtsOfInterest[indMaxLen << 1]], sinThetaMax = sinTheta[PtsOfInterest[indMaxLen << 1]];
                float rhoMax = rho[0] + PtsOfInterest[1 + (indMaxLen << 1)] * rFactor;

                //Includes best line found
                Lines.Add(new int[] { PtsOfInterest[indMaxLen << 1], PtsOfInterest[1 + (indMaxLen << 1)], Weights[indMaxLen] });
                LinesInfo.Add(new float[] { cosThetaMax, sinThetaMax, rhoMax });

                //Creates a new vector containing only points of interest which don't belong to the line
                for (int i = 0; i < RemainingThreshPts.Count; i+=2)
                {
                    float x = RemainingThreshPts[i];
                    float y = RemainingThreshPts[i+1];

                    if (Math.Abs(x * cosThetaMax + y * sinThetaMax - rhoMax) <= 2 * rFactor)
                    {
                        RemainingThreshPts.RemoveRange(i, 2);
                        i-=2;
                    }
                }

                if (RemainingThreshPts.Count >= MinLineLength)
                {
                    sw1.Start();
                    
                    //recomputes hough transfosm
                    CLCalc.Program.Variable RemThreshPts = new CLCalc.Program.Variable(RemainingThreshPts.ToArray());
                    //Clears Hough transform data
                    kernelClearHough.Execute(new CLCalc.Program.MemoryObject[] { CLHoughLineTransform }, new int[] { nPtsTheta, nPtsRho });
                    HoughArgs = new CLCalc.Program.Variable[] { CLHoughLineTransform, RemThreshPts, CLsinTheta, CLcosTheta, CLTheta, CLRho };
                    kernelLineHoughTransform.Execute(HoughArgs, RemainingThreshPts.Count >> 1);

                    //Retrieves points of interest
                    RetrievePtsOfInterestFromHT(CLHoughLineTransform, MinLineLength, QtdPtsOfInterest, PtsOfInterest, Weights, new int[] { nPtsTheta, nPtsRho });
                    
                    sw1.Stop();
                }
                else QtdPtsOfInterest[0] = 0;
            }

            //Window to use to check validity of lines and edges
            int windowLimits = (edgeWindowSize - 1) >> 1;

            //Finds edges. Third component is edge relevance
            List<float[]> edges = new List<float[]>();

            //Minimum angle distance in index
            int MinAngDist = (int)Math.Round(MaxLineAngles / ((theta[1] - theta[0]) * invPtsTheta));
            
            for (int i = 1; i < Lines.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (Math.Abs(Lines[i][0] - Lines[j][0]) > MinAngDist && Math.Abs(Lines[i][0] - Lines[j][0]) < nPtsTheta - 1 - MinAngDist)
                    {
                        float a11 = cosTheta[Lines[i][0]], a12 = sinTheta[Lines[i][0]];
                        float a21 = cosTheta[Lines[j][0]], a22 = sinTheta[Lines[j][0]];
                        
                        float r1 = rho[0] + Lines[i][1] * rFactor;
                        float r2 = rho[0] + Lines[j][1] * rFactor;

                        float invDet = 1.0f / (a11 * a22 - a12 * a21);

                        float x = invDet * (a22 * r1 - a12 * r2);
                        float y = invDet * (-a21 * r1 + a11 * r2);

                        if (x >= windowLimits && (int)x < bmpDim[0] - windowLimits && y >= windowLimits && (int)y < bmpDim[1] - windowLimits)
                        {
                            edges.Add(new float[] { x, y, Lines[i][2] + Lines[j][2] });
                        }
                    }
                }
            }


            //Found edges. Now removes edges which are placed in an empty space
            //SobelImg is populated with thinned image
            if (removeLonelyEdges)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    bool remove = true;
                    for (int p = -windowLimits; p <= windowLimits; p++)
                    {
                        for (int q = -windowLimits; q <= windowLimits; q++)
                        {
                            int x = (int)(edges[i][0] + p);
                            int y = (int)(edges[i][1] + q);
                            if (SobelImg[x + bmpDim[0] * y] == 0)
                            {
                                remove = false;
                                p = windowLimits + 1; q = windowLimits + 1;
                            }
                        }
                    }
                    if (remove)
                    {
                        edges.RemoveAt(i);
                        i--;
                    }
                }
            }

            //Sorts edges per X, then Y
            edges.Sort(SortByXY);

            //Checks what edges belong to what lines
            List<List<int>> edgesInLines = new List<List<int>>();
            foreach (float[] L in LinesInfo)
            {
                List<int> edgesInLine = new List<int>();
                for (int p=0; p<edges.Count;p++)
                {
                    //if edge belongs to line
                    if (Math.Abs(edges[p][0] * L[0] + edges[p][1] * L[1] - L[2]) <= 3 * rFactor)
                    {
                        edgesInLine.Add(p);
                    }
                }
                edgesInLines.Add(edgesInLine);
            }

            //Checks valid line segments. each int[] contains vertex indexes for a valid segment and original line identifier
            //int[3] {vertex0 vertex1 originalLine}
            List<int[]> ValidSegments = new List<int[]>();
            float minLineLen2 = (float)MinLineLength * (float)MinLineLength;

            for (int i = 0; i < edgesInLines.Count; i++)
            {
                for (int k = 1; k < edgesInLines[i].Count; k++)
                {
                    float x0 = edges[edgesInLines[i][k - 1]][0];
                    float y0 = edges[edgesInLines[i][k - 1]][1];
                    float xf = edges[edgesInLines[i][k]][0];
                    float yf = edges[edgesInLines[i][k]][1];

                    float Tx = xf - x0;
                    float Ty = yf - y0;
                    float Length = Tx * Tx + Ty * Ty;

                    //if (Length >= minLineLen2)
                    {
                        //Length now has the length of the segment in pixels
                        Length = (float)Math.Sqrt(Length);

                        //Normalizes tangent vector
                        Tx /= Length;
                        Ty /= Length;

                        //checks completeness of line segment
                        int BorderPtsFoundInSegmentCandidate = 0;
                        for (int p = windowLimits+1; p < Length - windowLimits; p++)
                        {
                            int curX = (int)(x0 + p * Tx);
                            int curY = (int)(y0 + p * Ty);
                            for (int q = -windowLimits; q <= windowLimits; q++)
                            {
                                for (int r = -windowLimits; r <= windowLimits; r++)
                                {
                                    if (SobelImg[curX+r + bmpDim[0] * (q+curY)] == 0)
                                    {
                                        q = windowLimits + 1; r = windowLimits + 1;
                                        BorderPtsFoundInSegmentCandidate++;
                                    }
                                }
                            }
                        }

                        //Percentage of segment which is filled with something
                        float segmentCompleteness = (float)BorderPtsFoundInSegmentCandidate / (Length - 2 * windowLimits);

                        if (segmentCompleteness >= RequiredSegmentCompleteness)
                        {
                            ValidSegments.Add(new int[] { edgesInLines[i][k - 1], edgesInLines[i][k], i });
                        }
                    }
                }
            }

            //Computes to what segments each edge belongs
            List<List<int>> SegmentsOwnEdges = new List<List<int>>();
            for (int i = 0; i < edges.Count; i++)
            {
                List<int> SegmentsOwnEdge=new List<int>();
                for (int j = 0; j < ValidSegments.Count; j++)
                {
                    if (ValidSegments[j][0] == i || ValidSegments[j][1] == i)
                        SegmentsOwnEdge.Add(j);
                }
                SegmentsOwnEdges.Add(SegmentsOwnEdge);
            }

            //Checks edges which only belong to 2 segments
            //Invalidates the ones which only belong to 2 segments that come from the same line
            for (int i = 0; i < edges.Count; i++)
            {
                if (
                    (SegmentsOwnEdges[i].Count < 2) ||
                    (SegmentsOwnEdges[i].Count == 2 && ValidSegments[SegmentsOwnEdges[i][0]][2] == ValidSegments[SegmentsOwnEdges[i][1]][2])
                    )
                {
                    edges[i][0] = -edges[i][0];
                    edges[i][1] = -edges[i][1];
                }
            }

            //Joins valid segments which contain invalid edges
            if (ValidSegments.Count > 0)
            {
                int curLine = ValidSegments[0][2];
                for (int i = 1; i < ValidSegments.Count; i++)
                {
                    if (curLine != ValidSegments[i][2])
                    {
                        curLine = ValidSegments[i][2];
                    }
                    else //segments come from the same line
                    {
                        if (edges[ValidSegments[i - 1][1]][0] < 0 && edges[ValidSegments[i][0]][0] < 0)
                        {
                            ValidSegments[i - 1][1] = ValidSegments[i][1];

                            //Connects valid segment to next valid segment in the same line
                            if (i + 1 < ValidSegments.Count && ValidSegments[i - 1][2] == ValidSegments[i + 1][2])
                            {
                                ValidSegments[i + 1][0] = ValidSegments[i - 1][1];
                            }

                            ValidSegments.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            //Removes abherrations: Valid segments cannot contain invalid vertexes
            //This shouldn't happen...
            for (int i = 0; i < ValidSegments.Count; i++)
            {
                if (edges[ValidSegments[i][1]][0] < 0 || edges[ValidSegments[i][0]][0] < 0)
                {
                    ValidSegments.RemoveAt(i);
                    i--;
                }
            }

            //Creates a graph connecting edges which belong to valid segments

            //Computes closed loops

            //Corrects geometry when 3 adjacent edges are collinear

            List<int> ValidEdges = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i][0] > 0) ValidEdges.Add(i);
            }

            VSegs = ValidSegments;
            return edges;
        }
        public List<int[]> VSegs;

        private int SortByRelevance(float[] v1, float[] v2)
        {
            return v1[2].CompareTo(v2[2]);
        }
        private int SortByXY(float[] v1, float[] v2)
        {
            int val = v1[0].CompareTo(v2[0]);
            if (val != 0) return val;
            else return v1[1].CompareTo(v2[1]);
        }

        #region McLabEn graph class
        /// <summary>McLabEn graph class. Hard to understand at this time....</summary>
        public class classeGrafo
        {
            public int MAXMALHASFECHADAS = 20;

            //TODOS OS INDICES COMECAM EM 1???? Conversao!!!
            private const string const_TOTMALHASFECHADAS = "Total de malhas fechadas: ";
            private const string const_UMNAOEXISTE = "Um dos nós não existe";
            private const string const_NAOHA = "Não há caminhos que partam do nó de origem e cheguem no destino.";
            private const string const_MALHASQNAOSETOCAM = "Malhas fechadas que não se tocam:";
            private const string const_TODASSETOCAM = "Todas as malhas fechadas se tocam.";
            private const string const_PARTEMDE = "Caminhos partindo de ";
            private const string const_CHEGAMEM = " que chegam em ";
            private const string const_CAMINHO = "Caminho ";
            public struct tipo_ligacao
            {
                //razao entre polinomios? talvez
                public string transffunc;
                public int alvo;
            }
            public struct tipo_no
            {
                public string nome;
                public tipo_ligacao[] ligacoes;
                public PointF posicao;
            }
            public struct tipo_grafo
            {
                public tipo_no[] nos;
                public int qtdnos;
            }
            public struct tipo_malha
            {
                public int[] sequencia;
                public string trffunc;
            }
            private struct tipo_MalhadeMalha
            {
                public tipo_malha[] malha;
            }

            tipo_malha[] malhas;
            bool fezmalhas;
            public tipo_grafo grafo;
            //total de malhas fechadas
            int qtdmalhas;
            //contém as malhas que nao se tocam n a n
            tipo_MalhadeMalha[] malhaDeMalhas;

            //para o cálculo de caminhos
            tipo_malha[] caminhos;
            int noOrig;
            int noFim;
            int qtdcaminhos;

            string[] transfReduzidas;
            public int malhasfechadas()
            {
                //os caminhos entre nos nao fazem mais sentido
                noOrig = 0;
                noFim = 0;

                //recebe um grafo e retorna as malhas fechadas desse grafo
                //Ex: se o grafo tem 3 malhas fechadas, podemos ter:
                //malhas(1)=[1 2 3] 1->2->3->1
                //malhas(2)=[2 3] 2->3->2
                //malhas(3)=[1 3 4] 1->3->4->1
                int[] lista_pais = null;
                string[] lista_trffunc = null;

                qtdmalhas = 0;

                lista_pais = new int[1];
                lista_trffunc = new string[1];

                int i = 0;
                int j = 0;

                //encontra as malhas em todos os nos
                for (j = 0; j < grafo.nos.Length; j++)
                {
                    lista_pais[0] = j;
                    for (i = 0; i < grafo.nos[j].ligacoes.Length; i++)
                    {
                        if (!(grafo.nos[j].ligacoes[i].alvo == 0))
                        {
                            lista_trffunc[0] = grafo.nos[j].ligacoes[i].transffunc;
                            rmf(ref lista_pais, ref lista_trffunc, ref grafo.nos[j].ligacoes[i].alvo);
                        }
                    }

                }

                return qtdmalhas;

            }

            private void rmf(ref int[] lista_pais, ref string[] lista_trffunc, ref int filho)
            {
                //lista_pais : 1..n

                //Detalhe: o filho tem sempre que ser maior do que o primeiro pai
                if (filho < lista_pais[0])
                    return;

                //Recursividade de Malhas Fechadas
                int i = 0;
                int j = 0;
                int qtdfilhos = 0;

                qtdfilhos = grafo.nos[filho].ligacoes.Length;

                //verificar se o filho atual é igual a algum pai
                int qtdpais = 0;
                bool chamar_novamente = false;

                qtdpais = lista_pais.Length;
                chamar_novamente = true;



                for (i = 0; i < qtdpais; i++)
                {
                    if (filho == lista_pais[i])
                    {
                        chamar_novamente = false;

                        //é igual ao primeiro pai: achei uma malha fechada
                        if (i == 0)
                        {
                            Array.Resize(ref malhas, qtdmalhas + 1);
                            malhas[qtdmalhas].sequencia = new int[qtdpais];
                            for (j = 0; j < qtdpais; j++)
                            {
                                malhas[qtdmalhas].sequencia[j] = lista_pais[j];
                            }

                            malhas[qtdmalhas].trffunc = "";
                            for (j = 0; j < qtdpais; j++)
                            {
                                malhas[qtdmalhas].trffunc = malhas[qtdmalhas].trffunc + "(" + lista_trffunc[j] + ")*";
                            }

                            qtdmalhas += 1;


                            //malhas[qtdmalhas].trffunc = Strings.Left(malhas[qtdmalhas].trffunc, Strings.Len(malhas[qtdmalhas].trffunc) - 1);
                            //tira o * do final

                            //DEBUG MANUAL
                            string temp = null;
                            temp = "Malha fechada número " + Convert.ToString(qtdmalhas) + ":";

                            for (j = 0; j < qtdpais; j++)
                            {
                                temp = temp + Convert.ToString(malhas[qtdmalhas - 1].sequencia[j]) + ",";
                                //temp = temp + grafo.nos(lista_pais(j)).nome + ","
                            }
                            //MsgBox(temp + "  Transferência:" + malhas(qtdmalhas).trffunc)
                        }
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }
                if (grafo.nos[filho].ligacoes.Length == 0)
                {
                    chamar_novamente = false;
                }
                else
                {
                    if (grafo.nos[filho].ligacoes[0].alvo < 0)
                        chamar_novamente = false;
                }

                if (chamar_novamente)
                {
                    int[] novalistapais = null;
                    string[] novalistatrffunc = null;

                    novalistapais = new int[qtdpais + 1];
                    novalistatrffunc = new string[qtdpais + 1];

                    for (i = 0; i < qtdpais; i++)
                    {
                        novalistapais[i] = lista_pais[i];
                        novalistatrffunc[i] = lista_trffunc[i];
                    }
                    novalistapais[qtdpais] = filho;

                    for (i = 0; i < qtdfilhos; i++)
                    {
                        if (grafo.nos[filho].ligacoes[i].alvo >= novalistapais[0])
                        {
                            novalistatrffunc[qtdpais] = grafo.nos[filho].ligacoes[i].transffunc;
                            rmf(ref novalistapais, ref novalistatrffunc, ref grafo.nos[filho].ligacoes[i].alvo);
                        }
                    }


                }

            }
            private void naosetocam()
            {
                //n é o total de malhas fechadas

                //calcula as malhas que nao se tocam 2 a 2, 3 a 3, etc.
                //malhaDeMalhas vai de 1 ate n
                //malhadeMalhas(1) contem a malha original
                //Ex: malhaDeMalhas(2) contem as malhas que nao se tocam 2 a 2
                //se malhaDeMalhas(2).malha(1).sequencia=[1 3] entao 1 e 3 nao se tocam
                bool continuar = false;
                int segur = 0;
                int varAVar = 0;
                //serve para eu saber que estou testanto as
                //malhas de var a var
                bool testarmalha = false;
                bool malhatoca = false;
                int i = 0;
                int j = 0;
                int k = 0;
                int q = 0;
                int p = 0;
                int qtdnaosetocamVaV = 0;

                int maxk = 0;
                int maxp = 0;
                int maxq = 0;

                malhaDeMalhas = new tipo_MalhadeMalha[1];
                malhaDeMalhas[0].malha = new tipo_malha[qtdmalhas];

                for (i = 0; i < qtdmalhas; i++)
                {
                    malhaDeMalhas[0].malha[i].sequencia = new int[1];
                    malhaDeMalhas[0].malha[i].sequencia[0] = i;
                    malhaDeMalhas[0].malha[i].trffunc = malhas[0].trffunc;
                }
                //nao se tocam 2 a 2, 3 a 3, etc.
                varAVar = 0;
                continuar = true;

                if (qtdmalhas == 0)
                    return;

                while (continuar & segur < this.MAXMALHASFECHADAS)
                {
                    continuar = false;
                    varAVar = varAVar + 1;
                    Array.Resize(ref malhaDeMalhas, varAVar + 1);
                    qtdnaosetocamVaV = 0;
                    for (i = 0; i < malhaDeMalhas[varAVar - 1].malha.Length; i++)
                    {
                        for (j = 0; j < malhas.Length; j++)
                        {
                            testarmalha = true;
                            //MsgBox UBound(malhaDeMalhas(varAVar - 1).malha)
                            //MsgBox UBound(malhaDeMalhas(varAVar - 1).malha(i).sequencia)
                            //If varAVar = 3 Then
                            //   MsgBox UBound(malhaDeMalhas(varAVar - 1).malha)
                            //End If

                            //a lista deve estar em ordem crescente
                            //ja que a ordem das malhas nao interessa nesse calculo
                            for (k = 0; k <= varAVar - 1; k++)
                            {
                                if (malhaDeMalhas[varAVar - 1].malha[i].sequencia[k] > j)
                                {
                                    testarmalha = false;
                                    break; // TODO: might not be correct. Was : Exit For
                                }
                            }
                            if (testarmalha)
                            {
                                for (k = 0; k <= varAVar - 1; k++)
                                {
                                    if (malhaDeMalhas[varAVar - 1].malha[i].sequencia[k] == j)
                                    {
                                        //essa sequencia ja esta contida, nao preciso testa-la
                                        testarmalha = false;
                                        break; // TODO: might not be correct. Was : Exit For
                                    }
                                }
                            }
                            if (testarmalha)
                            {
                                malhatoca = false;
                                maxk = varAVar - 1;
                                k = 0;
                                while (k <= maxk & !malhatoca)
                                {
                                    //vou testar a j-esima malha com as malhas
                                    //listadas em malhaDeMalhas(varAVar - 1).malha(i).sequencia
                                    maxp = malhas[j].sequencia.Length;
                                    p = 0;
                                    while (p < maxp & !malhatoca)
                                    {
                                        maxq = malhas[malhaDeMalhas[varAVar - 1].malha[i].sequencia[k]].sequencia.Length;
                                        q = 0;
                                        while (q < maxq & !malhatoca)
                                        {
                                            if (malhas[malhaDeMalhas[varAVar - 1].malha[i].sequencia[k]].sequencia[q] == malhas[j].sequencia[p])
                                            {
                                                malhatoca = true;
                                            }
                                            q = q + 1;
                                        }

                                        p = p + 1;
                                    }
                                    k = k + 1;
                                }
                                //entao nao se tocam varavar+1 a varavar+1
                                if (!malhatoca)
                                {
                                    continuar = true;
                                    Array.Resize(ref malhaDeMalhas[varAVar].malha, qtdnaosetocamVaV + 1);
                                    malhaDeMalhas[varAVar].malha[qtdnaosetocamVaV].sequencia = new int[varAVar + 1];
                                    for (k = 0; k <= varAVar - 1; k++)
                                    {
                                        malhaDeMalhas[varAVar].malha[qtdnaosetocamVaV].sequencia[k] = malhaDeMalhas[varAVar - 1].malha[i].sequencia[k];
                                    }
                                    malhaDeMalhas[varAVar].malha[qtdnaosetocamVaV].sequencia[varAVar] = j;

                                    //malhaDeMalhas[varAVar].malha[qtdnaosetocamVaV].trffunc = malhaDeMalhas[varAVar - 1].malha[i].trffunc + "*" + malhaDeMalhas[1].malha[j].trffunc;


                                    qtdnaosetocamVaV = qtdnaosetocamVaV + 1;
                                    //DEBUG MANUAL
                                    string temp = null;
                                    temp = "Nao se tocam " + Convert.ToString(varAVar) + " a " + Convert.ToString(varAVar) + ": ";
                                    for (k = 0; k < varAVar; k++)
                                    {
                                        temp = temp + Convert.ToString(malhaDeMalhas[varAVar].malha[qtdnaosetocamVaV - 1].sequencia[k]) + ",";
                                    }
                                    temp = temp + " trffunc " + malhaDeMalhas[varAVar].malha[qtdnaosetocamVaV - 1].trffunc;
                                    // MsgBox(temp)

                                }
                            }
                        }
                    }
                    segur = segur + 1;
                }
                if (segur >= this.MAXMALHASFECHADAS)
                {
                    System.Windows.Forms.MessageBox.Show("loop infinito?");
                }
            }
            private int achacaminhos(ref int origem, ref int destino)
            {
                int functionReturnValue = 0;
                if (!fezmalhas)
                {
                    malhasfechadas();
                    naosetocam();
                    fezmalhas = true;
                }
                //calcula todos os caminhos existentes entre a origem e o destino
                //retorna o total de caminhos

                //verifica se já está calculado
                if (!(noOrig == origem & destino == noFim))
                {
                    int[] lista_pais = null;
                    string[] lista_trffunc = null;

                    qtdcaminhos = 0;
                    caminhos = new tipo_malha[1];

                    lista_pais = new int[2];
                    lista_trffunc = new string[2];

                    int i = 0;

                    //tenta caminhar por todos os filhos

                    lista_pais[1] = origem;
                    for (i = 0; i < grafo.nos[origem].ligacoes.Length; i++)
                    {
                        lista_trffunc[1] = grafo.nos[origem].ligacoes[i].transffunc;
                        rec(ref lista_pais, ref lista_trffunc, ref grafo.nos[origem].ligacoes[i].alvo, ref caminhos, ref qtdcaminhos, ref destino);
                    }
                    functionReturnValue = qtdcaminhos;
                    noOrig = origem;
                    noFim = destino;
                }
                return functionReturnValue;
            }

            private void rec(ref int[] lista_pais, ref string[] lista_trffunc, ref int filho, ref tipo_malha[] caminhos, ref int qtdcaminhos, ref int destino)
            {
                //Recursividade para Encontrar Caminhos

                int i = 0;
                int qtdfilhos = 0;

                //"Debug Manual"
                string temp = null;
                temp = "Pais:";
                for (i = 0; i < lista_pais.Length; i++)
                {
                    temp = temp + Convert.ToString(lista_pais[i]) + ",";
                }
                temp = temp + "  Filho:" + Convert.ToString(filho);
                //MsgBox temp

                qtdfilhos = grafo.nos[filho].ligacoes.Length;

                //segundo passo: verificar se o filho atual é igual a algum pai
                int qtdpais = 0;
                bool chamar_novamente = false;

                qtdpais = lista_pais.Length;
                chamar_novamente = true;
                for (i = 0; i < qtdpais; i++)
                {
                    if (filho == lista_pais[i])
                    {
                        chamar_novamente = false;
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }

                if (filho == destino)
                {
                    //é igual ao destino: achei o que procurava
                    chamar_novamente = false;

                    int j = 0;

                    qtdcaminhos = qtdcaminhos + 1;
                    Array.Resize(ref caminhos, qtdcaminhos + 1);

                    caminhos[qtdcaminhos].sequencia = new int[qtdpais + 2];
                    for (j = 0; j < qtdpais; j++)
                    {
                        caminhos[qtdcaminhos].sequencia[j] = lista_pais[j];
                    }
                    caminhos[qtdcaminhos].sequencia[qtdpais + 1] = filho;

                    for (j = 0; j <= qtdpais; j++)
                    {
                        caminhos[qtdcaminhos].trffunc = caminhos[qtdcaminhos].trffunc + "(" + lista_trffunc[j] + ")*";
                    }
                    //caminhos[qtdcaminhos].trffunc = Strings.Left(caminhos[qtdcaminhos].trffunc, Strings.Len(caminhos[qtdcaminhos].trffunc) - 1);
                    //tira o * do final

                    //DEBUG MANUAL
                    temp = "Caminho entre os nós:";

                    for (j = 0; j < qtdpais + 1; j++)
                    {
                        temp = temp + Convert.ToString(caminhos[qtdcaminhos].sequencia[j]) + ",";
                        //temp = temp + grafo.nos(caminhos(qtdcaminhos).sequencia(j)).nome + ","
                    }
                    //MsgBox(temp + "  Transferência:" + caminhos(qtdcaminhos).trffunc)
                }

                if (grafo.nos[filho].ligacoes.Length == 0)
                {
                    chamar_novamente = false;
                }
                else
                {
                    if (grafo.nos[filho].ligacoes[1].alvo == 0)
                        chamar_novamente = false;
                }
                if (chamar_novamente)
                {
                    int[] novalistapais = null;
                    string[] novalistatrffunc = null;

                    novalistapais = new int[qtdpais + 2];
                    novalistatrffunc = new string[qtdpais + 2];

                    for (i = 0; i < qtdpais; i++)
                    {
                        novalistapais[i] = lista_pais[i];
                        novalistatrffunc[i] = lista_trffunc[i];
                    }
                    novalistapais[qtdpais + 1] = filho;

                    for (i = 0; i < qtdfilhos; i++)
                    {
                        //       If grafo.nos(filho).ligacoes(i).alvo >= novalistapais(1) Then
                        novalistatrffunc[qtdpais + 1] = grafo.nos[filho].ligacoes[i].transffunc;
                        rec(ref novalistapais, ref novalistatrffunc, ref grafo.nos[filho].ligacoes[i].alvo, ref caminhos, ref qtdcaminhos, ref destino);
                        //       End If
                    }

                }

            }

            public bool removeNo(string nomeNo)
            {
                //PENDENTE: falta remover as ligacoes que se referem a 
                //esse nó, e também atualizar as ligações dos demais

                int pos = localizaNo(nomeNo);
                //existe
                if (pos >= 0)
                {
                    int i = 0;
                    int j = 0;

                    //remove as ligações que partem dos outros nos e chegam no que se vai eliminar
                    for (i = 0; i < grafo.qtdnos; i++)
                    {
                        removeLig(grafo.nos[i].nome, nomeNo);
                    }

                    //falta trocar as referências
                    grafo.qtdnos -= 1;
                    for (i = pos; i < grafo.qtdnos; i++)
                    {
                        grafo.nos[i] = grafo.nos[i + 1];
                    }
                    Array.Resize(ref grafo.nos, grafo.qtdnos);

                    //troca os valores pos+1 por pos, pos+2 por pos+1, etc.
                    //jeito fácil: se o numero eh maior do que pos, subtrai 1
                    for (i = 0; i < grafo.qtdnos; i++)
                    {
                        for (j = 0; j < grafo.nos[i].ligacoes.Length; j++)
                        {
                            if (grafo.nos[i].ligacoes[j].alvo > pos)
                            {
                                grafo.nos[i].ligacoes[j].alvo -= 1;
                            }
                        }
                    }
                    fezmalhas = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public bool addNo(string nomeNo, PointF posicaoNo)
            {
                //nao existe o no
                if (localizaNo(nomeNo) < 0)
                {
                    Array.Resize(ref grafo.nos, grafo.qtdnos + 1);
                    grafo.nos[grafo.qtdnos].nome = nomeNo;
                    grafo.nos[grafo.qtdnos].posicao = posicaoNo;
                    grafo.nos[grafo.qtdnos].ligacoes = new tipo_ligacao[0];
                    fezmalhas = false;
                    grafo.qtdnos += 1;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public int localizaNo(string nomeNo)
            {
                int i = 0;
                bool achou = false;
                if (grafo.qtdnos <= 0)
                    return -1;
                while (i < grafo.qtdnos & !achou)
                {
                    //MsgBox(grafo.nos(i).nome + "=" + nomeNo + "?")
                    if (grafo.nos[i].nome == nomeNo)
                        achou = true;
                    i += 1;
                }
                i -= 1;
                if (achou)
                {
                    return i;
                }
                else
                {
                    return -1;
                }
            }
            public bool addLig(string origem, string destino, string trffunc)
            {
                int posOrig = localizaNo(origem);
                int posDest = localizaNo(destino);
                //um deles nao existe
                if (posOrig < 0 || posDest < 0)
                {
                    return false;
                }
                else
                {
                    int posLig = existeLig(posOrig, posDest);
                    //nao existe a ligacao
                    if (posLig < 0)
                    {
                        int n = grafo.nos[posOrig].ligacoes.Length;
                        Array.Resize(ref grafo.nos[posOrig].ligacoes, n + 1);
                        grafo.nos[posOrig].ligacoes[n].alvo = posDest;
                        grafo.nos[posOrig].ligacoes[n].transffunc = trffunc;
                        //ja existe, troca a func transferencia
                    }
                    else
                    {
                        grafo.nos[posOrig].ligacoes[posLig].transffunc = trffunc;
                    }
                    fezmalhas = false;
                    //precisa recalcular malhas fechadas
                    return true;
                }
            }
            public bool removeLig(string origem, string destino)
            {
                int posOrigem = localizaNo(origem);
                int posDest = localizaNo(destino);
                //existem os nos
                if (posOrigem >= 0 & posDest >= 0)
                {
                    int i = 0;
                    int pos = 0;
                    pos = existeLig(posOrigem, posDest);
                    //existe a ligacao
                    if (pos >= 0)
                    {
                        int n = grafo.nos[posOrigem].ligacoes.Length - 1;
                        for (i = pos; i < n; i++)
                        {
                            grafo.nos[posOrigem].ligacoes[i] = grafo.nos[posOrigem].ligacoes[i + 1];
                        }
                        Array.Resize(ref grafo.nos[posOrigem].ligacoes, n);
                        fezmalhas = false;
                        return true;
                        //nao existe ligacao
                    }
                    else
                    {
                        return false;
                    }
                    //nao existe algum dos nos
                }
                else
                {
                    return false;
                }
            }
            public int existeLig(int posOrigem, int posDest)
            {
                //retorna a posicao da ligacao ou -1 se nao existe
                int i = 0;
                bool achou = false;
                int n = grafo.nos[posOrigem].ligacoes.Length;
                while (i < n & !achou)
                {
                    if (grafo.nos[posOrigem].ligacoes[i].alvo == posDest)
                        achou = true;
                    i += 1;
                }
                i -= 1;
                if (achou)
                {
                    return i;
                }
                else
                {
                    return -1;
                }
            }
            public string determinante()
            {
                //determinante do sistema: 1-(prodtransmissao fechadas)+prodNaoseTocam2a2-prodNST3a3+...
                string resp = "1";
                if (!fezmalhas)
                {
                    malhasfechadas();
                    naosetocam();
                    fezmalhas = true;
                }
                int i = 0;
                int j = 0;
                string temp = null;
                int count = 1;

                for (i = 0; i < malhaDeMalhas.Length - 1; i++)
                {
                    temp = "";
                    for (j = 0; j < malhaDeMalhas[i].malha.Length; j++)
                    {
                        if (j > 1)
                        {
                            temp = temp + "+" + malhaDeMalhas[i].malha[j].trffunc;
                        }
                        else
                        {
                            temp = temp + malhaDeMalhas[i].malha[j].trffunc;
                        }
                    }
                    count *= -1;
                    if (count > 0)
                    {
                        resp = resp + "+";
                    }
                    else
                    {
                        resp = resp + "-";
                    }
                    resp = resp + "(" + temp + ")";
                }
                return resp;
            }
            public string malhasFechadasNSeTocam()
            {
                if (!fezmalhas)
                {
                    malhasfechadas();
                    naosetocam();
                    fezmalhas = true;
                }
                string resp = null;
                int i = 0;
                int j = 0;
                int k = 0;
                //diz malhas fechadas
                resp = const_TOTMALHASFECHADAS + Convert.ToString(qtdmalhas);
                for (i = 0; i < qtdmalhas; i++)
                {
                    resp = resp + "\n";
                    resp = resp + "Malha " + Convert.ToString(i) + ": ";
                    for (j = 0; j < malhas[i].sequencia.Length; j++)
                    {
                        resp = resp + grafo.nos[malhas[i].sequencia[j]].nome + "->";
                    }
                    resp = resp + grafo.nos[malhas[i].sequencia[0]].nome;
                }

                //diz quais nao se tocam n a n
                resp = resp + "\n";
                if (malhaDeMalhas.Length > 2)
                {
                    resp = resp + "\n" + const_MALHASQNAOSETOCAM;
                    for (i = 0; i < malhaDeMalhas.Length - 1; i++)
                    {
                        resp = resp + "\n" + Convert.ToString(i + 1) + " a " + Convert.ToString(i + 1) + ": ";
                        for (j = 0; j < malhaDeMalhas[i].malha.Length; j++)
                        {
                            if (!(j == 0))
                                resp = resp + "; ";
                            for (k = 0; k < malhaDeMalhas[i].malha[j].sequencia.Length; k++)
                            {
                                resp = resp + Convert.ToString(malhaDeMalhas[i].malha[j].sequencia[k]);
                                if (k == malhaDeMalhas[i].malha[j].sequencia.Length - 2)
                                {
                                    resp = resp + " e ";
                                }
                                else if (!(k == malhaDeMalhas[i].malha[j].sequencia.Length - 1))
                                {
                                    resp = resp + ", ";
                                }
                            }
                        }
                    }
                }
                else
                {
                    resp = resp + "\n" + const_TODASSETOCAM;
                }

                return resp;
            }
            public string printCaminhos(string origem, string destino)
            {
                int posOrig = 0;
                int posDest = 0;
                posOrig = localizaNo(origem);
                posDest = localizaNo(destino);
                string resp = null;
                //os nós existem
                if (posOrig >= 0 & posDest >= 0)
                {
                    achacaminhos(ref posOrig, ref posDest);
                    if (qtdcaminhos == 0)
                    {
                        return const_NAOHA;
                        //existem caminhos
                    }
                    else
                    {
                        resp = const_PARTEMDE + origem + const_CHEGAMEM + destino + ":";
                        int i = 0;
                        int j = 0;
                        for (i = 0; i <= qtdcaminhos; i++)
                        {
                            resp = resp + "\n" + const_CAMINHO + Convert.ToString(i) + ": ";
                            for (j = 0; j < caminhos[i].sequencia.Length; j++)
                            {
                                resp = resp + grafo.nos[caminhos[i].sequencia[j]].nome;
                                if (!(j == caminhos[i].sequencia.Length))
                                {
                                    resp = resp + "->";
                                }
                            }
                        }
                        return resp;
                    }
                }
                else
                {
                    return const_UMNAOEXISTE;
                }
            }
            public string transfFunc(string origem, string destino)
            {
                int posOrig = localizaNo(origem);
                int posDest = localizaNo(destino);
                if (posOrig >= 0 & posDest >= 0)
                {
                    string num = null;
                    string denom = null;
                    num = achaNumeradorTransferencia(posOrig, posDest);
                    denom = determinante();
                    return "(" + num + ")/(" + denom + ")";
                }
                return "";
            }
            private string achaNumeradorTransferencia(int posOrig, int posDest)
            {
                //recalcula os caminhos de ligacao
                achacaminhos(ref posOrig, ref posDest);
                transfReduzidas = new string[qtdcaminhos + 1];
                //cada caminho tem sua transferencia reduzida
                int k = 0;
                int i = 0;
                int j = 0;
                string temp = null;
                int p = 0;
                bool incluirMalha = false;
                int q = 0;
                int[] malhasExcluidas = new int[1];
                int qtdmexcs = 0;
                //quantidade de malhas excluídas

                int count = 0;
                for (k = 1; k <= qtdcaminhos; k++)
                {
                    transfReduzidas[k] = "1";
                    qtdmexcs = 0;
                    count = 1;
                    for (j = 0; j <= qtdmalhas; j++)
                    {
                        incluirMalha = true;
                        //verifica se algum no do caminho está dentro da malha
                        p = 1;
                        while (p < malhas[j].sequencia.Length & incluirMalha)
                        {
                            i = 1;
                            while (i < caminhos[k].sequencia.Length & incluirMalha)
                            {
                                if (malhas[j].sequencia[p] == caminhos[k].sequencia[i])
                                {
                                    incluirMalha = false;
                                    //MsgBox("Malha " + CStr(j) + " contém algum nó do caminho " + CStr(k))
                                }
                                i += 1;
                            }
                            p += 1;
                        }
                        if (!incluirMalha)
                        {
                            qtdmexcs += 1;
                            Array.Resize(ref malhasExcluidas, qtdmexcs + 1);
                            malhasExcluidas[qtdmexcs] = j;
                        }
                    }

                    for (i = 0; i < malhaDeMalhas.Length - 1; i++)
                    {
                        temp = "";
                        for (j = 0; j < malhaDeMalhas[i].malha.Length; j++)
                        {
                            incluirMalha = true;
                            //verifica se alguma das malhas excluídas está na malha
                            p = 1;
                            while (incluirMalha & p <= qtdmexcs)
                            {
                                q = 1;
                                while (incluirMalha & q < malhaDeMalhas[i].malha[j].sequencia.Length)
                                {
                                    if (malhaDeMalhas[i].malha[j].sequencia[q] == malhasExcluidas[p])
                                    {
                                        incluirMalha = false;
                                    }
                                    q += 1;
                                }
                                p += 1;
                            }
                            if (incluirMalha)
                            {
                                temp = temp + malhaDeMalhas[i].malha[j].trffunc + "+";
                            }
                        }
                        if (temp.Length > 0)
                        {
                            //temp = mid(temp, 1, Strings.Len(temp) - 1);
                            //retira o * residual

                            count *= -1;
                            if (count > 0)
                            {
                                transfReduzidas[k] = transfReduzidas[k] + "+";
                            }
                            else
                            {
                                transfReduzidas[k] = transfReduzidas[k] + "-";
                            }
                            transfReduzidas[k] = transfReduzidas[k] + "(" + temp + ")";
                        }
                    }
                }
                //constrói o numerador da função de transferencia
                string resp = null;
                for (k = 1; k <= qtdcaminhos; k++)
                {
                    if (transfReduzidas[k] == "1")
                    {
                        resp = resp + "(" + caminhos[k].trffunc + ")+";
                    }
                    else
                    {
                        resp = resp + "(" + transfReduzidas[k] + ")*(" + caminhos[k].trffunc + ")+";
                    }
                }
                //if (Strings.Len(resp) > 0)
                {
                    //resp = Strings.Mid(resp, 1, Strings.Len(resp) - 1);
                }
                return resp;
            }

        }
        #endregion

        private void testSelectManyLines(int nPtsTheta, int nPtsRho, int[] QtdPtsOfInterest, int[] Weights, int[] PtsOfInterest)
        {
            //Merges line values which are close. int[] { theta, rho, relevance }
            List<int[]> Lines = new List<int[]>();

            int thetaSeparation = nPtsTheta >> 2;
            int rhoSeparation = nPtsRho >> 2;

            for (int i = 0; i < QtdPtsOfInterest[0]; i++)
            {
                bool merged = false;
                int[] curPt = new int[] { PtsOfInterest[i << 1], PtsOfInterest[(i << 1) + 1], Weights[i] };

                for (int k = 0; k < Lines.Count; k++)
                {
                    if ((Math.Abs(curPt[0] - Lines[k][0]) <= thetaSeparation || Math.Abs(curPt[0] - Lines[k][0]) >= nPtsTheta - 1 - thetaSeparation)
                        && Math.Abs(curPt[1] - Lines[k][1]) <= rhoSeparation)
                    {
                        merged = true;
                        if (curPt[2] > Lines[k][2])
                        {
                            Lines[k][0] = curPt[0];
                            Lines[k][1] = curPt[1];
                            Lines[k][2] = curPt[2];
                        }
                        k = Lines.Count;
                    }
                }

                if (!merged) Lines.Add(curPt);
            }

            #region Removes redundant lines
            bool removed = true;



            while (removed)
            {
                removed = false;
                for (int i = 1; i < Lines.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if ((Math.Abs(Lines[i][0] - Lines[j][0]) <= thetaSeparation || Math.Abs(Lines[i][0] - Lines[j][0]) >= nPtsTheta - 1 - thetaSeparation)
                            && Math.Abs(Lines[i][1] - Lines[j][1]) <= rhoSeparation)
                        {
                            //i-th is more relevant than previous, j-th
                            if (Lines[i][2] > Lines[j][2])
                            {
                                Lines[j][0] = Lines[i][0];
                                Lines[j][1] = Lines[i][1];
                                Lines[j][2] = Lines[i][2];
                            }
                            //Removes i-th
                            Lines.RemoveAt(i);
                            i--;
                            j = i;
                            removed = true;
                        }
                    }
                }
            }



            #endregion
        }

        #endregion

        #region Bitmap representation for user info
        /// <summary>Returns a bitmap representation of last Hough transform.</summary>
        /// <param name="numberOfGeometryVectors">Number of geometry vectors. Used to compute point relevance</param>
        /// <param name="PercentageToBeRelevant">Number of votes, as % of total geometry vectors, that a point has to have to be considered relevant</param>
        public Bitmap GetLastGeneralizedHoughTransformRepresentation(int numberOfGeometryVectors, float PercentageToBeRelevant)
        {
            CLHoughTransformData.ReadFromDeviceTo(HoughTransform);
            return CLHoughTransform.TestFuncs.GHTRepresentation(HoughTransform, bmpDim[0], bmpDim[1], numberOfGeometryVectors, PercentageToBeRelevant);
        }

        /// <summary>Returns a bitmap representation of Hough line transform</summary>
        public Bitmap GetHoughLineTransformRepresentation()
        {
            CLHoughLineTransform.ReadFromDeviceTo(HoughLineTransformData);
            return CLHoughTransform.TestFuncs.GHTRepresentation(HoughLineTransformData, _nTheta, _nRho, bmpDim[1], 0.8f);
        }

        /// <summary>Retrieves a Bitmap representation of the Sobel edges after thinning</summary>
        public Bitmap RetrieveSobelRepresentation()
        {
            //Retrieves data from device memory
            if (UseThinning) CLSobelImgThinning.ReadFromDeviceTo(SobelImg);
            else CLSobelImg.ReadFromDeviceTo(SobelImg);

            Bitmap bmp = new Bitmap(bmpDim[0], bmpDim[1]);

            BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            //Reads zero-intensity pixels
            unsafe
            {
                byte* row;
                for (int y = 0; y < bmdbmp.Height; y++)
                {
                    row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                    for (int x = 0; x < bmdbmp.Width; x++)
                    {
                        int ind = x << 2;
                        byte b = SobelImg[x + bmpDim[0] * y];
                        row[ind] = b;
                        row[ind + 1] = b;
                        row[ind + 2] = b;
                        row[ind + 3] = 255;
                    }
                }
            }

            bmp.UnlockBits(bmdbmp);

            return bmp;
        }
        #endregion

        #endregion


        #region OpenCL kernels (static) and Source

        /// <summary>Initializes OpenCL and compiles kernels</summary>
        private static void InitCL()
        {
            //Enables OpenCL and checks if kernels are compiled
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();

            //if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL) throw new Exception("OpenCL not enabled");

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL && kernelmedianFilter == null)
            {
                CLSrc src = new CLSrc();
                CLCalc.Program.Compile(new string[] { src.srcAtomics, src.QSortSrc, src.srcMedianFilter, src.srcSobel, src.srcHough, src.srcLineHough, src.srcThinning, src.srcMorphologic });

                //kernelmedianFilter = new CLCalc.Program.Kernel("medianFilterWithoutLocal");
                kernelmedianFilter = new CLCalc.Program.Kernel("medianFilter5");
                kernelSobel = new CLCalc.Program.Kernel("sobelWithoutLocal");
                kernelRetrieveThresholdedPts = new CLCalc.Program.Kernel("RetrieveThresholdedPts");
                kernelGeneralizedHough = new CLCalc.Program.Kernel("GeneralizedHough");
                kernelGeneralizedHoughConstTP = new  CLCalc.Program.Kernel  ("GeneralizedHoughConstTP");
                kernelSobelBytes = new CLCalc.Program.Kernel("sobelBytes");
                kernelClearHough = new CLCalc.Program.Kernel("ClearHough");
                kernelRetrievePointsOfInterest = new CLCalc.Program.Kernel("RetrievePointsOfInterest");
                kernelImageThinning = new CLCalc.Program.Kernel("ImageThinning");
                kernelImageThinningByte = new CLCalc.Program.Kernel("ImageThinningByte");
                kernelShrinkBmpBy3 = new CLCalc.Program.Kernel("ShrinkBmpBy3");
                kernelLaplacian = new CLCalc.Program.Kernel("Laplacian");
                kernelBmpMean = new CLCalc.Program.Kernel("BmpMean");
                kernelDilate = new CLCalc.Program.Kernel("Dilate");
                kernelErode = new CLCalc.Program.Kernel("Erode");
                kernelDilateByte = new CLCalc.Program.Kernel("DilateByte");
                kernelErodeByte = new CLCalc.Program.Kernel("ErodeByte");
                kernelLineHoughTransform = new CLCalc.Program.Kernel("LineHoughTransform");
            }
        }

        private static CLCalc.Program.Kernel kernelmedianFilter;
        private static CLCalc.Program.Kernel kernelSobel, kernelSobelBytes, kernelRetrieveThresholdedPts;
        private static CLCalc.Program.Kernel kernelLaplacian;
        private static CLCalc.Program.Kernel kernelGeneralizedHough, kernelGeneralizedHoughConstTP, kernelClearHough, kernelRetrievePointsOfInterest;
        private static CLCalc.Program.Kernel kernelLineHoughTransform;
        private static CLCalc.Program.Kernel kernelImageThinning, kernelImageThinningByte;
        private static CLCalc.Program.Kernel kernelShrinkBmpBy3;
        private static CLCalc.Program.Kernel kernelBmpMean;
        private static CLCalc.Program.Kernel kernelDilate, kernelErode, kernelDilateByte, kernelErodeByte;

        /// <summary>Block size (workgroups)</summary>
        private static int BLOCK_SIZE = 16;

        /// <summary>OpenCL source</summary>
        private class CLSrc
        {
            #region Atomics enable
            public string srcAtomics = @"
#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
#pragma OPENCL EXTENSION cl_khr_global_int32_base_atomics : enable
#pragma OPENCL EXTENSION cl_khr_local_int32_base_atomics : enable
#pragma OPENCL EXTENSION cl_khr_global_int32_extended_atomics : enable
#pragma OPENCL EXTENSION cl_khr_local_int32_extended_atomics : enable

";
            #endregion

            #region Quicksort source
            public string QSortSrc = @"
//Quicksorts 25 values. Since the goal is to find the 12th element the algorithm can stop when this happens
void QuickSort25(float* vals)
{
    //Start-stop indexes
    int subListStarts[7];
    int subListEnds[7];
    int nLists = 1;

    subListStarts[0] = 0;
    subListEnds[0] = 24;

    int ind = nLists-1, ind0, indf, pivot;
    int leftIdx, rightIdx, inttemp, k;
    float temp;
    while (nLists > 0)
    {
        ind0 = subListStarts[ind];
        indf = subListEnds[ind];

        pivot = (ind0 + indf) >> 1;
        leftIdx = ind0;
        rightIdx = indf;

        while (leftIdx <= pivot && rightIdx >= pivot)
        {
            while (vals[leftIdx] < vals[pivot] && leftIdx <= pivot)
                leftIdx++;
            while (vals[rightIdx] > vals[pivot] && rightIdx >= pivot)
                rightIdx--;

            temp = vals[leftIdx];
            vals[leftIdx] = vals[rightIdx];
            vals[rightIdx] = temp;

            leftIdx++;
            rightIdx--;
            if (leftIdx - 1 == pivot)
            {
                rightIdx++;
                pivot = rightIdx;
            }
            else if (rightIdx + 1 == pivot)
            {
                leftIdx--;
                pivot = leftIdx;
            }
        }

        nLists--;
        inttemp = subListStarts[nLists];
        subListStarts[nLists] = subListStarts[ind];
        subListStarts[ind] = inttemp;

        inttemp = subListEnds[nLists];
        subListEnds[nLists] = subListEnds[ind];
        subListEnds[ind] = inttemp;

        if (pivot - 1 - ind0 > 0)
        {
            subListStarts[nLists] = ind0;
            subListEnds[nLists] = pivot - 1;
            nLists++;
        }
        if (indf - pivot - 1 > 0)
        {
            subListStarts[nLists] = pivot + 1;
            subListEnds[nLists] = indf;
            nLists++;
        }

        for (k = 0; k < nLists; k++)
        {
            if (subListStarts[k]<=12 && 12<=subListEnds[k])
            {
                ind = k; k = nLists+1;
            }
        }
        if (k == nLists) nLists=0;
    }
}

//Quicksorts 9 values. Since the goal is to find the 4th element the algorithm can stop when this happens
void QuickSort9(float* vals)
{
    //Start-stop indexes
    int subListStarts[7];
    int subListEnds[7];
    int nLists = 1;

    subListStarts[0] = 0;
    subListEnds[0] = 8;

    int ind = nLists-1, ind0, indf, pivot;
    int leftIdx, rightIdx, inttemp, k;
    float temp;
    while (nLists > 0)
    {
        ind0 = subListStarts[ind];
        indf = subListEnds[ind];

        pivot = (ind0 + indf) >> 1;
        leftIdx = ind0;
        rightIdx = indf;

        while (leftIdx <= pivot && rightIdx >= pivot)
        {
            while (vals[leftIdx] < vals[pivot] && leftIdx <= pivot)
                leftIdx++;
            while (vals[rightIdx] > vals[pivot] && rightIdx >= pivot)
                rightIdx--;

            temp = vals[leftIdx];
            vals[leftIdx] = vals[rightIdx];
            vals[rightIdx] = temp;

            leftIdx++;
            rightIdx--;
            if (leftIdx - 1 == pivot)
            {
                rightIdx++;
                pivot = rightIdx;
            }
            else if (rightIdx + 1 == pivot)
            {
                leftIdx--;
                pivot = leftIdx;
            }
        }

        nLists--;
        inttemp = subListStarts[nLists];
        subListStarts[nLists] = subListStarts[ind];
        subListStarts[ind] = inttemp;

        inttemp = subListEnds[nLists];
        subListEnds[nLists] = subListEnds[ind];
        subListEnds[ind] = inttemp;

        if (pivot - 1 - ind0 > 0)
        {
            subListStarts[nLists] = ind0;
            subListEnds[nLists] = pivot - 1;
            nLists++;
        }
        if (indf - pivot - 1 > 0)
        {
            subListStarts[nLists] = pivot + 1;
            subListEnds[nLists] = indf;
            nLists++;
        }

        for (k = 0; k < nLists; k++)
        {
            if (subListStarts[k]<=4 && 4<=subListEnds[k])
            {
                ind = k; k = nLists+1;
            }
        }
        if (k == nLists) nLists=0;
    }
}

//Quicksorts 25 values.
void FullQuickSort(float* vals)
{
    //Start-stop indexes
    int subListStarts[7];
    int subListEnds[7];
    int nLists = 1;

    subListStarts[0] = 0;
    subListEnds[0] = 24;

    int ind0, indf, pivot;
    int leftIdx, rightIdx;
    float temp;
    while (nLists > 0)
    {
        ind0 = subListStarts[nLists - 1];
        indf = subListEnds[nLists - 1];


        pivot = (ind0 + indf) >> 1;
        leftIdx = ind0;
        rightIdx = indf;

        while (leftIdx <= pivot && rightIdx >= pivot)
        {
            while (vals[leftIdx] < vals[pivot] && leftIdx <= pivot)
                leftIdx++;
            while (vals[rightIdx] > vals[pivot] && rightIdx >= pivot)
                rightIdx--;

            temp = vals[leftIdx];
            vals[leftIdx] = vals[rightIdx];
            vals[rightIdx] = temp;

            leftIdx++;
            rightIdx--;
            if (leftIdx - 1 == pivot)
            {
                rightIdx++;
                pivot = rightIdx;
            }
            else if (rightIdx + 1 == pivot)
            {
                leftIdx--;
                pivot = leftIdx;
            }
        }

        nLists--;
        if (pivot - 1 - ind0 > 0)
        {
            subListStarts[nLists] = ind0;
            subListEnds[nLists] = pivot - 1;
            nLists++;
        }
        if (indf - pivot - 1 > 0)
        {
            subListStarts[nLists] = pivot + 1;
            subListEnds[nLists] = indf;
            nLists++;
        }

    }
}
";
            #endregion

            #region Median filter
            public string srcMedianFilter = @"

#define BLOCK_SIZE 16

//Applies a 3x3 median filter
__kernel __attribute__((reqd_work_group_size(BLOCK_SIZE, BLOCK_SIZE, 1))) void

         medianFilter(__read_only  image2d_t imgSrc,
                      __write_only image2d_t imgFiltered)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
         
    __local uint4 P[BLOCK_SIZE+2][BLOCK_SIZE+2];
    
    //Identification of this workgroup
   int i = get_group_id(0);
   int j = get_group_id(1);

   //Identification of work-item
   int idX = get_local_id(0);
   int idY = get_local_id(1); 

   int ii = i*BLOCK_SIZE + idX;
   int jj = j*BLOCK_SIZE + idY;
   
   int2 coords = (int2)(ii, jj);

   //Reads pixels
   P[idX][idY] = read_imageui(imgSrc, smp, coords);

   //Needs to read extra elements for the 5x5 filter in the borders
   if (idX == BLOCK_SIZE-1 && idY == BLOCK_SIZE-1)
   {
      for (int p=0; p<3; p++)
      {
         coords.x = ii + p;
         for (int q=0; q<3; q++)
         {
            coords.y = jj + q;
            P[idX+p][idY+q] = read_imageui(imgSrc, smp, coords);
         }
      }
   }
   else if (idX == BLOCK_SIZE-1)
   {
      for (int p=1; p<3; p++)
      {
         coords.x = ii + p;
         P[idX+p][idY] = read_imageui(imgSrc, smp, coords);
      }
   }
   else if (idY == BLOCK_SIZE-1)
   {
      for (int q=1; q<3; q++)
      {
         coords.y = jj + q;
         P[idX][idY+q] = read_imageui(imgSrc, smp, coords);
      }
   }
   barrier(CLK_LOCAL_MEM_FENCE);
   
   //Aplies median filter to element P[idX][idY]
   float R, G, B;
   
   //Blue
   float vals[9];
   int ind;
   for (int i=0; i < 3;i++)
   {
       ind = 3*i;
       vals[ind]   =   (float)P[idX+i][idY].x;
       vals[ind+1] = (float)P[idX+i][idY+1].x;
       vals[ind+2] = (float)P[idX+i][idY+2].x;
   }
   QuickSort9(vals);
   B = vals[4];
   
   //Green
   for (int i=0; i < 3;i++)
   {
       ind = 3*i;
       vals[ind]   =   (float)P[idX+i][idY].y;
       vals[ind+1] = (float)P[idX+i][idY+1].y;
       vals[ind+2] = (float)P[idX+i][idY+2].y;
   }
   QuickSort9(vals);
   G = vals[4];
   
   //Red
   for (int i=0; i < 3;i++)
   {
       ind = 3*i;
       vals[ind]   =   (float)P[idX+i][idY].z;
       vals[ind+1] = (float)P[idX+i][idY+1].z;
       vals[ind+2] = (float)P[idX+i][idY+2].z;
   }
   QuickSort9(vals);
   R = vals[4];

   P[idX+1][idY+1] = (uint4)((uint)B, (uint)G, (uint)R, (uint)255);
   barrier(CLK_LOCAL_MEM_FENCE);


   coords = (int2)(ii+1, jj+1);
   write_imageui(imgFiltered, coords, P[idX+1][idY+1]);
}

//Applies a 5x5 median filter
__kernel __attribute__((reqd_work_group_size(BLOCK_SIZE, BLOCK_SIZE, 1))) void

         medianFilter5(__read_only  image2d_t imgSrc,
                       __write_only image2d_t imgFiltered)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
         
    __local uint4 P[BLOCK_SIZE+4][BLOCK_SIZE+4];
    
    //Identification of this workgroup
   int i = get_group_id(0);
   int j = get_group_id(1);

   //Identification of work-item
   int idX = get_local_id(0);
   int idY = get_local_id(1); 

   int ii = i*BLOCK_SIZE + idX;
   int jj = j*BLOCK_SIZE + idY;
   
   int2 coords = (int2)(ii, jj);

   //Reads pixels
   P[idX][idY] = read_imageui(imgSrc, smp, coords);

   //Needs to read extra elements for the 5x5 filter in the borders
   if (idX == BLOCK_SIZE-1 && idY == BLOCK_SIZE-1)
   {
      for (int p=0; p<5; p++)
      {
         coords.x = ii + p;
         for (int q=0; q<5; q++)
         {
            coords.y = jj + q;
            P[idX+p][idY+q] = read_imageui(imgSrc, smp, coords);
         }
      }
   }
   else if (idX == BLOCK_SIZE-1)
   {
      for (int p=1; p<5; p++)
      {
         coords.x = ii + p;
         P[idX+p][idY] = read_imageui(imgSrc, smp, coords);
      }
   }
   else if (idY == BLOCK_SIZE-1)
   {
      for (int q=1; q<5; q++)
      {
         coords.y = jj + q;
         P[idX][idY+q] = read_imageui(imgSrc, smp, coords);
      }
   }
   barrier(CLK_LOCAL_MEM_FENCE);
   
   //Aplies median filter to element P[idX][idY]
   float R, G, B;
   
   //Blue
   float vals[25];
   int ind;
   for (int i=0; i < 5;i++)
   {
       ind = 5*i;
       vals[ind]   =   (float)P[idX+i][idY].x;
       vals[ind+1] = (float)P[idX+i][idY+1].x;
       vals[ind+2] = (float)P[idX+i][idY+2].x;
       vals[ind+3] = (float)P[idX+i][idY+3].x;
       vals[ind+4] = (float)P[idX+i][idY+4].x;
   }
   QuickSort25(vals);
   B = vals[12];
   
   //Green
   for (int i=0; i < 5;i++)
   {
       ind = 5*i;
       vals[ind]   =   (float)P[idX+i][idY].y;
       vals[ind+1] = (float)P[idX+i][idY+1].y;
       vals[ind+2] = (float)P[idX+i][idY+2].y;
       vals[ind+3] = (float)P[idX+i][idY+3].y;
       vals[ind+4] = (float)P[idX+i][idY+4].y;
   }
   QuickSort25(vals);
   G = vals[12];
   
   //Red
   for (int i=0; i < 5;i++)
   {
       ind = 5*i;
       vals[ind]   =   (float)P[idX+i][idY].z;
       vals[ind+1] = (float)P[idX+i][idY+1].z;
       vals[ind+2] = (float)P[idX+i][idY+2].z;
       vals[ind+3] = (float)P[idX+i][idY+3].z;
       vals[ind+4] = (float)P[idX+i][idY+4].z;
   }
   QuickSort25(vals);
   R = vals[12];

   P[idX+2][idY+2] = (uint4)((uint)B, (uint)G, (uint)R, (uint)255);
   barrier(CLK_LOCAL_MEM_FENCE);


   coords = (int2)(ii+2, jj+2);
   write_imageui(imgFiltered, coords, P[idX+2][idY+2]);
}

//Applies a 5x5 median filter
__kernel void
         medianFilterWithoutLocal(__read_only  image2d_t imgSrc,
                                  __write_only image2d_t imgFiltered)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
         
   int idX = get_global_id(0);
   int idY = get_global_id(1);
   
   int2 coords;

   uint4 P[5][5];

   //Reads pixels
   for (int p=0;p<5;p++)
   {
      coords.x = idX + p;
      for (int q=0;q<5;q++)
      {
         coords.y = idY + q;
         P[p][q] = read_imageui(imgSrc, smp, coords);
      } 
   }


   
   //Aplies median filter to element P[idX][idY]
   float R, G, B;
   
   //Blue
   float vals[25];
   int ind;
   for (int i=0; i < 5;i++)
   {
       ind = 5*i;
       vals[ind]   =   (float)P[i][0].x;
       vals[ind+1] = (float)P[i][1].x;
       vals[ind+2] = (float)P[i][2].x;
       vals[ind+3] = (float)P[i][3].x;
       vals[ind+4] = (float)P[i][4].x;
   }
   QuickSort25(vals);
   B = vals[12];
   
   //Green
   for (int i=0; i < 5;i++)
   {
       ind = 5*i;
       vals[ind]   =   (float)P[i][0].y;
       vals[ind+1] = (float)P[i][1].y;
       vals[ind+2] = (float)P[i][2].y;
       vals[ind+3] = (float)P[i][3].y;
       vals[ind+4] = (float)P[i][4].y;
   }
   QuickSort25(vals);
   G = vals[12];
   
   //Red
   for (int i=0; i < 5;i++)
   {
       ind = 5*i;
       vals[ind]   =   (float)P[i][idY].z;
       vals[ind+1] = (float)P[i][1].z;
       vals[ind+2] = (float)P[i][2].z;
       vals[ind+3] = (float)P[i][3].z;
       vals[ind+4] = (float)P[i][4].z;
   }
   QuickSort25(vals);
   R = vals[12];

   uint4 newVal = (uint4)((uint)B, (uint)G, (uint)R, (uint)255);
   coords = (int2)(idX+2, idY+2);
   write_imageui(imgFiltered, coords, newVal);
}

";
            #endregion

            #region Sobel operator, laplacian and thresholded points retrieval
            public string srcSobel = @"

//Applies a Sobel operator to image
__kernel void
         sobelWithoutLocal(__read_only  image2d_t imgSrc,
                           __write_only image2d_t imgFiltered,
                           __constant int*        SobelThreshold)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);
   
   float4 P[3][3];
   uint4 pix;
   
   int2 coords;
   for (int i=0;i<3;i++)
   {
       coords.x = x+i;
       coords.y = y;   pix = read_imageui(imgSrc, smp, coords); P[i][0] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+1; pix = read_imageui(imgSrc, smp, coords); P[i][1] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+2; pix = read_imageui(imgSrc, smp, coords); P[i][2] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);

   }

       float4 dx =-3.0f*P[0][0] - 10.0f*P[0][1] - 3.0f*P[0][2]
                  +3.0f*P[2][0] + 10.0f*P[2][1] + 3.0f*P[2][2];

       float4 dy =-3.0f*P[0][0] - 10.0f*P[1][0] - 3.0f*P[2][0]
                  +3.0f*P[0][2] + 10.0f*P[1][2] + 3.0f*P[2][2];
              
   dx = fabs(dx); dy = fabs(dy);

   float gxx = fmax(fmax(dx.x,dx.y),dx.z);
   float gyy = fmax(fmax(dy.x,dy.y),dy.z);

   float G = mad(-0.3f, native_sqrt(mad(gxx,gxx,gyy*gyy)), 255.0f);

   G = fmax(0.0f, G);
   G = G < SobelThreshold[0] ? 0.0f : 255.0f;

//   if (G < 100.0f) 
//   {
//       G = 0.0f;
//   }
//   else if (G < 180.0f) G = 160.0f;
//   else G = 255.0f;
   
   

   uint4 outP = (uint4)((uint)G, (uint)G, (uint)G, (uint)255);
   
   coords.x = x+1;
   coords.y = y+1;
   
   write_imageui(imgFiltered, coords, outP);
}

//Applies a Sobel operator to image
__kernel void
         sobelBytes       (__read_only  image2d_t imgSrc,
                           __global uchar*   imgFiltered,
                           __constant int*   SobelThreshold)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);
   int w = get_global_size(0)+2;
   
   float4 P[3][3];
   uint4 pix;
   
   int2 coords;
   for (int i=0;i<3;i++)
   {
       coords.x = x+i;
       coords.y = y;   pix = read_imageui(imgSrc, smp, coords); P[i][0] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+1; pix = read_imageui(imgSrc, smp, coords); P[i][1] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+2; pix = read_imageui(imgSrc, smp, coords); P[i][2] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);

   }

   float4 dx = 3.0f*P[0][0] + 10.0f*P[0][1] + 3.0f*P[0][2]
              -3.0f*P[2][0] - 10.0f*P[2][1] - 3.0f*P[2][2];

   float4 dy = 3.0f*P[0][0] + 10.0f*P[1][0] + 3.0f*P[2][0]
              -3.0f*P[0][0] - 10.0f*P[1][2] - 3.0f*P[2][2];
              
   dx = fabs(dx); dy = fabs(dy);

   float gxx = fmax(fmax(dx.x,dx.y),dx.z);
   float gyy = fmax(fmax(dy.x,dy.y),dy.z);

   float G = mad(-0.3f, native_sqrt(mad(gxx,gxx,gyy*gyy)), 255.0f);

   G = fmax(0.0f, G);
   G = G < SobelThreshold[0] ? 0.0f : 255.0f;

   imgFiltered[x+1 + w*(y+1)] = (uchar)G;
}

//global_work_dim = width-2
__kernel void
         RetrieveThresholdedPts(__read_only  image2d_t imgSobel,
                                __global          int* Points,
                                __constant        int* Dimensions)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int w = Dimensions[0];
   int h = Dimensions[1];

   int x = get_global_id(0)+1;

   int2 coords;
   uint4 pix; 


   int k = 0;
   for (int y=1; y<h-1; y++)
   {
      coords = (int2)(x, y);
      pix = read_imageui(imgSobel, smp, coords);
      if (pix.x == (uint)0)
      {
         int wk = w*k;
         Points[x + wk]   = x;
         Points[x + wk + w] = y;
         k+=2;
      }
   }
}


//Dimensions should be {Width-2, Height-2}
__kernel void
         RetrieveThresholdedPtsOld(__read_only  image2d_t imgSobel,
                                __global          int* curPt,
                                __global          int* Points)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0)+1;
   int y = get_global_id(1)+1;

   int2 coords = (int2)(x, y);
   uint4 pix = read_imageui(imgSobel, smp, coords);

   if (pix.x == (uint)0)
   {
      int k = atom_inc(&curPt[0]) << 1;
      Points[k]   = x;
      Points[k+1] = y;
   }
}

__kernel void
         Laplacian  (__read_only  image2d_t imgSrc,
                     __write_only image2d_t imgFiltered,
                     __constant int*        LaplThreshold)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);
   
   float4 P[3][3];
   uint4 pix;
   
   int2 coords;
   for (int i=0;i<3;i++)
   {
       coords.x = x+i;
       coords.y = y;   pix = read_imageui(imgSrc, smp, coords); P[i][0] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+1; pix = read_imageui(imgSrc, smp, coords); P[i][1] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+2; pix = read_imageui(imgSrc, smp, coords); P[i][2] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);

   }

   float4 L =  0.7f*P[0][0] + 1.0f*P[0][1] + 0.7f*P[0][2]
              +1.0f*P[1][0] - 6.8f*P[1][1] + 1.0f*P[1][2]
              +0.7f*P[2][0] + 1.0f*P[2][1] + 0.7f*P[2][2];

              
   float level = mad(-6.0f, fmin(L.x, fmin(L.y, L.z)), 255.0f);

   
   level = clamp(level, 0.0f, 255.0f);

   //G = G < SobelThreshold[0] ? 0.0f : 255.0f;

   uint4 outP = (uint4)((uint)level, (uint)level, (uint)level, (uint)255);
   
   coords.x = x+1;
   coords.y = y+1;
   
   write_imageui(imgFiltered, coords, outP);
}
";
            #endregion

            #region Generalized Hough transform

            public string srcHough = @"


__kernel void GeneralizedHough(__global       int* GHT,
                               __constant     int* Dimensions,
                               __global const int* ThresholdedPts,
                               __constant     int* SearchGeometry,
                               __constant     int* numGeomPts)

{
    int i = get_global_id(0)<<1;
    int SearchGeometryCount = numGeomPts[0];
    
    int w = Dimensions[0];
    int h = Dimensions[1];

    int x = ThresholdedPts[i];
    int y = ThresholdedPts[i+1];
    int xHough, yHough;

    for (int j = 0; j < SearchGeometryCount; j++)
    {
        xHough = x + SearchGeometry[j]; j++;
        yHough = y + SearchGeometry[j];

        if (xHough >= 0 && xHough < w && yHough >= 0 && yHough < h)
        {
            int indInc = xHough + w * yHough;
            atom_inc(&GHT[indInc]);
        }

    }
}

__kernel void GeneralizedHoughConstTP(__global       int* GHT,
                                      __constant     int* Dimensions,
                                      __constant int* ThresholdedPts,
                                      __constant int* SearchGeometry,
                                      __constant int* numGeomPts)

{
    int i = get_global_id(0)<<1;
    int SearchGeometryCount = numGeomPts[0];
    
    int w = Dimensions[0];
    int h = Dimensions[1];

    int x = ThresholdedPts[i];
    int y = ThresholdedPts[i+1];
    int xHough, yHough;

    for (int j = 0; j < SearchGeometryCount; j++)
    {
        xHough = x + SearchGeometry[j]; j++;
        yHough = y + SearchGeometry[j];

        if (xHough >= 0 && xHough < w && yHough >= 0 && yHough < h)
        {
            int indInc = xHough + w * yHough;
            atom_inc(&GHT[indInc]);
        }

    }
}

__kernel void ClearHough(__global int * GHT)
{
   int x = get_global_id(0);
   int w = get_global_size(0);
   int y = get_global_id(1);
   GHT[x + w*y] = 0;
}

__kernel void
         RetrievePointsOfInterest(__global const int* GHT,
                                  __global       int* curPt,
                                  __global       int* Points, 
                                  __global       int* Weights, 
                                  __constant     int* InterestThreshold)
{

   int x = get_global_id(0);
   int w = get_global_size(0);
   int y = get_global_id(1);

   int val = GHT[x + w*y];
   if (val >= InterestThreshold[0])
   {
      int k = atom_inc(&curPt[0]);
      Weights[k]  = val;
      k = k<<1;
      Points[k]   = x;
      Points[k+1] = y;
   }
}
";

            #endregion

            #region Line Hough Transform

            public string srcLineHough = @"

//global_size(0) = PtsOfInterest.Length>>1
__kernel void LineHoughTransform(__global     int* lineTransform,
                                 __global     int* Points,
                                 __constant float* sinTheta,
                                 __constant float* cosTheta,
                                 __constant float* theta,
                                 __constant float* rho)

{
    int nPtsTheta = (int)theta[2];
    int nPtsRho = (int)rho[2];
    float invPtsTheta = 1.0f/(nPtsTheta-1);
    //float invPtsTheta = native_recip((float)(nPtsTheta-1));

    int i = get_global_id(0);
    float x = (float)Points[i << 1];
    float y = (float)Points[1 + (i << 1)];


    float theta0 = theta[0];
    float thetaSpan = theta[1] - theta0;

    float rho0 = rho[0];
    //Rho index factor: i = (numPointsRho-1)*(rho[i]-rho0)/(rhof-rho0)
    float rhoIndFactor = ((float)nPtsRho - 1.0f)/(rho[1]-rho0);

    float curTheta;
    float curRho;

    for (int k = 0; k < nPtsTheta; k++)
    {
        curTheta = mad(thetaSpan, (float)k * invPtsTheta, theta0);
        curRho = x * cosTheta[k] + y * sinTheta[k];

        //Finds bin corresponding to rho value
        int rhoInd = (int)round((curRho - rho0) * rhoIndFactor);

        rhoInd = rhoInd >= nPtsRho ? -1 : rhoInd;

        if (rhoInd >= 0) 
            atom_inc(&lineTransform[rhoInd * nPtsTheta + k]);
    }

}
";

            #endregion

            #region Image thinning and shrinking
            public string srcThinning = @"


//Applies a thinnnig algorithm
__kernel __attribute__((reqd_work_group_size(BLOCK_SIZE, BLOCK_SIZE, 1))) void

         ImageThinning(__read_only  image2d_t imgSrc,
                       __write_only image2d_t imgFiltered)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
         
    __local uint4 P[BLOCK_SIZE+2][BLOCK_SIZE+2];
    
    //Identification of this workgroup
   int i = get_group_id(0);
   int j = get_group_id(1);

   //Identification of work-item
   int idX = get_local_id(0);
   int idY = get_local_id(1); 

   int ii = i*BLOCK_SIZE + idX;
   int jj = j*BLOCK_SIZE + idY;
   
   int2 coords = (int2)(ii, jj);

   //Reads pixels
   P[idX][idY] = read_imageui(imgSrc, smp, coords);

   //Needs to read extra for the 3x3 filter
   if (idX == BLOCK_SIZE-1 && idY == BLOCK_SIZE-1)
   {
      for (int p=0; p<3; p++)
      {
         coords.x = ii + p;
         for (int q=0; q<3; q++)
         {
            coords.y = jj + q;
            P[idX+p][idY+q] = read_imageui(imgSrc, smp, coords);
         }
      }
   }
   else if (idX == BLOCK_SIZE-1)
   {
      for (int p=1; p<3; p++)
      {
         coords.x = ii + p;
         P[idX+p][idY] = read_imageui(imgSrc, smp, coords);
      }
   }
   else if (idY == BLOCK_SIZE-1)
   {
      for (int q=1; q<3; q++)
      {
         coords.y = jj + q;
         P[idX][idY+q] = read_imageui(imgSrc, smp, coords);
      }
   }
   barrier(CLK_LOCAL_MEM_FENCE);
 
  //Analyzing pixel P[idX+1][idY+1]
  if (P[idX+1][idY+1].x == 0)
  {
      uchar PP[9];
   
      PP[0] = P[idX+1][idY].x   == 0 ? (uchar)1 : (uchar)0;
      PP[1] = P[idX+2][idY].x   == 0 ? (uchar)1 : (uchar)0;
      PP[2] = P[idX+2][idY+1].x == 0 ? (uchar)1 : (uchar)0;
      PP[3] = P[idX+2][idY+2].x == 0 ? (uchar)1 : (uchar)0;
      PP[4] = P[idX+1][idY+2].x == 0 ? (uchar)1 : (uchar)0;
      PP[5] = P[idX][idY+2].x   == 0 ? (uchar)1 : (uchar)0;
      PP[6] = P[idX][idY+1].x   == 0 ? (uchar)1 : (uchar)0;
      PP[7] = P[idX][idY].x     == 0 ? (uchar)1 : (uchar)0;
      PP[8] = PP[0];
 
 
    int N = 0;
    N = (int)PP[0] + (int)PP[1] + (int)PP[2] + (int)PP[3] + (int)PP[4] + (int)PP[5] + (int)PP[6] + (int)PP[7];

    int S = 0;
    for (int ii = 0; ii < 8; ii++)
    {
        if (PP[ii] == 0 && PP[ii + 1] == 1) S++;
    }
    
    uchar bb = (2 <= N && N <= 6 && S == 1 && (PP[0] == 0 || PP[2] == 0 || PP[4] == 0) && (PP[6] == 0 || PP[2] == 0 || PP[4] == 0) && PP[7] != 0) ? (uchar)255 : (uchar)0;
    
    P[idX+1][idY+1] = (uint4)((uint)bb, (uint)bb, (uint)bb, (uint)255);
  }
   
   barrier(CLK_LOCAL_MEM_FENCE);

   coords = (int2)(ii+1, jj+1);
   write_imageui(imgFiltered, coords, P[idX+1][idY+1]);  
}

//Applies a thinnnig algorithm
__kernel __attribute__((reqd_work_group_size(BLOCK_SIZE, BLOCK_SIZE, 1))) void

         ImageThinningByte(__global const uchar* imgSrc,
                           __global       uchar* imgFiltered,
                           __constant     int*   dimensions)
{
    __local uchar P[BLOCK_SIZE+2][BLOCK_SIZE+2];
    
    
   int w = dimensions[0];
   
    //Identification of this workgroup
   int i = get_group_id(0);
   int j = get_group_id(1);

   //Identification of work-item
   int idX = get_local_id(0);
   int idY = get_local_id(1); 

   int ii = i*BLOCK_SIZE + idX;
   int jj = j*BLOCK_SIZE + idY;
   
   int2 coords = (int2)(ii, jj);

   //Reads pixels
   P[idX][idY] = imgSrc[coords.x + w*coords.y];

   //Needs to read extra for the 3x3 filter
   if (idX == BLOCK_SIZE-1 && idY == BLOCK_SIZE-1)
   {
      for (int p=0; p<3; p++)
      {
         coords.x = ii + p;
         for (int q=0; q<3; q++)
         {
            coords.y = jj + q;
            P[idX+p][idY+q] = imgSrc[coords.x + w*coords.y];
         }
      }
   }
   else if (idX == BLOCK_SIZE-1)
   {
      for (int p=1; p<3; p++)
      {
         coords.x = ii + p;
         P[idX+p][idY] = imgSrc[coords.x + w*coords.y];
      }
   }
   else if (idY == BLOCK_SIZE-1)
   {
      for (int q=1; q<3; q++)
      {
         coords.y = jj + q;
         P[idX][idY+q] = imgSrc[coords.x + w*coords.y];
      }
   }
   barrier(CLK_LOCAL_MEM_FENCE);
 
  //Analyzing pixel P[idX+1][idY+1]
  if (P[idX+1][idY+1] == 0)
  {
      uchar PP[9];
   
      PP[0] = P[idX+1][idY]   == 0 ? (uchar)1 : (uchar)0;
      PP[1] = P[idX+2][idY]   == 0 ? (uchar)1 : (uchar)0;
      PP[2] = P[idX+2][idY+1] == 0 ? (uchar)1 : (uchar)0;
      PP[3] = P[idX+2][idY+2] == 0 ? (uchar)1 : (uchar)0;
      PP[4] = P[idX+1][idY+2] == 0 ? (uchar)1 : (uchar)0;
      PP[5] = P[idX][idY+2]   == 0 ? (uchar)1 : (uchar)0;
      PP[6] = P[idX][idY+1]   == 0 ? (uchar)1 : (uchar)0;
      PP[7] = P[idX][idY]     == 0 ? (uchar)1 : (uchar)0;
      PP[8] = PP[0] == 0 ? (uchar )1 : (uchar)0;
 
 
    int N = 0;
    N = (int)PP[0] + (int)PP[1] + (int)PP[2] + (int)PP[3] + (int)PP[4] + (int)PP[5] + (int)PP[6] + (int)PP[7];

    int S = 0;
    for (int ii = 0; ii < 8; ii++)
    {
        if (PP[ii] == 0 && PP[ii + 1] == 1) S++;
    }
    
    P[idX+1][idY+1] = (2 <= N && N <= 6 && S == 1 && (PP[0] == 0 || PP[2] == 0 || PP[4] == 0) && (PP[6] == 0 || PP[2] == 0 || PP[4] == 0) && PP[7] != 0) ? (uchar)255 : (uchar)0;
  }
   
   barrier(CLK_LOCAL_MEM_FENCE);

   coords = (int2)(ii+1, jj+1);
   imgFiltered[coords.x + w*coords.y] = P[idX+1][idY+1];  
}

//global_size = {Width, Height}
__kernel void ShrinkBmpBy3(__read_only  image2d_t imgSrc,
                           __write_only image2d_t imgShrink)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);         
   
   //Reads pixels that will need to be averaged
   int x3 = 3*x, y3 = 3*y;
   int2 coords;
   
   float4 newPix = (float4)(0.0f,0.0f,0.0f,0.0f);
   
   uint4 pix;
   for (int ii=0; ii < 3; ii++)
   {
      coords.x = x3+ii;
      for (int jj = 0; jj < 3; jj++)
      {
          coords.y = y3+jj;
          pix = read_imageui(imgSrc, smp, coords);
          newPix += (float4)((float)pix.x, (float)pix.y, (float)pix.z, (float)pix.w);
      }
   }
   newPix *= 0.11111111f;
   
   coords.x=x;coords.y=y;
   pix = (uint4)((uint)newPix.x,(uint)newPix.y,(uint)newPix.z,(uint)newPix.w);
   
   write_imageui(imgShrink, coords, pix);
}

//global_size = {Width, Height}
__kernel void BmpMean(__read_only  image2d_t imgSrc,
                      __read_only  image2d_t imgNext,
                      __write_only image2d_t imgMean,
                      __constant      float* SrcWeight)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);         
   

   int2 coords = (int2)(x,y);
   
   uint4 pix1 = read_imageui(imgSrc, smp, coords);
   uint4 pix2 = read_imageui(imgNext, smp, coords);
   
   float4 newPix = (float4)((float)pix1.x, (float)pix1.y, (float)pix1.z, (float)pix1.w);
   float4 newPix2 = (float4)((float)pix2.x, (float)pix2.y, (float)pix2.z, (float)pix2.w);

   //float4 newPix = (float4)(pix1);
   //float4 newPix2 = (float4)(pix2);
   float w = SrcWeight[0];
   newPix = w*newPix + newPix2;

   newPix /= w+1.0f;
   
   pix1 = (uint4)((uint)newPix.x,(uint)newPix.y,(uint)newPix.z,(uint)newPix.w);
   //pix1 = (uint4)(newPix);
   
   write_imageui(imgMean, coords, pix1);
}
";
            #endregion

            #region Erosion and dilation

            public string srcMorphologic = @"

//global_dim = {Width-WindowSize[0]/2, Height-WindowSize[0]/2}
__kernel void Dilate(read_only  image2d_t src,
                     write_only image2d_t dst,
                     __constant int*      WindowSize)


{

   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
   int wSize = WindowSize[0];
   int x = get_global_id(0);
   int y = get_global_id(1);

   //Found a black pixel?
   int found = -1;

   int2 coords; uint4 pix;
   for(int i=0;i<wSize;i++)
   {
      coords.x = x+i;
      for(int j=0;j<wSize;j++)
      {
         coords.y = y+j;
         pix = read_imageui(src, smp, coords);
         if (pix.x == 0) found = 1;
      }
   }
   
   wSize = wSize>>1;
   coords.x = x+wSize;
   coords.y = y+wSize;
   pix = found > 0 ? 
        (uint4)((uint)0,(uint)0,(uint)0,(uint)255) : 
        (uint4)((uint)255,(uint)255,(uint)255,(uint)255);
   
   write_imageui(dst, coords, pix);
}

//global_dim = {Width-WindowSize[0]/2, Height-WindowSize[0]/2}
__kernel void Erode (read_only  image2d_t src,
                     write_only image2d_t dst,
                     __constant int*      WindowSize)


{

   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
   int wSize = WindowSize[0];
   int x = get_global_id(0);
   int y = get_global_id(1);

   //Found any white pixels?
   int found = -1;

   int2 coords; uint4 pix;
   for(int i=0;i<wSize;i++)
   {
      coords.x = x+i;
      for(int j=0;j<wSize;j++)
      {
         coords.y = y+j;
         pix = read_imageui(src, smp, coords);
         if (pix.x > 0) found = 1;
      }
   }
   
   wSize = wSize>>1;
   coords.x = x+wSize;
   coords.y = y+wSize;
   pix = found < 0 ? 
        (uint4)((uint)0,(uint)0,(uint)0,(uint)255) : 
        (uint4)((uint)255,(uint)255,(uint)255,(uint)255);
   
   write_imageui(dst, coords, pix);
}

//global_dim = {Width-WindowSize[0]/2, Height-WindowSize[0]/2}
__kernel void DilateByte(__global const  uchar* src,
                         __global        uchar* dst,
                         __constant int*      WindowSize)
  

{
         
   int wSize = WindowSize[0];
   int x = get_global_id(0);
   int w = get_global_size(0)+(wSize>>1);
   int y = get_global_id(1);

   //Found a black pixel?
   int found = -1;

   int2 coords; uchar pix;
   for(int i=0;i<wSize;i++)
   {
      coords.x = x+i;
      for(int j=0;j<wSize;j++)
      {
         coords.y = y+j;
         pix = src[coords.x + w*coords.y];
         if (pix == 0) found = 1;
      }
   }
   
   wSize = wSize>>1;
   coords.x = x+wSize;
   coords.y = y+wSize;
   pix = found > 0 ? 
        (uchar)0 : 
        (uchar)255;
   
   dst[coords.x + w*coords.y] = pix;
}

//global_dim = {Width-WindowSize[0]/2, Height-WindowSize[0]/2}
__kernel void ErodeByte(__global const uchar* src,
                        __global       uchar* dst,
                        __constant int*      WindowSize)


{
         
   int wSize = WindowSize[0];
   int x = get_global_id(0);
   int w = get_global_size(0)+(wSize>>1);
   int y = get_global_id(1);

   //Found any white pixels?
   int found = -1;

   int2 coords; uchar pix;
   for(int i=0;i<wSize;i++)
   {
      coords.x = x+i;
      for(int j=0;j<wSize;j++)
      {
         coords.y = y+j;
         pix = src[coords.x + w*coords.y];
         if (pix > 0) found = 1;
      }
   }
   
   wSize = wSize>>1;
   coords.x = x+wSize;
   coords.y = y+wSize;
   pix = found < 0 ? 
        (uchar)0 : 
        (uchar)255;
   
   dst[coords.x + w*coords.y] = pix;
}

";

            #endregion
        }

        #endregion

        #region Static methods to deal with Geometry representation and transformation, image rescaling function

        /// <summary>Rescales a bitmap to the desired width/height</summary>
        /// <param name="bmp">Bitmap to rescale</param>
        /// <param name="newW">New width</param>
        /// <param name="newH">New height</param>
        public static Bitmap ResizeBitmap(Bitmap bmp, int newW, int newH)
        {
            Bitmap resp = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);

            BitmapData bmd = resp.LockBits(new Rectangle(0, 0, resp.Width, resp.Height),
             System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
             System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            //Write data
            unsafe
            {
                for (int y = 0; y < bmd.Height; y++)
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);

                    for (int x = 0; x < bmd.Width; x++)
                    {
                        float tempw = 1.0f / (float)bmd.Width * (float)bmdbmp.Width;
                        float temph = 1.0f / (float)bmd.Height * (float)bmdbmp.Height;

                        //opencl receives bmdbmp.height and width
                        int xmin = (int)(x * tempw);
                        int xmax = (int)(xmin + tempw);
                        int ymin = (int)(y * temph);
                        int ymax = (int)(ymin + temph);

                        float[] c = new float[4];
                        for (int i = 0; i < 3; i++)
                        {
                            for (int yy = ymin; yy < ymax; yy++)
                            {
                                byte* rowBmp = (byte*)bmdbmp.Scan0 + (yy * bmdbmp.Stride);
                                for (int xx = xmin; xx < xmax; xx++)
                                {
                                    c[i] += rowBmp[i + (xx << 2)];
                                }
                            }
                        }
                        float temp = 1.0f / ((xmax - xmin) * (ymax - ymin));
                        c[0] *= temp;
                        c[1] *= temp;
                        c[2] *= temp;

                        row[(x << 2)] = (byte)c[0]; // Data[x + width * y];
                        row[(x << 2) + 1] = (byte)c[1]; // Data[x + width * y];
                        row[(x << 2) + 2] = (byte)c[2]; // Data[x + width * y];
                        row[(x << 2) + 3] = 255; // Data[x + width * y];
                    }
                }
            }

            //Unlock bits
            resp.UnlockBits(bmd);
            bmp.UnlockBits(bmdbmp);

            return resp;

        }

        /// <summary>Shrinks bitmap by a factor of 3. Very fast.</summary>
        /// <param name="bmp">Bitmap to shrink</param>
        public static Bitmap ResizeBitmapShrink3(Bitmap bmp)
        {
            InitCL();

            CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
            CLCalc.Program.Image2D imgShrink = new CLCalc.Program.Image2D(new Bitmap(bmp.Width / 3, bmp.Height / 3));

            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, imgShrink };
            
            kernelShrinkBmpBy3.Execute(args, new int[] { imgShrink.Width, imgShrink.Height });

            return imgShrink.ReadBitmap();
        }

        /// <summary>Retrieves a vector representation of a B&W image where black pixels are the edges. Returns a set of [v0x v0y v1x v1y ...]
        /// representing vector from point to center</summary>
        /// <param name="bmp">Bitmap representing edge geometry.</param>
        /// <param name="dimensions">Dimensions {Width, Height} of generated geometry.</param>
        public static List<int> RetrieveGeometryVectors(Bitmap bmp, out int[] dimensions)
        {
            List<int> Pts = new List<int>();
            BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
             System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            //Reads zero-intensity pixels
            unsafe
            {
                byte* row;
                int ind;
                for (int y = 0; y < bmdbmp.Height; y++)
                {
                    row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                    for (int x = 0; x < bmdbmp.Width; x++)
                    {
                        ind = x << 2;
                        if (row[ind] == 0 && row[ind + 1] == 0 && row[ind + 2] == 0)
                        {
                            Pts.Add(x);
                            Pts.Add(y);
                        }
                    }
                }
            }

            bmp.UnlockBits(bmdbmp);

            //Finds center of gravity. Finds dimensions
            int[] c = new int[2];
            dimensions = new int[2];

            int xmin = Pts[0], xmax = Pts[0];
            int ymin = Pts[1], ymax = Pts[1];

            for (int i = 0; i < Pts.Count; i += 2)
            {
                c[0] += Pts[i];
                c[1] += Pts[i + 1];

                if (xmin > Pts[i]) xmin = Pts[i];
                if (xmax < Pts[i]) xmax = Pts[i];
                if (ymin > Pts[i + 1]) ymin = Pts[i + 1];
                if (ymax < Pts[i + 1]) ymax = Pts[i + 1];
            }
            dimensions[0] = xmax - xmin;
            dimensions[1] = ymax - ymin;

            c[0] /= Pts.Count >> 1;
            c[1] /= Pts.Count >> 1;

            //Reworks vectors to point to center of gravity. 
            for (int i = 0; i < Pts.Count; i += 2)
            {
                Pts[i] = c[0] - Pts[i];
                Pts[i + 1] = c[1] - Pts[i + 1];
            }


            return Pts;
        }

        /// <summary>Scales and rotates a given geometry vector</summary>
        /// <param name="Geometry">Geometry to rescale</param>
        /// <param name="scaleX">X scale. Greater than one - expands image, lower than one - shrinks image</param>
        /// <param name="scaleY">Y scale. Greater than one - expands image, lower than one - shrinks image</param>
        /// <param name="angle">Clockwise rotation angle in radians</param>
        public static List<int> ScaleRotateGeometryVector(List<int> Geometry, float scaleX, float scaleY, float angle)
        {
            List<int> resp = new List<int>();
            List<int[]> CheckRedundancy = new List<int[]>();

            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);

            for (int i = 0; i < Geometry.Count; i += 2)
            {
                float x0 = scaleX * (float)Geometry[i];
                float y0 = scaleY * (float)Geometry[i + 1];

                float x = c * x0 + s * y0;
                float y = -s * x0 + c * y0;

                int[] newPt = new int[2] { (int)Math.Round(x), (int)Math.Round(y) };

                int ind = -1;
                for (int j = 0; j < CheckRedundancy.Count; j++)
                {
                    if (CheckRedundancy[j][0] == newPt[0] && CheckRedundancy[j][1] == newPt[1])
                    {
                        ind = j;
                        j = CheckRedundancy.Count;
                    }
                }

                if (ind < 0)
                {
                    CheckRedundancy.Add(newPt);
                    resp.Add(newPt[0]);
                    resp.Add(newPt[1]);
                }
            }
            return resp;
        }

        /// <summary>Retrieves a bitmap representation of a given geometry</summary>
        /// <param name="Geometry">Geometry to represent</param>
        public static Bitmap RetrieveGeometryRepresentation(List<int> Geometry)
        {
            //Finds geometry dimensions
            int xmin = Geometry[0], xmax = Geometry[0], ymin = Geometry[1], ymax = Geometry[1];

            for (int i = 0; i < Geometry.Count; i += 2)
            {
                if (xmin > Geometry[i]) xmin = Geometry[i];
                if (xmax < Geometry[i]) xmax = Geometry[i];
                if (ymin > Geometry[i + 1]) ymin = Geometry[i + 1];
                if (ymax < Geometry[i + 1]) ymax = Geometry[i + 1];
            }

            int w = xmax - xmin + 1;
            int h = ymax - ymin + 1;

            Bitmap bmp = new Bitmap(w, h);

            BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                             System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                for (int i = 0; i < Geometry.Count; i += 2)
                {
                    byte* row = (byte*)bmdbmp.Scan0 + ((Geometry[i+1]-ymin) * bmdbmp.Stride);
                    int ind = (Geometry[i]-xmin) << 2;

                    row[ind + 3] = 255;
                }

            }

            bmp.UnlockBits(bmdbmp);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            return bmp;
        }

        /// <summary>Precomputes all geometries derived from a given geometry. Returns a List of Geometries (list ints) 
        /// and the corresponding parameters in GeomParameters</summary>
        /// <param name="Geometry">Base geometry</param>
        /// <param name="Parameters">Geometry modification parameters</param>
        /// <param name="GeomParameters">Geometry modification parameters for the specific geometry.</param>
        public static List<List<int>> GenerateGeometries(List<int> Geometry, GeometryFindParameters Parameters, out List<float[]> DerivedGeomParameters)
        {
            List<List<int>> DerivedGeoms = new List<List<int>>();
            DerivedGeomParameters = new List<float[]>();

            for (int indAng = 0; indAng < Parameters.AnglesToSearch.Length; indAng++)
            {
                if (Parameters.ScaleXYTogether)
                {
                    for (int indScale = 0; indScale < Parameters.XScales.Length; indScale++)
                    {
                        //Creates new geometry
                        List<int> curGeom = ScaleRotateGeometryVector(Geometry, Parameters.XScales[indScale],
                            Parameters.YScales[indScale], Parameters.AnglesToSearch[indAng]);
                        DerivedGeoms.Add(curGeom);

                        float[] GeomParameters = new float[] { Parameters.AnglesToSearch[indAng], Parameters.XScales[indScale], Parameters.YScales[indScale] };
                        DerivedGeomParameters.Add(GeomParameters);
                    }
                }
                else
                {
                    for (int iX = 0; iX < Parameters.XScales.Length; iX++)
                    {
                        for (int iY = 0; iY < Parameters.YScales.Length; iY++)
                        {
                            //Creates new geometry
                            List<int> curGeom = ScaleRotateGeometryVector(Geometry, Parameters.XScales[iX],
                                Parameters.YScales[iY], Parameters.AnglesToSearch[indAng]);
                            DerivedGeoms.Add(curGeom);

                            float[] GeomParameters = new float[] { Parameters.AnglesToSearch[indAng], Parameters.XScales[iX], Parameters.YScales[iY] };
                            DerivedGeomParameters.Add(GeomParameters);

                        }
                    }
                }
            }
            return DerivedGeoms;
        }

        #endregion


        #region Static methods for images, mostly test functions

        /// <summary>Test functions that helped develop OpenCL code</summary>
        public static class TestFuncs
        {
            /// <summary>Applies 5x5 median filter to a bitmap</summary>
            /// <param name="bmp">Bitmap to be filtered</param>
            public static Bitmap MedianFilter(Bitmap bmp)
            {
                InitCL();

                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {

                    CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                    CLCalc.Program.Image2D imgFilt = new CLCalc.Program.Image2D(bmp);

                    CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, imgFilt };

                    int groupSizeX = (bmp.Width - 4) / BLOCK_SIZE;
                    int groupSizeY = (bmp.Height - 4) / BLOCK_SIZE;

                    kernelmedianFilter.Execute(args, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });

                    return imgFilt.ReadBitmap();
                }
                else
                {
                    return MedianNoCL(bmp);
                }
            }

            private static Bitmap MedianNoCL(Bitmap bmpSrc)
            {
                Bitmap bmp = (Bitmap)bmpSrc.Clone();

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch(); sw.Start();
                //Reference (algorithm was modified):
                //http://fourier.eng.hmc.edu/e161/lectures/morphology/node2.html A thinning algorithm

                BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
     System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                BitmapData bmdsrc = bmpSrc.LockBits(new Rectangle(0, 0, bmpSrc.Width, bmpSrc.Height),
System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                //Reads zero-intensity pixels
                unsafe
                {
                    for (int y = 1; y < bmdbmp.Height - 1; y++)
                    {
                        byte* row = (byte*)bmdsrc.Scan0 + (y * bmdsrc.Stride);
                        byte* rowPrev = (byte*)bmdsrc.Scan0 + ((y - 1) * bmdsrc.Stride);
                        byte* rowNext = (byte*)bmdsrc.Scan0 + ((y + 1) * bmdsrc.Stride);


                        byte* rowDest = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);

                        for (int x = 1; x < bmdbmp.Width - 1; x++)
                        {
                            for (int k = 0; k < 2; k++)
                            {
                                int ind = (x << 2)+k;
                                int indPrev = ((x - 1) << 2)+k;
                                int indNext = ((x + 1) << 2)+k;

                                //Reads neighbors
                                float[] PP = new float[9];

                                //P7 P0 P1
                                //P6 C  P2
                                //P5 P4 P3

                                PP[0] = rowPrev[ind];
                                PP[1] = rowPrev[indNext];
                                PP[2] = row[indNext];
                                PP[3] = rowNext[indNext];
                                PP[4] = rowNext[ind];
                                PP[5] = rowNext[indPrev];
                                PP[6] = row[indPrev];
                                PP[7] = rowPrev[indPrev];
                                PP[8] = row[ind];

                                QuickSort9(PP);
                                rowDest[ind] = (byte)PP[4];
                            }

                        }
                    }


                }


                bmp.UnlockBits(bmdbmp);
                bmpSrc.UnlockBits(bmdsrc);

                sw.Stop();
                //this.Text = "Thinning " + sw.Elapsed.ToString();

                return bmp;
            }

            private static void QuickSort9(float[] vals)
            {
                //Start-stop indexes
                int[] subListStarts = new int[7];
                int[] subListEnds = new int[7];
                int nLists = 1;

                subListStarts[0] = 0;
                subListEnds[0] = 8;

                int ind = nLists - 1, ind0, indf, pivot;
                int leftIdx, rightIdx, inttemp, k;
                float temp;
                while (nLists > 0)
                {
                    ind0 = subListStarts[ind];
                    indf = subListEnds[ind];

                    pivot = (ind0 + indf) >> 1;
                    leftIdx = ind0;
                    rightIdx = indf;

                    while (leftIdx <= pivot && rightIdx >= pivot)
                    {
                        while (vals[leftIdx] < vals[pivot] && leftIdx <= pivot)
                            leftIdx++;
                        while (vals[rightIdx] > vals[pivot] && rightIdx >= pivot)
                            rightIdx--;

                        temp = vals[leftIdx];
                        vals[leftIdx] = vals[rightIdx];
                        vals[rightIdx] = temp;

                        leftIdx++;
                        rightIdx--;
                        if (leftIdx - 1 == pivot)
                        {
                            rightIdx++;
                            pivot = rightIdx;
                        }
                        else if (rightIdx + 1 == pivot)
                        {
                            leftIdx--;
                            pivot = leftIdx;
                        }
                    }

                    nLists--;
                    inttemp = subListStarts[nLists];
                    subListStarts[nLists] = subListStarts[ind];
                    subListStarts[ind] = inttemp;

                    inttemp = subListEnds[nLists];
                    subListEnds[nLists] = subListEnds[ind];
                    subListEnds[ind] = inttemp;

                    if (pivot - 1 - ind0 > 0)
                    {
                        subListStarts[nLists] = ind0;
                        subListEnds[nLists] = pivot - 1;
                        nLists++;
                    }
                    if (indf - pivot - 1 > 0)
                    {
                        subListStarts[nLists] = pivot + 1;
                        subListEnds[nLists] = indf;
                        nLists++;
                    }

                    for (k = 0; k < nLists; k++)
                    {
                        if (subListStarts[k] <= 4 && 4 <= subListEnds[k])
                        {
                            ind = k; k = nLists + 1;
                        }
                    }
                    if (k == nLists) nLists = 0;
                }
            }

            /// <summary>Applies edge operator to an image.</summary>
            /// <param name="bmp">Bitmap to be processed</param>
            public static Bitmap SobelEdge(Bitmap bmp, int threshold)
            {
                InitCL();
                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {
                    CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                    CLCalc.Program.Image2D imgFilt = new CLCalc.Program.Image2D(bmp);
                    CLCalc.Program.Variable sobelThresh = new CLCalc.Program.Variable(new int[] { threshold });

                    CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, imgFilt, sobelThresh };

                    kernelSobel.Execute(args, new int[] { bmp.Width - 2, bmp.Height - 2 });

                    return imgFilt.ReadBitmap();
                }
                else return SobelNoCL(bmp);
            }

            private static int PIXELSIZE = 4;
            private static Bitmap SobelNoCL(Bitmap bmp)
            {
                Bitmap resp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

                BitmapData bmd = resp.LockBits(new Rectangle(0, 0, resp.Width, resp.Height),
                 System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                 System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                float[] Gx = new float[bmp.Width * bmp.Height];
                float[] Gy = new float[bmp.Width * bmp.Height];

                unsafe
                {
                    for (int y = 1; y < bmd.Height - 1; y++)
                    {
                        byte* rowbmp = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                        byte* rowbmpNext = rowbmp + bmdbmp.Stride;
                        byte* rowbmpPrev = rowbmp - bmdbmp.Stride;

                        for (int x = 1; x < bmd.Width - 1; x++)
                        {
                            int xPixSize = x * PIXELSIZE;
                            float temp1, temp2 = 0, temp3 = 0;
                            temp1 = rowbmpPrev[xPixSize + PIXELSIZE] - rowbmpPrev[xPixSize - PIXELSIZE];
                            temp1 += 3.0f * (rowbmp[xPixSize + PIXELSIZE] - rowbmp[xPixSize - PIXELSIZE]);
                            temp1 += rowbmpNext[xPixSize + PIXELSIZE] - rowbmpNext[xPixSize - PIXELSIZE];

                            temp2 = rowbmpPrev[xPixSize + PIXELSIZE + 1] - rowbmpPrev[xPixSize - PIXELSIZE + 1];
                            temp2 += 3.0f * (rowbmp[xPixSize + PIXELSIZE + 1] - rowbmp[xPixSize - PIXELSIZE + 1]);
                            temp2 += rowbmpNext[xPixSize + PIXELSIZE + 1] - rowbmpNext[xPixSize - PIXELSIZE + 1];

                            temp3 = rowbmpPrev[xPixSize + PIXELSIZE + 2] - rowbmpPrev[xPixSize - PIXELSIZE + 2];
                            temp3 += 3.0f * (rowbmp[xPixSize + PIXELSIZE + 2] - rowbmp[xPixSize - PIXELSIZE + 2]);
                            temp3 += rowbmpNext[xPixSize + PIXELSIZE + 2] - rowbmpNext[xPixSize - PIXELSIZE + 2];

                            if (temp1 < 0) temp1 = -temp1; if (temp2 < 0) temp2 = -temp2; if (temp3 < 0) temp3 = -temp3;
                            Gx[x + y * bmp.Width] = Math.Max(temp1, Math.Max(temp2, temp3));

                            temp1 = rowbmpNext[xPixSize + PIXELSIZE] - rowbmpPrev[xPixSize + PIXELSIZE];
                            temp1 += 3.0f * (rowbmpNext[xPixSize] - rowbmpPrev[xPixSize]);
                            temp1 += rowbmpNext[xPixSize - PIXELSIZE] - rowbmpPrev[xPixSize - PIXELSIZE];

                            temp2 = rowbmpNext[xPixSize + PIXELSIZE + 1] - rowbmpPrev[xPixSize + PIXELSIZE + 1];
                            temp2 += 3.0f * (rowbmpNext[xPixSize + 1] - rowbmpPrev[xPixSize + 1]);
                            temp2 += rowbmpNext[xPixSize - PIXELSIZE + 1] - rowbmpPrev[xPixSize - PIXELSIZE + 1];

                            temp3 = rowbmpNext[xPixSize + PIXELSIZE + 2] - rowbmpPrev[xPixSize + PIXELSIZE + 2];
                            temp3 += 3.0f * (rowbmpNext[xPixSize + 2] - rowbmpPrev[xPixSize + 2]);
                            temp3 += rowbmpNext[xPixSize - PIXELSIZE + 2] - rowbmpPrev[xPixSize - PIXELSIZE + 2];

                            if (temp1 < 0) temp1 = -temp1; if (temp2 < 0) temp2 = -temp2; if (temp3 < 0) temp3 = -temp3;
                            Gy[x + y * bmp.Width] = Math.Max(temp1, Math.Max(temp2, temp3));
                        }
                    }
                }

                unsafe
                {
                    for (int y = 0; y < bmd.Height; y++)
                    {
                        byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);
                        byte* rowbmp = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);

                        for (int x = 0; x < bmd.Width; x++)
                        {
                            if (y >= bmd.Height - 2 || x >= bmd.Width - 2)
                            {
                                row[x * PIXELSIZE] = 255;
                                row[x * PIXELSIZE + 1] = 255;
                                row[x * PIXELSIZE + 2] = 255;
                                row[x * PIXELSIZE + 3] = 255;
                            }
                            else
                            {
                                float temp = (float)Math.Sqrt(Gx[x + y * bmp.Width] * Gx[x + y * bmp.Width] + Gy[x + y * bmp.Width] * Gy[x + y * bmp.Width]) * 0.5f;
                                if (temp < 32) temp = 0;
                                if (temp > 255) temp = 255;
                                temp = 255 - temp;
                                //threshold
                                temp = temp < 125 ? 0 : 255;
                                row[x * PIXELSIZE] = (byte)temp;
                                row[x * PIXELSIZE + 1] = (byte)temp;
                                row[x * PIXELSIZE + 2] = (byte)temp;
                                row[x * PIXELSIZE + 3] = 255;
                            }

                        }
                    }
                }


                //Unlock bits
                resp.UnlockBits(bmd);
                bmp.UnlockBits(bmdbmp);

                return resp;

            }

            /// <summary>Applies laplacian operator to an image.</summary>
            /// <param name="bmp">Bitmap to be processed</param>
            public static Bitmap Laplacian(Bitmap bmp)
            {
                InitCL();

                CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                CLCalc.Program.Image2D imgFilt = new CLCalc.Program.Image2D(new Bitmap(bmp.Width, bmp.Height));
                CLCalc.Program.Variable laplThresh = new CLCalc.Program.Variable(new int[] { 100 });

                CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, imgFilt, laplThresh };

                kernelLaplacian.Execute(args, new int[] { bmp.Width - 2, bmp.Height - 2 });

                return imgFilt.ReadBitmap();
            }


            /// <summary>Applies thinning operator to an image.</summary>
            /// <param name="bmp">Bitmap to be processed</param>
            public static Bitmap ApplyThinning(Bitmap bmp)
            {
                InitCL();
                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {

                    CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                    CLCalc.Program.Image2D imgFilt = new CLCalc.Program.Image2D(new Bitmap(bmp.Width, bmp.Height));

                    CLCalc.Program.MemoryObject[] args0 = new CLCalc.Program.MemoryObject[] { img, imgFilt };
                    CLCalc.Program.MemoryObject[] args1 = new CLCalc.Program.MemoryObject[] { imgFilt, img };

                    int groupSizeX = (bmp.Width - 2) / BLOCK_SIZE;
                    int groupSizeY = (bmp.Height - 2) / BLOCK_SIZE;

                    //The more thinning the merrier
                    kernelImageThinning.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                    kernelImageThinning.Execute(args1, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                    kernelImageThinning.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                    kernelImageThinning.Execute(args1, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                    kernelImageThinning.Execute(args0, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                    kernelImageThinning.Execute(args1, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });

                    return img.ReadBitmap();
                }
                else return thinningNoCL(bmp);
            }

            private static Bitmap thinningNoCL(Bitmap bmpSrc)
            {
                Bitmap bmp = (Bitmap)bmpSrc.Clone();

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch(); sw.Start();
                //Reference (algorithm was modified):
                //http://fourier.eng.hmc.edu/e161/lectures/morphology/node2.html A thinning algorithm

                BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
     System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                //Reads zero-intensity pixels
                for (int i = 0; i < 4; i++)
                {
                    unsafe
                    {
                        for (int y = 1; y < bmdbmp.Height - 1; y++)
                        {
                            byte* row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                            byte* rowPrev = (byte*)bmdbmp.Scan0 + ((y - 1) * bmdbmp.Stride);
                            byte* rowNext = (byte*)bmdbmp.Scan0 + ((y + 1) * bmdbmp.Stride);
                            for (int x = 1; x < bmdbmp.Width - 1; x++)
                            {
                                int ind = x << 2;
                                if (row[ind] == 0)
                                {
                                    int indPrev = (x - 1) << 2;
                                    int indNext = (x + 1) << 2;

                                    //Reads neighbors
                                    byte[] PP = new byte[9];

                                    //P7 P0 P1
                                    //P6 C  P2
                                    //P5 P4 P3

                                    PP[0] = rowPrev[ind] == 0 ? (byte)1 : (byte)0;
                                    PP[1] = rowPrev[indNext] == 0 ? (byte)1 : (byte)0;
                                    PP[2] = row[indNext] == 0 ? (byte)1 : (byte)0;
                                    PP[3] = rowNext[indNext] == 0 ? (byte)1 : (byte)0;
                                    PP[4] = rowNext[ind] == 0 ? (byte)1 : (byte)0;
                                    PP[5] = rowNext[indPrev] == 0 ? (byte)1 : (byte)0;
                                    PP[6] = row[indPrev] == 0 ? (byte)1 : (byte)0;
                                    PP[7] = rowPrev[indPrev] == 0 ? (byte)1 : (byte)0;
                                    PP[8] = PP[0];

                                    int N = 0;
                                    N = (int)PP[0] + (int)PP[1] + (int)PP[2] + (int)PP[3] + (int)PP[4] + (int)PP[5] + (int)PP[6] + (int)PP[7];

                                    int S = 0;
                                    for (int ii = 0; ii < 8; ii++)
                                    {
                                        if (PP[ii] == 0 && PP[ii + 1] == 1) S++;
                                    }

                                    if (2 <= N && N <= 6 && S == 1 && (PP[0] == 0 || PP[2] == 0 || PP[4] == 0) && (PP[6] == 0 || PP[2] == 0 || PP[4] == 0) && PP[7] != 0)
                                    {
                                        row[ind + 2] = 255;
                                    }
                                }

                            }
                        }

                        for (int y = 0; y < bmdbmp.Height; y++)
                        {
                            byte* row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                            for (int x = 0; x < bmdbmp.Width; x++)
                            {
                                int ind = x << 2;
                                if (x == 0 || x == bmp.Width - 1 || y == 0 || y == bmp.Height - 1)
                                {
                                    row[ind] = 255;
                                    row[ind + 1] = 255;
                                    row[ind + 2] = 255;
                                }
                                else
                                {
                                    if (row[ind + 2] == 255 && row[ind] == 0)
                                    {
                                        row[ind] = 255;
                                        row[ind + 1] = 255;
                                    }
                                }
                            }
                        }

                        //toRight = !toRight;
                    }
                }

                bmp.UnlockBits(bmdbmp);

                sw.Stop();
                //this.Text = "Thinning " + sw.Elapsed.ToString();

                return bmp;
            }

            /// <summary>Retrieves zero-intensity points from a Bitmap into an array</summary>
            /// <param name="bmp">Bitmap to process</param>
            public static List<int> RetrieveThresholdedPoints(Bitmap bmp)
            {
                List<int> Pts = new List<int>();

                #region OpenCL - proved to be slower
                /*
            CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);

            int[] ThreshPts = new int[bmp.Width * (bmp.Height >> 3)];
            int[] Dim = new int[] { bmp.Width, bmp.Height };



            CLCalc.Program.Variable CLDim = new CLCalc.Program.Variable(Dim);
            CLCalc.Program.Variable CLPts = new CLCalc.Program.Variable(ThreshPts);
            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, CLPts, CLDim };

            kernelRetrieveThresholdedPts.Execute(args, bmp.Width - 2);
            CLPts.ReadFromDeviceTo(ThreshPts);

            for (int x = 1; x < bmp.Width-1; x++)
            {
                for (int y = 0; y < bmp.Height>>3; y += 2)
                {
                    if (ThreshPts[x + y * bmp.Width] != 0)
                    {
                        int yw = y * bmp.Width;
                        Pts.Add(ThreshPts[x + yw]);
                        Pts.Add(ThreshPts[x + yw + bmp.Width]);
                    }
                    else y = bmp.Height;
                }
            }
            */
                #endregion

                // No OpenCL

                BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                 System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                unsafe
                {
                    byte* row;
                    int ind;
                    for (int y = 1; y < bmdbmp.Height - 3; y++)
                    {
                        row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                        for (int x = 1; x < bmdbmp.Width - 4; x++)
                        {
                            ind = x << 2;
                            if (row[ind] == 0)
                            {
                                Pts.Add(x);
                                Pts.Add(y);
                            }

                            x++;
                            ind = x << 2;
                            if (row[ind] == 0)
                            {
                                Pts.Add(x);
                                Pts.Add(y);
                            }

                            x++;
                            ind = x << 2;
                            if (row[ind] == 0)
                            {
                                Pts.Add(x);
                                Pts.Add(y);
                            }
                        }
                    }
                }

                bmp.UnlockBits(bmdbmp);


                return Pts;
            }



            /// <summary>Computes Generalized Hough Transform of a given image</summary>
            /// <param name="ThresholdedPts">Image boundary points</param>
            /// <param name="bmpWidth">Bitmap width</param>
            /// <param name="bmpHeight">Bitmap height</param>
            /// <param name="SearchGeometry">Geometry to look for</param>
            /// <param name="GeomDim">Geometry dimensions - width, height</param>
            /// <param name="PercentageToBeRelevant">Number of votes, as % of total geometry vectors, that a point has to have to be considered relevant</param>
            /// <param name="Centers">Centers found</param>
            public static int[] GeneralizedHoughTransform(List<int> ThresholdedPts, int bmpWidth, int bmpHeight, List<int> SearchGeometry, int[] GeomDim, float PercentageToBeRelevant, out List<float[]> Centers)
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swGHT = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swRelevant = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swRelevantCL = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swMergeCenters = new System.Diagnostics.Stopwatch();
                sw.Start();

                //Hough transform dimensions
                int w = bmpWidth;
                int h = bmpHeight;

                int[] GHT = new int[w * h];

                int qtdRelPts;
                int[] RelevantPts = new int[10000];
                int[] Weights = new int[5000];

                if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL)
                {
                    #region No OpenCL: preliminary
                    swGHT.Start();
                    for (int i = 0; i < ThresholdedPts.Count; i += 2)
                    {
                        //int i = get_global_id(0)<<1;
                        int x = ThresholdedPts[i];
                        int y = ThresholdedPts[i + 1];

                        for (int j = 0; j < SearchGeometry.Count; j += 2)
                        {
                            int xHough = x + SearchGeometry[j];
                            int yHough = y + SearchGeometry[j + 1];

                            if (xHough >= 0 && xHough < w && yHough >= 0 && yHough < h)
                            {
                                GHT[xHough + w * yHough]++;
                            }
                        }
                    }

                    //number of votes to consider a point as relevant
                    int nToBeRelevant = (int)((float)(SearchGeometry.Count >> 1) * PercentageToBeRelevant);

                    RelevantPts = new int[10000];
                    Weights = new int[5000];
                    qtdRelPts = 0;

                    int ghtInd;
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            ghtInd = x + w * y;
                            if (GHT[ghtInd] >= nToBeRelevant)
                            {
                                int ind = qtdRelPts << 1;
                                RelevantPts[ind] = x;
                                RelevantPts[1 + ind] = y;
                                Weights[qtdRelPts] = GHT[ghtInd];
                                qtdRelPts++;
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region OpenCL
                    CLCalc.Program.Variable CLght = new CLCalc.Program.Variable(GHT);
                    CLCalc.Program.Variable CLThreshPts = new CLCalc.Program.Variable(ThresholdedPts.ToArray());
                    CLCalc.Program.Variable CLdim = new CLCalc.Program.Variable(new int[] { bmpWidth, bmpHeight });
                    CLCalc.Program.Variable CLGeomCount = new CLCalc.Program.Variable(new int[] { SearchGeometry.Count });

                    swGHT.Start();

                    CLCalc.Program.Variable CLGeom = new CLCalc.Program.Variable(SearchGeometry.ToArray());

                    CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLght, CLdim, CLThreshPts, CLGeom, CLGeomCount };
                    kernelGeneralizedHough.Execute(args, ThresholdedPts.Count >> 1);
                    CLCalc.Program.Sync();
                    #endregion

                    swGHT.Stop();

                    swRelevant.Start();
                    CLght.ReadFromDeviceTo(GHT);


                    //number of votes to consider a point as relevant
                    int nToBeRelevant = (int)((float)(SearchGeometry.Count >> 1) * PercentageToBeRelevant);

                    RelevantPts = new int[10000];
                    Weights = new int[5000];
                    qtdRelPts = 0;

                    int ghtInd;
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            ghtInd = x + w * y;
                            if (GHT[ghtInd] >= nToBeRelevant)
                            {
                                int ind = qtdRelPts << 1;
                                RelevantPts[ind] = x;
                                RelevantPts[1 + ind] = y;
                                Weights[qtdRelPts] = GHT[ghtInd];
                                qtdRelPts++;
                            }
                        }
                    }
                    swRelevant.Stop();


                    CLCalc.Program.Variable CLRelPts = new CLCalc.Program.Variable(RelevantPts);
                    CLCalc.Program.Variable CLWeights = new CLCalc.Program.Variable(Weights);
                    int[] QtdRelPts = new int[1];
                    CLCalc.Program.Variable CLQtdRelPts = new CLCalc.Program.Variable(QtdRelPts);
                    CLCalc.Program.Variable CLnToBeRel = new CLCalc.Program.Variable(new int[] { nToBeRelevant });


                    swRelevantCL.Start();
                    args = new CLCalc.Program.Variable[] { CLght, CLQtdRelPts, CLRelPts, CLWeights, CLnToBeRel };
                    kernelRetrievePointsOfInterest.Execute(args, new int[] { bmpWidth, bmpHeight });

                    CLQtdRelPts.ReadFromDeviceTo(QtdRelPts);
                    qtdRelPts = QtdRelPts[0];
                    //Reads only the needed amount of points. Need to use Cloo for that
                    unsafe
                    {
                        fixed (void* ponteiro = RelevantPts)
                        {
                            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Read<int>((Cloo.ComputeBuffer<int>)CLRelPts.VarPointer, true, 0, QtdRelPts[0] << 1,
                                (IntPtr)ponteiro, null);
                        }

                        fixed (void* ponteiro = Weights)
                        {
                            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Read<int>((Cloo.ComputeBuffer<int>)CLWeights.VarPointer, true, 0, QtdRelPts[0],
                                (IntPtr)ponteiro, null);
                        }
                    }
                    swRelevantCL.Stop();


                    //List<int> lst1 = new List<int>();
                    //List<int> lst2 = new List<int>();
                    //for (int i = 0; i < 2 * qtdRelPts; i++)
                    //{
                    //    lst1.Add(RelevantPts[i]);
                    //    lst2.Add(RelevPts[i]);
                    //}
                    //lst1.Sort();
                    //lst2.Sort();
                    //double d = 0;
                    //for (int i = 0; i < lst1.Count; i++)
                    //{
                    //    d += Math.Abs(lst1[i] - lst2[i]);
                    //}
                }
                swMergeCenters.Start();

                Centers = new List<float[]>();
                float xx, yy, weight;
                int indd;

                int MergeIndex;
                for (int i = 0; i < qtdRelPts; i++)
                {
                    indd = i << 1;
                    xx = (float)RelevantPts[indd];
                    yy = (float)RelevantPts[indd + 1];
                    weight = (float)Weights[i];

                    MergeIndex = -1;
                    for (int j = Centers.Count - 1; j >= 0; j--)
                    {
                        if (Math.Abs(xx - Centers[j][0]) < 0.6f * (float)GeomDim[0] && Math.Abs(yy - Centers[j][1]) < 0.6f * (float)GeomDim[1])
                        {
                            //computes weighted average
                            float totWeight = weight + Centers[j][2];
                            float invTotWeight = 1.0f / totWeight;

                            Centers[j][0] = (Centers[j][0] * Centers[j][2] + xx * weight) * invTotWeight;
                            Centers[j][1] = (Centers[j][1] * Centers[j][2] + yy * weight) * invTotWeight;

                            Centers[j][2] = totWeight;

                            MergeIndex = j;
                            j = -1;
                        }
                    }

                    if (MergeIndex < 0)
                    {
                        Centers.Add(new float[] { xx, yy, weight });
                    }

                }
                swMergeCenters.Stop();
                sw.Stop();

                return GHT;
            }


            /// <summary>Returns a bitmap representation of a Hough transform</summary>
            /// <param name="GHT">Generalized Hough transform</param>
            /// <param name="w">Image width</param>
            /// <param name="h">Image height</param>
            /// <param name="numberOfGeometryVectors">Number of geometry vectors. Used to compute point relevance</param>
            /// <param name="PercentageToBeRelevant">Number of votes, as % of total geometry vectors, that a point has to have to be considered relevant</param>
            public static Bitmap GHTRepresentation(int[] GHT, int w, int h, int numberOfGeometryVectors, float PercentageToBeRelevant)
            {
                Bitmap bmp = new Bitmap(w, h);

                //Finds GHT maximum
                float invmax = 0;
                for (int i = 0; i < GHT.Length; i++)
                {
                    if (GHT[i] > invmax) invmax = GHT[i];
                }
                invmax = 255.0f / invmax;

                BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
     System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                int nToBeRelevant = (int)((float)numberOfGeometryVectors * PercentageToBeRelevant);

                //Reads zero-intensity pixels
                unsafe
                {
                    byte* row;
                    int ind, indGHT;
                    for (int y = 0; y < bmdbmp.Height; y++)
                    {
                        row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);
                        for (int x = 0; x < bmdbmp.Width; x++)
                        {
                            ind = x << 2;
                            indGHT = x + w * y;

                            byte b = (byte)(255.0f - invmax * (float)GHT[indGHT]);

                            if (GHT[indGHT] > nToBeRelevant)
                            {
                                row[ind] = b;
                                row[ind + 1] = b;
                                row[ind + 2] = 255;
                                row[ind + 3] = 255;
                            }
                            else
                            {
                                row[ind] = b;
                                row[ind + 1] = b;
                                row[ind + 2] = b;
                                row[ind + 3] = 255;
                            }
                        }
                    }
                }

                bmp.UnlockBits(bmdbmp);
                return bmp;
            }

            /// <summary>Blends two bitmaps. Current mean bitmap has weight 4.</summary>
            /// <param name="bmp">New bitmap to add</param>
            /// <param name="bmpMean">Current mean bitmap</param>
            public static Bitmap BitmapMean(Bitmap bmp, ref CLCalc.Program.Image2D bmpMean, float curWeight)
            {
                InitCL();

                CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                CLCalc.Program.Image2D imgAvg = new CLCalc.Program.Image2D(bmp);
                CLCalc.Program.Variable CLcurweight = new CLCalc.Program.Variable(new float[] { curWeight });

                CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { bmpMean, img, imgAvg, CLcurweight};

                kernelBmpMean.Execute(args, new int[] { bmp.Width, bmp.Height });

                bmpMean = imgAvg;

                return bmpMean.ReadBitmap();
            }

            /// <summary>Performs an Opening morphologic operation with a square of WindowSize</summary>
            /// <param name="bmp">B&W bitmap to Close</param>
            /// <param name="WindowSize">Square size</param>
            public static Bitmap MorphologicClosing(Bitmap bmp, int WindowSize)
            {
                InitCL();

                CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                CLCalc.Program.Image2D imgFilt = new CLCalc.Program.Image2D(new Bitmap(bmp.Width, bmp.Height));
                CLCalc.Program.Variable CLwindowSize = new CLCalc.Program.Variable(new int[] { WindowSize });

                CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, imgFilt, CLwindowSize };
                CLCalc.Program.MemoryObject[] args2 = new CLCalc.Program.MemoryObject[] { imgFilt, img, CLwindowSize };

                kernelDilate.Execute(args, new int[] { bmp.Width - (WindowSize >> 1), bmp.Height - (WindowSize >> 1) });
                kernelErode.Execute(args2, new int[] { bmp.Width - (WindowSize >> 1), bmp.Height - (WindowSize >> 1) });

                return img.ReadBitmap();
            }

            /// <summary>Performs a Closing morphologic operation with a square of WindowSize</summary>
            /// <param name="bmp">B&W bitmap to Open</param>
            /// <param name="WindowSize">Square size</param>
            public static Bitmap MorphologicOpening(Bitmap bmp, int WindowSize)
            {
                InitCL();

                CLCalc.Program.Image2D img = new CLCalc.Program.Image2D(bmp);
                CLCalc.Program.Image2D imgFilt = new CLCalc.Program.Image2D(new Bitmap(bmp.Width, bmp.Height));
                CLCalc.Program.Variable CLwindowSize = new CLCalc.Program.Variable(new int[] { WindowSize });

                CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { img, imgFilt, CLwindowSize };
                CLCalc.Program.MemoryObject[] args2 = new CLCalc.Program.MemoryObject[] { imgFilt, img, CLwindowSize };

                kernelErode.Execute(args, new int[] { bmp.Width - (WindowSize >> 1), bmp.Height - (WindowSize >> 1) });
                kernelDilate.Execute(args2, new int[] { bmp.Width - (WindowSize >> 1), bmp.Height - (WindowSize >> 1) });

                return img.ReadBitmap();
            }
        }

        #endregion
    }
    

}
