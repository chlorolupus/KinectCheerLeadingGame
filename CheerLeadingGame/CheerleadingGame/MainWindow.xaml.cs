using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

using System.Media;

namespace W9_ClappingHands
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Assignment based on W9_GestureBasic code by FuHongBo
    /// Sound taken from bishi bash special 3 (konami)
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private int GestureRequired = 0; // 0 = LupRDown , 1 = LUpRUp, 2 = LDownRDown, 3 = LDownRUp
        private int TimeUntilCompletion = 150; // frame before game announce game over
        private int CurrentTime = 0; // current frame
        private bool gameStarted = false;
        private bool gameOver = true;
        private bool TutorialRan = false;
        private int currentStance = 0; // 0 = all mid, 1 = LupRDown , 2 = LUpRUp, 3 = LDownRDown, 4 = LDownRUp
        private int score = 0;
        private Random rand;
        private Skeleton[] skeletons = null;
        private JointType[] bones = { 
                                      // torso 
                                      JointType.Head, JointType.ShoulderCenter,
                                      JointType.ShoulderCenter, JointType.ShoulderLeft,
                                      JointType.ShoulderCenter, JointType.ShoulderRight,
                                      JointType.ShoulderCenter, JointType.Spine, 
                                      JointType.Spine, JointType.HipCenter,
                                      JointType.HipCenter, JointType.HipLeft, 
                                      JointType.HipCenter, JointType.HipRight,
                                      // left arm 
                                      JointType.ShoulderLeft, JointType.ElbowLeft,
                                      JointType.ElbowLeft, JointType.WristLeft,
                                      JointType.WristLeft, JointType.HandLeft,
                                      // right arm 
                                      JointType.ShoulderRight, JointType.ElbowRight,
                                      JointType.ElbowRight, JointType.WristRight,
                                      JointType.WristRight, JointType.HandRight,
                                      // left leg
                                      JointType.HipLeft, JointType.KneeLeft,
                                      JointType.KneeLeft, JointType.AnkleLeft,
                                      JointType.AnkleLeft, JointType.FootLeft,
                                      // right leg
                                      JointType.HipRight, JointType.KneeRight,
                                      JointType.KneeRight, JointType.AnkleRight,
                                      JointType.AnkleRight, JointType.FootRight,
                                    };

        private DrawingGroup drawingGroup; // Drawing group for skeleton rendering output
        private DrawingImage drawingImg; // Drawing image that we will display

        //BackgroundImg image
        private BitmapImage BackgroundImg;
        private Rect BackgroundImg_Rect;

        // Cheerleader Image Group
        private BitmapImage CheerLeaderStart;
        private BitmapImage CheerLeaderLDownRDown;
        private BitmapImage CheerLeaderLDownRUp;
        private BitmapImage CheerLeaderLUpRDown;
        private BitmapImage CheerLeaderLUpRUp;

        private Rect CheerLeaderStart_Rect;
        //Pom Pom Up and Down
        private BitmapImage PomPomLDown;
        private BitmapImage PomPomLUp;
        private BitmapImage PomPomRDown;
        private BitmapImage PomPomRUp;

        private Rect PomPomL_Rect;
        private Rect PomPomR_Rect;

        //Result
        private BitmapImage CorrectCircle;
        private BitmapImage WrongCross;

        private Rect Result_Rect;

        //Logo and instructions
        private BitmapImage Logo;
        private BitmapImage TutorialScreen;

        private Rect Logo_Rect;
        private Rect Tutorial_Rect;

        // Sound effects
        private SoundPlayer correctSound = new SoundPlayer("cheerLeader/correct.wav");
        private SoundPlayer wrongSound = new SoundPlayer("cheerLeader/wrong.wav");
        private SoundPlayer readySound = new SoundPlayer("cheerLeader/ready.wav");

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImages()
        {
            BackgroundImg = new BitmapImage(new Uri("cheerLeader/cheerLeader_bg.png", UriKind.Relative));
            CheerLeaderStart = new BitmapImage(new Uri("cheerLeader/ch_start.png", UriKind.Relative));
            CheerLeaderLDownRDown = new BitmapImage(new Uri("cheerLeader/ch_downdown.png", UriKind.Relative));
            CheerLeaderLDownRUp = new BitmapImage(new Uri("cheerLeader/ch_downup.png", UriKind.Relative));
            CheerLeaderLUpRDown = new BitmapImage(new Uri("cheerLeader/ch_updown.png", UriKind.Relative));
            CheerLeaderLUpRUp = new BitmapImage(new Uri("cheerLeader/ch_upup.png", UriKind.Relative));
            PomPomLDown = new BitmapImage(new Uri("cheerLeader/ic_downLeft.png", UriKind.Relative));
            PomPomLUp = new BitmapImage(new Uri("cheerLeader/ic_topLeft.png", UriKind.Relative));
            PomPomRDown = new BitmapImage(new Uri("cheerLeader/ic_downRight.png", UriKind.Relative));
            PomPomRUp = new BitmapImage(new Uri("cheerLeader/ic_topRight.png", UriKind.Relative));
            CorrectCircle = new BitmapImage(new Uri("cheerLeader/circle.png", UriKind.Relative));
            WrongCross = new BitmapImage(new Uri("cheerLeader/cross.png", UriKind.Relative));
            Logo = new BitmapImage(new Uri("cheerLeader/cheerLeader_logo.png", UriKind.Relative));
            TutorialScreen = new BitmapImage(new Uri("cheerLeader/cheerLeader_instruction.png", UriKind.Relative)); 

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (KinectSensor.KinectSensors.Count == 0)
            {
                MessageBox.Show("No Kinects detected", "Depth Sensor Basics");
                Application.Current.Shutdown();
            }
            else
            {
                sensor = KinectSensor.KinectSensors[0];
                if (sensor == null)
                {
                    MessageBox.Show("Kinect is not ready to use", "Depth Sensor Basics");
                    Application.Current.Shutdown();
                }
            }

            // skeleton stream 
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(sensor_SkeletonFrameReady);
            skeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];

            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            drawingImg = new DrawingImage(drawingGroup);
            // Display the drawing using our image control
            skeletonImg.Source = drawingImg;
            // prevent drawing outside of our render area
            drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 640, 480));
            LoadImages();
            // start the kinect
            sensor.Start();
            BackgroundImg_Rect = new Rect(0, 0, 640, 480);
            Result_Rect = new Rect(224, 0, 192, 168);
            CheerLeaderStart_Rect = new Rect(226, 205, 188, 234);
            PomPomL_Rect = new Rect(261, 48, 123, 70);
            PomPomR_Rect = new Rect(261, 48, 123, 70);
            Logo_Rect = new Rect(0, 0, 640, 480);
            Tutorial_Rect = new Rect(0, 0, 640, 480);
        }



        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            
            using (DrawingContext dc = this.drawingGroup.Open()) // clear the drawing
            {
                // draw a transparent BackgroundImg to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, 640, 480));
                dc.DrawImage(BackgroundImg, BackgroundImg_Rect);
                statusTxt.Text = "No Skeleton Detected";
                
                if (currentStance == 0)
                {
                    dc.DrawImage(CheerLeaderStart, CheerLeaderStart_Rect);
                }
                else if (currentStance == 1)
                {
                    dc.DrawImage(CheerLeaderLUpRDown, CheerLeaderStart_Rect);
                }
                else if (currentStance == 2)
                {
                    dc.DrawImage(CheerLeaderLUpRUp, CheerLeaderStart_Rect);
                }
                else if (currentStance == 3)
                {
                    dc.DrawImage(CheerLeaderLDownRDown, CheerLeaderStart_Rect);
                }
                else if (currentStance == 4)
                {
                    dc.DrawImage(CheerLeaderLDownRUp, CheerLeaderStart_Rect);
                }

                if (TutorialRan == false)
                {
                    dc.DrawImage(Logo, Logo_Rect);
                }

                
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame != null)
                    {
                        frame.CopySkeletonDataTo(skeletons);

                        // Add your code below 

                        // Find the closest skeleton 
                        Skeleton skeleton = GetPrimarySkeleton(skeletons);

                        if (skeleton == null) return;
                        statusTxt.Text = "Detected. Put both hands at your chest.";
                        ScoreTxt.Text = "Score: " + score.ToString();
                        //DrawSkeleton(skeleton, dc, Brushes.GreenYellow, new Pen(Brushes.DarkGreen, 6));
                        rand = new Random();
                        
                        if (gameStarted == false)
                        {
                            if (gameOver == false && TutorialRan == true)
                            {
                                dc.DrawImage(CorrectCircle, Result_Rect);
                                statusTxt.Text = "Good work. Put both hands at your chest.";
                            }
                            else if (TutorialRan == true)
                            {
                                dc.DrawImage(WrongCross, Result_Rect);
                                statusTxt.Text = "Game Over. Put both hands at your chest.";
                            }

                            if (MatchHandAtChestHigh(skeleton))
                            {   
                                GestureRequired = rand.Next() % 4;
                                if (gameOver == true)
                                {
                                    score = 0;
                                }

                                if (TutorialRan == false) 
                                {
                                    dc.DrawImage(TutorialScreen, Tutorial_Rect);
                                    CurrentTime++;
                                    if (CurrentTime >= 300)
                                    {
                                        TutorialRan = true;
                                        CurrentTime = 0;
                                    }
                                }
                                else 
                                {
                                    gameStarted = true;
                                    readySound.Play();
                                }
                                gameOver = false;

                            }
                        }
                       
                        //Check win condition met
                        else if (gameStarted == true && CurrentTime <= TimeUntilCompletion)
                        {
                            if (GestureRequired == 0)
                            {
                                dc.DrawImage(PomPomLUp, PomPomL_Rect);
                                dc.DrawImage(PomPomRDown, PomPomL_Rect);
                                statusTxt.Text = "Left Up Right Down";
                                if (MatchLeftUpRightDown(skeleton))
                                {
                                    score += 100;
                                    CurrentTime = 0;
                                    correctSound.Play();
                                    if (TimeUntilCompletion > 20)
                                    {
                                        TimeUntilCompletion -= 10;
                                    }
                                    gameStarted = false;
                                }
                                else if (MatchLeftDownRightDown(skeleton) || MatchLeftUpRightUp(skeleton) || MatchLeftDownRightUp(skeleton))
                                {
                                    wrongSound.Play();
                                    CurrentTime = 0;
                                    gameStarted = false;
                                    gameOver = true;
                                }
                            }
                            else if (GestureRequired == 1)
                            {
                                dc.DrawImage(PomPomLUp, PomPomL_Rect);
                                dc.DrawImage(PomPomRUp, PomPomL_Rect);
                                statusTxt.Text = "Left Up Right Up";
                                if (MatchLeftUpRightUp(skeleton))
                                {
                                    score += 100;
                                    CurrentTime = 0;
                                    correctSound.Play();
                                    if (TimeUntilCompletion > 20)
                                    {
                                        TimeUntilCompletion -= 10;

                                    }
                                    gameStarted = false;
                                }
                                else if (MatchLeftUpRightDown(skeleton) || MatchLeftDownRightDown(skeleton) || MatchLeftDownRightUp(skeleton))
                                {
                                    wrongSound.Play();
                                    CurrentTime = 0;
                                    gameStarted = false;
                                    gameOver = true;
                                }
                            }
                            else if (GestureRequired == 2)
                            {
                                dc.DrawImage(PomPomLDown, PomPomL_Rect);
                                dc.DrawImage(PomPomRDown, PomPomL_Rect);
                                statusTxt.Text = "Left Down Right Down";
                                if (MatchLeftDownRightDown(skeleton))
                                {
                                    score += 100;
                                    CurrentTime = 0;
                                    correctSound.Play();
                                    if (TimeUntilCompletion > 20)
                                    {
                                        TimeUntilCompletion -= 10;
                                    }
                                    gameStarted = false;
                                }
                                else if (MatchLeftUpRightDown(skeleton) || MatchLeftUpRightUp(skeleton) || MatchLeftDownRightUp(skeleton))
                                {
                                    wrongSound.Play();
                                    CurrentTime = 0;
                                    gameStarted = false;
                                    gameOver = true;
                                }
                            }
                            if (GestureRequired == 3)
                            {
                                dc.DrawImage(PomPomLDown, PomPomL_Rect);
                                dc.DrawImage(PomPomRUp, PomPomL_Rect);
                                statusTxt.Text = "Left Down Right Up";
                                if (MatchLeftDownRightUp(skeleton))
                                {
                                    score += 100;
                                    CurrentTime = 0;
                                    correctSound.Play();
                                    if (TimeUntilCompletion > 20)
                                    {
                                        TimeUntilCompletion -= 10;
                                    }
                                    gameStarted = false;
                                }
                                else if (MatchLeftUpRightDown(skeleton) || MatchLeftUpRightUp(skeleton) || MatchLeftDownRightDown(skeleton))
                                {
                                    wrongSound.Play();
                                    CurrentTime = 0;
                                    gameStarted = false;
                                    gameOver = true;
                                }
                            }
                            CurrentTime++;
                        }
                        else
                        {
                            wrongSound.Play();
                            CurrentTime = 0;
                            gameStarted = false;
                            gameOver = true;
                        }
                    }
                }
            }
        }
        private Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                //Find the closest skeleton       
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null) skeleton = skeletons[i];
                        else if (skeleton.Position.Z > skeletons[i].Position.Z)
                            skeleton = skeletons[i];
                    }
                }
            }

            return skeleton;
        }

        private void DrawSkeleton(Skeleton skeleton, DrawingContext dc, Brush jointBrush, Pen bonePen)
        {
            for (int i = 0; i < bones.Length; i += 2)
                DrawBone(skeleton, dc, bones[i], bones[i + 1], bonePen);

            // Render joints
            foreach (Joint j in skeleton.Joints)
            {
                if (j.TrackingState == JointTrackingState.NotTracked) continue;

                dc.DrawEllipse(jointBrush, null, SkeletonPointToScreenPoint(j.Position), 5, 5);
            }
        }

        private void DrawBone(Skeleton skeleton, DrawingContext dc, JointType jointType0, JointType jointType1, Pen bonePen)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked) return;

            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred) return;

            //dc.DrawLine(new Pen(Brushes.Red, 5),
            dc.DrawLine(bonePen,
                SkeletonPointToScreenPoint(joint0.Position),
                SkeletonPointToScreenPoint(joint1.Position));
        }

        private Point SkeletonPointToScreenPoint(SkeletonPoint sp)
        {
            ColorImagePoint pt = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                sp, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(pt.X, pt.Y);
        }

        // ------------------------------------------------------------
        private float GetJointDistance(Joint j1, Joint j2)
        {
            float distanceX = j1.Position.X - j2.Position.X;
            float distanceY = j1.Position.Y - j2.Position.Y;
            float distanceZ = j1.Position.Z - j2.Position.Z;
            return (float)Math.Sqrt(distanceX * distanceX
                + distanceY * distanceY + distanceZ * distanceZ);
        }


        //        private float prev_dist = 0.0f;
        //       private float threshold = 0.2f;

        private SoundPlayer soundPlayer = new SoundPlayer("Clap.wav"); // class exercise 2

        private bool MatchHandAtChestHigh(Skeleton skeleton)
        {
            if (skeleton == null) return false;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {
                if (hr.Position.Y < skeleton.Joints[JointType.Head].Position.Y && hl.Position.Y < skeleton.Joints[JointType.Head].Position.Y &&
                   hr.Position.Y > skeleton.Joints[JointType.Spine].Position.Y && hl.Position.Y > skeleton.Joints[JointType.Spine].Position.Y)
                {
                    statusTxt.Text = "Hands at chest high.";
                    currentStance = 0;
                    return true;
                }
                else return false;
            }
            else return false;
        }
        private bool MatchLeftUpRightDown(Skeleton skeleton)
        {
            if (skeleton == null) return false;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {
                if (hl.Position.Y > skeleton.Joints[JointType.Head].Position.Y &&
                   hr.Position.Y < skeleton.Joints[JointType.Spine].Position.Y)
                {
                    statusTxt.Text = "Left hand up / right hand down.";
                    currentStance = 1;
                    return true;
                }
                else return false;
            }
            else return false;
        }
        private bool MatchLeftUpRightUp(Skeleton skeleton)
        {
            if (skeleton == null) return false;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {
                if (hl.Position.Y > skeleton.Joints[JointType.Head].Position.Y &&
                    hr.Position.Y > skeleton.Joints[JointType.Head].Position.Y)
                {
                    statusTxt.Text = "Left hand up / right hand up.";
                    currentStance = 2;
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private bool MatchLeftDownRightDown(Skeleton skeleton)
        {
            if (skeleton == null) return false;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {
                if (hl.Position.Y < skeleton.Joints[JointType.Spine].Position.Y &&
                    hr.Position.Y < skeleton.Joints[JointType.Spine].Position.Y)
                {
                    statusTxt.Text = "Left hand down / right hand down.";
                    currentStance = 3;
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private bool MatchLeftDownRightUp(Skeleton skeleton)
        {
            if (skeleton == null) return false;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {
                if (hl.Position.Y < skeleton.Joints[JointType.Spine].Position.Y &&
                    hr.Position.Y > skeleton.Joints[JointType.Head].Position.Y)
                {
                    statusTxt.Text = "Left hand down / right hand up.";
                    currentStance = 4;
                    return true;
                }
                else return false;
            }
            else return false;
        }

    }
}
