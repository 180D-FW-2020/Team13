using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;

public class FingerTracking
{
    private Mat handHist = new Mat();
    private bool handHistCreated = false;

    private RawImage webcamPreview;
    private RawImage calibrationPreview;
    private WebCamTexture webcamTexture;
    private Mat frame = new Mat();
    // private Mat roi = new Mat(new int[] {90, 10, 3}, MatType.CV_32SC1, 0);
    private bool previewEnabled;

    private int[] hand_x;
    private int[] hand_y;
    private bool xy_done = false;

    private Vector2 fingerPos;

    public FingerTracking(bool enablePreview, RawImage preview, RawImage calibration)
    {
        float aspectRatio = (float) Constants.CAMERA_INPUT_WIDTH / Constants.CAMERA_INPUT_HEIGHT;

        previewEnabled = enablePreview;
        if (previewEnabled)
        {
            webcamPreview = preview;
            RectTransform transform = webcamPreview.rectTransform;
            transform.sizeDelta = new Vector2(transform.rect.width, transform.rect.width / aspectRatio);

            calibrationPreview = calibration;
            RectTransform transform_c = calibrationPreview.rectTransform;
            transform_c.sizeDelta = new Vector2(transform_c.rect.width, transform_c.rect.width / aspectRatio);
        }
        else
        {
            preview.enabled = false;
        }

        webcamTexture = new WebCamTexture(Constants.CAMERA_INPUT_WIDTH, (int)(Constants.CAMERA_INPUT_WIDTH / aspectRatio), Constants.CAMERA_INPUT_FPS);
        webcamTexture.Play();
    }

    public Mat getHistogram()
    {
        Mat hsvFrame = new Mat();
        Cv2.CvtColor(frame, hsvFrame, ColorConversionCodes.BGR2HSV);
        // GET PIXELS INSIDE THE BOXES (?)
        // for (int i = 0; i < 9; i++)
        // {
        //     for (int j = 0; j < 10; j++)
        //     {
        //         for (int k = 0; k < 3; k++)
        //         {
        //             roi.At<int>(i * 10 + j, j, k) = hsvFrame.At<int>(hand_x[i + j], hand_y[i + j], k);
        //         }
        //     }
        // }

        Cv2.CalcHist(new Mat[] {hsvFrame}, new int[] {0, 1}, null, handHist, 2, new int[] {180, 256}, new Rangef[] { new Rangef(0,180), new Rangef(0,256) });
        Cv2.Normalize(handHist, handHist, 0, 255, NormTypes.MinMax);
        return handHist;
    }

    public Mat histMaking()
    {
        Mat hsv = new Mat();
        Mat dst = new Mat();
        Mat disc = new Mat();
        Mat thresh = new Mat();
        Mat merge_thresh = new Mat();
        Mat result = new Mat();

        Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);
        Cv2.CalcBackProject(new Mat[] {hsv}, new int[] {0, 1}, handHist, dst, new Rangef[] { new Rangef(0,180), new Rangef(0,256) });
        disc = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(31, 31));
        Cv2.Filter2D(dst, dst, -1, disc);
        Cv2.Threshold(dst, thresh, 150, 255, ThresholdTypes.Binary);
        Cv2.Merge(new Mat[] { thresh, thresh, thresh }, merge_thresh);
        Cv2.BitwiseAnd(frame, merge_thresh, result);

        return result;
    }

    public Point[][] contours(Mat histMaskImage)
    {
        Mat gray = new Mat();
        Mat thresh = new Mat();

        Cv2.CvtColor(histMaskImage, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.Threshold(gray, thresh, 0, 255, 0);
        var cont = Cv2.FindContoursAsArray(thresh, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
        return cont;
    }

    public Point[] get_max_cont(Point[][] contour_list)
    {
        int max_i = 0;
        double max_c = Cv2.ContourArea(contour_list[0]);
        for (int i = 1; i < contour_list.Length; i++)
        {
            double new_max = Cv2.ContourArea(contour_list[i]);
            if (new_max > max_c) {
                max_c = new_max;
                max_i = i;
            }
        }
        return contour_list[max_i];
    }

    public int[] centroid(Point[] max_cont)
    {
        Moments moment = Cv2.Moments(max_cont);
        if (moment.M00 != 0)
            return new int[] { (int)(moment.M10 / moment.M00), (int)(moment.M01 / moment.M00) };
        else
            return new int[] { -1, -1 };
    }

    // public void farthest_point(, , int[] cnt_centroid)
    // {
    //     //
    // }

    public void manageImageOPR()
    {
        Mat histMaskImage = histMaking();

        Cv2.Erode(histMaskImage, histMaskImage, null, iterations: 2);
        Cv2.Dilate(histMaskImage, histMaskImage, null, iterations: 2);

        Point[][] contour_list = contours(histMaskImage);
        Point[] max_cont = get_max_cont(contour_list);
        int[] cnt_centroid = centroid(max_cont);
        Cv2.Circle(frame, cnt_centroid[0], cnt_centroid[1], 5, new Scalar(255, 0, 255), -1);

        if (max_cont != null)
        {
            // 144-145 here don't work due to typing ??? (see .py file lines 154-155)
            // int[] hull = new int[max_cont.Length];
            // Cv2.ConvexHull(max_cont, hull, returnPoints: false);
            // Vec4i[] defects = Cv2.ConvexityDefects(max_cont, hull);
            // far_point = farthest_point(defects, max_cont, cnt_centroid);
        }
    }

    public void AnalyzeFrame()
    {
        handHistCreated = true;
        handHist = getHistogram();
    }

    public Vector2 getPosition()
    {
        Update();
        return fingerPos;
    }

    public void Update()
    {
        frame = OpenCvSharp.Unity.TextureToMat(webcamTexture);
        if (!xy_done) {
            hand_x = new int[] { 6 * frame.Rows / 20,  6 * frame.Rows / 20,  6 * frame.Rows / 20,
                                 9 * frame.Rows / 20,  9 * frame.Rows / 20,  9 * frame.Rows / 20, 
                                 12 * frame.Rows / 20, 12 * frame.Rows / 20, 12 * frame.Rows / 20
                            };
            hand_y = new int[] { 9 * frame.Width / 20, 10 * frame.Width / 20, 11 * frame.Width / 20, 
                                 9 * frame.Width / 20, 10 * frame.Width / 20, 11 * frame.Width / 20, 
                                 9 * frame.Width / 20, 10 * frame.Width / 20, 11 * frame.Width / 20, 
                            };
            xy_done = true;
        }

        if (previewEnabled)
        {
            webcamPreview.texture = OpenCvSharp.Unity.MatToTexture(frame);
        }

        if (handHistCreated) {
            manageImageOPR();
        } else {
            // draw boxes on frame
            for (int i = 0; i < 9; i++)
            {
                Cv2.Rectangle(frame, 
                              new Point(hand_y[i], hand_x[i]), new Point(hand_y[i] + 10, hand_x[i] + 10), 
                              new Scalar(0, 255, 0), 1
                );
            }
        }

        calibrationPreview.texture = OpenCvSharp.Unity.MatToTexture(frame);
        fingerPos = new Vector2(0.0f, 0.0f);
        // UPDATE THE RETICLE POSITION (X,Y)
    }
}
