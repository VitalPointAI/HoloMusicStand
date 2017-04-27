using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Urho;
using Urho.HoloLens;
using Windows.Storage;
using Windows.Data.Pdf;
using Windows.Storage.Pickers;
using System.IO;
using System.Threading.Tasks;
using Urho.Resources;

namespace HoloUrhoSharp1
{
    internal class Program
    {
        [MTAThread]
        static void Main()
        {
            var options = new ApplicationOptions("Data");
            var appViewSource = new UrhoAppViewSource<MusicStandApplication>(options);
            appViewSource.UrhoAppViewCreated += AppViewCreated;
            Urho.Application.UnhandledException += Application_UnhandledException;
            CoreApplication.Run(appViewSource);
        }

        static void AppViewCreated(UrhoAppView view)
        {
            view.WindowIsSet += View_WindowIsSet;
            view.AppStarted += View_AppStarted;
        }

        static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.Exception);
            if (Debugger.IsAttached)
                Debugger.Break();
            // e.Handled = true;
        }

        static void View_AppStarted(HoloApplication app)
        {
        }

        static void View_WindowIsSet(CoreWindow window)
        {
            // Subscribe to CoreWindow events, for example Input
            // window.KeyDown += 
        }
    }


    public class MusicStandApplication : HoloApplication
    {
        Node MusicNode;
        Urho.Shapes.Plane Music;
        uint PageCount = 0;
        uint CurrentPage = 0;

        public MusicStandApplication(ApplicationOptions opts) : base(opts) { }

        protected override async void Start()
        {
            await OpenPDF();

            // base.Start() creates a basic scene
            base.Start();

            MusicNode = Scene.CreateChild();
            MusicNode.Position = new Vector3(0, 0, 1.5f); // Lets place the music 1.5 meter away
            MusicNode.Rotation = new Quaternion(0,90,-90);
            MusicNode.Scale = new Vector3(0.4f * 1.414f, 0, 0.4f);

            //subscribe to some input events:
            EnableGestureManipulation = true;
            EnableGestureTapped = true;

            // Create a Plane component for holding the music
            Music = MusicNode.CreateComponent<Urho.Shapes.Plane>();            
            
            // Override the default material (material is a set of tecniques, parameters and textures)
            //Material MusicMaterial = ResourceCache.GetMaterial("Page_" + CurrentPage.ToString());
            Music.SetMaterial(Material.FromImage(ResourceCache.GetImage("Page_"+CurrentPage.ToString())));
            
// requires Microphone capability enabled
await RegisterCortanaCommands(new Dictionary<string, Action> {
        { "bigger",  () => MusicNode.Scale *= 1.2f },
        { "smaller", () => MusicNode.Scale *= 0.8f },
        { "next", () => NextPage() },
        { "previous", () => PreviousPage() }
    });
await TextToSpeech("Lets play som music!");
        }

        private void PreviousPage()
        {
            if (CurrentPage > 0)
            {
                CurrentPage--;
                Music.SetMaterial(Material.FromImage(ResourceCache.GetImage("Page_" + CurrentPage.ToString())));
            }
        }

        private void NextPage()
        {
            if (CurrentPage < PageCount)
            {
                CurrentPage++;
                Music.SetMaterial(Material.FromImage(ResourceCache.GetImage("Page_" + CurrentPage.ToString())));
            }
        }

        // HoloLens optical stabilization (optional)
        //public override Vector3 FocusWorldPoint => MusicNode.WorldPosition;

        protected override void OnUpdate(float timeStep)
        {
        }

        // handle input:

        Vector3 MusicPostionBeforeManipulations;
        public override void OnGestureManipulationStarted()
        {
            MusicPostionBeforeManipulations = MusicNode.Position;
        }

        public override void OnGestureManipulationUpdated(Vector3 relativeHandPosition)
        {
            MusicNode.Position = relativeHandPosition + MusicPostionBeforeManipulations;
        }

        public override void OnGestureTapped()
        {
            NextPage();
        }
        private async Task<Boolean> OpenPDF()
        {
            /*
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".pdf");

            StorageFile file = await openPicker.PickSingleFileAsync();
            */
            try
            {                
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri(@"ms-appx:///Data/Boy Paganini - Cello.pdf"));
                if (file != null)
                {
                    await LoadPdfFileAsync(file);
                    return true;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return false;
        }
private async Task LoadPdfFileAsync(StorageFile selectedFile)
{
    PdfDocument pdfDocument = await PdfDocument.LoadFromFileAsync(selectedFile); ;
    if (pdfDocument != null && pdfDocument.PageCount > 0)
    {
        PageCount = pdfDocument.PageCount;
        for (int pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
        {
            var pdfPage = pdfDocument.GetPage((uint)pageIndex);
            if (pdfPage != null)
            {
                MemoryStream ImageStream = new MemoryStream();
                PdfPageRenderOptions pdfPageRenderOptions = new PdfPageRenderOptions();
                pdfPageRenderOptions.DestinationWidth = (uint)(1024);                                
                await pdfPage.RenderToStreamAsync(ImageStream.AsRandomAccessStream(), pdfPageRenderOptions);
                ImageStream.Position = 0;

                Image i = new Image();
                i.Load(new MemoryBuffer(ImageStream));
                i.Name = "Page_" + pageIndex.ToString();
                ResourceCache.AddManualResource(i);

                pdfPage.Dispose();
            }
        }
    }
}
    }
}