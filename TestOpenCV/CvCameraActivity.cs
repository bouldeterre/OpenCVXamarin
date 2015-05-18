using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Util;
using Java.IO;
using Org.Opencv.Android;
using Org.Opencv.Core;
using Org.Opencv.Highgui;
using Org.Opencv.Imgproc;
using Org.Opencv.Objdetect;
using Org.Opencv.Video;
using Size = Org.Opencv.Core.Size;

namespace TestOpenCV
{
    public abstract class CvCameraActivity : Activity, CameraBridgeViewBase.ICvCameraViewListener
    {
        private enum FacePart
        {
            Head,
            Eyes,
            RightEye,
            LeftEye
        }


        /** Global variables */
        private const String FaceCascadeName = "lbpcascade_frontalface.xml";
        private const String EyeCascadeName = "haarcascade_eye_tree_eyeglasses.xml";
        private const String REyeCascadeName = "haarcascade_righteye_2splits.xml";
        private const String LEyeCascadeName = "haarcascade_lefteye_2splits.xml";

        private CascadeClassifier _faceCascade;
        private CascadeClassifier _eyeCascade;
        private CascadeClassifier _reyeCascade;
        private CascadeClassifier _leyeCascade;
        private VideoCapture _videoCapture;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            
            _faceCascade = CreateCascadeFile(FaceCascadeName);
            _eyeCascade = CreateCascadeFile(EyeCascadeName);
            _reyeCascade = CreateCascadeFile(REyeCascadeName);
            _leyeCascade = CreateCascadeFile(LEyeCascadeName);
        }

        private CascadeClassifier CreateCascadeFile(string namefile)
        {
            CascadeClassifier currentCascade = null;

            try
            {
                var inputstr = Resources.Assets.Open(namefile);
                var cascadeDir = GetDir("cascade", FileCreationMode.Private);
                var mCascadeFile = new File(cascadeDir, namefile);

                var os = new FileOutputStream(mCascadeFile);

                var buffer = new byte[4096];
                int bytesRead;


                while ((bytesRead = inputstr.Read(buffer, 0, buffer.Length)) > 0)
                {
                    os.Write(buffer, 0, bytesRead);
                }

                inputstr.Close();
                os.Close();

                currentCascade = new CascadeClassifier(mCascadeFile.AbsolutePath);
                if (currentCascade.Empty())
                {
                    Log.Error("OPENCV", "Failed to load cascade classifier");
                    currentCascade = null;
                }
                else
                    Log.Info("OPENCV", "Loaded cascade classifier from " + mCascadeFile.AbsolutePath);
            }
            catch (IOException e)
            {
                Log.Info("OPENCV", "face cascade not found");
            }
            return currentCascade;
        }

        public void OnCameraViewStarted(int width, int height)
        {
        }

        public void OnCameraViewStopped()
        {
        }

        public Mat OnCameraFrame(Mat frame)
        {
            var frameGray = new Mat();
            var faces = new MatOfRect();

            Imgproc.CvtColor(frame, frameGray, Imgproc.ColorBgr2gray);
            Imgproc.EqualizeHist(frameGray, frameGray);

            var size = frame.Size();
            //-- Detect faces
            _faceCascade.DetectMultiScale(frameGray, faces, 1.1, 2, 0,
                new Size(200, 200), new Size(370, 370));

            if (!faces.Empty())
            {
                var faceslist = faces.ToArray();

                Core.Rectangle(frame, new Point(faceslist[0].X, faceslist[0].Y),
                    new Point(faceslist[0].X + faceslist[0].Width, faceslist[0].Y + faceslist[0].Height),
                    new Scalar(255, 0, 255));

#if MultifacesDetection
                for (int i = 0; i < faceslist.Length; i++)
                {
                    Point center = new Point(faceslist[i].X + faceslist[i].Width * 0.5,
                    faceslist[i].Y + faceslist[i].Height * 0.5);
                    Core.Ellipse(frame, center, new Size(faceslist[i].Width * 0.5, faceslist[i].Height * 0.5), 0, 0, 360,
                        new Scalar(255, 0, 255), 4, 8, 0);
                }
#endif

                // split it
                var eyeareaRight = new Rect(faceslist[0].X + faceslist[0].Width/16,
                    (int) (faceslist[0].Y + (faceslist[0].Height/4.5)),
                    (faceslist[0].Width - 2*faceslist[0].Width/16)/2, (int) (faceslist[0].Height/3.0));
                var eyeareaLeft = new Rect(faceslist[0].X + faceslist[0].Width/16
                                           + (faceslist[0].Width - 2*faceslist[0].Width/16)/2,
                    (int) (faceslist[0].Y + (faceslist[0].Height/4.5)),
                    (faceslist[0].Width - 2*faceslist[0].Width/16)/2, (int) (faceslist[0].Height/3.0));
#if DEBUG
                // draw the area - mGray is working grayscale mat, if you want to
                // see area in rgb preview, change mGray to mRgba
                Core.Rectangle(frame, eyeareaLeft.Tl(), eyeareaLeft.Br(),
                    new Scalar(255, 0, 0, 255), 2);
                Core.Rectangle(frame, eyeareaRight.Tl(), eyeareaRight.Br(),
                    new Scalar(255, 0, 0, 255), 2);
#endif
                MatOfRect reye = DetectArea(frameGray, eyeareaRight, FacePart.RightEye);
                Rect[] eyelist;
                if (!reye.Empty())
                {
                    eyelist = reye.ToArray();
                    Core.Rectangle(frame, new Point(eyelist[0].X + eyeareaRight.X, eyelist[0].Y + eyeareaRight.Y),
                        new Point(eyelist[0].Width + eyelist[0].X + eyeareaRight.X,
                            eyelist[0].Height + eyelist[0].Y + eyeareaRight.Y), new Scalar(200, 200, 0));
                }

                MatOfRect leye = DetectArea(frameGray, eyeareaLeft, FacePart.LeftEye);
                if (!leye.Empty())
                {
                    eyelist = leye.ToArray();

                    Core.Rectangle(frame, new Point(eyelist[0].X + eyeareaLeft.X, eyelist[0].Y + eyeareaLeft.Y),
                        new Point(eyelist[0].Width + eyelist[0].X + eyeareaLeft.X,
                            eyelist[0].Height + eyelist[0].Y + eyeareaLeft.Y), new Scalar(200, 200, 0));
                }

#if DoubleEyeDetection
                                  
                // compute the eye area
                 #if DEBUG

                Core.Rectangle(frame, new Point(eyearea.X, eyearea.Y),
                    new Point(eyearea.X + eyearea.Width, eyearea.Y + eyearea.Height), new Scalar(255, 0, 0));
                #endif

                var eyearea = new Rect(faceslist[0].X + faceslist[0].Width/8,
                    (int) (faceslist[0].Y + (faceslist[0].Height/4.5)), faceslist[0].Width - 2*faceslist[0].Width/8,
                    (int) (faceslist[0].Height/3.0));

                var eyes = new MatOfRect();
                var faceRoi = new Mat(frameGray, eyearea);
                //-- In each face, detect eyes
                _eyeCascade.DetectMultiScale(faceRoi, eyes, 1.1, 2, 0, new Size(35, 35), new Size(75, 75));
               
                if (!eyes.Empty())
                {
                    var eyelist = eyes.ToArray();
                    for (var j = 0; j < eyelist.Length; j++)
                    {
                        Core.Rectangle(frame, new Point(eyelist[j].X + eyearea.X, eyelist[j].Y + eyearea.Y),
                            new Point(eyelist[j].Width + eyelist[j].X + eyearea.X,
                                eyelist[j].Height + eyelist[j].Y + eyearea.Y), new Scalar(200, 200, 0));
                    }
                }
#endif
            }

            return frame;
        }

        private MatOfRect DetectArea(Mat all, Rect area, FacePart part)
        {
            var faceRoi = new Mat(all, area);
            var res = new MatOfRect();

            switch (part)
            {
                case FacePart.Head:
                    _faceCascade.DetectMultiScale(faceRoi, res, 1.1, 2, 0,
                        new Size(200, 200), new Size(370, 370));
                    break;
                case FacePart.Eyes:
                    _eyeCascade.DetectMultiScale(faceRoi, res, 1.1, 2, 0, new Size(35, 35), new Size(75, 75));
                    break;
                case FacePart.RightEye:
                    _reyeCascade.DetectMultiScale(faceRoi, res, 1.1, 2, 0, new Size(30, 30), new Size(75, 75));
                    break;
                case FacePart.LeftEye:
                    _leyeCascade.DetectMultiScale(faceRoi, res, 1.1, 2, 0, new Size(30, 30), new Size(75, 75));
                    break;
            }
            return res;
            //-- In each face, detect eyes
        }
    }
}