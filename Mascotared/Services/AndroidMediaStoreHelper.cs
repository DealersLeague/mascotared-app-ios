#if ANDROID
using Android.Content;
using Android.Provider;
using AndroidX.Core.Content;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace Mascotared;

public class AndroidMediaStoreHelper
{
    public async Task<string?> SaveImageToGallery(string imagePath, string fileName)
    {
        try
        {
            var context = Platform.CurrentActivity ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null) return null;

            var contentValues = new ContentValues();
            contentValues.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            contentValues.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
            contentValues.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryPictures + "/MascotaRed");

            var resolver = context.ContentResolver;
            var uri = resolver.Insert(MediaStore.Images.Media.ExternalContentUri, contentValues);

            if (uri != null)
            {
                using var outputStream = resolver.OpenOutputStream(uri);
                if (outputStream != null)
                {
                    var imageBytes = await File.ReadAllBytesAsync(imagePath);
                    await outputStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                    outputStream.Close();
                }

                // Notificar a la galería que se agregó una nueva imagen
                var mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                mediaScanIntent.SetData(uri);
                context.SendBroadcast(mediaScanIntent);

                return uri.ToString();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando imagen: {ex.Message}");
        }

        return null;
    }
}
#endif
