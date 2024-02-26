using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ComicBookReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int _index = 0;
        private int _imageAmount = 0;
        private string[] images = [];
        private BitmapImage imageSource;
        private string _cbrFilePath = string.Empty;
        private bool _dragStarted = false;

        private HashSet<string> _openedFiles = new HashSet<string>();

        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set
            {
                imageSource = value;
                OnPropertyChanged();
            }
        }

        public int ImagesAmount
        {
            get { return _imageAmount; }
            set
            {
                _imageAmount = value;
                OnPropertyChanged();
            }
        }

        public int CurrentIndex
        {
            get { return _index; }
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CurrentIndex = (int)e.NewValue;
            if (!_dragStarted)
            {
                SetImage(images[CurrentIndex]);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ReadFolder(string cprFilePath)
        {
            var directory = Directory.CreateDirectory(new FileInfo(cprFilePath).Name);
            _openedFiles.Add(directory.FullName);
            using (var archive = RarArchive.Open(cprFilePath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    if (File.Exists(Path.Combine(directory.FullName, entry.Key))) continue;
                    entry.WriteToDirectory(directory.FullName, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = false
                    });
                }
            }
            images = Directory.GetFiles(directory.FullName, "*", SearchOption.AllDirectories).ToArray();
            ImagesAmount = images.Length - 1;
            SetImage(images[_index]);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right && _index < images.Length - 1)
            {
                SetImage(images[++_index]);
                CurrentIndex = _index;
            }
            else if (e.Key == Key.Left && _index > 0)
            {
                SetImage(images[--_index]);
                CurrentIndex = _index;
            }
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //.cb7 → 7z
            //.cba → ACE
            //.cbr → RAR
            //.cbt → TAR
            //.cbz → ZIP

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "CPR", // Default file name
                DefaultExt = ".cbr", // Default file extension
                Filter = "Comic book archive (.cbr)|*.cbr" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ReadFolder(filename);
            }
        }

        private void slider2_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _dragStarted = true;
        }

        private void slider2_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _dragStarted = false;
            SetImage(images[(int)((Slider)sender).Value]);
        }

        private void SetImage(string filePath)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath);
            image.EndInit();

            ImageSource = image;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            images = [];
            ImageSource = null;

            //do my stuff before closing
            Console.WriteLine();
            foreach (var file in _openedFiles)
            {
                Directory.Delete(file, true);
            }
            base.OnClosing(e);
        }
    }
}