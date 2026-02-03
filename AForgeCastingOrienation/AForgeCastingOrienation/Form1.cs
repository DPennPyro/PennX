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
    /// <summary>
    /// Main form for the AForge Casting Orientation application.
    /// Provides video capture, image processing, edge detection, and shape detection functionality.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Constants

        private const int CAMERA_INIT_DELAY_MS = 2000;
        private const int SHAPE_ROTATION_DELAY_MS = 300;
        private const int PEN_WIDTH_SHAPE_DETECTION = 4;
        private const int PEN_WIDTH_FOCUS_AREA = 2;
        private const int BLOB_MIN_HEIGHT = 15;
        private const int BLOB_MIN_WIDTH = 15;
        private const int BLOB_MAX_HEIGHT = 100;
        private const int BLOB_MAX_WIDTH = 100;
        private const int FOCUS_RECT_LEFT_X = 100;
        private const int FOCUS_RECT_LEFT_Y = 220;
        private const int FOCUS_RECT_WIDTH = 80;
        private const int FOCUS_RECT_HEIGHT = 40;

        #endregion

        #region Fields

        private bool CannyEdgeDetector = false;
        private bool DifferenceEdgeDetector = false;
        private bool HomogenityEdgeDetector = false;
        private bool SobelEdgeDetector = false;
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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Form1 class
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            FillSourceDropDown();
            CaptureFrame = false;
            ConservativeSmoothing = false;
            Invert = false;
            findShapes = false;
        }

        #endregion

        #region Camera Management

        /// <summary>
        /// Populates the camera dropdown with available video devices
        /// </summary>
        public void FillSourceDropDown()
        {
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
            catch (Exception ex)
            {
                SimpleLogger.LogError("Failed to enumerate video devices", ex);
                btnActivate.Enabled = false;
                camera1Combo.Items.Add("No cameras found");
                camera1Combo.SelectedIndex = 0;
                camera1Combo.Enabled = false;
            }
        }

        /// <summary>
        /// Activates the selected camera and starts video capture
        /// </summary>
        private void ActivateCamera()
        {
            try
            {
                VideoCaptureDevice videoSource = new VideoCaptureDevice(videoDevices[camera1Combo.SelectedIndex].MonikerString);
                vpSource.VideoSource = videoSource;
                vpSource.Start();

                stopWatch = null;
                timer.Start();
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Failed to activate camera", ex);
                MessageBox.Show("Failed to activate camera: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Stops camera capture and cleans up resources
        /// </summary>
        private void StopCameras()
        {
            try
            {
                timer.Stop();
                vpSource.SignalToStop();
                vpSource.WaitForStop();
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error stopping cameras", ex);
            }
        }

        #endregion

        #region Button Click Event Handlers

        /// <summary>
        /// Handles the activate camera button click
        /// </summary>
        private void btnActivate_Click(object sender, EventArgs e)
        {
            try
            {
                ActivateCamera();
                btnActivate.Enabled = false;
                btnCancel.Enabled = true;

                gbEdgeFilters.Enabled = true;
                gbImgFilters.Enabled = true;

                Application.DoEvents();
                System.Threading.Thread.Sleep(CAMERA_INIT_DELAY_MS);
                CaptureFrame = true;
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error in btnActivate_Click", ex);
                MessageBox.Show("Error activating camera: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the cancel button click to stop camera capture
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error in btnCancel_Click", ex);
                MessageBox.Show("Error canceling: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the capture frame button click
        /// </summary>
        private void btnCapture_Click(object sender, EventArgs e)
        {
            CaptureFrame = true;
        }

        /// <summary>
        /// Saves the shapes image to a file selected by the user
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (pbShapes.Image == null)
                {
                    MessageBox.Show("No image to save", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                    saveFileDialog.Title = "Save Shapes Image";
                    saveFileDialog.FileName = "shapes.png";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (Bitmap image = new Bitmap(pbShapes.Image))
                        {
                            image.Save(saveFileDialog.FileName);
                        }
                        MessageBox.Show("Image saved successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error saving shapes image", ex);
                MessageBox.Show("Error saving image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Saves the captured image to a file selected by the user
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (pbCapture.Image == null)
                {
                    MessageBox.Show("No image to save", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                    saveFileDialog.Title = "Save Captured Image";
                    saveFileDialog.FileName = "capture.png";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (Bitmap image = new Bitmap(pbCapture.Image))
                        {
                            image.Save(saveFileDialog.FileName);
                        }
                        MessageBox.Show("Image saved successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error saving capture image", ex);
                MessageBox.Show("Error saving image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Video Frame Processing

        /// <summary>
        /// Event handler called when a new video frame is available
        /// </summary>
        private void vpSource_NewFrame(object sender, ref Bitmap image)
        {
            if (CaptureFrame)
            {
                FillPictureBoxes(ref image);
                CaptureFrame = false;
            }
        }

        /// <summary>
        /// Main method to process and display captured frames
        /// </summary>
        private void FillPictureBoxes(ref Bitmap image)
        {
            Bitmap tmpImg = null;
            Bitmap tmpImg2 = null;

            try
            {
                tmpImg = image;
                tmpImg2 = image;

                ResizeImages(ref tmpImg, ref tmpImg2);
                FiltersSequence processingFilter = CreateImageFilters();

                bool hasFilter = processingFilter.Count > 0;
                if (hasFilter)
                {
                    tmpImg2 = processingFilter.Apply(tmpImg2);
                }

                if (bwSwitch)
                {
                    switchBandW(ref tmpImg);
                }

                ApplyEdgeDetector(ref tmpImg);

                if (findShapes)
                {
                    tmpImg = FindShapes(tmpImg, ref tmpImg2).Result;
                }
                else
                {
                    pbCapture.Image = tmpImg;
                    pbShapes.Image = tmpImg2;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error in FillPictureBoxes", ex);
                MessageBox.Show("Error processing image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Resizes images to fit the picture boxes
        /// </summary>
        private void ResizeImages(ref Bitmap tmpImg, ref Bitmap tmpImg2)
        {
            ResizeBicubic resizeFilter = new ResizeBicubic(pbCapture.Width, pbCapture.Height);
            tmpImg = resizeFilter.Apply(tmpImg);

            resizeFilter = new ResizeBicubic(pbShapes.Width, pbShapes.Height);
            tmpImg2 = resizeFilter.Apply(tmpImg2);
        }

        /// <summary>
        /// Creates a sequence of image filters based on selected options
        /// </summary>
        private FiltersSequence CreateImageFilters()
        {
            FiltersSequence processingFilter = new FiltersSequence();

            if (ConservativeSmoothing)
            {
                processingFilter.Add(new AForge.Imaging.Filters.ConservativeSmoothing());
            }

            if (Invert)
            {
                processingFilter.Add(new AForge.Imaging.Filters.Invert());
            }

            if (HSLswitch)
            {
                processingFilter.Add(new AForge.Imaging.Filters.HSLFiltering());
            }

            if (sepiaSwitch)
            {
                processingFilter.Add(new AForge.Imaging.Filters.Sepia());
            }

            if (Skeletonization)
            {
                processingFilter.Add(new AForge.Imaging.Filters.GrayscaleBT709());
                processingFilter.Add(new AForge.Imaging.Filters.SimpleSkeletonization());
            }

            return processingFilter;
        }

        /// <summary>
        /// Applies the selected edge detector to the image
        /// </summary>
        private void ApplyEdgeDetector(ref Bitmap tmpImg)
        {
            if (CannyEdgeDetector)
            {
                CannyEdgeDetector filter = new CannyEdgeDetector();
                tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                filter.ApplyInPlace(tmpImg);
            }
            else if (DifferenceEdgeDetector)
            {
                DifferenceEdgeDetector dFilter = new DifferenceEdgeDetector();
                tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                dFilter.ApplyInPlace(tmpImg);
            }
            else if (HomogenityEdgeDetector)
            {
                HomogenityEdgeDetector hFilter = new HomogenityEdgeDetector();
                tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                hFilter.ApplyInPlace(tmpImg);
            }
            else if (SobelEdgeDetector)
            {
                SobelEdgeDetector hFilter = new SobelEdgeDetector();
                tmpImg = Grayscale.CommonAlgorithms.BT709.Apply(tmpImg);
                hFilter.ApplyInPlace(tmpImg);

                BlobCounter bc = new BlobCounter(tmpImg);
                Rectangle[] brecs = bc.GetObjectsRectangles();
            }
        }

        #endregion

        #region Image Processing Methods

        /// <summary>
        /// Processes an image to detect and highlight shapes (alternative method, currently unused)
        /// </summary>
        private void ProcessImage(Bitmap bitmap)
        {
            BitmapData bitmapData = null;
            Graphics g = null;
            Pen yellowPen = null;
            Pen redPen = null;
            Pen brownPen = null;
            Pen greenPen = null;
            Pen bluePen = null;

            try
            {
                bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite, bitmap.PixelFormat);

                ColorFiltering colorFilter = new ColorFiltering();
                colorFilter.Red = new IntRange(0, 64);
                colorFilter.Green = new IntRange(0, 64);
                colorFilter.Blue = new IntRange(0, 64);
                colorFilter.FillOutsideRange = false;
                colorFilter.ApplyInPlace(bitmapData);

                BlobCounter blobCounter = new BlobCounter();
                blobCounter.FilterBlobs = true;
                blobCounter.MinHeight = 5;
                blobCounter.MinWidth = 5;
                blobCounter.MaxHeight = 50;
                blobCounter.MaxWidth = 50;

                blobCounter.ProcessImage(bitmapData);
                Blob[] blobs = blobCounter.GetObjectsInformation();
                bitmap.UnlockBits(bitmapData);
                bitmapData = null;

                SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

                g = Graphics.FromImage(bitmap);
                yellowPen = new Pen(Color.Yellow, 2);
                redPen = new Pen(Color.Red, 2);
                brownPen = new Pen(Color.Brown, 2);
                greenPen = new Pen(Color.Green, 2);
                bluePen = new Pen(Color.Blue, 2);

                for (int i = 0, n = blobs.Length; i < n; i++)
                {
                    List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);

                    AForge.Point center;
                    float radius;

                    if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                    {
                        g.DrawEllipse(yellowPen,
                            (float)(center.X - radius), (float)(center.Y - radius),
                            (float)(radius * 2), (float)(radius * 2));
                    }
                    else
                    {
                        List<IntPoint> corners;

                        if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                        {
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

                pbShapes.Image = bitmap;
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error in ProcessImage", ex);
                MessageBox.Show("Error processing image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (bitmapData != null)
                {
                    try { bitmap.UnlockBits(bitmapData); } catch { }
                }
                yellowPen?.Dispose();
                redPen?.Dispose();
                greenPen?.Dispose();
                bluePen?.Dispose();
                brownPen?.Dispose();
                g?.Dispose();
            }
        }

        /// <summary>
        /// Applies black and white filtering with edge detection
        /// </summary>
        private void switchBandW(ref Bitmap tmpImg)
        {
            Bitmap orig = tmpImg;
            Bitmap clone = null;

            try
            {
                clone = new Bitmap(orig.Width, orig.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error in switchBandW", ex);
                throw;
            }
        }

        /// <summary>
        /// Converts a list of IntPoints to an array of Points
        /// </summary>
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);
            }

            return array;
        }

        #endregion

        #region Shape Detection

        /// <summary>
        /// Draws focus areas on the image
        /// </summary>
        private Bitmap DrawFocusArea(Bitmap img)
        {
            using (Graphics g = Graphics.FromImage(img))
            using (Pen p = new Pen(Color.Red, PEN_WIDTH_FOCUS_AREA))
            {
                Rectangle lr = new Rectangle(FOCUS_RECT_LEFT_X, FOCUS_RECT_LEFT_Y, FOCUS_RECT_WIDTH, FOCUS_RECT_HEIGHT);
                Rectangle rr = new Rectangle(360, FOCUS_RECT_LEFT_Y, FOCUS_RECT_WIDTH, FOCUS_RECT_HEIGHT);

                g.DrawRectangle(p, lr);
                g.DrawRectangle(p, rr);
            }

            return img;
        }

        /// <summary>
        /// Detects and highlights shapes in the image by rotating and analyzing blobs
        /// </summary>
        private async Task<Bitmap> FindShapes(Bitmap img, ref Bitmap MarkedImg)
        {
            Blob[] Mblobs = new Blob[0];

            for (int x = 0; x < 359; x++)
            {
                await Task.Delay(SHAPE_ROTATION_DELAY_MS);

                img = RotateAndDetectBlobs(img, ref Mblobs, ref MarkedImg);

                pbCapture.Image = img;
                pbShapes.Image = MarkedImg;
            }

            return img;
        }

        /// <summary>
        /// Rotates the image and detects blobs in the current orientation
        /// </summary>
        private Bitmap RotateAndDetectBlobs(Bitmap img, ref Blob[] Mblobs, ref Bitmap MarkedImg)
        {
            BitmapData bitmapData = null;

            try
            {
                RotateBilinear filter = new RotateBilinear(1, true);
                img = filter.Apply(img);

                bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, img.PixelFormat);

                BlobCounter blobCounter = new BlobCounter();
                blobCounter.FilterBlobs = true;
                blobCounter.MinHeight = BLOB_MIN_HEIGHT;
                blobCounter.MinWidth = BLOB_MIN_WIDTH;
                blobCounter.MaxHeight = BLOB_MAX_HEIGHT;
                blobCounter.MaxWidth = BLOB_MAX_WIDTH;

                blobCounter.ProcessImage(bitmapData);
                Blob[] blobs = blobCounter.GetObjectsInformation();
                img.UnlockBits(bitmapData);
                bitmapData = null;

                AccumulateBlobs(ref Mblobs, blobs);
                DrawDetectedShapes(blobCounter, Mblobs, MarkedImg);
            }
            catch (Exception ex)
            {
                SimpleLogger.LogError("Error in RotateAndDetectBlobs", ex);
                MessageBox.Show("Error detecting shapes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (bitmapData != null)
                {
                    try { img.UnlockBits(bitmapData); } catch { }
                }
            }

            return img;
        }

        /// <summary>
        /// Accumulates detected blobs across multiple rotations
        /// </summary>
        private void AccumulateBlobs(ref Blob[] Mblobs, Blob[] blobs)
        {
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
                blobs.CopyTo(Mblobs, temp.Length);
            }
        }

        /// <summary>
        /// Draws detected shapes on the marked image
        /// </summary>
        private void DrawDetectedShapes(BlobCounter blobCounter, Blob[] Mblobs, Bitmap MarkedImg)
        {
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            using (Graphics g = Graphics.FromImage(MarkedImg))
            using (Pen yellowPen = new Pen(Color.Yellow, PEN_WIDTH_SHAPE_DETECTION))
            using (Pen redPen = new Pen(Color.Red, PEN_WIDTH_SHAPE_DETECTION))
            using (Pen brownPen = new Pen(Color.Brown, PEN_WIDTH_SHAPE_DETECTION))
            using (Pen greenPen = new Pen(Color.Green, PEN_WIDTH_SHAPE_DETECTION))
            using (Pen bluePen = new Pen(Color.Blue, PEN_WIDTH_SHAPE_DETECTION))
            {
                for (int i = 0; i < Mblobs.Length; i++)
                {
                    List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(Mblobs[i]);

                    AForge.Point center;
                    float radius;

                    if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                    {
                        g.DrawEllipse(yellowPen,
                            (float)(center.X - radius), (float)(center.Y - radius),
                            (float)(radius * 2), (float)(radius * 2));
                    }
                    else
                    {
                        List<IntPoint> corners;

                        if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                        {
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
            }
        }

        /// <summary>
        /// Detects corners in the captured image using Moravec algorithm
        /// </summary>
        public void DetectCorners()
        {
            if (pbCapture.Image == null)
            {
                return;
            }

            using (Bitmap image = new Bitmap(pbCapture.Image))
            using (Graphics graphics = Graphics.FromImage(image))
            using (SolidBrush brush = new SolidBrush(Color.Red))
            using (Pen pen = new Pen(brush))
            {
                MoravecCornersDetector mcd = new MoravecCornersDetector();
                List<IntPoint> corners = mcd.ProcessImage(image);

                foreach (IntPoint corner in corners)
                {
                    graphics.DrawRectangle(pen, corner.X - 1, corner.Y - 1, 3, 3);
                }

                pbCapture.Image = image;
            }
        }

        #endregion

        #region Checkbox Event Handlers

        /// <summary>
        /// Handles conservative smoothing filter checkbox change
        /// </summary>
        private void cbConservativeSmoothing_CheckedChanged(object sender, EventArgs e)
        {
            ConservativeSmoothing = cbConservativeSmoothing.Checked;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles invert filter checkbox change
        /// </summary>
        private void cbInvert_CheckedChanged(object sender, EventArgs e)
        {
            Invert = cbInvert.Checked;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles HSL filtering checkbox change
        /// </summary>
        private void cbHSL_CheckedChanged(object sender, EventArgs e)
        {
            HSLswitch = cbHSL.Checked;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles sepia filter checkbox change
        /// </summary>
        private void cbSepia_CheckedChanged(object sender, EventArgs e)
        {
            sepiaSwitch = cbSepia.Checked;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles skeletonization filter checkbox change
        /// </summary>
        private void cbSkeletonization_CheckedChanged(object sender, EventArgs e)
        {
            Skeletonization = cbSkeletonization.Checked;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles black and white filter checkbox change
        /// </summary>
        private void cbBW_CheckedChanged(object sender, EventArgs e)
        {
            bwSwitch = cbBW.Checked;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles find shapes checkbox change
        /// </summary>
        private void cbFindShapes_CheckedChanged(object sender, EventArgs e)
        {
            findShapes = cbFindShapes.Checked;
            CaptureFrame = true;
        }

        #endregion

        #region Edge Detector Radio Button Handlers

        /// <summary>
        /// Handles the none edge detector radio button change
        /// </summary>
        private void rbNone_CheckedChanged(object sender, EventArgs e)
        {
            CannyEdgeDetector = false;
            DifferenceEdgeDetector = false;
            HomogenityEdgeDetector = false;
            SobelEdgeDetector = false;
            CaptureFrame = true;
        }

        /// <summary>
        /// Handles the Canny edge detector radio button change
        /// </summary>
        private void rbCannyEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            CannyEdgeDetector = rbCannyEdgeDetector.Checked;
            if (CannyEdgeDetector)
            {
                CaptureFrame = true;
            }
        }

        /// <summary>
        /// Handles the difference edge detector radio button change
        /// </summary>
        private void rbDifferenceEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            DifferenceEdgeDetector = rbDifferenceEdgeDetector.Checked;
            if (DifferenceEdgeDetector)
            {
                CaptureFrame = true;
            }
        }

        /// <summary>
        /// Handles the homogenity edge detector radio button change
        /// </summary>
        private void rbHomogenityEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            HomogenityEdgeDetector = rbHomogenityEdgeDetector.Checked;
            if (HomogenityEdgeDetector)
            {
                CaptureFrame = true;
            }
        }

        /// <summary>
        /// Handles the Sobel edge detector radio button change
        /// </summary>
        private void rbSobelEdgeDetector_CheckedChanged(object sender, EventArgs e)
        {
            SobelEdgeDetector = rbSobelEdgeDetector.Checked;
            if (SobelEdgeDetector)
            {
                CaptureFrame = true;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Changes the pixel format of a bitmap image
        /// </summary>
        /// <param name="inputImage">Input bitmap image</param>
        /// <param name="newFormat">Target pixel format</param>
        /// <param name="r">Rectangle region to clone</param>
        /// <returns>Cloned bitmap with new pixel format</returns>
        private static Bitmap ChangePixelFormat(Bitmap inputImage, System.Drawing.Imaging.PixelFormat newFormat, Rectangle r)
        {
            return (inputImage.Clone(r, newFormat));
        }

        #endregion
    }
}
