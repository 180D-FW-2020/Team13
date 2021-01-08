using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.ComponentModel;
using UnityEngine.UI;

// Performs all CV related tasks using the OpenCVSharp library
public class ComputerVisionInput
{
    private WebCamTexture webcamTexture;
    private RawImage webcamPreview;

    private Mat frame = new Mat();
    private Mat blurredFrame = new Mat();
    private Mat hsv = new Mat();
    private Mat mask = new Mat();
    private Mat greenLower, greenUpper;

    private Point2f center;
    private float radius;
    private bool previewEnabled;

    // initialize OpenCV variables and preview window
    public ComputerVisionInput(float[] greenLowerHSV, float[] greenUpperHSV, bool enablePreview, RawImage preview)
    {
        previewEnabled = enablePreview;
        float aspectRatio = (float) Constants.CAMERA_INPUT_WIDTH / Constants.CAMERA_INPUT_HEIGHT;
        if (previewEnabled)
        {
            webcamPreview = preview;
            RectTransform transform = webcamPreview.rectTransform;
            transform.sizeDelta = new Vector2(transform.rect.width, transform.rect.width / aspectRatio);
        }
        else
        {
            preview.enabled = false;
        }

        webcamTexture = new WebCamTexture(Constants.CAMERA_INPUT_WIDTH, Constants.CAMERA_INPUT_HEIGHT, Constants.CAMERA_INPUT_FPS);
        webcamTexture.Play();

        greenLower = new Mat(1, 3, MatType.CV_32F, greenLowerHSV);
        greenUpper = new Mat(1, 3, MatType.CV_32F, greenUpperHSV);
    }


    // Process next webcam frame
    public Vector2 Update()
    {
        frame = OpenCvSharp.Unity.TextureToMat(webcamTexture);
        Cv2.GaussianBlur(frame, blurredFrame, new Size(21, 21), 0);
        Cv2.CvtColor(blurredFrame, hsv, ColorConversionCodes.BGR2HSV);

        Cv2.InRange(hsv, greenLower, greenUpper, mask);
        Cv2.Erode(mask, mask, null, iterations: 8);
        Cv2.Dilate(mask, mask, null, iterations: 8);

        var countours = Cv2.FindContoursAsArray(mask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        if (countours.Length > 0)
        {
            double maxArea = 0;
            Point[] largestContour = countours[0];
            foreach (var contour in countours)
                if (Cv2.ContourArea(contour) > maxArea)
                    largestContour = contour;

            Cv2.MinEnclosingCircle(largestContour, out center, out radius);
            if (radius > 10)
            {
                Cv2.Circle(frame, center, (int)radius, new Scalar(0, 0, 255), -1);
            }

            float center_x = (float)((webcamTexture.width - center.X) / webcamTexture.width) * Screen.width;
            float center_y = (float)((webcamTexture.height - center.Y) / webcamTexture.height) * Screen.height;
            center = new Point2f(center_x, center_y);
        }

        if (previewEnabled) webcamPreview.texture = OpenCvSharp.Unity.MatToTexture(frame);
        return new Vector2(center.X, center.Y);
    }
}
