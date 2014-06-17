using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net;

namespace Simnova.Graphics
{
    public class ImageUtil
    {
        public static Image CropImage(Image imageToCrop, Rectangle cropArea)
        {
            var bmpImage = new Bitmap(imageToCrop);
            var bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
            return (Image)(bmpCrop);
        }

        public static Image CropAndFill(Image imageToResize, Size newSize)
        {
            float nPercent = 0;
            var sourceWidth = imageToResize.Width;
            var sourceHeight = imageToResize.Height;

            var startWidth = 0;
            var startHeight = 0;

            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)newSize.Width / (float)sourceWidth);
            nPercentH = ((float)newSize.Height / (float)sourceHeight);

            nPercent = nPercentH < nPercentW ? nPercentW : nPercentH;
            var destWidth = (int)(sourceWidth * nPercent);
            var destHeight = (int)(sourceHeight * nPercent);

            var b = new Bitmap(newSize.Width, newSize.Height);
            var g = System.Drawing.Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            startWidth = (newSize.Width - destWidth) < 0 ? (newSize.Width - destWidth) / 2 : 0;
            startHeight = (newSize.Height - destHeight) < 0 ? (newSize.Height - destHeight) / 2 : 0;

            g.DrawImage(imageToResize, startWidth, startHeight, destWidth, destHeight);
            g.Dispose();
            return (Image)b;
        }

        
        /// <summary>
        /// Fits an image to a given size. If new dimensions are different the resulting image can be letterboxed (filling bigger dimension with space) or the image can be centered in the space available cropping either width or height as needed to fill the space available.
        /// </summary>
        /// <param name="imageToResize"></param>
        /// <param name="newSize"></param>
        /// <param name="letterbox"></param>
        /// <returns></returns>
        public static Image ResizeImage(Image imageToResize, Size newSize, bool letterbox)
        {
            var sourceWidth = imageToResize.Width;
            var sourceHeight = imageToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)newSize.Width / (float)sourceWidth);
            nPercentH = ((float)newSize.Height / (float)sourceHeight);

            if (!letterbox) // fill smallest dimension - no crop
            {
                nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;
            }
            else // fill largest dimension - crop
            {
                nPercent = nPercentH > nPercentW ? nPercentH : nPercentW;
            }

            var destWidth = (int)(sourceWidth * nPercent);
            var destHeight = (int)(sourceHeight * nPercent);

            var b = new Bitmap(destWidth, destHeight);
            var g = System.Drawing.Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var startWidth = 0;
            var startHeight = 0;
            if (!letterbox)
            {
                startWidth = (newSize.Width - destWidth) > 0 ? (newSize.Width - destWidth) / 2 : 0;
                startHeight = (newSize.Height - destHeight) > 0 ? (newSize.Height - destHeight) / 2 : 0;
            }

            g.DrawImage(imageToResize, startWidth, startHeight, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }

        public static Image ResizeImageConstrainProportionsByHeight(Image imgToResize, int height)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = ((float)height / (float)sourceHeight);

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            var b = new Bitmap(destWidth, destHeight);
            var g = System.Drawing.Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }




        public static Image AddTextToImage(Image image, PointF centerPoint, Size boundingArea, string imageText, FontFamily fontFamily, Color fontColor)
        {
            using (var grfx = System.Drawing.Graphics.FromImage(image))
            {
                grfx.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var verticalPoint = centerPoint.Y - ((float)boundingArea.Height / 2);
                var stringSize = RenderString(grfx, imageText, new PointF(centerPoint.X, verticalPoint), boundingArea, fontFamily, fontColor);

                //increase height if possible 
                if (stringSize.Height < boundingArea.Height)
                {
                    boundingArea.Height = (int)(boundingArea.Height + ((boundingArea.Height - stringSize.Height) / 2));
                }
            }

            return image;
        }

        /// <summary>
        /// Places a smaller image over another background image at a particular coordinate.
        /// </summary>
        /// <param name="backgroundImage"></param>
        /// <param name="foregroundImage"></param>
        /// <param name="foregroundImageX"></param>
        /// <param name="foregroundImageY"></param>
        /// <returns></returns>
        public static Image LayerImages(Image backgroundImage, Image foregroundImage, int foregroundImageX, int foregroundImageY)
        {
            using (var grfx = System.Drawing.Graphics.FromImage(backgroundImage))
            {
                grfx.TextRenderingHint = TextRenderingHint.AntiAlias;
                grfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

                grfx.DrawImage(foregroundImage, foregroundImageX, foregroundImageY, foregroundImage.Width, foregroundImage.Height);
            }

            return backgroundImage;
        }


        /// <summary>
        /// Renders a string of text filling a given size at centered at a given point in the image.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="stringToRender"></param>
        /// <param name="centerPoint"></param>
        /// <param name="maxSize"></param>
        /// <param name="fontFamily"></param>
        /// <param name="fontColor"></param>
        /// <param name="drawBoundingBox">Draws a bounding box to help identify area text will fill.</param>
        /// <returns></returns>
        private static SizeF RenderString(System.Drawing.Graphics graphics, string stringToRender, PointF centerPoint, Size maxSize, FontFamily fontFamily, Color fontColor, bool drawBoundingBox = false)
        {
            const FontStyle fontStyle = FontStyle.Regular;
            var fontBrush = new SolidBrush(fontColor);

            var newFontSize = AppropriateFont(graphics, maxSize, stringToRender, new Font(fontFamily, 10, fontStyle, GraphicsUnit.Pixel));
            var sizedFont = new Font(fontFamily, newFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            var fontSpace = graphics.MeasureString(stringToRender, sizedFont);

            fontSpace = new SizeF(fontSpace.Width, (float)((decimal)fontSpace.Height * (decimal).75));

            var newTextPoint = new PointF(centerPoint.X, centerPoint.Y - (maxSize.Height - fontSpace.Height) / 2);

            if (drawBoundingBox)
            {
                var newCenterPoint = new PointF(centerPoint.X - (float)maxSize.Width/2, centerPoint.Y - (float)maxSize.Height/2);
                graphics.DrawRectangle(new Pen(Color.Yellow), newCenterPoint.X, newCenterPoint.Y, maxSize.Width, maxSize.Height);                
            }

            var stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(stringToRender, sizedFont, fontBrush, newTextPoint, stringFormat);

            return fontSpace;
        }

        private static float AppropriateFont(System.Drawing.Graphics g, Size layoutSize, string s, Font f)
        {
            //http://www.switchonthecode.com/tutorials/csharp-tutorial-font-scaling 
            var p = g.MeasureString(s, f);
            var hRatio = layoutSize.Height / p.Height;
            var wRatio = layoutSize.Width / p.Width;
            var ratio = Math.Min(hRatio, wRatio);
            var fontSize = f.Size * ratio;
            return fontSize;
        }

        public static byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            var ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }

        public static Image ImageFromString(string imageData)
        {
            var byteArray = Convert.FromBase64String(imageData);
            var ms = new MemoryStream(byteArray);
            var image = Image.FromStream(ms);
            return image;
        }

        public static Image LoadImage(string imageUrl)
        {
            return Image.FromStream(WebRequest.Create(imageUrl).GetResponse().GetResponseStream());
        }

    }
}
