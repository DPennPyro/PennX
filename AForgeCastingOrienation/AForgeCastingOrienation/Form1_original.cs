using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging.Filters;
using AForge.Imaging;
using System.Drawing.Imaging;
using System.Diagnostics;
using AForge.Math.Geometry;


namespace AForgeCastingOrienation
{
    public partial class Form1 : Form
    {
        public bool CannyEdgeDetector = false;
        public bool DifferenceEdgeDetector = false;
        public bool HomogenityEdgeDetector = false;
        public bool SobelEdgeDetector = false;
        public bool CaptureFrame { get; set; }


 
        public bool ConservativeSmoothing { get; set; }
        public bool Invert { get; set; }
        public bool HSLswitch { get; set; }
        public bool sepiaSwitch { get; set; }
        public bool Skeletonization { get; set; }

        public bool bwSwitch { get; set; }

        public bool findShapes { get; set; }

        FilterInfoCollection videoDevices;
        private Bitmap SourceFrame;
        private FiltersSequence filterList = new FiltersSequence();
        private IFilter grayscaleFilter = new GrayscaleBT709();
        private IFilter pixellateFilter = new Pixellate();
        private Difference differenceFilter = new Difference();
        private MoveTowards moveTowardsFilter = new MoveTowards();
      

        private Stopwatch stopWatch = null;
       

        public Form1()
        {
            InitializeComponent();
            FillSourceDropDown();
            CaptureFrame = false;
            ConservativeSmoothing = false;
            Invert = false;
            findShapes = false;
        }


        public void FillSourceDropDown()
        {
            //List CAMS
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    throw new Exception();
                }

                for (int i = 1, n = videoDevices.Count; i <= n; i++)
                {
                    string cameraName = i + " : " + videoDevices[i - 1].Name;
                    camera1Combo.Items.Add(cameraName);
                }

                camera1Combo.SelectedIndex = 0;
            }
            catch
            {
                btnActivate.Enabled = false;
                camera1Combo.Items.Add("No cameras found");
                camera1Combo.SelectedIndex = 0;
                camera1Combo.Enabled = false;
            }
            return;
        }

        private void ActivateCamra()
        {
            VideoCaptureDevice videoSource = new VideoCaptureDevice(videoDevices[camera1Combo.SelectedIndex].MonikerString);

            // filter  original frame
            vpSource.VideoSource = videoSource;
            vpSource.Start();
           
            //for frame rate calculations
            stopWatch = null;
            timer.Start();

        }

        private void StopCameras()
        {
            timer.Stop();
            vpSource.SignalToStop();         
            vpSource.WaitForStop();     
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            ActivateCamra();
            btnActivate.Enabled = false;
            btnCancel.Enabled = true;

            gbEdgeFilters.Enabled = true;
            gbImgFilters.Enabled = true;

            
            //get frame
            Application.DoEvents();
            System.Threading.Thread.Sleep(2000);
            CaptureFrame = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            StopCameras();
            btnActivate.Enabled = true;
            btnCancel.Enabled = false;

            gbEdgeFilters.Enabled = false;
            rbNone.Checked = true;

            gbImgFilters.Enabled = false;

            cbConservativeSmoothing.Checked = false;
            cbInvert.Checked = false;
            cbHSL.Checked = false;
            cbSepia.Checked = false;
            cbSkeletonization.Checked = false;
            cbBW.Checked = false;

            pbCapture.Image = null;
            pbShapes.Image = null;
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            CaptureFrame = true;
        }

        private void vpSource_NewFrame(object sender, ref Bitmap image)
        {

            if (CaptureFrame)
            {
               
                FillPictureBoxes(ref image);

               // AForge.Imaging.Filters.HueModifier filter = new AForge.Imaging.Filters.HueModifier(242);
                // filter.ApplyInPlace(image);

                //AForge.Imaging.Filters.HSLFiltering filter =    new AForge.Imaging.Filters.HSLFiltering();
                //filter.Hue = new IntRange(400, 20);
                //filter.UpdateHue = true;
                //filter.UpdateLuminance = true;


                //filter.ApplyInPlace(image);


                CaptureFrame = false;
            }


            //if (test)
            //{
            //    // create filter
            //    CannyEdgeDetector filter = new CannyEdgeDetector();
            //    // apply the filter
            //    Bitmap gsImage = Grayscale.CommonAlgorithms.BT709.Apply(image);
            //    filter.ApplyInPlace(gsImage);

            //    //resize img to fit picturebox
            //    ResizeBicubic resizeFilter = new ResizeBicubic(pbCapture.Width, pbCapture.Height);
            //    gsImage = resizeFilter.Apply(gsImage);

            //    pbCapture.Image = gsImage;
            //   // image = DrawFocusArea(gsImage);
            //}
            //else
            //{
            //   // image = DrawFocusArea(image);
            //}
          
        }

        private void FillPictureBoxes(ref Bitmap image)
        {
            Bitmap tmpImg = image;
            Bitmap tmpImg2 = image;


            try
            {


                bool hasFilter = false;
                //setup resize and filtersequesce


                //resize img to fit picturebox
                ResizeBicubic resizeFilter = new ResizeBicubic(0, 0);

                resizeFilter = new ResizeBicubic(pbCapture.Width, pbCapture.Height);
                tmpImg = resizeFilter.Apply(tmpImg);

                resizeFilter = new ResizeBicubic(pbShapes.Width, pbShapes.Height);
                tmpImg2 = resizeFilter.Apply(tmpImg2);




                FiltersSequence processingFilter = new FiltersSequence();


                //List all filters
                IFilter ConservativeSmoothingFilter = new AForge.Imaging.Filters.ConservativeSmoothing();
                IFilter InvertFilter = new AForge.Imaging.Filters.Invert();
                IFilter HSLFilteringFilter = new AForge.Imaging.Filters.HSLFiltering();
                IFilter SepiaFilter = new AForge.Imaging.Filters.Sepia();
                IFilter grayscaleFilter = new AForge.Imaging.Filters.GrayscaleBT709();
                IFilter SkeletonizationFilter = new AForge.Imaging.Filters.SimpleSkeletonization();
                IFilter pixFilter = new AForge.Imaging.Filters.Pixellate();


                ////apply filter and process img---------------------------------------------






                if (ConservativeSmoothing)
                {
                    processingFilter.Add(ConservativeSmoothingFilter);
                    hasFilter = true;
                }

                if (Invert)
                {
                    processingFilter.Add(InvertFilter);
                    hasFilter = true;
                }

                if (HSLswitch)
                {
                    processingFilter.Add(HSLFilteringFilter);
                    hasFilter = true;
                }

                if (sepiaSwitch)
                {
                    processingFilter.Add(SepiaFilter);
                    hasFilter = true;
                }


                if (Skeletonization)
                {
                    processingFilter.Add(grayscaleFilter);
                    processingFilter.Add(SkeletonizationFilter);
                    hasFilter = true;
                }

                //apply the filter(s) to image
                if (hasFilter)
                {
                    //tmpImg = processingFilter.Apply(tmpImg);
                    tmpImg2 = processingFilter.Apply(tmpImg2);
                }

                processingFilter.Clear();


                if (bwSwitch)
                {
                    switchBandW(ref tmpImg);
                }



                if (CannyEdgeDetector)
                {
                    // create filter
                    CannyEdgeDetector filter = new CannyEdgeDetector();
                    // apply the filter
                    tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                    filter.ApplyInPlace(tmpImg);


                    // image = DrawFocusArea(gsImage);
                }
                else
                {
                    // image = DrawFocusArea(image);
                }


                if (DifferenceEdgeDetector)
                {
                    DifferenceEdgeDetector dFilter = new DifferenceEdgeDetector();
                    // apply the filter
                    tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                    dFilter.ApplyInPlace(tmpImg);
                }


                if (HomogenityEdgeDetector)
                {
                    HomogenityEdgeDetector hFilter = new HomogenityEdgeDetector();
                    // apply the filter
                    tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                    hFilter.ApplyInPlace(tmpImg);
                }


                if (SobelEdgeDetector)
                {

                    SobelEdgeDetector hFilter = new SobelEdgeDetector();
                    // apply the filter
                    tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                    hFilter.ApplyInPlace(tmpImg);

                    BlobCounter bc = new BlobCounter(tmpImg);
                    Rectangle[] brecs = bc.GetObjectsRectangles();


                    //Graphics pg = Graphics.FromImage(tmpImg);
                    //Pen p = new Pen(Color.White, 2);

                    //foreach (Rectangle r in brecs)
                    //{
                    //    pg.DrawRectangle(p, r);
                    //}

                }




                if (findShapes)
                {
                    tmpImg = FindShapes(tmpImg, ref tmpImg2);
                    //ProcessImage(image);
                }
                else
                {
                    pbCapture.Image = tmpImg;//set picturebox image----------------
                    pbShapes.Image = tmpImg2;//set picturebox image----------------
                }





                // Graphics g = Graphics.FromImage(tmpImg);
                // Pen p = new Pen(Color.Red, 2);

                // Rectangle lr = new Rectangle(100, 120, 80, 40);
                //// Rectangle rr = new Rectangle(360, 220, 80, 40);

                // g.DrawRectangle(p, lr);
                // //g.DrawRectangle(p, rr);

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }


          //  pbCapture.Image = tmpImg;//set picturebox image----------------
         //   pbShapes.Image = tmpImg2;//set picturebox image----------------
          
         
        }

        private void ProcessImage(Bitmap bitmap)
        {
            // lock image
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            // step 1 - turn background to black
            ColorFiltering colorFilter = new ColorFiltering();

            colorFilter.Red = new IntRange(0, 64);
            colorFilter.Green = new IntRange(0, 64);
            colorFilter.Blue = new IntRange(0, 64);
            colorFilter.FillOutsideRange = false;

            colorFilter.ApplyInPlace(bitmapData);

            // step 2 - locating objects
            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 5;
            blobCounter.MinWidth = 5;

            blobCounter.MaxHeight = 50;
            blobCounter.MaxWidth = 50;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            bitmap.UnlockBits(bitmapData);

            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Graphics g = Graphics.FromImage(bitmap);
            Pen yellowPen = new Pen(Color.Yellow, 2); // circles
            Pen redPen = new Pen(Color.Red, 2);       // quadrilateral
            Pen brownPen = new Pen(Color.Brown, 2);   // quadrilateral with known sub-type
            Pen greenPen = new Pen(Color.Green, 2);   // known triangle
            Pen bluePen = new Pen(Color.Blue, 2);     // triangle

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);

               AForge.Point center;
               float radius;

                // is circle ?
               if (shapeChecker.IsCircle(edgePoints, out center, out radius))
               {
                   g.DrawEllipse(yellowPen,
                       (float)(center.X - radius), (float)(center.Y - radius),
                       (float)(radius * 2), (float)(radius * 2));
               }
                else
                {
                    List<IntPoint> corners;

                    // is triangle or quadrilateral
                    if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                    {
                        // get sub-type
                        PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);

                        Pen pen;

                        if (subType == PolygonSubType.Unknown)
                        {
                            pen = (corners.Count == 4) ? redPen : bluePen;
                        }
                        else
                        {
                            pen = (corners.Count == 4) ? brownPen : greenPen;
                        }

                        g.DrawPolygon(pen, ToPointsArray(corners));
                    }
                }
            }

            yellowPen.Dispose();
            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            brownPen.Dispose();
            g.Dispose();

            // put new image to clipboard
           // Clipboard.SetDataObject(bitmap);
            // and to picture box
            pbShapes.Image = bitmap;

           // UpdatePictureBoxPosition();
        }




        private void switchBandW(ref Bitmap tmpImg)
        {
            Bitmap orig = tmpImg;
            Bitmap clone = new Bitmap(orig.Width, orig.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }

            FiltersSequence commonSeq = new FiltersSequence();
            commonSeq.Add(Grayscale.CommonAlgorithms.BT709);
            commonSeq.Add(new BradleyLocalThresholding());
            commonSeq.Add(new DifferenceEdgeDetector());

            tmpImg = commonSeq.Apply(clone);
        }

        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);
            }

            return array;
        }
      
        private void cbConservativeSmoothing_CheckedChanged(object sender, EventArgs e)
        {
            if (cbInvert.Checked)
            {
                ConservativeSmoothing = true;              
            }
            else
            {
                ConservativeSmoothing = false;
            }
            CaptureFrame = true;
        }
        private void cbInvert_CheckedChanged(object sender, EventArgs e)
        {
            if (cbInvert.Checked)
            {
                Invert = true;
            }
            else
            {
                Invert = false;
            }
            CaptureFrame = true;
            
        }
        private void cbHSL_CheckedChanged(object sender, EventArgs e)
        {
            if (cbHSL.Checked)
            {
                HSLswitch = true;             
            }
            else
            {
                HSLswitch = false;
            }
            CaptureFrame = true;
        }
        private void cbSepia_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSepia.Checked)
            {
                sepiaSwitch = true;               
            }
            else
            {
                sepiaSwitch = false;
            }
            CaptureFrame = true;
        }
        private void cbSkeletonization_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSkeletonization.Checked)
            {
                Skeletonization = true;               
            }
            else
            {
                Skeletonization = false;
            }
            CaptureFrame = true;
        }
        private void cbBW_CheckedChanged(object sender, EventArgs e)
        {
            if (cbBW.Checked)
            {
                bwSwitch = true;
            }
            else
            {
                bwSwitch = false;
            }
            CaptureFrame = true;
        }
        private void cbFindShapes_CheckedChanged(object sender, EventArgs e)
        {
            if (cbFindShapes.Checked)
            {
                findShapes = true;
            }
            else
            {
                findShapes = false;              
            }
            CaptureFrame = true;
        }


        private Bitmap DrawFocusArea(Bitmap img)
        {
            Graphics g = Graphics.FromImage(img);
            Pen p = new Pen(Color.Red, 2);

            Rectangle lr = new Rectangle(100, 220, 80, 40);
            Rectangle rr = new Rectangle(360, 220, 80, 40);

            g.DrawRectangle(p, lr);
            g.DrawRectangle(p, rr);

            return img;
           
        }


        private Bitmap FindShapes(Bitmap img, ref Bitmap MarkedImg)
        {
            Blob[] Mblobs = new Blob[0];
         

            for (int x = 0; x < 359; x++)
            {
                System.Threading.Thread.Sleep(300);
                //  RotateNearestNeighbor filter = new RotateNearestNeighbor(1 , true);
                // apply the filter
                //  img = filter.Apply(img);

                RotateBilinear filter = new RotateBilinear(1, true);
                 //apply the filter
                  img = filter.Apply(img);


                //Find blobs
                try
                {
                    BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, img.PixelFormat);


                    // step 1 - turn background to black
                    ////ColorFiltering colorFilter = new ColorFiltering();

                    ////colorFilter.Red = new IntRange(0, 64);
                    ////colorFilter.Green = new IntRange(0, 64);
                    ////colorFilter.Blue = new IntRange(0, 64);

                    ////colorFilter.FillOutsideRange = false;

                    ////colorFilter.ApplyInPlace(bitmapData);

                    // step 2 - locating objects
                    BlobCounter blobCounter = new BlobCounter();

                    blobCounter.FilterBlobs = true;
                    blobCounter.MinHeight = 15;
                    blobCounter.MinWidth = 15;

                    blobCounter.MaxHeight = 100;
                    blobCounter.MaxWidth = 100;

                    blobCounter.ProcessImage(bitmapData);
                    Blob[] blobs = blobCounter.GetObjectsInformation();
                    img.UnlockBits(bitmapData);

                    if (Mblobs.Length == 0 && blobs.Length > 0)
                    {
                        Mblobs = new Blob[blobs.Length];
                        blobs.CopyTo(Mblobs, 0);
                    }
                    else if (Mblobs.Length > 0 && blobs.Length > 0)
                    {
                        Blob[] temp = Mblobs;
                        Mblobs = new Blob[temp.Length + blobs.Length];
                        temp.CopyTo(Mblobs, 0);
                        blobs.CopyTo(Mblobs, Mblobs.Length - 1);
                    }


                    //find shapes

                    SimpleShapeChecker shapeChecker = new SimpleShapeChecker();


                    Graphics g = Graphics.FromImage(MarkedImg);
                    Pen yellowPen = new Pen(Color.Yellow, 4); // circles
                    Pen redPen = new Pen(Color.Red, 4);       // quadrilateral
                    Pen brownPen = new Pen(Color.Brown, 4);   // quadrilateral with known sub-type
                    Pen greenPen = new Pen(Color.Green, 4);   // known triangle
                    Pen bluePen = new Pen(Color.Blue, 4);     // triangle

                    //test draw
                    // g.DrawEllipse(redPen, MarkedImg.Width / 2 + 10, MarkedImg.Height / 2,1, 20);
                    // g.DrawEllipse(redPen, MarkedImg.Width / 2, MarkedImg.Height / 2+10, 20, 1);


                    for (int i = 0; i < Mblobs.Length; i++)
                    {
                       List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(Mblobs[i]);

                        AForge.Point center;
                        float radius;

                        // is circle ?
                        if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                        {
                            g.DrawEllipse(yellowPen,
                                (float)(center.X - radius), (float)(center.Y - radius),
                                (float)(radius * 2), (float)(radius * 2));
                        }
                        else
                        {



                            List<IntPoint> corners;

                            // is triangle or quadrilateral
                            if (shapeChecker.IsConvexPolygon(edgePoints, out corners) )
                            {
                                // get sub-type
                                PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);

                                Pen pen;

                                if (subType == PolygonSubType.Unknown)
                                {
                                    pen = (corners.Count == 4) ? redPen : bluePen;
                                }
                                else
                                {
                                    pen = (corners.Count == 4) ? brownPen : greenPen;
                                }


                                g.DrawPolygon(pen, ToPointsArray(corners));
                                
                            }
                        }
                    }

                    yellowPen.Dispose();
                    redPen.Dispose();
                    greenPen.Dispose();
                    bluePen.Dispose();
                    brownPen.Dispose();
                    g.Dispose();



                    pbCapture.Image = img;//set picturebox image----------------
                    pbShapes.Image = MarkedImg;//set picturebox image----------------

                   

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
           

            /////////////////////////////////////////////
          

            /////////////////////////////////////////////////////////
            return img;
        }

                
        //public Boolean CompareImages(Bitmap img, Rectangle lr, Rectangle rr, double compareLevel, float similarityThreshold)
        //{
        //    pbMaster.Image = null;
        //    pbCurrent.Image = null;

        //    // Load images into bitmaps
        //    var imageOne = new Bitmap(img);
        //    //var imageTwo = new Bitmap(img);


        //    Rectangle l = new Rectangle(168, 135, 41, 26);


        //    var newBitmap1 = ChangePixelFormat(new Bitmap(imageOne), System.Drawing.Imaging.PixelFormat.Format24bppRgb,l);

        //    if (cbNewMaster.Checked)
        //    {
        //        newBitmap1.Save(@"C:\temp\Master.bmp");
        //    }
        //    pbCurrent.Image = newBitmap1;

        //    var MasterBitmap = new Bitmap(@"C:\temp\Master.bmp");
        //    pbMaster.Image = MasterBitmap;
           

        //    //  newBitmap1 = SaveBitmapToFile(newBitmap1, filepath, image, BitMapExtension);
           
        //    //var newBitmap2 = ChangePixelFormat(new Bitmap(imageTwo), System.Drawing.Imaging.PixelFormat.Indexed, rr);

        //   // newBitmap2 = SaveBitmapToFile(newBitmap2, filepath, targetImage, BitMapExtension);

        //    // Setup the AForge library
        //    ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(similarityThreshold);
           
           


        //    // Process the images
        //    var results = tm.ProcessImage(newBitmap1, MasterBitmap);
          
          
        //    // Compare the results, 0 indicates no match so return false
        //    if (results.Length <= 0)
        //    {
        //        return false;
        //    }
        //    lblPercent.Text = (double.Parse(results[0].Similarity.ToString())*100).ToString();

        //    // Return true if similarity score is equal or greater than the comparison level
        //    var match = results[0].Similarity >= compareLevel;

        //    //BitmapData data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),ImageLockMode.ReadWrite, img.PixelFormat);
        //    //foreach (TemplateMatch m in results)
        //    //{
        //    //    Drawing.Rectangle(data, m.Rectangle, Color.White);
        //    //}

            
        //   return match;
        //}
     
         
        /// <summary>
        /// Change the pixel format of the bitmap image
        /// </summary>
        /// <param name="inputImage">Bitmapped image</param>
        /// <param name="newFormat">Bitmap format - 24bpp</param>
        /// <returns>Bitmap image</returns>
        private static Bitmap ChangePixelFormat(Bitmap inputImage, System.Drawing.Imaging.PixelFormat newFormat,Rectangle r)
        {
            return (inputImage.Clone(r, newFormat));
        }

    

        private void pbMaster_Click(object sender, EventArgs e)
        {
          
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //var MasterBitmap = new Bitmap(@"C:\temp\Master.bmp");
            //pbMaster.Image = MasterBitmap;
        }

       

        public void DetectCorners()
        {
            // Load image and create everything you need for drawing
            Bitmap image = new Bitmap(pbCapture.Image);
            Graphics graphics = Graphics.FromImage(image);
            SolidBrush brush = new SolidBrush(Color.Red);
            Pen pen = new Pen(brush);

            // Create corner detector and have it process the image
            MoravecCornersDetector mcd = new MoravecCornersDetector();
            List<IntPoint> corners = mcd.ProcessImage(image);

            // Visualization: Draw 3x3 boxes around the corners
            foreach (IntPoint corner in corners)
            {
                graphics.DrawRectangle(pen, corner.X - 1, corner.Y - 1, 3, 3);
            }

            // Display
            pbCapture.Image = image;
        }


        

     

        private void rbNone_CheckedChanged(object sender, EventArgs e)
        {
            CannyEdgeDetector = false;
            DifferenceEdgeDetector = false;
            HomogenityEdgeDetector = false;
            SobelEdgeDetector = false;
            CaptureFrame = true;
        }

        private void rbCannyEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            if (rbCannyEdgeDetector.Checked)
            {
                CannyEdgeDetector = true;
                CaptureFrame = true;
            }
            else
            {
                CannyEdgeDetector = false;
            }
        }

        private void rbDifferenceEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            if (rbDifferenceEdgeDetector.Checked)
            {
                DifferenceEdgeDetector = true;
                CaptureFrame = true;
            }
            else
            {
                DifferenceEdgeDetector = false;
            }
        }

        private void rbHomogenityEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHomogenityEdgeDetector.Checked)
            {
                HomogenityEdgeDetector = true;
                CaptureFrame = true;
            }
            else
            {
                HomogenityEdgeDetector = false;
            }
        }

        private void rbSobelEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSobelEdgeDetector.Checked)
            {
                SobelEdgeDetector = true;
                CaptureFrame = true;
            }
            else
            {
                SobelEdgeDetector = false;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Bitmap image = new Bitmap(pbShapes.Image);
            image.Save(@"C:\\temp\Test.png");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bitmap image = new Bitmap(pbCapture.Image);
            image.Save(@"C:\\temp\Cap.png");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

      
       
    }
}
