namespace RSLBot.Core.CoreHelpers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.OCR;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using RSLBot.Shared.Settings;

    public class ImageAnalyzer
    {
        private readonly SharedSettings sharedSettings;
        private static Emgu.CV.OCR.Tesseract? Tesseract;
        private static Emgu.CV.OCR.Tesseract? TesseractDigit;

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
            if (Tesseract != null && TesseractDigit != null) return;

            var baseDir = AppContext.BaseDirectory;
            // Emgu expects datapath pointing to a directory that contains the 'tessdata' folder
            // e.g., <base>/tessdata/*.traineddata
            var dataPath = Directory.Exists(Path.Combine(baseDir, "tessdata")) ? Path.Combine(baseDir, "tessdata") : ".";

            var lang = sharedSettings.Language.Code.ToLower();

            Tesseract ??= new Emgu.CV.OCR.Tesseract(dataPath, lang, OcrEngineMode.TesseractOnly);
            TesseractDigit ??= new Emgu.CV.OCR.Tesseract(dataPath, lang, OcrEngineMode.TesseractOnly);

            // Configure general engine
            Tesseract.PageSegMode = PageSegMode.Auto;
            Tesseract.SetVariable("preserve_interword_spaces", "1");

            // Configure digit-focused engine
            TesseractDigit.PageSegMode = PageSegMode.SingleLine;
            TesseractDigit.SetVariable("tessedit_char_whitelist", "0123456789/ ");
            TesseractDigit.SetVariable("classify_bln_numeric_mode", "1");
            TesseractDigit.SetVariable("preserve_interword_spaces", "1");
        }

        public string FindText(Bitmap raidScreenshots, bool onlyDigit = false, Rectangle imageRectangle = default)
        {
            Init();

            var tesCurrent = onlyDigit ? TesseractDigit! : Tesseract!;

            if (raidScreenshots == null)
                throw new ArgumentException("Error 0x100000001");

            // Crop and convert to grayscale
            using var baseGray = imageRectangle == default
                ? raidScreenshots.ToImage<Gray, byte>()
                : raidScreenshots.Clone(imageRectangle, PixelFormat.DontCare).ToImage<Gray, byte>();

            // Preprocess: upscale, denoise, normalize contrast, threshold
            using var scaled = baseGray.Resize(2.0, Inter.Cubic);

            if (onlyDigit)
            {
                var mean = CvInvoke.Mean(scaled).V0;
                if (mean < 127)
                {
                    CvInvoke.BitwiseNot(scaled, scaled);
                }

                // Tighten PSM to improve numeric stability
                // tesCurrent.PageSegMode = PageSegMode.SingleBlock;
                // tesCurrent.SetVariable("tessedit_char_whitelist", "0123456789/ ");
                // tesCurrent.SetVariable("classify_bln_numeric_mode", "1");
                // tesCurrent.SetVariable("preserve_interword_spaces", "1");
            }

            tesCurrent.SetImage(scaled);
            tesCurrent.Recognize();

            return tesCurrent.GetUTF8Text().Trim();
        }

        /*public string FindText(Bitmap raidScreenshots, bool onlyDigit = false, Rectangle imageRectangle = default)
        {
            Init();

            var tesCurrent = onlyDigit ? TesseractDigit! : Tesseract!;

            if (raidScreenshots == null)
                throw new ArgumentException("Error 0x100000001");

            // Crop and convert to grayscale
            using var baseGray = imageRectangle == default
                ? raidScreenshots.ToImage<Gray, byte>()
                : raidScreenshots.Clone(imageRectangle, PixelFormat.DontCare).ToImage<Gray, byte>();

            // Preprocess: upscale, denoise, normalize contrast, threshold
            using var scaled = baseGray.Resize(2.0, Inter.Cubic);

            // Slight blur to reduce noise
            CvInvoke.GaussianBlur(scaled, scaled, new Size(3, 3), 0);

            // Optional: morphological opening (removed due to enum availability differences across Emgu versions)
            // Fallback: use scaled directly as the "opened" image
            using var opened = scaled.Clone();

            // Adaptive threshold tends to work better on game UI with variable backgrounds
            using var thresh = new Image<Gray, byte>(opened.Size);
            CvInvoke.AdaptiveThreshold(opened, thresh, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 31, 5);

            // For digits, invert if necessary to ensure dark text on white background
            // Heuristic: if mean is low, invert
            if (onlyDigit)
            {
                var mean = CvInvoke.Mean(thresh).V0;
                if (mean < 127)
                {
                    CvInvoke.BitwiseNot(thresh, thresh);
                }

                // Tighten PSM to improve numeric stability
                tesCurrent.PageSegMode = PageSegMode.SingleBlock;
                tesCurrent.SetVariable("tessedit_char_whitelist", "0123456789/+-%.,");
                tesCurrent.SetVariable("classify_bln_numeric_mode", "1");
            }

            tesCurrent.SetImage(thresh);
            tesCurrent.Recognize();

            return tesCurrent.GetUTF8Text().Trim();
        }*/

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

        public Rectangle FindImage(Bitmap window, Bitmap part, Rectangle imageRectangle = default, double accuracy = 0.98)
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

        private record Match(Rectangle Rectangle, double Score);

        /// <summary>
        /// Finds all non-overlapping occurrences of a template image within a larger image using an optimized approach.
        /// </summary>
        /// <param name="window">The larger image to search within.</param>
        /// <param name="part">The template image to search for.</param>
        /// <param name="imageRectangle">An optional region of interest within the window to perform the search.</param>
        /// <param name="accuracy">The minimum similarity score (0.0 to 1.0) to consider a match.</param>
        /// <returns>A list of rectangles representing the locations of found images.</returns>
        public List<Rectangle> FindAllImages(Bitmap window, Bitmap part, Rectangle imageRectangle = default,
            double accuracy = 0.95)
        {
            if (window == null || part == null)
            {
                throw new ArgumentNullException(window == null ? nameof(window) : nameof(part),
                    "Input bitmaps cannot be null.");
            }

            using var img = imageRectangle == default
                ? window.ToImage<Bgra, byte>()
                : window.Clone(imageRectangle, PixelFormat.Format24bppRgb).ToImage<Bgra, byte>();
            using var tmp = part.ToImage<Bgra, byte>();
            using var result = new Mat();

            CvInvoke.MatchTemplate(img, tmp, result, TemplateMatchingType.CcorrNormed);

            // --- Optimization Start ---

            // Step 1: Threshold the result matrix to get a binary mask of all points above the accuracy.
            // This is significantly faster than iterating pixel by pixel.
            using var thresholdedResult = new Mat();
            CvInvoke.Threshold(result, thresholdedResult, accuracy, 1.0, ThresholdType.Binary);

            // FindNonZero requires a single-channel, 8-bit image.
            using var mask = new Mat();
            thresholdedResult.ConvertTo(mask, DepthType.Cv8U);

            // Step 2: Get the coordinates of all high-confidence points in a single, fast operation.
            using var locations = new VectorOfPoint();
            CvInvoke.FindNonZero(mask, locations);

            if (locations.Size == 0)
            {
                return new List<Rectangle>();
            }

            // Step 3: Create a list of matches, retrieving the *original* score for each found location.
            var potentialMatches = new List<Match>();
            var resultMatrix = new Matrix<float>(result.Rows, result.Cols, result.DataPointer);

            for (int i = 0; i < locations.Size; i++)
            {
                var point = locations[i];
                potentialMatches.Add(new Match(
                    new Rectangle(point, tmp.Size),
                    resultMatrix[point.Y, point.X]
                ));
            }

            // --- Optimization End ---

            // Step 4: Sort matches by score in descending order. This part remains the same.
            var sortedMatches = potentialMatches.OrderByDescending(m => m.Score).ToList();

            // Step 5: Apply Non-Maximum Suppression (NMS). This logic is still necessary and correct.
            var finalResults = new List<Rectangle>();
            while (sortedMatches.Count > 0)
            {
                var bestMatch = sortedMatches[0];
                finalResults.Add(bestMatch.Rectangle);
                sortedMatches.RemoveAll(match => bestMatch.Rectangle.IntersectsWith(match.Rectangle));
            }

            // Adjust coordinates if a specific search rectangle was used.
            if (imageRectangle != default)
            {
                return finalResults.Select(r =>
                    new Rectangle(r.X + imageRectangle.X, r.Y + imageRectangle.Y, r.Width, r.Height)).ToList();
            }

            return finalResults;
        }
    }


}
