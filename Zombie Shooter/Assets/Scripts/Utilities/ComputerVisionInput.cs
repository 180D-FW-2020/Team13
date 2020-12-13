using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.ComponentModel;
using UnityEngine.UI;

public class ComputerVisionInput
{
    private WebCamTexture webcamTexture;
    private RawImage webcamPreview;
    private float aspectRatio = 1280f / 720f;

    private Mat frame = new Mat();
    private Mat blurredFrame = new Mat();
    private Mat hsv = new Mat();
    private Mat mask = new Mat();
    private Mat greenLower, greenUpper;

    private Vector2 shape;
    private Point2f center;
    private float radius;
    private bool previewEnabled;

    public ComputerVisionInput(WebCamDevice device, float[] greenLowerHSV, float[] greenUpperHSV, bool enablePreview, RawImage preview)
    {
        previewEnabled = enablePreview;

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

        webcamTexture = new WebCamTexture(Constants.CAMERA_INPUT_WIDTH, (int)(Constants.CAMERA_INPUT_WIDTH / aspectRatio), Constants.CAMERA_INPUT_FPS);
        webcamTexture.Play();

        shape = new Vector2(webcamTexture.width, webcamTexture.height);
        greenLower = new Mat(1, 3, MatType.CV_32F, greenLowerHSV);
        greenUpper = new Mat(1, 3, MatType.CV_32F, greenUpperHSV);
    }


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

            Debug.Log($"Position: ({center.X},{center.Y}), Screen Size: ({Screen.width},{Screen.height})");

            // float center_x = (float)((shape.x - center.X) / shape.x) * Screen.width;
            // float center_y = (float)((shape.y - center.Y) / shape.y) * Screen.height;
            // center = new Point2f(center_x, center_y);
        }

        if (previewEnabled) webcamPreview.texture = OpenCvSharp.Unity.MatToTexture(frame);
        return new Vector2(center.X, center.Y);
    }
}
