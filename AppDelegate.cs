using Foundation;
using UIKit;
using System.Threading.Tasks;
using System.Threading;
using AVFoundation;
using CoreMedia;
using CoreImage;
using CoreVideo;
using CoreGraphics;
using System;
using CoreFoundation;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace SamplyGame.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate, IAVCaptureVideoDataOutputSampleBufferDelegate
    { 
        NSError error;
        public bool HasProcessed { get; set; }
        public LightBuzz5 _lightbuzz = new LightBuzz5();
        // public Posenet Posenet = new Posenet();
        public override UIWindow Window
        {
            get;
            set;
        }
        public TimeSpan LastProcessTime { get; set; }
        //Camera logic
        private readonly AVCaptureDeviceType CameraType = AVCaptureDeviceType.BuiltInWideAngleCamera;
        private readonly AVCaptureDevicePosition CameraPosition = AVCaptureDevicePosition.Front;
        private readonly AVCaptureVideoOrientation CameraOrientation = AVCaptureVideoOrientation.Portrait;//LandscapeRight;
        private readonly NSString CameraResolution = AVCaptureSession.Preset352x288;//Preset640x480;
        private AVCaptureSession _session;
        private AVCaptureDeviceInput _input;
        private AVCaptureVideoDataOutput _output;
        // Visualization
        private readonly double EllipseSize = 10.0;
        private readonly float LineSize = 2.0f;

        private readonly AVCaptureDeviceType[] DeviceTypes = new AVCaptureDeviceType[]
        {
            AVCaptureDeviceType.BuiltInDualCamera,
            AVCaptureDeviceType.BuiltInDualWideCamera,
            AVCaptureDeviceType.BuiltInDuoCamera,
            AVCaptureDeviceType.BuiltInTelephotoCamera,
            AVCaptureDeviceType.BuiltInTripleCamera,
            AVCaptureDeviceType.BuiltInTrueDepthCamera,
            AVCaptureDeviceType.BuiltInUltraWideCamera,
            AVCaptureDeviceType.BuiltInWideAngleCamera

        };

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            try
            {
                LastProcessTime = DateTime.Now.TimeOfDay;
                //PHPhotoLibrary.RequestAuthorization(status =>
                //{
                //    switch (status)
                //    {
                //        case PHAuthorizationStatus.Authorized:
                //            // Add code do run if user authorized permission, if needed.
                //            break;
                //        case PHAuthorizationStatus.Denied:
                //            // Add code do run if user denied permission, if needed.
                //            break;
                //        case PHAuthorizationStatus.Restricted:
                //            // Add code do run if user restricted permission, if needed.
                //            break;
                //        default:
                //            break;
                //    }
                //});

                //StartCamera();
                LaunchGame();

                // Joints.ScreenHeight = Convert.ToInt32(Window.Frame.Height);
                //Joints.ScreenWidth = Convert.ToInt32(Window.Frame.Width);

                sw.Restart();
                StartCamera();

                return true;
            }
            catch (Exception ex)
            {

            }
            return true;
        }

        async void LaunchGame()
        {
            try
            {
                await Task.Yield();

                new SamplyGame().Run();
            }
            catch (Exception ex)
            {
                // Crashes.TrackError(ex);
            }
        }



        [Export("captureOutput:didDropSampleBuffer:fromConnection:")]
        public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            sampleBuffer.Dispose();
        }

        [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
        public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            try
            {
                if (sw.ElapsedMilliseconds < 1000)
                {
                    return;
                }

                sw.Restart();

                using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
                    {
                        using (CIImage ciImage = CIImage.FromImageBuffer(pixelBuffer))
                        {
                            using (CIContext temporaryContext = CIContext.FromOptions(null))
                            {
                                using (CGImage cgImage = temporaryContext.CreateCGImage(ciImage, new CGRect(0, 0, pixelBuffer.Width, pixelBuffer.Height)))
                                {
                                    Process(UIImage.FromImage(cgImage));
                                }
                            }
                        }
                    }
                //}
            }
            catch (Exception ex)
            {

            }
            finally
            {
                sampleBuffer.Dispose();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                //GC.Collect();
            }
        }

        Stopwatch sw = new Stopwatch();
        List<Joint> joints = new List<Joint>();
        public bool FinishedGettingJoints { get; set; } = true;

        public void Process(UIImage image)
        {
            try
            {

                //List<Joint> joints = null;
                LightBuzz5Output prediction = null;

                //using (var resizedImage = image.Scale(new CGSize(256, 256)))
                var resizedImage = image.Scale(new CGSize(256, 256));
                //{
                    using (var buffer = GetBuffer(resizedImage.CGImage))
                    {
                        prediction = _lightbuzz.GetPrediction(buffer, out error);

                        //while (prediction == null && error == null)
                        //{
                        //}

                        if (prediction != null)
                        {
                            if (error != null)
                            {
                            }
                            else
                            {
                                //await Task.Run(async () => await GetJoints(prediction, (int)image.Size.Width, (int)image.Size.Height));
                                //if (FinishedGettingJoints)
                                //{
                                //    FinishedGettingJoints = false;
                                //    var width = (int)image.Size.Width; var height = (int)image.Size.Height;
                                //    GetJoints(prediction, width, height);
                                //    FinishedGettingJoints = true;
                                //}
                            }
                        }
                    }
                //}
                var width = (int)image.Size.Width; var height = (int)image.Size.Height;

                if (prediction != null)
                {
                    Task.Run(() =>
                    {
                        if (FinishedGettingJoints)
                        {
                            FinishedGettingJoints = false;
                            
                            GetJoints(prediction, width, height);
                            FinishedGettingJoints = true;
                        }
                    });
                }



                //if (joints != null)
                //{

                    //var nose = joints[0];

                    //BeginInvokeOnMainThread(() =>
                    //{
                    //    //var oldFrame = ImageView1.Frame;
                    //    ImageView1.Frame = new CGRect(View.Bounds.Right * 0.8, nose.Y + 100, View.Bounds.Width * 0.2, View.Bounds.Height * 0.3);
                    //});
                    //SamplyGame.Joints = joints;

                    //joints = null;
                //}
                //if (SamplyGame.IsRestarting)
                //{
                //    SamplyGame.IsRestarting = false;
                //    StopCamera();
                //    Thread.Sleep(1000);
                //    StartCamera();
                //}

                //image.Dispose();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                image.Dispose();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
        }


        public void StartCamera()
        {
            try
            {

                AVCaptureDevice[] cameras = AVCaptureDeviceDiscoverySession.Create(DeviceTypes, AVMediaType.Video, AVCaptureDevicePosition.Unspecified).Devices;

                foreach (AVCaptureDevice camera in cameras)
                {
                    if (camera.DeviceType == CameraType && camera.Position == CameraPosition)
                    {
                        // System.Diagnostics.Debug.WriteLine(camera.DeviceType + " " + camera.Position);

                        NSError error = new NSError();

                        _input = new AVCaptureDeviceInput(camera, out error);

                        if (error == null) break;
                    }
                }

                if (_input == null)
                {
                    // System.Diagnostics.Debug.WriteLine("Camera not available. Check the camera type and camera position specified.");
                    return;
                }

                _session = new AVCaptureSession
                {
                    SessionPreset = CameraResolution
                };

                _output = new AVCaptureVideoDataOutput
                {
                    AlwaysDiscardsLateVideoFrames = true,
                    MinFrameDuration = new CMTime(1, 30),
                    WeakVideoSettings = new CVPixelBufferAttributes
                    {
                        PixelFormatType = CVPixelFormatType.CV32BGRA
                    }.Dictionary
                };

                _output.SetSampleBufferDelegateQueue(this, new DispatchQueue("Video-Queue"));

                _session.AddInput(_input);
                _session.AddOutput(_output);
                AVCaptureConnection connection = _output.Connections.Where(c => c.SupportsVideoOrientation).FirstOrDefault();
                connection.VideoOrientation = CameraOrientation;

                var mirrorConnection = _output.Connections.Where(i => i.SupportsVideoMirroring).FirstOrDefault();

                if (mirrorConnection != null)
                {
                    mirrorConnection.AutomaticallyAdjustsVideoMirroring = false;
                    mirrorConnection.VideoMirrored = true;
                }


                _session.CommitConfiguration();
                _session.StartRunning();
            }
            catch (Exception ex)
            {
            }
        }

        public void StopCamera()
        {
            try
            {
                _session?.StopRunning();

                while (_session != null && _session.Running)
                {
                    //wait for it to stop
                }

                if (_output != null)
                {
                    _session.RemoveOutput(_output);
                    _output.Dispose();
                    _output = null;
                }
                if (_input != null)
                {
                    _session.RemoveInput(_input);
                    _input.Dispose();
                    _input = null;
                }
                //Remove if breaks code			//Remove if breaks code			//Remove if breaks code
                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }

                //Remove if breaks code			//Remove if breaks code			//Remove if breaks code

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                //Thread.Sleep(2000);
            }
            catch (Exception ex)
            {

            }
        }


        public CVPixelBuffer GetBuffer(CGImage image)
        {
            try
            {
                const int _bitsPerPixel = 8;
                nint width = (nint)image.Width;
                nint height = (nint)image.Height;
                nint bytesPerRow = (nint)image.BytesPerRow;
                var bufferData = new byte[height * bytesPerRow];
                var colorspace = image.ColorSpace;
                var bmi = image.BitmapInfo;

                var buffer = CVPixelBuffer.Create(width, height, CVPixelFormatType.CV32BGRA, bufferData, bytesPerRow, null, out CVReturn status);

                if (status != CVReturn.Success)
                    throw new Exception("Failed to allocate pixel buffer");

                buffer.Lock(CVPixelBufferLock.None);

                using (var mBitmapContext = new CGBitmapContext(buffer.GetBaseAddress(0), width, height, _bitsPerPixel, bytesPerRow, colorspace, bmi))
                {
                    try
                    {
                        var rect = new CGRect(0, 0, image.Width, image.Height);
                        mBitmapContext.InterpolationQuality = CGInterpolationQuality.High;
                        mBitmapContext.ConcatCTM(CGAffineTransform.MakeRotation(0));
                        mBitmapContext.DrawImage(rect, image);
                        buffer.Unlock(CVPixelBufferLock.None);

                    }
                    catch (Exception ex)
                    {
                        //System.Diagnostics.Debug.WriteLine(ex);
                    }
                }

                return buffer;
            }
            catch (Exception ex)
            {

            }
            finally
            {
                image.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return null;
        }

        public void GetJoints(LightBuzz5Output prediction, int sourceWidth, int sourceHeight)
        {
            try
            {
                //if (prediction == null) return null;
                //if (prediction.Final_heatmaps_0 == null) return null;

                var heatmaps = prediction.Final_heatmaps_0;
                var keypointCount = (int)heatmaps.Shape[2] - 1;
                var heatmapWidth = (int)heatmaps.Shape[3];
                var heatmapHeight = (int)heatmaps.Shape[4];

                List<Joint> points = new List<Joint>();

                for (int i = 0; i < keypointCount; i++)
                {
                    int positionX = 0;
                    int positionY = 0;

                    float confidence = 0.0f;

                    for (int x = 0; x < heatmapWidth; x++)
                    {
                        for (int y = 0; y < heatmapHeight; y++)
                        {
                            int index = y + heatmapHeight * (x + heatmapWidth * i);

                            if (heatmaps[index].FloatValue > confidence)
                            {
                                confidence = heatmaps[index].FloatValue;

                                positionX = x;
                                positionY = y;
                            }
                        }
                    }

                    points.Add(new Joint
                    {
                        X = (float)sourceWidth * (float)positionY / (float)heatmapHeight,
                        Y = (float)sourceHeight * (float)positionX / (float)heatmapWidth,
                        Confidence = confidence
                    });
                }

                joints = points;
                points = null;
                // await Task.Delay(10);
                //return points;
            }
            catch (Exception ex)
            {

            }
        }


    }
}