using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using Java.IO;
using Org.Opencv.Android;
using Org.Opencv.Core;
using Org.Opencv.Imgproc;
using Org.Opencv.Objdetect;
using Console = System.Console;
using File = Java.IO.File;
using IOException = Java.IO.IOException;
using Range = Org.Opencv.Core.Range;
using Size = Org.Opencv.Core.Size;

namespace TestOpenCV
{
    internal class CallbackLoader : BaseLoaderCallback
    {
        public override void OnManagerConnected(int status)
        {
            switch (status)
            {
                case LoaderCallbackInterface.Success:
                {
                    Log.Info("TestOpenCV", "OpenCV loaded successfully");
                }
                    break;
                default:
                    base.OnManagerConnected(status);
                    break;
            }
        }
        protected CallbackLoader(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CallbackLoader(Context p0) : base(p0)
        {
        }
    }

    public abstract class CustomActivity : Activity, CameraBridgeViewBase.ICvCameraViewListener
    {
        /** Global variables */
        private readonly String face_cascade_name = "haarcascade_frontalface_alt.xml";
        private readonly String eye_cascade_name = "haarcascade_eye_tree_eyeglasses.xml";

        private CascadeClassifier face_cascade;
        private CascadeClassifier eye_cascade;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            face_cascade = CreateCascadeFile(face_cascade_name);
            eye_cascade = CreateCascadeFile(eye_cascade_name);
        }

        CascadeClassifier CreateCascadeFile(string namefile)
        {
            CascadeClassifier current_cascade = null;

            Stream inputstr;

            try
            {
                inputstr = Resources.Assets.Open(namefile);
                File cascadeDir = GetDir("cascade", FileCreationMode.Private);
                File mCascadeFile = new File(cascadeDir, namefile);

                FileOutputStream os = new FileOutputStream(mCascadeFile);

                byte[] buffer = new byte[4096];
                int bytesRead;


                while ((bytesRead = inputstr.Read(buffer, 0, buffer.Length)) > 0)
                {
                    os.Write(buffer, 0, bytesRead);
                }

                inputstr.Close();
                os.Close();

                current_cascade = new CascadeClassifier(mCascadeFile.AbsolutePath);
                if (current_cascade.Empty())
                {
                    Log.Error("OPENCV", "Failed to load cascade classifier");
                    current_cascade = null;
                }
                else
                    Log.Info("OPENCV", "Loaded cascade classifier from " + mCascadeFile.AbsolutePath);
                
            }
            catch (IOException e)
            {
                Log.Info("OPENCV", "face cascade not found");
            }
            return current_cascade;
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
            face_cascade.DetectMultiScale(frameGray, faces, 1.1, 2, 0,
                new Size(200, 200), new Size(370, 370));

            if (!faces.Empty())
            {
                Rect[] faceslist = faces.ToArray();
                int i = 0;
                Point center = new Point(faceslist[i].X + faceslist[i].Width * 0.5,
                    faceslist[i].Y + faceslist[i].Height * 0.5);
                Core.Rectangle(frame, new Point(faceslist[i].X, faceslist[i].Y), new Point(faceslist[i].X + faceslist[i].Width, faceslist[i].Y + faceslist[i].Height), new Scalar(255, 0, 255));
                return frame;
                //Core.Ellipse(frame, center, new Size(faceslist[i].Width * 0.5, faceslist[i].Height * 0.5), 0, 0, 360,
                //    new Scalar(255, 0, 255), 4, 8, 0);
                /*
                for (int i = 0; i < faceslist.Length; i++)
                {
                    Point center = new Point(faceslist[i].X + faceslist[i].Width * 0.5,
                    faceslist[i].Y + faceslist[i].Height * 0.5);
                    Core.Ellipse(frame, center, new Size(faceslist[i].Width * 0.5, faceslist[i].Height * 0.5), 0, 0, 360,
                        new Scalar(255, 0, 255), 4, 8, 0);
                }*/


                Mat faceROI = new Mat(frameGray, faceslist[i]);
                // Mat faceROI = new Mat(new Size(faceslist[i].X, faceslist[i].Y), frame.Type());
                var eyes = new MatOfRect();

                //-- In each face, detect eyes
                eye_cascade.DetectMultiScale(faceROI, eyes, 1.1, 2, 0, new Size(10, 10), new Size(100, 100));

                if (!eyes.Empty()) // todo todo
                {
                    Rect[] eyelist = eyes.ToArray();
                    for (int j = 0; j < eyelist.Length; j++)
                    {
                        Point eye_center = new Point(faceslist[i].X + eyelist[j].X + eyelist[j].Width/2, faceslist[i].Y + eyelist[j].Y + eyelist[j].Height/2 );
                        int radius = (int) Math.Round((eyelist[j].Width + eyelist[j].Height) * 0.25);
                        Core.Circle( frame, eye_center, radius, new Scalar( 255, 0, 0 ), 4, 8, 0 );
                    }

                }

            }
            return frame;
        }
    }

    [Activity(Label = "TestOpenCV", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : CustomActivity
    {
        private LinearLayout _layout;
        private Button _button;

        private CallbackLoader _loader;
        private CameraBridgeViewBase _mOpenCvCameraView;

        public MainActivity()
        {
            // _loader = new CallbackLoader(this);
            if (!OpenCVLoader.InitDebug())
            {
                Console.WriteLine("Init OpenCV Failed");
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            try
            {
                SetContentView(Resource.Layout.Main);

                _mOpenCvCameraView = new JavaCameraView(this, 1);

                _layout = FindViewById<LinearLayout>(Resource.Id.CameraLayout);
                _layout.AddView(_mOpenCvCameraView);

                _button = FindViewById<Button>(Resource.Id.button1);

                _mOpenCvCameraView.EnableView();
                _mOpenCvCameraView.EnableFpsMeter();
                _button.Click += delegate
                {
                    _mOpenCvCameraView.SetCvCameraViewListener(this);
                };

            }
            catch (Exception e)
            {
                throw;
            }
        }        
    }
}