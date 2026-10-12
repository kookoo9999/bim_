using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace Common
{
    public class ImageCutter
    {
        private List<BitmapSource> refImage = new List<BitmapSource>();
        private List<BitmapSource> defImage = new List<BitmapSource>();
        private int columnCount = 5;
        private int cutWidth = 96;
        private int cutHeight = 96;
        private int setCount = 2;

        public List<BitmapSource> RefImage
        {
            get
            {
                return refImage;
            }
            set
            {
                refImage = value;
            }
        }
        public List<BitmapSource> DefImage
        {
            get
            {
                return defImage;
            }
            set
            { 
                defImage = value;
            }
        }
        
        public void Clear()
        {
            #region BitmapSource List Clear
            if (refImage != null)
            {
                refImage.Clear();
            }

            if (defImage != null)
            {
                defImage.Clear();
            }
            #endregion
        }

        public bool CuttingImage(BitmapSource abmpMergedImage, List<int> aSelectedIndexList, int anSelectPieceImage = 3)
        {
            if (abmpMergedImage == null)
                return false;
            if (abmpMergedImage.PixelWidth != cutWidth * columnCount)
                return false;
            if ((abmpMergedImage.PixelHeight % (cutHeight * setCount)) != 0)
                return false;
            int nRowCount = abmpMergedImage.PixelHeight / (cutHeight * setCount);
            if (aSelectedIndexList.Count <= 0)
                return false;

            int nMaxIndex = aSelectedIndexList.Max();
            int nMinIndex = aSelectedIndexList.Min();
            if ((nMinIndex < 0) || (nMaxIndex >= columnCount * nRowCount))
                return false;

            Byte[] arrMergedImage = new Byte[abmpMergedImage.PixelWidth * abmpMergedImage.PixelHeight];
            Byte[] arrPieceImage = new Byte[cutWidth * cutHeight];
            abmpMergedImage.CopyPixels(arrMergedImage, abmpMergedImage.PixelWidth, 0);

            int nCurrRow, nCurrCol;
            BitmapSource bmpPieceImage;
            foreach (int nImageIndex in aSelectedIndexList)
            {
                nCurrRow = nImageIndex / columnCount;
                nCurrCol = nImageIndex % columnCount;

                for (int i = 0; i < setCount; i++)
                {
                    if ((anSelectPieceImage & ((int)Math.Pow(2, i))) == 0)
                        continue;

                    for (int y = 0; y < cutHeight; y++)
                    {
                        for (int x = 0; x < cutWidth; x++)
                        {
                            arrPieceImage[y * cutWidth + x] = arrMergedImage[(nCurrRow * (cutHeight * 2) + cutHeight * i + y) * abmpMergedImage.PixelWidth + (nCurrCol * cutWidth + x)];
                        }
                    }
                    bmpPieceImage = BitmapSource.Create(cutWidth, cutHeight, 96, 96, PixelFormats.Indexed8, BitmapPalettes.Gray256, arrPieceImage, cutWidth) as BitmapSource;
                    switch (i)
                    {
                        case 0:
                            refImage.Add(bmpPieceImage);
                            break;
                        case 1:
                            defImage.Add(bmpPieceImage);
                            break;
                    }
                }
            }
            return true;
        }

        public bool CuttingImage(BitmapSource abmpMergedImage, int anCutImageIndex)
        {
            List<int> selectedIndexList = new List<int>();
            selectedIndexList.Add(anCutImageIndex);

            return CuttingImage(abmpMergedImage, selectedIndexList);
        }

        public bool CuttingImage(BitmapSource abmpMergedImage, int anCutStartPos, int anCutEndPos)
        {
            List<int> selectedIndexList = new List<int>();
            for (int i = anCutStartPos; i < anCutEndPos;i++ )
                selectedIndexList.Add(i);

            return CuttingImage(abmpMergedImage, selectedIndexList);
        }

        public bool CuttingImage(string astrCuttingImageUri, int anCutStartPos, int anCutEndPos)
        {
            BitmapSource bmpMergedImage = Common.BitmapImageLoader.LoadBitmapAsIndexed8(new Uri(astrCuttingImageUri), BitmapCacheOption.OnLoad, BitmapCreateOptions.None) as BitmapSource;

            return CuttingImage(bmpMergedImage, anCutStartPos, anCutEndPos);
        }
        
        public BitmapSource CuttingImage(BitmapSource CuttingImage, int CutImageIndex, string IsRefImage)
        {
            Byte[] imageStream = new Byte[CuttingImage.PixelWidth * CuttingImage.PixelHeight];
            CuttingImage.CopyPixels(imageStream, CuttingImage.PixelWidth, 0);

            int TargetPixels = 0;
            int RowCount = CuttingImage.PixelHeight / cutHeight;

            Byte[] cutImage = new Byte[cutWidth * cutHeight];
            int index = 0;
            int LimitRow = 0;
            int LimitCol = 0;

            index = (CutImageIndex + 1) * 2;
            LimitRow = ((index - 1) / (columnCount * 2)) * 2;
            LimitCol = (index % (columnCount * 2)) / 2 - 1;
            if (LimitCol < 0)
            {
                LimitCol = columnCount - 1;
            }

            int Row = LimitRow;
            int Col = LimitCol;

            int toggle = 0;

            if (IsRefImage == "DEF")
            {
                toggle = 1;
            }

            for (int i = 0; i < cutHeight; i++)          //Target Image Set
            {
                for (int j = (i * CuttingImage.PixelWidth) + (Col * cutWidth) + ((Row + toggle) * cutHeight * CuttingImage.PixelWidth); j < (i * CuttingImage.PixelWidth + cutWidth) + (Col * cutWidth) + ((Row + toggle) * cutHeight * CuttingImage.PixelWidth); j++)
                {
                    cutImage[TargetPixels++] = imageStream[j];
                }
            }

            BitmapSource refImage = BitmapSource.Create(cutWidth, cutHeight, 96, 96, PixelFormats.Indexed8, BitmapPalettes.Gray256, cutImage, cutWidth) as BitmapSource;
            return refImage;          
        }
    }
}
