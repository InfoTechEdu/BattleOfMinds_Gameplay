using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class ImageLoaderCoroutine : MonoBehaviour
{
    public static void DownloadImage(string url, ref Sprite sprite)
    {

    }

}
public class ImageLoader
{
    public async static Task<Texture2D> LoadImage(string url)
    {
        Texture2D texture = new Texture2D(200, 200);


        Debug.Log("Downloading image by url - " + url);
        try
        {
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                    texture.LoadImage(bytes);
                }
            }
            return texture;
        }
        catch (Exception ex)
        {
            Debug.Log(string.Format("Failed to load the image by url {0}. Message: {1}", url, ex.Message));
        }

        return null;
    }


    //original
    //public async static Task<BitmapImage> LoadImage(Uri uri)
    //{
    //    BitmapImage bitmapImage = new BitmapImage();

    //    try
    //    {
    //        using (HttpClient client = new HttpClient())
    //        {
    //            using (var response = await client.GetAsync(uri))
    //            {
    //                response.EnsureSuccessStatusCode();

    //                using (IInputStream inputStream = await response.Content.ReadAsInputStreamAsync())
    //                {
    //                    bitmapImage.SetSource(inputStream.AsStreamForRead());
    //                }
    //            }
    //        }
    //        return bitmapImage;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine("Failed to load the image: {0}", ex.Message);
    //    }

    //    return null;
    //}
}