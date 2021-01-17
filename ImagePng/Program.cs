using System;

namespace ImagePng
{
    class Program
    {
        static void Main(string[] args)
        {
            ImagePng image = new ImagePng("test.png");
            Console.WriteLine("Width: {0}, Height: {1}", image.Width, image.Height);

            ColorRGBA pixel = image.GetPixel(0, 0);
            Console.WriteLine("({0}, {1}, {2}, {3})", pixel.R, pixel.G, pixel.B, pixel.A);
        }
    }
}
