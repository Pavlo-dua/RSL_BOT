namespace RSLBot.Core.CoreHelpers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.OCR;
    using Emgu.CV.Structure;
    using RSLBot.Shared.Settings;

    public class ImageAnalyzer
    {
        private readonly SharedSettings sharedSettings;
        private static Emgu.CV.OCR.Tesseract Tesseract;
        private static Emgu.CV.OCR.Tesseract TesseractDigit;

        public ImageAnalyzer(SharedSettings sharedSettings)
        {
            this.sharedSettings = sharedSettings;
        }

        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        //[StructLayout(LayoutKind.Sequential)]
        //public struct RECT
        //{
        //    public int left;
        //    public int top;
        //    public int right;
        //    public int bottom;
        //}

        private void Init()
        {
            if (Tesseract == null)
            {
                Tesseract = new Emgu.CV.OCR.Tesseract(".", sharedSettings.Language.Code.ToLower(), OcrEngineMode.TesseractOnly);
                TesseractDigit = new Emgu.CV.OCR.Tesseract(".", sharedSettings.Language.Code.ToLower(), OcrEngineMode.TesseractOnly, "1234567890/+");
            }
        }

        public string FindText(Bitmap raidScreenshots, bool onlyDigit = false, Rectangle imageRectangle = default)
        {
            Init();

            TesseractDigit.PageSegMode = PageSegMode.SingleLine;
            Tesseract.PageSegMode = PageSegMode.Auto;

            var tesCurrent = onlyDigit ? TesseractDigit : Tesseract;

            if (raidScreenshots == null)
                throw new ArgumentException("Error 0x100000001");

            using var img = imageRectangle == default
                ? raidScreenshots.ToImage<Gray, byte>()
                : raidScreenshots.Clone(imageRectangle, PixelFormat.DontCare).ToImage<Gray, byte>().Resize(2, Inter.Nearest);

            tesCurrent.SetImage(img);
            tesCurrent.Recognize();

            return tesCurrent.GetUTF8Text().Trim();
        }

        public static Bitmap GetBitmapByRectangel(Bitmap window, Rectangle rec)
        {
            return window.Clone(rec, PixelFormat.Format24bppRgb);
        }

        private static Point GetLeftMaxMat(Mat mat, double accuracy = 0.988)
        {
            var minVal = 0.0;
            var maxVal = 0.0;

            var minLoc = new Point();
            var maxLoc = new Point();

            var savedLoc = new Point();

            mat.Split().All(m =>
            { 
                CvInvoke.MinMaxLoc(m, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
                if (maxVal >= accuracy)
                    if (savedLoc.X == 0 || savedLoc.X > maxLoc.X)
                        savedLoc = maxLoc;

                return true;
            });

            return savedLoc;
        }

        //private static List<Rectangle> ProcessImage(Mat img, int minContourArea = 60, int maxCounterArea = 300, int maxWightLine = 8)
        //{
        //    using (UMat gray = new UMat())
        //    using (UMat cannyEdges = new UMat())
        //    using (Mat lineImage = new Mat(img.Size, DepthType.Cv8U, 3)) //image to drtaw lines on
        //    {
        //        //Convert the image to grayscale and filter out the noise
        //        CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

        //        //Remove noise
        //        CvInvoke.GaussianBlur(gray, gray, new Size(3, 3), 1);

        //        #region Canny and edge detection
        //        double cannyThreshold = 180.0;
        //        double cannyThresholdLinking = 120.0;
        //        CvInvoke.Canny(gray, cannyEdges, cannyThreshold, cannyThresholdLinking);
        //        LineSegment2D[] lines = CvInvoke.HoughLinesP(
        //            cannyEdges,
        //            1, //Distance resolution in pixel-related units
        //            Math.PI / 45.0, //Angle resolution measured in radians.
        //            20, //threshold
        //            50, //min Line width
        //            maxWightLine); //gap between lines
        //        #endregion

        //        #region Find triangles and rectangles
        //        List<Triangle2DF> triangleList = new List<Triangle2DF>();
        //        List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle
        //        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
        //        {
        //            CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List,
        //                ChainApproxMethod.ChainApproxSimple);
        //            int count = contours.Size;
        //            for (int i = 0; i < count; i++)
        //            {
        //                using (VectorOfPoint contour = contours[i])
        //                using (VectorOfPoint approxContour = new VectorOfPoint())
        //                {
        //                    CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05,
        //                        true);

        //                    var ca = CvInvoke.ContourArea(approxContour, false);

        //                    if (ca > minContourArea && ca < maxCounterArea)
        //                    {
        //                        else if (approxContour.Size == 4) //The contour has 4 vertices.
        //                        {
        //                            #region determine if all the angles in the contour are within [80, 100] degree
        //                            bool isRectangle = true;
        //                            Point[] pts = approxContour.ToArray();
        //                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

        //                            for (int j = 0; j < edges.Length; j++)
        //                            {
        //                                double angle = Math.Abs(
        //                                    edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
        //                                if (angle < 85 || angle > 95)
        //                                {
        //                                    isRectangle = false;
        //                                    break;
        //                                }
        //                            }

        //                            #endregion

        //                            if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        #endregion

        //        return boxList;
        //    }
        //}

        //public static Rectangle FindImageInRectangles(Bitmap window, Bitmap part, Rectangle imageRectangle = default, double accuracy = 0.988)
        //{

        //}

        public Rectangle FindImage(Bitmap window, Bitmap part, Rectangle imageRectangle = default, double accuracy = 0.988)
        {
            if (window == null || part == null)
                throw new ArgumentException("Error 0x100000002");

            //Init();

            using var img = imageRectangle == default
                ? window.ToImage<Bgra, byte>()
                : window.Clone(imageRectangle, PixelFormat.Format24bppRgb).ToImage<Bgra, byte>();
            using var tmp = part.ToImage<Bgra, byte>();

            using var imgOut = new Mat();

            CvInvoke.MatchTemplate(img, tmp, imgOut, TemplateMatchingType.CcorrNormed);

            var minVal = 0.0;
            var maxVal = 0.0;

            var minLoc = new Point();
            var maxLoc = new Point();

            CvInvoke.MinMaxLoc(imgOut, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            if (maxVal > 1 || maxVal < accuracy)
                return default;

            var r = new Rectangle(maxLoc, tmp.Size);

            if (imageRectangle != default)
            {
                r.X += imageRectangle.X;
                r.Y += imageRectangle.Y;
            }

            return r;
        }

        public List<Rectangle> FindAllImages(Bitmap window, Bitmap part, Rectangle imageRectangle = default, double accuracy = 0.9)
        {
            if (window == null || part == null)
                throw new ArgumentException("Error 0x100000002");

            Init();

            using var img = imageRectangle == default
                ? window.ToImage<Bgra, byte>()
                : window.Clone(imageRectangle, PixelFormat.Format24bppRgb).ToImage<Bgra, byte>();
            using var tmp = part.ToImage<Bgra, byte>();
            var result = new List<Rectangle>();

            using (var matResult = new Mat())
            {
                CvInvoke.MatchTemplate(img, tmp, matResult, TemplateMatchingType.CcorrNormed);

                double minVal = 0, maxVal = 0;
                Point minLoc = new Point(), maxLoc = new Point();

                while (true)
                {
                    CvInvoke.MinMaxLoc(matResult, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
                    if (maxVal >= accuracy)
                    {
                        var matchRect = new Rectangle(maxLoc, tmp.Size);

                        if (imageRectangle != default)
                        {
                            matchRect.X += imageRectangle.X;
                            matchRect.Y += imageRectangle.Y;
                        }
                        result.Add(matchRect);

                        // Zero out the area around the found match to prevent re-detecting it.
                        var clearRect = new Rectangle(maxLoc.X - tmp.Width / 2, maxLoc.Y - tmp.Height / 2, tmp.Width, tmp.Height);
                        CvInvoke.Rectangle(matResult, clearRect, new MCvScalar(0), -1);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }


}
