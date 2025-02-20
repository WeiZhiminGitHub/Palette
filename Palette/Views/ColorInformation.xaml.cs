﻿using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualBasic;
using Palette.Extension;
using Palette.Models;
using Palette.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps.Serialization;

namespace Palette.Views
{
    /// <summary>
    /// ColorInformation.xaml 的交互逻辑
    /// </summary>
    public partial class ColorInformation : UserControl
    {
        public ColorInformation()
        {
            InitializeComponent();
            this.DataContext = new ColorInformationViewModel();


            RenderColorPicker(100);
            CirThumb_ValueChanging(new Point(140, 140));

            WeakReferenceMessenger.Default.Register<CalculationColorTMessage, string>(this, AppToken.ColorToken,
                (r, m) =>
                {
                    if (m.CalType == CalculationColor.Complementary)
                    {
                        var vm = m.Parameter as ColorInformationViewModel;
                        // vm.ComplementaryBrush = CalculateComplementary();
                    }
                });
        }

        private bool _canExecute = false;
        static bool _callbackOperation = false;
        private int radius = 130;
        private WriteableBitmap bitmap;
        /// <summary>
        /// 最高亮度颜色
        /// </summary>
        private static Color CurHighestBrightnessColor;
        private static Color BrightnessRecordColor;
        public SolidColorBrush CurrentBrush
        {
            get { return (SolidColorBrush)GetValue(CurrentBrushProperty); }
            set { SetValue(CurrentBrushProperty, value); }
        }
        // Using a DependencyProperty as the backing store for CurrentBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentBrushProperty =
            DependencyProperty.Register("CurrentBrush", typeof(SolidColorBrush), typeof(ColorInformation),
                new PropertyMetadata(Brushes.White, OnCurrentBrushChangedCallBack, CurrentBrushCoerceValue));

        private static object CurrentBrushCoerceValue(DependencyObject d, object baseValue)
        {
            if (baseValue != null && baseValue is SolidColorBrush)
            {
                return (SolidColorBrush)baseValue;
            }
            return DependencyProperty.UnsetValue;
        }
        private static void OnCurrentBrushChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (_callbackOperation)
            {
                var colorInformation = d as ColorInformation;
                SolidColorBrush solidColorBrush = e.NewValue as SolidColorBrush;
                var point = Utility.GetPointLocationBySolidColorBrush(solidColorBrush, new Point(140, 140), 130);
                colorInformation?.cirThumb.SetPosition(point);
                HSBColor hsb = new HSBColor(solidColorBrush.Color);
                colorInformation.saturationSlideblock.Value = hsb.S * 100;
                colorInformation.transparentSlideblock.EndGradientColor = solidColorBrush.Color;
                colorInformation.transparentSlideblock.Value = hsb.B * 100;
                colorInformation.ellipesMask.Opacity = 1 - hsb.B;
                hsb.S = 1;
                colorInformation.saturationSlideblock.EndGradientColor = hsb.SolidColorBrush.Color;
                _callbackOperation = false;
            }
            var ci = d as ColorInformation;
            var vm = ci.DataContext as ColorInformationViewModel;
            if (vm != null)
            {
                vm.ComplementaryBrush = CalculateComplementaryColor(e.NewValue as SolidColorBrush);
                var ab = CalculateColor(e.NewValue as SolidColorBrush, 30);
                vm.AdjacentBrush1 = ab.Item1;
                vm.AdjacentBrush2 = ab.Item2;
                var cb = CalculateColor(e.NewValue as SolidColorBrush, 60);
                vm.ContrastingBrush1 = cb.Item1;
                vm.ContrastingBrush2 = cb.Item2;
                var mb = CalculateColor(e.NewValue as SolidColorBrush, 90);
                vm.MediumBrush1 = mb.Item1;
                vm.MediumBrush2 = mb.Item2;
                vm.MediumBrush3 = vm.ComplementaryBrush;
            }
        }
        private void RenderColorPicker(double brightness)
        {
            bitmap = new WriteableBitmap(radius * 2 + 20, radius * 2 + 20, 96.0, 96.0, PixelFormats.Pbgra32, null);
            Utility.DrawingAllPixel(bitmap, (x, y) =>
            {
                RGBColor rgb = new RGBColor(255, 255, 255, 0);
                double H = 0;
                Vector vector = Point.Subtract(new Point(x, y), new Point(radius + 10, radius + 10));
                var angle = Math.Atan(vector.Y / vector.X) * 180 / Math.PI;
                if (vector.X < 0)
                {
                    H = 270 + angle;
                }
                else if (vector.X > 0)
                {
                    H = 90 + angle;
                }
                else
                {
                    if (vector.Y < 0)
                    {
                        H = 0;
                    }
                    else if (vector.Y > 0)
                    {
                        H = 180;
                    }
                    else
                    {
                        return new RGBColor(255, (int)(255 * brightness), (int)(255 * brightness), (int)(255 * brightness));
                    }
                }
                //计算饱和度
                double S;
                if (vector.Length >= radius)
                {
                    S = 1;
                }
                else
                {
                    S = vector.Length / radius;
                }
                //亮度值
                double B = brightness;
                return new HSBColor(H, S, B).RgbColor;
            });
            this.img.Source = bitmap;
        }
        /// <summary>
        /// 亮度值改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void transparentSlideblock_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_callbackOperation) return;
            this.ellipesMask.Opacity = 1 - e.NewValue / 100;                  //设置调色盘亮度
            RenderBrush(e.NewValue / 100);                                    //渲染当前颜色
            this.saturationSlideblock.MaskOpacity = 1 - e.NewValue / 100;       //设置饱和度滑块背景亮度
            BrightnessRecordColor = this.transparentSlideblock.EndGradientColor;  //记录设置亮度为0时
        }
        private void RenderBrush(double brightness)
        {
            RGBColor rgb = new RGBColor((int)(CurHighestBrightnessColor.A),
                (int)(CurHighestBrightnessColor.R * brightness),
                (int)(CurHighestBrightnessColor.G * brightness),
                (int)(CurHighestBrightnessColor.B * brightness));
            this.CurrentBrush = rgb.SolidColorBrush;
        }
        private void saturationSlideblock_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_canExecute || _callbackOperation) return;
            //修改亮度滑块渐变色饱和度
            HSBColor hsv = new HSBColor(BrightnessRecordColor);
            hsv.S = e.NewValue / 100;
            this.transparentSlideblock.EndGradientColor = hsv.SolidColorBrush.Color;

            HSBColor hsb = new HSBColor(BrightnessRecordColor);
            hsb.S = e.NewValue / 100;
            hsb.B = 1 - this.ellipesMask.Opacity;
            _callbackOperation = true;
            this.CurrentBrush = hsb.SolidColorBrush;
        }
        private void CirThumb_ValueChanging(Point obj)
        {
            HSBColor hsv;
            #region 计算HSB颜色
            double H = 0;
            Vector vector = Point.Subtract(obj, new Point(radius + 10, radius + 10));
            var angle = Math.Atan(vector.Y / vector.X) * 180 / Math.PI;
            if (vector.X < 0)
            {
                H = 270 + angle;
            }
            else if (vector.X > 0)
            {
                H = 90 + angle;
            }
            else
            {
                if (vector.Y < 0)
                {
                    H = 0;
                }
                else if (vector.Y > 0)
                {
                    H = 180;
                }
                else
                {
                    hsv = new HSBColor();
                }
            }
            double S = vector.Length / radius;
            double B = 1;
            #endregion
            hsv = new HSBColor(H, S, B);
            CurHighestBrightnessColor = hsv.SolidColorBrush.Color;                  //记录当前最高亮度值 颜色
            hsv.B = 1 - this.ellipesMask.Opacity;
            this.CurrentBrush = hsv.SolidColorBrush;                                //设置当前颜色
            this.transparentSlideblock.EndGradientColor = this.CurrentBrush.Color;  //亮度滑块终点色设值
            BrightnessRecordColor = this.transparentSlideblock.EndGradientColor;
            _canExecute = false;
            this.saturationSlideblock.Value = S * 100;    //饱和度滑块  ：设置滑块位置
            _canExecute = true;
            hsv.S = 1;                                  //               设置滑块对应最大饱和度颜色
            this.saturationSlideblock.EndGradientColor = hsv.SolidColorBrush.Color;
        }
        private void hexText_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var tb = sender as TextBox;
                var hex = tb.Text;
                if (string.IsNullOrWhiteSpace(hex)) return;
                var color = (Color)ColorConverter.ConvertFromString(hex);
                RGBColor rgb = new RGBColor(color);
                if (rgb.SolidColorBrush != null)
                {
                    _callbackOperation = true;
                    this.CurrentBrush = rgb.SolidColorBrush;
                }
                else
                {
                    MessageExtension.Show("令牌无效");
                    tb.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageExtension.Show(ex.Message);
                return;
            }
        }
        private void rgbText_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"^\d{1,3},\d{1,3},\d{1,3}$");
                var tb = sender as TextBox;
                var rgbstr = tb.Text;
                if (!regex.IsMatch(rgbstr))
                {
                    MessageExtension.Show("令牌无效");
                }
                string[] rgbs = rgbstr.Split(",");
                foreach (var item in rgbs)
                {
                    int i = int.Parse(item);
                    if (i > 255 && i < 0)
                    {
                        MessageExtension.Show("令牌无效");
                        return;
                    }
                }
                RGBColor rgb = new RGBColor(255, int.Parse(rgbs[0]), int.Parse(rgbs[1]), int.Parse(rgbs[2]));
                if (rgb.SolidColorBrush != null)
                {
                    _callbackOperation = true;
                    this.CurrentBrush = rgb.SolidColorBrush;
                }
                else
                {
                    MessageExtension.Show("令牌无效");
                    tb.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageExtension.Show(ex.Message);
                return;
            }
        }
        private void hsbText_LostFocus(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"^\d{1,3},\d{1,3}%,\d{1,3}%$");
            try
            {
                var tb = sender as TextBox;
                var hsbstr = tb.Text;

                if (!regex.IsMatch(hsbstr))
                {
                    MessageExtension.Show("令牌无效");
                    return;
                }
                string[] hsbs = hsbstr.Split(",");
                int h = int.Parse(hsbs[0]);
                if (h < 0 || h >= 360)
                {
                    MessageExtension.Show("令牌无效");
                    return;
                }
                int index1 = hsbstr.IndexOf(",");
                int index2 = hsbstr.IndexOf("%");
                int length1 = index2 - index1 - 1;
                // var a = hsbstr.Substring(index1+1, length1);
                int s = int.Parse(hsbstr.Substring(index1 + 1, length1));
                if (s > 100)
                {
                    MessageExtension.Show("令牌无效");
                    return;
                }
                int length2 = hsbstr.Length - index2 - 3;
                int b = int.Parse(hsbstr.Substring(index2 + 2, length2));
                if (b > 100)
                {
                    MessageExtension.Show("令牌无效");
                    return;
                }

                HSBColor hsb = new HSBColor(h, (double)s / 100, (double)b / 100);
                if (hsb.SolidColorBrush != null)
                {
                    _callbackOperation = true;
                    this.CurrentBrush = hsb.SolidColorBrush;
                }
                else
                {
                    MessageExtension.Show("令牌无效");
                    tb.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageExtension.Show(ex.Message);
                return;
            }

        }
        private static SolidColorBrush CalculateComplementaryColor(SolidColorBrush brush)
        {
            HSBColor hsb = new HSBColor(brush.Color);
            var h = hsb.H + 180;
            if (h > 360)
                h -= 360;
            hsb.H = h;

            return hsb.SolidColorBrush;
        }
        private static (SolidColorBrush, SolidColorBrush) CalculateColor(SolidColorBrush brush, int degree)
        {
            HSBColor hsb1 = new HSBColor(brush.Color);
            var h1 = hsb1.H + degree;
            if (h1 > 360)
            {
                h1 -= 360;
            }
            hsb1.H = h1;

            HSBColor hsb2 = new HSBColor(brush.Color);
            var h2 = hsb2.H - degree;
            if (h2 < 0)
            {
                h2 += 360;
            }
            hsb2.H = h2;
            return new(hsb1.SolidColorBrush, hsb2.SolidColorBrush);
        }
    }
}
