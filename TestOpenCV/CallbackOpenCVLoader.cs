using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Org.Opencv.Android;

namespace TestOpenCV
{    
        class CallbackOpenCvLoader : BaseLoaderCallback
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

            protected CallbackOpenCvLoader(IntPtr javaReference, JniHandleOwnership transfer)
                : base(javaReference, transfer)
            {
            }

            public CallbackOpenCvLoader(Context p0)
                : base(p0)
            {
            }
        }
}