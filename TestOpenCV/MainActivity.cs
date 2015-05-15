using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Org.Opencv.Android;

namespace TestOpenCV
{
    [Activity(Label = "TestOpenCV", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : CvCameraActivity
    {
        private LinearLayout _layout;
        private Button _button;

        private CallbackOpenCvLoader _loader;
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
            const int cameranumber = 1;

            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            try
            {
                SetContentView(Resource.Layout.Main);

                _mOpenCvCameraView = new JavaCameraView(this, cameranumber);

                _layout = FindViewById<LinearLayout>(Resource.Id.CameraLayout);
                _layout.AddView(_mOpenCvCameraView);

                _button = FindViewById<Button>(Resource.Id.button1);

                _mOpenCvCameraView.EnableView();
                _mOpenCvCameraView.EnableFpsMeter();
                _button.Click += delegate { _mOpenCvCameraView.SetCvCameraViewListener(this); };
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}