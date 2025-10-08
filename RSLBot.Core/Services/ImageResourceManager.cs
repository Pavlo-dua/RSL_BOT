using System.Drawing.Imaging;

namespace RSLBot.Core.Services;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;


   /// <summary>
    /// A singleton manager to pre-load and cache all image resources from a directory.
    /// This prevents repeated disk access and improves performance.
    /// It is thread-safe and implements IDisposable to release image resources on exit.
    /// </summary>
    public sealed class ImageResourceManager : IDisposable
    {
        // The dictionary to hold the cached Bitmap objects.
        // Keys are relative paths (e.g., "Templates\ArenaClassic\arena_classic_ver.png").
        private readonly Dictionary<string, Bitmap> _imageStore = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        
        // The root directory where images are stored.
        private readonly string _baseDirectory = "Configuration";
        private readonly string[] _supportedExtensions = { ".png", ".bmp" };

        /// <summary>
        /// Private constructor to enforce the singleton pattern.
        /// It triggers the initial loading of all images.
        /// </summary>
        public ImageResourceManager()
        {
            LoadAllImages();
        }

        /// <summary>
        /// Indexer to retrieve a loaded Bitmap by its relative path.
        /// </summary>
        /// <param name="relativePath">The relative path to the image from the 'Configuration' directory.</param>
        /// <returns>The cached Bitmap object.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the requested image was not found.</exception>
        public Bitmap this[string relativePath]
        {
            get
            {
                // Normalize path separators to ensure consistency.
                string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar).ToLower();
                
                if (_imageStore.TryGetValue(normalizedPath, out var bitmap))
                {
                    // Return a clone to prevent the original cached bitmap from being disposed by the caller.
                    return (Bitmap)bitmap.Clone();
                }
                
                throw new KeyNotFoundException($"The image resource '{relativePath}' was not found in the cache.");
            }
        }

             /// <summary>
        /// Scans the base directory recursively and loads all supported image files into memory.
        /// </summary>
        private void LoadAllImages()
        {
            if (!Directory.Exists(_baseDirectory))
            {
                // Handle the case where the configuration directory does not exist.
                Console.WriteLine($"Warning: Image resource directory not found at '{_baseDirectory}'");
                return;
            }

            // Get the full path of the parent directory of our base directory.
            // This will be used to create relative paths that include the base directory itself.
            string executionDirectory = Directory.GetParent(Path.GetFullPath(_baseDirectory))?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;

            // Get all files from the base directory and its subdirectories.
            var imageFiles = Directory.EnumerateFiles(_baseDirectory, "*.png", SearchOption.AllDirectories);

            foreach (var filePath in imageFiles)
            {
                try
                {
                    // Calculate the relative path from the execution directory to include the base directory.
                    // Example: "Y:\Project\Configuration\Templates\img.png" -> "Configuration\Templates\img.png"
                    string relativePath = Path.GetRelativePath(executionDirectory, filePath);
                    
                    // --- FIX ---
                    // Load the original bitmap and immediately convert it to a standard pixel format
                    // to avoid issues with indexed formats that Emgu.CV cannot handle.
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var originalBitmap = new Bitmap(fs))
                    {
                        // Create a new bitmap with the desired 32bpp ARGB format.
                        var bitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height, PixelFormat.Format32bppArgb);
                        
                        // Draw the original image onto the new bitmap, which performs the conversion.
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(originalBitmap, new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));
                        }
                        
                        // Store the converted, non-indexed bitmap.
                        _imageStore[relativePath] = bitmap;
                    }
                }
                catch (Exception ex)
                {
                    // Log an error if a file is corrupted or not a valid image.
                    Console.WriteLine($"Error loading image '{filePath}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Disposes all cached Bitmap objects to release memory.
        /// </summary>
        public void Dispose()
        {
            foreach (var bitmap in _imageStore.Values)
            {
                bitmap.Dispose();
            }
            _imageStore.Clear();
        }
    }