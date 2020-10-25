import numpy as np
import cv2
import imutils
import socket
import signal

width = 600
green_lower = (30, 80, 100)
green_upper = (70, 255, 255)

def track_ball(frame):
    frame = imutils.resize(frame, width)
    
    blurred = cv2.GaussianBlur(frame, (11, 11), 0)
    hsv = cv2.cvtColor(blurred, cv2.COLOR_BGR2HSV)

    mask = cv2.inRange(hsv, green_lower, green_upper)
    mask = cv2.erode(mask, None, iterations=4)
    mask = cv2.dilate(mask, None, iterations=4)

    bins = cv2.findContours(mask.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    bins = imutils.grab_contours(bins)
    center = None

    if len(bins) > 0:
        # find largest contour
        c = max(bins, key=cv2.contourArea)
        ((x, y), radius) = cv2.minEnclosingCircle(c)
        M = cv2.moments(c)
        center = (int(M["m10"] / M["m00"]), int(M["m01"] / M["m00"]))
        # check blob size
        if radius > 10:
            cv2.circle(frame, (int(x), int(y)), int(radius),
                (0, 255, 255), 2)
            cv2.circle(frame, center, 5, (0, 0, 255), -1)

    return frame, mask, center

if __name__ == '__main__':
    cap = cv2.VideoCapture(0)
    
    print("Connected")
    
    while True:
        ret, frame = cap.read()
        
        frame, mask, center = track_ball(frame)
        height, width, channels = frame.shape

        if center is not None:
            (center_x, center_y) = center
            center_x = (width - center_x)/width
            center_y = (height - center_y)/height

            print(str(center_x) + " " + str(center_y))

        cv2.imshow('Mask', mask)
        cv2.imshow('Video', frame)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
    
    cap.release()
    cv2.destroyAllWindows()