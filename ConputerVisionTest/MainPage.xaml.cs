using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.Text;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Net.Http;
using System.Net.Http.Headers;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace ConputerVisionTest
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
	{
        private string _subscriptionKey;


        public MainPage()
		{
			this.InitializeComponent();
            _subscriptionKey = "b1e514ef0f5b493xxxxx56a509xxxxxx";

        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".gif");
            openPicker.FileTypeFilter.Add(".bmp");
            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                await ShowPreviewAndAnalyzeImage(file);
            }
        }

        private async Task ShowPreviewAndAnalyzeImage(StorageFile file)
        {
            //preview image
            var bitmap = await LoadImage(file);
            ImageToAnalyze.Source = bitmap;

            //analyze image
            var results = await AnalyzeImage(file);

            //"fr", "ru", "it", "hu", "ja", etc...
            var ocrResults = await AnalyzeImageForText(file, "en");

            //parse result
            ResultsTextBlock.Text = ParseResult(results) + "\n\n " + ParseOCRResults(ocrResults);
        }

        private async Task<AnalysisResult> AnalyzeImage(StorageFile file)
        {

            VisionServiceClient VisionServiceClient = new VisionServiceClient(_subscriptionKey);

            using (Stream imageFileStream = await file.OpenStreamForReadAsync())
            {
                // Analyze the image for all visual features
                VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories
            , VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType
            , VisualFeature.Tags };
                AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);
                return analysisResult;
            }
        }

        private static async Task<BitmapImage> LoadImage(StorageFile file)
        {
            BitmapImage bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);

            bitmapImage.SetSource(stream);

            return bitmapImage;
        }

        private async Task<OcrResults> AnalyzeImageForText(StorageFile file, string language)
        {
            //language = "fr", "ru", "it", "hu", "ja", etc...
            VisionServiceClient VisionServiceClient = new VisionServiceClient(_subscriptionKey);
            using (Stream imageFileStream = await file.OpenStreamForReadAsync())
            {
                OcrResults ocrResult = await VisionServiceClient.RecognizeTextAsync(imageFileStream, language);
                return ocrResult;
            }
        }


        private async void button_Click(object sender, RoutedEventArgs e)
		{
            //string imageFilePath = @"R:\test.jpg";

            //Uri fileUri = new Uri(imageFilePath);
            //var result = DoWork(fileUri, true);
            //var result = DoWork(imageFilePath, true);




            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (var inputStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    var MyImage = new Image();
                    var decoder = await BitmapDecoder.CreateAsync(inputStream);
                    var provider = await decoder.GetPixelDataAsync();
                    var data = provider.DetachPixelData();
                    MyImage.Source = await CreateImageFromBuffer((int)decoder.PixelWidth, (int)decoder.PixelHeight, data, decoder.BitmapPixelFormat);
                    var byteData = new byte[inputStream.Size];
                    await inputStream.ReadAsync(byteData.AsBuffer(), (uint)byteData.Length, InputStreamOptions.None);
                    //MakeAnalysisRequest(byteData);

                    VisionServiceClient VisionServiceClient = new VisionServiceClient("");  // require key
                    OcrResults ocrResult = await VisionServiceClient.RecognizeTextAsync(byteData, "ja");

                }
            }

        }


        private async void MakeAnalysisRequest(byte[] byteData)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "visualFeatures=Categories,Description,Color&language=en";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Display the JSON response.
                Response.Text = JsonPrettyPrint(contentString);
            }
        }



        public async Task<BitmapImage> CreateImageFromBuffer(int width, int height, Byte[] pixData, BitmapPixelFormat format = BitmapPixelFormat.Bgra8)
        {
            var image = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                var enc = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
                enc.SetPixelData(format, BitmapAlphaMode.Ignore, (uint)width, (uint)height, width, height, pixData);

                await enc.FlushAsync();
                await image.SetSourceAsync(stream);
            }
            return image;
        }


        static async Task<OcrResults> UploadAndRecognizeImage(string imageFilePath, string language)
		{

			// Create Project Oxford Vision API Service client

			VisionServiceClient VisionServiceClient = new VisionServiceClient("d6f05f9e994347ba9eca928029e020ef");

            //using (Stream imageFileStream = File.OpenRead(imageFilePath))
            using (var imageFileStream = File.OpenRead(imageFilePath))
            {
                // Upload an image and perform OCR
                OcrResults ocrResult = await VisionServiceClient.RecognizeTextAsync(imageFileStream, language);
				return ocrResult;
			}
		}

		/// Perform the work for this scenario

		static async Task DoWork(string imageUri, bool upload)
		{
			string languageCode = "ja";
			var ocrResult = new OcrResults();
            //ocrResult = await UploadAndRecognizeImage(imageUri.LocalPath, languageCode);
            ocrResult = await UploadAndRecognizeImage(imageUri, languageCode);

            // Log analysis result in the log window
            //
            LogOcrResults(ocrResult);
		}

		static void LogOcrResults(OcrResults results)
		{
			StringBuilder stringBuilder = new StringBuilder();

			if (results != null && results.Regions != null)
			{
				stringBuilder.Append("Details:");
				stringBuilder.AppendLine();
				foreach (var item in results.Regions)
				{
					foreach (var line in item.Lines)
					{
						foreach (var word in line.Words)
						{
							stringBuilder.Append(word.Text);
							stringBuilder.Append(" ");
						}

						stringBuilder.AppendLine();
					}

					stringBuilder.AppendLine();
				}
			}

            //Console.WriteLine(stringBuilder.ToString());

            //textBox.Text = stringBuilder.ToString();
            

        }


	}
}
