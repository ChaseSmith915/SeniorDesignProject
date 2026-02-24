using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using System;
using System.Collections.Generic;
using System.Text;

namespace HourGuard
{
    internal static class GetIconFromAppInfo
    {
        internal static ImageSource GetAppIcon(ApplicationInfo appInfo)
        {
            try
            {
                var pm = Android.App.Application.Context.PackageManager;
                Drawable iconDrawable = appInfo.LoadIcon(pm);

                if (iconDrawable is BitmapDrawable bitmapDrawable)
                {
                    return BitmapToImageSource(bitmapDrawable.Bitmap);
                }

                if (iconDrawable is AdaptiveIconDrawable adaptive)
                {
                    var bitmap = DrawableToBitmap(adaptive);
                    return BitmapToImageSource(bitmap);
                }
            }
            catch
            {
                // Ignore icon failures
            }

            return ImageSource.FromFile("default_app_icon.png");
        }

        private static Bitmap DrawableToBitmap(Drawable drawable)
        {
            Bitmap bitmap = Bitmap.CreateBitmap(
                drawable.IntrinsicWidth > 0 ? drawable.IntrinsicWidth : 100,
                drawable.IntrinsicHeight > 0 ? drawable.IntrinsicHeight : 100,
                Bitmap.Config.Argb8888);

            Canvas canvas = new Canvas(bitmap);
            drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
            drawable.Draw(canvas);

            return bitmap;
        }

        private static ImageSource BitmapToImageSource(Bitmap bitmap)
        {
            return ImageSource.FromStream(() =>
            {
                var stream = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                stream.Position = 0;
                return stream;
            });
        }
    }
}
