using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.DataSourcesRaster;

namespace GIS_FIre
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 选中图层的标识
        /// </summary>
        private ILayer pGlobeLayer = null;
        /// <summary>
        /// 用于影响范围的OnmouseDown判断
        /// -1表示不进行绘制
        /// </summary>
        private double allowEffectHighlight = -1;
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            axTOCControl1.SetBuddyControl(axMapControlMain);
        }

        private string mapDocumentName = string.Empty;

        /* 查昊天 5.26 */
        #region 功能栏-文件
        private void 打开OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ESRI.ArcGIS.SystemUI.ICommand pCommand;
            //pCommand = new ESRI.ArcGIS.Controls.ControlsOpenDocCommand();
            //pCommand.OnCreate(axMapControlMain.Object);
            //pCommand.OnClick();

            System.Windows.Forms.OpenFileDialog openFileDialog2;
            openFileDialog2 = new OpenFileDialog();
            openFileDialog2.Title = "打开mxd文件";
            openFileDialog2.Filter = "Map Documents (*.mxd)|*.mxd";
            openFileDialog2.ShowDialog();
            string sFilePath = openFileDialog2.FileName;

            if (axMapControlMain.CheckMxFile(sFilePath))
            {
                axMapControlMain.MousePointer =
                esriControlsMousePointer.esriPointerHourglass;
                axMapControlMain.LoadMxFile(sFilePath, 0, Type.Missing);
                axMapControlMain.MousePointer = esriControlsMousePointer.esriPointerDefault;
                //axMapControlSmall.LoadMxFile(sFilePath, 0, Type.Missing);
            }
            else
            {
                MessageBox.Show(sFilePath + " 不是mxd文件");
                return;
            }
        }

        private void 添加数据TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ESRI.ArcGIS.SystemUI.ICommand pCommand;
            pCommand = new ESRI.ArcGIS.Controls.ControlsAddDataCommand();
            pCommand.OnCreate(axMapControlMain.Object);
            pCommand.OnClick();
        }

        private void 保存SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ESRI.ArcGIS.SystemUI.ICommand pCommand;
            //pCommand = new ESRI.ArcGIS.Controls.ControlsEditingSaveCommand();
            //pCommand.OnCreate(axMapControlMain.Object);
            //pCommand.OnClick();

            if (axMapControlMain.CheckMxFile(mapDocumentName))
            {

                IMapDocument mapDoc = new MapDocumentClass();
                mapDoc.Open(mapDocumentName, string.Empty);


                if (mapDoc.get_IsReadOnly(mapDocumentName))
                {
                    MessageBox.Show("当前文档为只读型!");
                    mapDoc.Close();
                    return;
                }
                mapDoc.ReplaceContents((IMxdContents)axMapControlMain.Map);

                mapDoc.Save(mapDoc.UsesRelativePaths, false);

                mapDoc.Close();
            }
        }

        private void 另存为ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ESRI.ArcGIS.SystemUI.ICommand pCommand;
            pCommand = new ESRI.ArcGIS.Controls.ControlsSaveAsDocCommand();
            pCommand.OnCreate(axMapControlMain.Object);
            pCommand.OnClick();
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        /* 虞世宁 5.27 */
        #region 图层右键菜单
        private void axTOCControl1_OnMouseDown(object sender, ESRI.ArcGIS.Controls.ITOCControlEvents_OnMouseDownEvent e)
        {
            if (axMapControlMain.LayerCount > 0)
            {
                esriTOCControlItem pItem = new esriTOCControlItem();
                pGlobeLayer = new FeatureLayer();
                IBasicMap pBasicMap = (IBasicMap)new Map();
                object pOther = new object();
                object pIndex = new object();
                //获取点击的位置
                axTOCControl1.HitTest(e.x, e.y, ref pItem, ref pBasicMap, ref pGlobeLayer, ref pOther, ref pIndex);
                //点击的是Layer的话，则弹出上下文菜单
                if (e.button == 2 && pItem == esriTOCControlItem.esriTOCControlItemLayer)
                {
                    contextMenuStrip1.Show(axTOCControl1, e.x, e.y);
                }
            }
        }

        private void 移除MenuItem_Click(object sender, EventArgs e)
        {
            if (pGlobeLayer != null)
            {
                IMap pMap = axMapControlMain.Map;
                pMap.DeleteLayer(pGlobeLayer);
                pGlobeLayer = null;
            }
        }

        private void 缩放至图层MenuItem_Click(object sender, EventArgs e)
        {
            if (pGlobeLayer != null)
            {
                (axMapControlMain.Map as IActiveView).Extent = pGlobeLayer.AreaOfInterest;
                (axMapControlMain.Map as IActiveView).PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            }
        }

        private void 打开属性表MenuItem_Click(object sender, EventArgs e)
        {
            AttributeTable_Load();
            gbx_attribution.Text = "Attribution—" + pGlobeLayer.Name;
            tabControl1.SelectedIndex = 1;
            tabControl1.Enabled = true;
        }
        /// <summary>
        /// 创建属性表并且显示
        /// </summary>
        
        private void AttributeTable_Load()
        {
            IFeatureLayer pFeatureLayer = pGlobeLayer as IFeatureLayer;
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            DataTable dt = new DataTable();
            if (pFeatureClass != null)
            {
                DataColumn dc;
                for (int i = 0; i < pFeatureClass.Fields.FieldCount; i++)
                {
                    dc = new DataColumn(pFeatureClass.Fields.get_Field(i).Name);
                    dt.Columns.Add(dc);//获取所有列的属性值
                }
                IFeatureCursor pFeatureCursor = pFeatureClass.Search(null, false);
                IFeature pFeature = pFeatureCursor.NextFeature();
                DataRow dr;
                while (pFeature != null)
                {
                    dr = dt.NewRow();
                    for (int j = 0; j < pFeatureClass.Fields.FieldCount; j++)
                    {
                        //判断feature的形状
                        if (pFeature.Fields.get_Field(j).Name == "Shape")
                        {
                            if (pFeature.Shape.GeometryType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
                            {
                                dr[j] = "Point";
                            }
                            if (pFeature.Shape.GeometryType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline)
                            {
                                dr[j] = "PolyLine";
                            }
                            if (pFeature.Shape.GeometryType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
                            {
                                dr[j] = "Polygon";
                            }
                        }
                        else
                        {
                            dr[j] = pFeature.get_Value(j).ToString();//增加行
                        }
                    }
                    dt.Rows.Add(dr);
                    pFeature = pFeatureCursor.NextFeature();
                }
                dgv_attribution.DataSource = dt;
            }
        }

        private void 上一层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExchangeLayers(0);
        }
        /// <summary>
        /// 图层上移或者下移
        /// </summary>
        /// <param name="UD">上移为0，下移为其他</param>
        
        public void ExchangeLayers(int UD)
        {
            IMap pMap = axMapControlMain.Map;
            ILayer pTempLayer;
            int index = -1;
            for (int i = 0; i < pMap.LayerCount; i++)    //寻找当前图层的序号
            {
                pTempLayer = pMap.get_Layer(i);
                if (pTempLayer == pGlobeLayer)
                {
                    index = i;
                    break;
                }
            }
            if (UD == 0)  //向上移
            {
                if (index > 0)
                    pMap.MoveLayer(pGlobeLayer, index - 1);
            }
            else    //向下移
            {
                if (index < pMap.LayerCount - 1)
                    pMap.MoveLayer(pGlobeLayer, index + 1);
            }
        }

        private void 下一层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExchangeLayers(1);
        }
        #endregion

        /* 查昊天 6.5 */
        #region 鹰眼地图
        private void axMapControlMain_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            //mapDocumentName = axMapControlMain.DocumentFilename;
            //IEagOpt pEagOpt = new GeoMapAO();
            //pEagOpt.AxMapControl1 = axMapControlMain;
            ////pEagOpt.AxMapControl2 = axMapControl2;
            //pEagOpt.NewGeomap();
            //copyToPageLayout();

            IMap pmap = axMapControlMain.Map;
            axMapControlSmall.Map.ClearLayers();
            int i;
            for (i = 0; i < pmap.LayerCount; i++)
            {
                IObjectCopy objectcopy = new ObjectCopyClass();
                object toCopyLayer = axMapControlMain.get_Layer(i);
                object copiedLayer = objectcopy.Copy(toCopyLayer);

                axMapControlSmall.Map.AddLayer(copiedLayer as ILayer);
            }
            axMapControlSmall.Extent = axMapControlMain.FullExtent;
            axMapControlSmall.Refresh();
        }
        private void axMapControlMain_OnExtentUpdated(object sender, IMapControlEvents2_OnExtentUpdatedEvent e)
        {
            IMap pmap = axMapControlMain.Map;
            axMapControlSmall.Map.ClearLayers();
            int i;
            for (i = 0; i < pmap.LayerCount; i++)
            {
                IObjectCopy objectcopy = new ObjectCopyClass();
                object toCopyLayer = axMapControlMain.get_Layer(i);
                object copiedLayer = objectcopy.Copy(toCopyLayer);

                axMapControlSmall.Map.AddLayer(copiedLayer as ILayer);
            }
            axMapControlSmall.Extent = axMapControlMain.FullExtent;
            axMapControlSmall.Refresh();

            IEnvelope pEnv;
            pEnv = e.newEnvelope as IEnvelope;
            IGraphicsContainer graphicscontainer;
            IActiveView activewer;
            graphicscontainer = axMapControlSmall.Map as IGraphicsContainer;
            activewer = graphicscontainer as IActiveView;
            graphicscontainer.DeleteAllElements();
            IElement plement;
            plement = new RectangleElementClass();
            plement.Geometry = pEnv;

            IRgbColor rgbcol = new RgbColorClass();
            rgbcol.RGB = 255;
            rgbcol.Transparency = 255;
            ILineSymbol poutline = new SimpleLineSymbolClass();
            poutline.Width = 1;
            poutline.Color = rgbcol;
            IRgbColor pcolor = new RgbColorClass();
            pcolor.RGB = 255;
            pcolor.Transparency = 0;
            IFillSymbol fillsym = new SimpleFillSymbolClass();
            fillsym.Color = pcolor;
            fillsym.Outline = poutline;

            IFillShapeElement pfillshapeelement;
            pfillshapeelement = plement as IFillShapeElement;
            pfillshapeelement.Symbol = fillsym;

            plement = pfillshapeelement as IElement;
            graphicscontainer.AddElement(plement, 0);
            activewer.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }
        public static string pMouseOperator = null;
        private void button1_Click(object sender, EventArgs e)
        {
            //BufferSettings bufferSettings = new BufferSettings(axMapControlMain.Map, player, axMapControl1);
            //bufferSettings.Show();

        }
        #endregion

        /* 虞世宁 6.9 */
        #region IDW与影响范围
        /// <summary>
        /// 反距离加权平均插值
        /// </summary>
        private void IDW_Interpolation()
        {
            IFeatureLayer featureLayer = pGlobeLayer as IFeatureLayer;
            IFeatureClass featureClass = featureLayer.FeatureClass;
            IGeoDataset geo = featureClass as IGeoDataset;
            ESRI.ArcGIS.GeoAnalyst.IFeatureClassDescriptor feades = new ESRI.ArcGIS.GeoAnalyst.FeatureClassDescriptorClass();
            feades.Create(featureClass, null, "risk");

            object obj = null;
            object extend = geo.Extent;
            object o = null;
            ESRI.ArcGIS.GeoAnalyst.IRasterRadius rasterrad = new ESRI.ArcGIS.GeoAnalyst.RasterRadiusClass();
            rasterrad.SetVariable(12, ref obj);
            object dCell = 1;//可以根据不同的点图层进行设置

            ESRI.ArcGIS.GeoAnalyst.IInterpolationOp3 interpla = new ESRI.ArcGIS.GeoAnalyst.RasterInterpolationOpClass();
            ESRI.ArcGIS.GeoAnalyst.IRasterAnalysisEnvironment rasanaenv = interpla as ESRI.ArcGIS.GeoAnalyst.IRasterAnalysisEnvironment;
            rasanaenv.SetCellSize(ESRI.ArcGIS.GeoAnalyst.esriRasterEnvSettingEnum.esriRasterEnvValue, ref dCell);
            rasanaenv.SetExtent(ESRI.ArcGIS.GeoAnalyst.esriRasterEnvSettingEnum.esriRasterEnvValue, ref extend, ref o);
            IGeoDataset pGeoDataSet;
            try
            {
                pGeoDataSet = interpla.IDW((IGeoDataset)feades, 2, rasterrad, ref obj);
            }
            catch (System.Runtime.InteropServices.COMException) { MessageBox.Show("请选择点要素"); return; }
            IRaster pOutRsater1 = (IRaster)pGeoDataSet;
            IRasterLayer rasterLayer = new RasterLayerClass();
            rasterLayer.CreateFromRaster(pOutRsater1);
            rasterLayer.Name = "riskMap";
            ClassifyRenderRaster(rasterLayer, 10);
            this.axMapControlMain.AddLayer(rasterLayer, 0);
            this.axMapControlMain.ActiveView.Refresh();
        }
        /// <summary>
        /// 分层渲染
        /// </summary>
        /// <param name="pRasterLayer"></param>
        /// <param name="ClassifyNum">最大10</param>
        public void ClassifyRenderRaster(IRasterLayer pRasterLayer, int ClassifyNum)
        {
            IRasterClassifyColorRampRenderer pRClassRend = new RasterClassifyColorRampRenderer() as IRasterClassifyColorRampRenderer;
            IRaster pRaster = pRasterLayer.Raster;
            IRasterBandCollection pRBandCol = pRaster as IRasterBandCollection;
            IRasterBand pRBand = pRBandCol.Item(0);
            if (pRBand.Histogram == null)
                pRBand.ComputeStatsAndHist();
            pRClassRend.ClassCount = ClassifyNum;// NaturalBreaks
            int[] breaks = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            IColor[] colors = GetColor();
            IFillSymbol fillSymbol = new SimpleFillSymbol() as IFillSymbol;
            for (int i = 0; i < pRClassRend.ClassCount; i++)
            {
                fillSymbol.Color = colors[i];
                pRClassRend.set_Symbol(i, fillSymbol as ISymbol);
                pRClassRend.set_Break(i, breaks[i]); ;
            }
            pRasterLayer.Renderer = pRClassRend as IRasterRenderer; //this
        }
        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <returns></returns>
        private IColor[] GetColor()
        {
            IColor[] colors = new IColor[10];
            colors[0] = new RgbColorClass() { Red = 56, Green = 168, Blue = 0 };
            colors[1] = new RgbColorClass() { Red = 90, Green = 186, Blue = 0 };
            colors[2] = new RgbColorClass() { Red = 131, Green = 207, Blue = 0 };
            colors[3] = new RgbColorClass() { Red = 176, Green = 224, Blue = 0 };
            colors[4] = new RgbColorClass() { Red = 228, Green = 245, Blue = 0 };
            colors[5] = new RgbColorClass() { Red = 255, Green = 225, Blue = 0 };
            colors[6] = new RgbColorClass() { Red = 255, Green = 170, Blue = 0 };
            colors[7] = new RgbColorClass() { Red = 255, Green = 115, Blue = 0 };
            colors[8] = new RgbColorClass() { Red = 255, Green = 55, Blue = 0 };
            colors[9] = new RgbColorClass() { Red = 255, Green = 10, Blue = 0 };
            return colors;
        }

        private void iDWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IDW_Interpolation();
        }

        private void 自写IDW不推荐ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IFeatureLayer featureLayer = pGlobeLayer as IFeatureLayer;
            try
            {
                FireAlarm fireAlarm = new FireAlarm(axMapControlMain, featureLayer);
                fireAlarm.Show();
            }
            catch (NullReferenceException) { MessageBox.Show("请单击选择需要分析的图层！"); }
        }

        private void 绘制影响范围ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string input = Microsoft.VisualBasic.Interaction.InputBox("矩形范围(m),double类型(>0)", "请输入影响范围", "100", -1, -1);
            double range = 0;

            try
            {
                range = double.Parse(input);
            }
            catch (FormatException) { MessageBox.Show("请正确输入范围"); return; }
            if (range < 0)
            { MessageBox.Show("请正确输入范围"); return; }
            allowEffectHighlight = range;

        }
        private void GetNearFeature(IMapControlEvents2_OnMouseDownEvent e, AxMapControl axMapControl1)
        {
            IPoint clickedPoint = new PointClass();
            clickedPoint.PutCoords(e.mapX, e.mapY);
            IFeatureLayer pFeatureLayer = axMapControl1.get_Layer(0) as IFeatureLayer;
            // 创建缓冲区
            ITopologicalOperator topologicalOperator = clickedPoint as ITopologicalOperator;
            IGeometry bufferGeometry = topologicalOperator.Buffer(allowEffectHighlight) as IGeometry;

            // 创建空间过滤器
            ISpatialFilter spatialFilter = new SpatialFilterClass();
            spatialFilter.Geometry = bufferGeometry;
            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains; // 在缓冲区内的要素

            IFeatureSelection pFSelection = pFeatureLayer as IFeatureSelection;
            if (pFSelection != null)
            {
                //根据空间过滤关系选择要素
                pFSelection.SelectFeatures(spatialFilter, esriSelectionResultEnum.esriSelectionResultNew, false);
                //刷新视图
                axMapControl1.ActiveView.Refresh();
            }
        }

        private void axMapControlMain_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (e.button == 1)
            {
                if (allowEffectHighlight != -1)  //已开启影响范围绘制功能
                    GetNearFeature(e, axMapControlMain);
            }
            else if (e.button == 2)
            {
                CancalHighlight();
            }

        }
        /// <summary>
        /// 取消高亮
        /// </summary>
        private void CancalHighlight()
        {
            if (pGlobeLayer != null)
            {
                IFeatureSelection pFSelection = pGlobeLayer as IFeatureSelection;
                pFSelection.Clear();
                axMapControlMain.ActiveView.Refresh();
            }
        }

        private void 取消绘制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            allowEffectHighlight = -1;
        }

        private void 可燃性分析ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("请输入risk值[0-9]", "请输入风险值", "0", -1, -1);
            int range;

            try
            {
                range = int.Parse(input);
            }
            catch (FormatException) { MessageBox.Show("请正确输入风险值"); return; }
            if (range < 0 && range > 9)
            { MessageBox.Show("请正确输入风险值"); return; }

            if (pGlobeLayer != null)
            {
                IFeatureSelection pFSelection = pGlobeLayer as IFeatureSelection;
                IQueryFilter pQuery = new QueryFilterClass();   //添加筛选器
                string whereClause = "risk =" + range;
                pQuery.WhereClause = whereClause;  //添加条件
                pFSelection.SelectFeatures(pQuery, esriSelectionResultEnum.esriSelectionResultNew, false);
                axMapControlMain.ActiveView.Refresh(); //刷新
            }
            else
            {
                MessageBox.Show("请选择图层");
            }
        }
        #endregion
    }


}
