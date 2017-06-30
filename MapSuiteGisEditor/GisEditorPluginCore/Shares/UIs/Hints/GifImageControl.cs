/*
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements.  See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership.  The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License.  You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GIFImageControl : System.Windows.Controls.Image
    {
        delegate void OnFrameChangedDelegate();
        private Bitmap bitmap;
        BitmapSource bitmapSource;

        public void AnimatedImageControl(Stream gifImageStream)
        {
            bitmap = new Bitmap(gifImageStream);
            Width = bitmap.Width;
            Height = bitmap.Height;
            ImageAnimator.Animate(bitmap, OnFrameChanged);
            bitmapSource = GetBitmapSource();
            Source = bitmapSource;
        }

        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                   new OnFrameChangedDelegate(OnFrameChangedInMainThread));
        }

        private void OnFrameChangedInMainThread()
        {
            ImageAnimator.UpdateFrames();
            if (bitmapSource != null)
                bitmapSource.Freeze();
            bitmapSource = GetBitmapSource();
            Source = bitmapSource;
            InvalidateVisual();
        }

        private BitmapSource GetBitmapSource()
        {
            IntPtr inptr = bitmap.GetHbitmap();
            bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                inptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(inptr);
            return bitmapSource;
        }

        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);
    }
}
