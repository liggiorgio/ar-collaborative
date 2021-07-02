using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARC
{
    public class ARCRuntimeLibraryBuilder
    {
        public List<Texture2D> referenceImages { get; private set; }
        public MutableRuntimeReferenceImageLibrary runtimeLibrary { get; private set; }

        public ARCRuntimeLibraryBuilder()
        {
            referenceImages = new List<Texture2D>();
        }

        ~ARCRuntimeLibraryBuilder()
        {
            ClearReferenceImages();
        }

        public void AddReferenceImage(Texture2D image)
        {
            referenceImages.Add(image);
        }

        public void ClearReferenceImages()
        {
            foreach (Texture2D image in referenceImages) { Texture2D.Destroy(image); }
            referenceImages.Clear();
        }

        public bool BuildLibrary(ARTrackedImageManager manager)
        {
            runtimeLibrary = manager.CreateRuntimeLibrary() as MutableRuntimeReferenceImageLibrary;

            foreach (Texture2D image in referenceImages)
            {
                runtimeLibrary.ScheduleAddImageWithValidationJob(image, image.name, 1f);
            }

            if (runtimeLibrary != null)
            {
                manager.referenceLibrary = runtimeLibrary;
                manager.requestedMaxNumberOfMovingImages = runtimeLibrary.count;
                return true;
            }

            return false;
        }
    }
}