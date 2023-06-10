using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;

namespace GIS_FIre
{
    public partial class FireAlarm : Form
    {
        private IFeatureLayer featureLayer;
        private IEnvelope mapExtent;
        private List<int> pointValue = new List<int>();

        /* 虞世宁 6.9 */
        #region 影响范围
        public FireAlarm(AxMapControl mainMapControl, IFeatureLayer buildingLayer)
        {
            InitializeComponent();
            axMapControl1.AddLayer(buildingLayer);
            this.featureLayer = buildingLayer;
            ExportMap("axMapControl_Active_View.jpg", 4000, 3);
            mapExtent = axMapControl1.ActiveView.Extent;
        }
        /// <summary>
        /// 导出活动窗体的图像
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="resolution"></param>
        /// <param name="resample"></param>
        private void ExportMap(string filePath, double resolution, int resample)
        {
            IExport pExport = null;
            pExport = new ExportJPEG() as IExport;
            // 设置导出路径
            pExport.ExportFileName = filePath;
            if (pExport is IOutputRasterSettings)
            {
                IOutputRasterSettings pOutputRasterSettings = pExport as IOutputRasterSettings;
                pOutputRasterSettings.ResampleRatio = resample;
            }
            IPrintAndExport pPrintAndExport = new PrintAndExport();
            pPrintAndExport.Export(axMapControl1.ActiveView, pExport, resolution, false, null);

        }
        /// <summary>
        /// 获取面要素的所有中心点点集
        /// </summary>
        /// <param name="pFeatureLayer">面要素</param>
        /// <returns></returns>
        private IPointArray GetPointGraph(IFeatureLayer pFeatureLayer)
        {
            IPointArray CenterPoint = new PointArray() as IPointArray;
            if (pFeatureLayer.FeatureClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
            { }
            else
                return null;
            //判断图层类型{ 。。。。。。。。。}
            IFeatureClass pFeatureClassPolygon = pFeatureLayer.FeatureClass;
            IFeatureCursor pPolyCursor = pFeatureClassPolygon.Search(null, false);
            IFeature pPolyFeature = pPolyCursor.NextFeature();//开始遍历要素
            int fieldIndex = pPolyFeature.Fields.FindField("risk");

            while (pPolyFeature != null)
            {
                IPolygon pPolygon = pPolyFeature.ShapeCopy as IPolygon;
                IArea pArea = pPolygon as IArea;
                IPoint pPoint = pArea.Centroid;

                // 获取字段的值
                int fieldValue = (int)pPolyFeature.get_Value(fieldIndex);
                pointValue.Add(fieldValue);
                CenterPoint.Add(pPoint);
                pPolyFeature = pPolyCursor.NextFeature();
                //MessageBox.Show(pPoint.X.ToString());}
            }
            return CenterPoint;

        }
        /// <summary>
        /// 将要素坐标转换到图像坐标
        /// </summary>
        /// <param name="point">要素点</param>
        /// <param name="imageWidth">图像宽</param>
        /// <param name="imageHeight">图像高</param>
        /// <returns></returns>
        private PointF ConvertToImageCoordinates(IPoint point, int imageWidth, int imageHeight)
        {
            double xRatio = (point.X - mapExtent.XMin) / mapExtent.Width;
            double yRatio = (point.Y - mapExtent.YMin) / mapExtent.Height;

            float imageX = (float)(xRatio * imageWidth);
            float imageY = (float)(imageHeight - yRatio * imageHeight);

            return new PointF(imageX, imageY);
        }
        /// <summary>
        /// 生成插值后的图像
        /// </summary>
        /// <returns></returns>
        private Bitmap GetRisk()
        {
            int imageWidth = Convert.ToInt32(mapExtent.Width * 0.2);
            int imageHeight = Convert.ToInt32(mapExtent.Height * 0.2);
            
            IPointArray CenterPoint = GetPointGraph(featureLayer);
            double[,]  riskValues = new double[imageWidth, imageHeight];

            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    double riskSum = 0.0; // 风险值之和
                    double weightSum = 0.0; // 权重之和

                    // 遍历每个点数据
                    for (int i = 0; i < CenterPoint.Count; i++)
                    {

                        PointF pointF = ConvertToImageCoordinates(CenterPoint.get_Element(i), imageWidth, imageHeight);

                        double distance = Math.Sqrt(Math.Pow(pointF.X - x, 2) + Math.Pow(pointF.Y - y, 2)); // 计算当前像素与点的距离
                        double weight = 0;
                        //if (distance < 100) 
                        weight = 1.0 / Math.Pow(distance, 2); // 计算权重，这里使用了简单的反距离平方权重

                        riskSum += pointValue[i] * weight; // 加权风险值之和
                        weightSum += weight; // 权重之和
                    }

                    double riskValue = 0;
                    if (weightSum > 0.027)
                        riskValue = riskSum / weightSum; // 计算插值后的风险值
                    riskValues[x, y] = riskValue; // 存储风险值到数组中
                }
            }

            Bitmap riskMap = new Bitmap(imageWidth, imageHeight); // 创建风险图像
            Color[] colors = SetColor();
            // 绘制风险图像
            for (int x1 = 0; x1 < imageWidth; x1++)
            {
                for (int y1 = 0; y1 < imageHeight; y1++)
                {
                    double riskValue = riskValues[x1, y1];

                    // 根据风险值选择颜色
                    Color color = colors[(int)riskValue]; // 根据风险值选择合适的颜色

                    riskMap.SetPixel(x1, y1, color); // 在风险图像中设置像素颜色
                }
            }
            return riskMap;
        }
        /// <summary>
        /// 分级设色
        /// </summary>
        /// <returns></returns>
        private Color[] SetColor()
        {
            Color[] color = new Color[10];
            color[0] = Color.FromArgb(56, 168, 0);
            color[1] = Color.FromArgb(90, 186, 0);
            color[2] = Color.FromArgb(131, 207, 0);
            color[3] = Color.FromArgb(176, 224, 0);
            color[4] = Color.FromArgb(228, 245, 0);
            color[5] = Color.FromArgb(255, 225, 0);
            color[6] = Color.FromArgb(255, 170, 0);
            color[7] = Color.FromArgb(255, 115, 0);
            color[8] = Color.FromArgb(255, 55, 0);
            color[9] = Color.FromArgb(255, 0, 0);
            return color;
        }
        private void btn_show_Click(object sender, EventArgs e)
        {
            Image bottomImage = (Image)new Bitmap("axMapControl_Active_View.jpg");
            Image topImage = (Image)GetRisk();
            // 确定合并后图像的大小为下面的图像的大小
            int mergedWidth = topImage.Width;
            int mergedHeight = topImage.Height;

            // 创建一个新的 Bitmap 对象，大小为合并后的图像大小
            Bitmap mergedImage = new Bitmap(mergedWidth, mergedHeight);

            // 创建一个 Graphics 对象，将其关联到新创建的 Bitmap 对象上
            using (Graphics g = Graphics.FromImage(mergedImage))
            {
                // 绘制下面的图像
                g.DrawImage(bottomImage, new Rectangle(0, 0, mergedWidth, mergedHeight));

                // 设置上面的图像的透明度为 85%
                float alpha = 0.85f;
                System.Drawing.Imaging.ImageAttributes imageAttributes = new System.Drawing.Imaging.ImageAttributes();
                System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                colorMatrix.Matrix33 = alpha;
                imageAttributes.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

                // 绘制上面的图像并应用透明度
                g.DrawImage(topImage, new Rectangle(0, 0, topImage.Width, topImage.Height), 0, 0, topImage.Width, topImage.Height, GraphicsUnit.Pixel, imageAttributes);
            }

            // 将合并后的图像赋值给 PictureBox 的 Image 属性
            pictureBox1.Image = mergedImage;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }
        #endregion
    }
}
