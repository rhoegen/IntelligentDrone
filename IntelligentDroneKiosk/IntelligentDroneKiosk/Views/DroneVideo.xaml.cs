using DJI.WindowsSDK;
using IntelligentDroneKiosk.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IntelligentDroneKiosk.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DroneVideo : Page
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        private DJIVideoParser.Parser videoParser;
        public WriteableBitmap VideoSource;
        private byte[] decodedDataBuf;
        private object bufLock = new object();

        //used to process every Xth frame to ease load on api
        private int frameCount = 0;

        public DroneVideo()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            InitializeVideoFeedModule();

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UninitializeVideoFeedModule();
        }

        private void InitializeVideoFeedModule()
        {
            try
            {
                this.videoParser = new DJIVideoParser.Parser();
            }
            catch (FileNotFoundException e)
            {
                Debug.WriteLine(e.FileName);
            }

            this.videoParser.Initialize();
            this.videoParser.SetVideoDataCallack(0, 0, ReceiveDecodedData);
            Debug.WriteLine(DJISDKManager.Instance.SDKRegistrationResultCode);
            if (DJISDKManager.Instance.SDKRegistrationResultCode == SDKError.NO_ERROR)
            {
                //Register event receiver
                DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
            }
        }

        private void UninitializeVideoFeedModule()
        {
            if (DJISDKManager.Instance.SDKRegistrationResultCode == SDKError.NO_ERROR)
            {
                this.videoParser.SetVideoDataCallack(0, 0, null);
                DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated -= OnVideoPush;
            }
        }

        void OnVideoPush(VideoFeed sender, [ReadOnlyArray] ref byte[] bytes)
        {
            this.videoParser.PushVideoData(0, 0, bytes, bytes.Length);
        }

        async void ReceiveDecodedData(byte[] data, int width, int height)
        {
            lock (bufLock)
            {
                if (decodedDataBuf == null)
                {
                    decodedDataBuf = data;
                }
                else
                {
                    data.CopyTo(decodedDataBuf.AsBuffer());
                }
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (VideoSource == null || VideoSource.PixelWidth != width || VideoSource.PixelHeight != height)
                {
                    VideoSource = new WriteableBitmap((int)width, (int)height);
                    fpvImage.Source = VideoSource;
                }

                lock (bufLock)
                {
                    decodedDataBuf.AsBuffer().CopyTo(VideoSource.PixelBuffer);
                }
                VideoSource.Invalidate();
                //Only process every 120th frame to ease load on the api (needed if using free/trial Azure subscriptions)
                if (frameCount % 120 == 0)
                {
                    SendToCognitiveServices(VideoSource);
                }
                frameCount++;
            });
        }

        /// <summary>
        /// Gets the analysis of the specified image file by using
        /// the Computer Vision REST API.
        /// </summary>
        /// <param name="byteData">A byte array of the image file to analyze.</param>
        async Task<ObjectIdentifier> MakeComputerVisionObjectRequest(byte[] byteData)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", localSettings.Values["ComputerVisionSubKey"].ToString());

                // Request parameters. A third optional parameter is "details".
                // The Analyze Image method returns information about the following
                // visual features:
                // Categories:  categorizes image content according to a
                //              taxonomy defined in documentation.
                // Description: describes the image content with a complete
                //              sentence in supported languages.
                // Color:       determines the accent color, dominant color, 
                //              and whether an image is black & white.
                string requestParameters =
                    "visualFeatures=objects";

                //Get Computer Vision URI from settings
                string tempUri = localSettings.Values["ComputerVisionEndpointUrl"].ToString();
                //remote trailing "/" if present
                tempUri = tempUri.EndsWith("/") ? tempUri.Substring(0, tempUri.Length - 1) : tempUri;
                //Add request parameters
                string uri = tempUri + "?" + requestParameters;

                HttpResponseMessage response;

                // Read the contents of the specified local image
                // into a byte array.
                //byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(uri, content);
                }

                // Asynchronously get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                Debug.WriteLine(contentString);
                try
                {
                    JObject jObject = JObject.Parse(contentString);

                    //TODO: Delete these lines after testing of new solution
                    //JToken tagName = jObject.SelectToken("$.predictions[0]['tagName']");
                    //JToken probability = jObject.SelectToken("$.predictions[0]['probability']");

                    //Sort the JSON Response by probability descending to get the most accurate prediction
                    IEnumerable<JToken> tokenCollection = jObject.SelectToken("$.objects").OrderByDescending(c => c["confidence"].Value<double>());
                    if(tokenCollection.Count() > 0) {
                        JToken token = tokenCollection.First();
                        //JToken boundingBox = jObject.SelectToken("$.predictions[0]['boundingBox']");
                        JToken tagName = token.SelectToken("$.object");
                        JToken boundingBox = token.SelectToken("$.rectangle");
                        JToken probability = token.SelectToken("$.confidence");

                        ObjectIdentifier objIdent = new ObjectIdentifier();
                        objIdent.Left = (double)boundingBox.SelectToken("x");
                        objIdent.Top = (double)boundingBox.SelectToken("y");
                        objIdent.Width = (double)boundingBox.SelectToken("w");
                        objIdent.Height = (double)boundingBox.SelectToken("h");
                        objIdent.Tag = tagName.ToString() + "; " + probability.ToString();
                        return objIdent;
                    }
                    return null;                    
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("\n" + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets the analysis of the specified image file by using
        /// the Computer Vision REST API.
        /// </summary>
        /// <param name="byteData">A byte array of the image file to analyze.</param>
        async Task<String> MakeComputerVisionRequest(byte[] byteData)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", localSettings.Values["ComputerVisionSubKey"].ToString());

                // Request parameters. A third optional parameter is "details".
                // The Analyze Image method returns information about the following
                // visual features:
                // Categories:  categorizes image content according to a
                //              taxonomy defined in documentation.
                // Description: describes the image content with a complete
                //              sentence in supported languages.
                // Color:       determines the accent color, dominant color, 
                //              and whether an image is black & white.
                string requestParameters =
                    "visualFeatures=Categories,Description,Color";

                //Get Computer Vision URI from settings
                string tempUri = localSettings.Values["ComputerVisionEndpointUrl"].ToString();
                //remote trailing "/" if present
                tempUri = tempUri.EndsWith("/") ? tempUri.Substring(0, tempUri.Length - 1) : tempUri;
                //Add request parameters
                string uri = tempUri + "?" + requestParameters;

                HttpResponseMessage response;

                // Read the contents of the specified local image
                // into a byte array.
                //byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(uri, content);
                }

                // Asynchronously get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Display the JSON response.
                //Debug.WriteLine("\nResponse:\n\n{0}\n", JToken.Parse(contentString).ToString());
                //Now extract just the description generated by Computer Vision Service using JSON.Net
                try
                {
                    JObject jObject = JObject.Parse(contentString);
                    //Get the first Caption that is proviced using JPath
                    JToken value = jObject.SelectToken("$.description.captions[0]['text']");
                    Debug.WriteLine(value.ToString());
                    return value.ToString();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return "";
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("\n" + e.Message);
                return "";
            }
        }

        private void drawRectangle(double x, double y, double width, double height, string label)
        {
            Rect.Width = width;
            Rect.Height = height;
            Rect.SetValue(Canvas.LeftProperty, x);
            Rect.SetValue(Canvas.TopProperty, y);
            Rect.Visibility = Visibility.Visible;
                        
            RectText.Text = label;
            RectText.SetValue(Canvas.LeftProperty, x);
            RectText.SetValue(Canvas.TopProperty, y);
            
            RectBorder.SetValue(Canvas.LeftProperty, x);
            RectBorder.SetValue(Canvas.TopProperty, y);
            RectBorder.Visibility = Visibility.Visible;
            RectText.Visibility = Visibility.Visible;
        }

        private async void SendToCognitiveServices(WriteableBitmap VideoSource)
        {
            //Encode the raw image file from the DJI Feed to JPG for processing in Azure
            byte[] imageAsJpeg = await EncodeJpeg(VideoSource);

            textBoxCsDescription.Text = await MakeComputerVisionRequest(imageAsJpeg);
            //Get the name and position of the most accurately recognized object according to the model defined by "projectID"
            ObjectIdentifier obj = await MakeCustomVisionRequest(imageAsJpeg);
            ObjectIdentifier compVisionRect = await MakeComputerVisionObjectRequest(imageAsJpeg);
            try
            {
                //draw the rectangle where the object is found on the canvas overlaying the image
                //Custom Vision gives x, y, width and height as percentage of the image dimensions. To get the pixels, the values have to be multiplied with the image height/width
                //TODO: +60 offset in height is needed to account for the text field, clean solution needed
                double rectX = obj.Left * VideoSource.PixelWidth;
                double rectY = obj.Top * VideoSource.PixelHeight+250;
                double rectWidth = obj.Width * VideoSource.PixelWidth;
                double rectHeight = obj.Height * VideoSource.PixelHeight;
                string rectText = obj.Tag;

                Debug.WriteLine("X: " + rectX + ", Y: " + rectY + ", Width: " + rectWidth + ", Height: " + rectHeight);

                //drawRectangle(rectX,rectY ,rectWidth ,rectHeight , rectText);
                drawRectangle(compVisionRect.Left, compVisionRect.Top, compVisionRect.Width, compVisionRect.Height, compVisionRect.Tag);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        async Task<ObjectIdentifier> MakeCustomVisionRequest(byte[] image)
        {
            var client = new HttpClient();

            // Request headers - replace this example key with your valid subscription key.
            client.DefaultRequestHeaders.Add("Prediction-Key", localSettings.Values["CustomVisionPredictKey"].ToString());

            // Prediction URL - replace this example URL with your valid prediction URL.
            string url = localSettings.Values["CustomVisionEndpointUrl"].ToString() + (localSettings.Values["CustomVisionEndpointUrl"].ToString().EndsWith("/") ? "" : "/") + localSettings.Values["ObjDetectProjectId"].ToString() + "/image";

            HttpResponseMessage response;

            using (var content = new ByteArrayContent(image))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);
                string contentString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(contentString);
                try
                {
                    JObject jObject = JObject.Parse(contentString);

                    //TODO: Delete these lines after testing of new solution
                    //JToken tagName = jObject.SelectToken("$.predictions[0]['tagName']");
                    //JToken probability = jObject.SelectToken("$.predictions[0]['probability']");

                    //Sort the JSON Response by probability descending to get the most accurate prediction
                    IEnumerable<JToken> tokenCollection = jObject.SelectToken("$.predictions").OrderByDescending(c => c["probability"].Value<double>());
                    JToken token = tokenCollection.First();
                    //JToken boundingBox = jObject.SelectToken("$.predictions[0]['boundingBox']");
                    JToken tagName = token.SelectToken("$.tagName");
                    JToken boundingBox = token.SelectToken("$.boundingBox");
                    JToken probability = token.SelectToken("$.probability");

                    ObjectIdentifier objIdent = new ObjectIdentifier();
                    objIdent.Left = (double)boundingBox.SelectToken("left");
                    objIdent.Top = (double)boundingBox.SelectToken("top");
                    objIdent.Width = (double)boundingBox.SelectToken("width");
                    objIdent.Height = (double)boundingBox.SelectToken("height");
                    objIdent.Tag = tagName.ToString() + "; " + probability.ToString();

                    return objIdent;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        private async Task<byte[]> EncodeJpeg(WriteableBitmap bmp)
        {
            SoftwareBitmap soft = SoftwareBitmap.CreateCopyFromBuffer(bmp.PixelBuffer, BitmapPixelFormat.Bgra8, bmp.PixelWidth, bmp.PixelHeight);
            byte[] array = null;

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch { }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }

            return array;
        }
    }
}
