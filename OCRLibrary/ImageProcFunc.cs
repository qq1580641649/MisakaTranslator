﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OCRLibrary
{
    public class ImageProcFunc
    {
        public static Dictionary<string, string> lstHandleFun = new Dictionary<string, string>() {
            { "不进行处理" , "ImgFunc_NoDeal" },
            { "OTSU二值化处理" , "ImgFunc_OTSU" }
        };

        public static Dictionary<string, string> lstOCRLang = new Dictionary<string, string>() {
            { "英语" , "eng" },
            { "日语" , "jpn" },
        };

        /// <summary>
        /// 根据方法名自动处理
        /// </summary>
        /// <param name="b"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Bitmap Auto_Thresholding(Bitmap b, string func) {
            switch (func) {
                case "ImgFunc_NoDeal":
                    return b;
                case "ImgFunc_OTSU":
                    return OtsuThreshold(b);
            }
            return b;
        }


        /// <summary>
        /// 得到图片的Base64编码
        /// </summary>
        /// <param name="img">欲处理的图片</param>
        /// <returns></returns>
        public static string GetFileBase64(Image img)
        {
            string fileName = Environment.CurrentDirectory + "\\OCRRes.png";
            try
            {
                img.Save(fileName, ImageFormat.Png);
            }
            catch (System.NullReferenceException ex)
            {
                throw ex;
            }
            FileStream filestream = new FileStream(fileName, FileMode.Open);
            byte[] arr = new byte[filestream.Length];
            filestream.Read(arr, 0, (int)filestream.Length);
            string baser64 = Convert.ToBase64String(arr);
            filestream.Close();
            return baser64;
        }
        

        /// <summary>
        /// 按固定阈值进行二值化处理
        /// </summary>
        /// <param name="b">图片</param>
        /// <param name="thresh">阈值 0-255</param>
        /// <returns></returns>
        public static Bitmap Thresholding(Bitmap b, byte thresh)
        {
            byte threshold = thresh;
            int width = b.Width;
            int height = b.Height;
            BitmapData data = b.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)data.Scan0;
                int offset = data.Stride - width * 4;
                byte R, G, B, gray;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        R = p[2];
                        G = p[1];
                        B = p[0];
                        gray = (byte)((R * 19595 + G * 38469 + B * 7472) >> 16);

                        if (gray >= threshold)
                        {
                            p[0] = p[1] = p[2] = 255;
                        }
                        else
                        {
                            p[0] = p[1] = p[2] = 0;
                        }
                        p += 4;
                    }
                    p += offset;
                }
                b.UnlockBits(data);
                return b;
            }
        }


        /// <summary>
        /// 自动二值化处理-OTSU
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Bitmap OtsuThreshold(Bitmap b)
        {
            // 图像灰度化   
            // b = Gray(b);   
            int width = b.Width;
            int height = b.Height;
            byte threshold = 0;
            int[] hist = new int[256];
            
            int AllPixelNumber = 0, PixelNumberSmall = 0, PixelNumberBig = 0;
            double MaxValue, AllSum = 0, SumSmall = 0, SumBig, ProbabilitySmall, ProbabilityBig, Probability;
            
            BitmapData data = b.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* p = (byte*)data.Scan0;
                int offset = data.Stride - width * 4;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        hist[p[0]]++;
                        p += 4;
                    }
                    p += offset;
                }
                b.UnlockBits(data);
            }

            //计算灰度为I的像素出现的概率   
            for (int i = 0; i < 256; i++)
            {
                AllSum += i * hist[i];     //   质量矩   
                AllPixelNumber += hist[i];  //  质量    
            }
            
            MaxValue = -1.0;
            for (int i = 0; i < 256; i++)
            {
                PixelNumberSmall += hist[i];
                PixelNumberBig = AllPixelNumber - PixelNumberSmall;
                if (PixelNumberBig == 0)
                {
                    break;
                }
                SumSmall += i * hist[i];
                SumBig = AllSum - SumSmall;
                ProbabilitySmall = SumSmall / PixelNumberSmall;
                ProbabilityBig = SumBig / PixelNumberBig;
                Probability = PixelNumberSmall * ProbabilitySmall * ProbabilitySmall + PixelNumberBig * ProbabilityBig * ProbabilityBig;

                if (Probability > MaxValue)
                {
                    MaxValue = Probability;
                    threshold = (byte)i;
                }
            }

            return Thresholding(b, threshold);
        }


        /// <summary>
        /// 按固定颜色进行二值化处理,即阈值颜色变白，非阈值颜色变黑
        /// </summary>
        /// <param name="b"></param>
        /// <param name="thresh"></param>
        /// <returns></returns>
        public static Bitmap ColorThreshold(Bitmap b, Color thresh)
        {
            int width = b.Width;
            int height = b.Height;
            BitmapData data = b.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)data.Scan0;
                int offset = data.Stride - width * 4;
                byte R, G, B, gray;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        R = p[2];
                        G = p[1];
                        B = p[0];
                        
                        if (R == thresh.R && G == thresh.G && B == thresh.B)
                        {
                            p[0] = p[1] = p[2] = 255;
                        }
                        else
                        {
                            p[0] = p[1] = p[2] = 0;
                        }
                        p += 4;
                    }
                    p += offset;
                }
                b.UnlockBits(data);
                return b;
            }
        }


        /// <summary>
        /// 将System.Drawing.Image转换成System.Windows.Media.Imaging.BitmapImage
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapImage ImageToBitmapImage(Image bitmap)
        {
            if (bitmap == null) {
                return null;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png); //格式选Bmp时，不带透明度

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }

        }
    }
}
