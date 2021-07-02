using System;
using System.Linq;
using UnityEngine;

namespace ARC
{
    public class ARCImageContainer
    {
        public int chunks { get; private set; }
        public string[] content { get; private set; }
        public float progress { get { return ((float) content.Count(c => c != null)) / chunks; } }

        const int CHUNK_SIZE = 1024;

        public ARCImageContainer(int count)
        {
            chunks = count;
            content = new string[chunks];
        }

        public ARCImageContainer(Texture2D image)
        {        
            byte[] imageBytes = image.EncodeToJPG();
            string imageBase64 = Convert.ToBase64String(imageBytes);

            chunks = imageBase64.Length / CHUNK_SIZE + 1;
            content = new string[chunks];

            for (int i = 0; i < chunks; ++i)
            {
                try { content[i] = imageBase64.Substring(i * CHUNK_SIZE, CHUNK_SIZE); }
                catch { content[i] = imageBase64.Substring(i * CHUNK_SIZE, imageBase64.Length - i * CHUNK_SIZE); }
            }
        }

        public Texture2D DecodeImage()
        {
            string imageBase64 = "";
            foreach (string chunk in content) { imageBase64 += chunk; }
            
            byte[] imageBytes = Convert.FromBase64String(imageBase64);

            Texture2D image = new Texture2D(0, 0);
            if (ImageConversion.LoadImage(image, imageBytes)) { return image; }
            return null;
        }
    }
}