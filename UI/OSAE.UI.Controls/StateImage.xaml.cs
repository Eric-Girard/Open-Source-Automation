﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace OSAE.UI.Controls
{
    /// <summary>
    /// Interaction logic for StateImage.xaml
    /// </summary>
    public partial class StateImage : UserControl
    {
        public OSAEObject screenObject { get; set; }
        public Point Location;
        public DateTime LastUpdated;
        public DateTime LastStateChange;
        
        public string StateMatch;
        public string CurState;
        public string CurStateLabel;

        public string ObjectName;
        private OSAEImageManager imgMgr = new OSAEImageManager();
        private OSAEImage img1;
        private OSAEImage img2;
        private OSAEImage img3;
        private OSAEImage img4;
        private int imageFrames = 0;
        private int currentFrame = 0;
        private int frameDelay = 100;
        private bool repeatAnimation;
     

        public StateImage(OSAEObject sObject)
        {
            InitializeComponent();

            screenObject = sObject;
            try
            {

                ObjectName = screenObject.Property("Object Name").Value;
                CurState = OSAEObjectStateManager.GetObjectStateValue(ObjectName).Value;
                LastStateChange = OSAEObjectStateManager.GetObjectStateValue(ObjectName).LastStateChange;
                Image.ToolTip = ObjectName + "\n" + CurState + " since: " + LastStateChange;

                Image.Tag = ObjectName;
                Image.MouseLeftButtonUp += new MouseButtonEventHandler(State_Image_MouseLeftButtonUp);

                foreach (OSAEObjectProperty p in screenObject.Properties)
                {
                    if (p.Value.ToLower() == CurState.ToLower())
                    {
                        StateMatch = p.Name.Substring(0, p.Name.LastIndexOf(' '));
                    }
                }

                string imgName = screenObject.Property(StateMatch + " Image").Value;
                string imgName2 = screenObject.Property(StateMatch + " Image 2").Value;
                string imgName3 = screenObject.Property(StateMatch + " Image 3").Value;
                string imgName4 = screenObject.Property(StateMatch + " Image 4").Value;
                try
                {
                    repeatAnimation = Convert.ToBoolean(screenObject.Property("Repeat Animation").Value);
                }
                catch (Exception ex)
                {
                    OSAEObjectPropertyManager.ObjectPropertySet(screenObject.Name, "Repeat Animation", "TRUE", "GUI");
                    repeatAnimation = true;
                }
                try
                {
                    frameDelay = Convert.ToInt16(screenObject.Property("Frame Delay").Value);
                }
                catch (Exception ex)
                {
                    frameDelay = 100;
                    OSAEObjectPropertyManager.ObjectPropertySet(screenObject.Name, "Frame Delay", "100", "GUI");
                }
                img1 = imgMgr.GetImage(imgName);
                if (img1.Data != null)
                {
                    var imageStream = new MemoryStream(img1.Data);
                    var bitmapImage = new BitmapImage();

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = imageStream;
                    bitmapImage.EndInit();

                    Image.Source = bitmapImage;
                    Image.Visibility = System.Windows.Visibility.Visible;
                    
                    imageFrames = 1;
                    currentFrame = 1;
                }
                else
                {
                    Image.Source = null;
                    Image.Visibility = System.Windows.Visibility.Hidden;
                }
                // Primary Frame is loaded, load up additional frames for the time to display.
                img2 = imgMgr.GetImage(imgName2);
                if (img2.Data != null)
                    imageFrames = 2;
                img3 = imgMgr.GetImage(imgName3);
                if (img3.Data != null)
                    imageFrames = 3;
                img4 = imgMgr.GetImage(imgName4);
                if (img4.Data != null)
                    imageFrames = 4;
            }
            catch (Exception ex)
            {
            }

            if (imageFrames > 1)
            {
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(frameDelay);
                timer.Tick += this.timer_Tick;
                timer.Start();
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (currentFrame < imageFrames)
                currentFrame += 1;
            else if (currentFrame == imageFrames)
                currentFrame = 1;
            var imageStream = new MemoryStream(img1.Data);;
            switch (currentFrame)
            {
                case 1:
                    imageStream = new MemoryStream(img1.Data);
                    break;
                case 2:
                    imageStream = new MemoryStream(img2.Data);
                    break;
                case 3:
                    imageStream = new MemoryStream(img3.Data);
                    break;
                case 4:
                    imageStream = new MemoryStream(img4.Data);
                    break;
            }
            var bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.StreamSource = imageStream;
            bitmapImage.EndInit();
            Image.Source = bitmapImage;
            Image.Visibility = System.Windows.Visibility.Visible;
        }


        public void Update()
        {
            try
            {
                CurState = OSAEObjectStateManager.GetObjectStateValue(ObjectName).Value;
                CurStateLabel = OSAEObjectStateManager.GetObjectStateValue(ObjectName).StateLabel;
                LastStateChange = OSAEObjectStateManager.GetObjectStateValue(ObjectName).LastStateChange;
            }
            catch (Exception ex)
            {

            }
            
            foreach (OSAEObjectProperty p in screenObject.Properties)
            {
                if (p.Value.ToLower() == CurState.ToLower())
                {
                    StateMatch = p.Name.Substring(0, p.Name.LastIndexOf(' '));
                }
            }

            try
            {
                //Location.X = Double.Parse(screenObject.Property(StateMatch + " X").Value);
               //Location.Y = Double.Parse(screenObject.Property(StateMatch + " Y").Value);
                Location.X = Double.Parse(OSAE.OSAEObjectPropertyManager.GetObjectPropertyValue(screenObject.Name, StateMatch + " X").Value);
                Location.Y = Double.Parse(OSAE.OSAEObjectPropertyManager.GetObjectPropertyValue(screenObject.Name, StateMatch + " Y").Value);                 

                string imgName = screenObject.Property(StateMatch + " Image").Value;
                OSAEImage img = imgMgr.GetImage(imgName);

                if (img.Data != null)
                {
                    var imageStream = new MemoryStream(img.Data);
                    this.Dispatcher.Invoke((Action)(() =>
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = imageStream;
                    bitmapImage.EndInit();
                    Image.Source = bitmapImage;
                    Image.ToolTip = ObjectName + "\n" + CurStateLabel + " since: " + LastStateChange;
                    }));
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void State_Image_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {             
            if (CurState == "ON")
            {
                OSAEMethodManager.MethodQueueAdd(ObjectName, "OFF", "", "", "GUI");
                OSAEObjectStateManager.ObjectStateSet(ObjectName, "OFF", "GUI");
            }
            else
            {
                OSAEMethodManager.MethodQueueAdd(ObjectName, "ON", "", "", "GUI");
                OSAEObjectStateManager.ObjectStateSet(ObjectName, "ON", "GUI");
            }        
        }            
    }
}