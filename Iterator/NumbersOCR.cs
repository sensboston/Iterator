using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Iterator
{
    public class CharTemplate
    {
        public char Char { get; set; }
        public Bitmap Template { get; set; }
        public double Brightness { get; set; }
    }

    public class NumbersOCR
    {
        const string WHITE_COLOR = "ffffffff";
        List<CharTemplate> _digits = new List<CharTemplate>();
        double[] _subResults, _brighResults;

        public NumbersOCR()
        {
            var template = BitmapImageToBitmap(new BitmapImage(new Uri("pack://application:,,,/digits.png")));

            // This is out numbers; some of 'em required a few images for good recognition (worst chars are '0' and '4')
            var numbers = "-0012344456789.";
            // This is a widths of our number templates
            int[] widths = new int[] { 5, 7, 8, 7, 7, 7, 8, 8, 7, 7, 7, 7, 7, 7, 3 };

            int x = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                var rect = new RectangleF(x, 0, widths[i], template.Height);
                _digits.Add(new CharTemplate() { Char = numbers[i], Template = CopyBitmapRect(template, rect) });
                _digits.Last().Brightness = CalcRectBrightness(_digits.Last().Template, new Rect(0, 0, _digits.Last().Template.Width, _digits.Last().Template.Height));
                x += widths[i];
            }

            _subResults = new double[_digits.Count];
            _brighResults = new double[_digits.Count];
        }

        public string Recognize(BitmapImage bitmapImage)
        {
            return Recognize(BitmapImageToBitmap(bitmapImage));
        }

        public string Recognize(Bitmap bitmap)
        {
            string diffResult = string.Empty, brightResult = string.Empty;
            int startX = -1, startY = -1, endX = -1;

            // First, determine start Y,
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                    if (bitmap.GetPixel(x, y).Name != WHITE_COLOR)
                    {
                        startY = y;
                        break;
                    }
                if (startY != -1) break;
            }
            // start X
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                    if (bitmap.GetPixel(x, y).Name != WHITE_COLOR)
                    {
                        startX = x;
                        break;
                    }
                if (startX != -1) break;
            }
            // and end X
            for (int x = bitmap.Width-1; x > startX; x--)
            {
                for (int y = 0; y < bitmap.Height; y++)
                    if (bitmap.GetPixel(x, y).Name != WHITE_COLOR)
                    {
                        endX = x;
                        break;
                    }
                if (endX != -1) break;
            }

            // If it's not a completely white bitmap (we assume white background), let's compare chars with templates
            if (startX > -1 && startY > -1)
            {
                // Now, try to recognize characters
                while (startX < endX)
                {
                    for (int i = 0; i < _digits.Count; i++)
                    {
                        // First, compare bitmaps and save brightness differences
                        Rect rect = new Rect(startX, startY, _digits[i].Template.Width, _digits[i].Template.Height);
                        double b = CompareBitmaps(bitmap, rect, _digits[i].Template);
                        if ((i == 0 || i == _digits.Count-1) && b > 4) b = 100;
                        _subResults[i] = b;

                        // Than find the difference between digit region brightness and rectangle brightness 
                        _brighResults[i] = Math.Abs(_digits[i].Brightness - CalcRectBrightness(bitmap, rect));
                    }
                    int minIndex = Array.IndexOf(_subResults, _subResults.Min());
                    int brIndex = Array.IndexOf(_brighResults, _brighResults.Min());

                    diffResult += _digits[minIndex].Char;
                    brightResult += _digits[brIndex].Char;

                    startX += _digits[minIndex].Template.Width;
                }
            }

            //Debug.Assert(diffResult != brightResult);
            if (diffResult != brightResult)
                Debug.WriteLine(diffResult + " != " + brightResult);

            return diffResult;
        }

        /// <summary>
        /// Subtract one bitmap from another and summarize differences
        /// </summary>
        /// <param name="source"></param>
        /// <param name="srcRect"></param>
        /// <param name="target"></param>
        /// <param name="threshhold"></param>
        /// <returns></returns>
        double CompareBitmaps(Bitmap source, Rect srcRect, Bitmap target)
        {
            double diff = 0;

            if (srcRect.Left + srcRect.Width <= source.Width && srcRect.Top + srcRect.Height <= source.Height)
            {
                for (int x = 0; x < srcRect.Width; x++)
                    for (int y = 0; y < srcRect.Height; y++)
                    {
                        Color srcPixel = source.GetPixel(x + (int)srcRect.X, y + (int)srcRect.Y);
                        Color dstPixel = target.GetPixel(x, y);
                        Color substract = Color.FromArgb((Math.Abs(srcPixel.R - dstPixel.R) + Math.Abs(srcPixel.G - dstPixel.G) + Math.Abs(srcPixel.B - dstPixel.B)));
                        diff += substract.GetBrightness();
                    }
            }
            return diff;
        }

        double CalcRectBrightness(Bitmap bitmap, Rect rect)
        {
            double result = 0;

            if (rect.Left + rect.Width <= bitmap.Width && rect.Top + rect.Height <= bitmap.Height)
            {
                for (int x = (int)rect.X; x < rect.X + rect.Width; x++)
                    for (int y = (int)rect.Y; y < rect.Y + rect.Height; y++)
                    {
                        Color c = bitmap.GetPixel(x, y);
                        result += c.GetBrightness();
                    }
            }
            return result;
        }

        Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                return new Bitmap(new Bitmap(outStream));
            }
        }

        Bitmap CopyBitmapRect(Bitmap source, RectangleF srcRect)
        {
            Bitmap result = new Bitmap((int)srcRect.Width, (int)srcRect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(source, new RectangleF(0, 0, result.Width, result.Height), srcRect, GraphicsUnit.Pixel);
            }
            return result;
        }

    }
}
