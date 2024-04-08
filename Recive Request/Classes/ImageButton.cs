﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Recive_Request.Classes
{
    public class ImageButton : Button
    {
        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton),
                                                     new FrameworkPropertyMetadata(typeof(ImageButton)));
        }


        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(ImageButton), new PropertyMetadata(default(ImageSource)));

    }
}