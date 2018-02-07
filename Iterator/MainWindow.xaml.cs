using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Iterator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PExHelper _pex;
        bool _testRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            _pex = new PExHelper(this);
            _pex.Init();
            _pex.OnBanckrupcy += (_, __) => { if (!_testRunning) MessageBox.Show("You are bankrupt!"); };
            _pex.OnEndGame += (_, __) => { if (!_testRunning) MessageBox.Show("End of game"); };

            GetParameters_Click(null, null);
            ReadSummary_Click(null, null);
        }

        private void GetParameters_Click(object sender, RoutedEventArgs e)
        {
            AircraftPurchasesPerQtr.Text = _pex.AircraftPurchasesPerQtr.ToString();
            Hiring.Text = _pex.Hiring.ToString();
            PeoplesFare.Text = _pex.PeoplesFare.ToString();
            MarketingAsFracOfRevenue.Text = _pex.MarketingAsFracOfRevenue.ToString();
            TargetServiceScope.Text = _pex.TargetServiceScope.ToString();
        }

        private void SetParameters_Click(object sender, RoutedEventArgs e)
        {
            uint uval = 0; double dval = 0;
            if (uint.TryParse(AircraftPurchasesPerQtr.Text, out uval)) _pex.AircraftPurchasesPerQtr = uint.Parse(AircraftPurchasesPerQtr.Text);
            if (uint.TryParse(Hiring.Text, out uval)) _pex.Hiring = uint.Parse(Hiring.Text);
            if (double.TryParse(PeoplesFare.Text, out dval)) _pex.PeoplesFare = double.Parse(PeoplesFare.Text);
            if (double.TryParse(MarketingAsFracOfRevenue.Text, out dval)) _pex.MarketingAsFracOfRevenue = double.Parse(MarketingAsFracOfRevenue.Text);
            if (double.TryParse(TargetServiceScope.Text, out dval)) _pex.TargetServiceScope = double.Parse(TargetServiceScope.Text);
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            _pex.Run();
            ReadSummary_Click(sender, e);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _pex.Back();
            ReadSummary_Click(sender, e);
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            _pex.Restart();
            ReadSummary_Click(sender, e);
        }

        private void ReadSummary_Click(object sender, RoutedEventArgs e)
        {
            _pex.ReadSummary();
            Title = $"Quorter {_pex.Quarter}";

            if (_pex.TestBitmaps.Count >= 7 && !_testRunning)
            {
                SetImageSource(image0, _pex.TestBitmaps[0]);
                SetImageSource(image1, _pex.TestBitmaps[1]);
                SetImageSource(image2, _pex.TestBitmaps[2]);
                SetImageSource(image3, _pex.TestBitmaps[3]);
                SetImageSource(image4, _pex.TestBitmaps[4]);
                SetImageSource(image5, _pex.TestBitmaps[5]);
                SetImageSource(image6, _pex.TestBitmaps[6]);
                SetImageSource(image7, _pex.TestBitmaps[7]);
                if (_pex.TestBitmaps.Count > 7) SetImageSource(image8, _pex.TestBitmaps[8]);
            }

            listView1.ItemsSource = listView2.ItemsSource = null;
            listView1.ItemsSource = _pex.Summary.Values.Take(8);
            listView2.ItemsSource = _pex.Summary.Values.Skip(8);
        }

        void SetImageSource(System.Windows.Controls.Image image, Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                image.Source = bitmapimage;
            }
        }

        private void RunOCRTest_Click(object sender, RoutedEventArgs e)
        {
            _testRunning = true;
            Random rnd = new Random((int)DateTime.Now.Ticks);

            for (int i = 0; i < 1000; i++)
            {
                Title = $"Quarter {_pex.Quarter}";

                _pex.AircraftPurchasesPerQtr = (uint) (1 + rnd.Next(2));
                _pex.Hiring = (uint)(30 + rnd.Next(200));
                //_pex.PeoplesFare =
                //_pex.MarketingAsFracOfRevenue
                _pex.TargetServiceScope = 0.1 + 1 / (rnd.Next(10) + 1);

                int prevQuarter = _pex.Quarter;
                _pex.Run();
                _pex.ReadSummary();

                if (prevQuarter == _pex.Quarter)
                    _pex.Restart();
            }

            _testRunning = false;
        }
    }
}
