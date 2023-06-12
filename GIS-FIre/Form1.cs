using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using ESRI.ArcGIS;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using System.Data.OleDb;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.NetworkAnalysis;
using ESRI.ArcGIS.NetworkAnalyst;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;

namespace GIS_FIre
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            axTOCControl1.SetBuddyControl(axMapControlMain);
            IPolygon pAreaPolygon = new PolygonClass();
            pAreaPointCol = (IPointCollection)pAreaPolygon;
        }

        private IActiveView m_ipActiveView;
        public event EventHandler SendMsgEvent;//定义事件
        public string[] CoorPoint = null;//存储路径分析坐标位置
        public static Form1 mainForm;
        public routeForm routeForm;
        private ILayer pGlobeLayer = null;
        private double allowEffectHighlight = -1;//用于影响范围的OnmouseDown判断,-1表示不进行绘制

        # region 测量变量
        private FormMeasureResult frmMeasureResult = null;
        private string xx, yy;                               //暂时性记录坐标信息
        private INewLineFeedback pNewLineFeedback;           //追踪线对象
        private INewPolygonFeedback pNewPolygonFeedback;     //追踪面对象
        private IPoint pPointPt = null;                      //鼠标点击点
        private double dSegmentLength = 0;                   //片段距离
        private double dToltalLength = 0;                    //量测总长度
        private string sMapUnits = "未知单位";               //地图单位变量
        private IPoint pMovePt = null;                       //鼠标移动时的当前点
        private object missing = Type.Missing;
        private IPointCollection pAreaPointCol;
        //private IPointCollection pAreaPointCol = new MultipointClass();  //面积量算时画的点进行存储
        #endregion

        private string mapDocumentName = string.Empty;

        /* 查昊天 5.26 */
        #region 功能栏
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
            //BufferSettings bufferSettings = new BufferSettings(axMapControlMain.Map, player, axMapControlMain);
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
        private void GetNearFeature(IMapControlEvents2_OnMouseDownEvent e, AxMapControl axMapControlMain)
        {
            IPoint clickedPoint = new PointClass();
            clickedPoint.PutCoords(e.mapX, e.mapY);
            IFeatureLayer pFeatureLayer = axMapControlMain.get_Layer(0) as IFeatureLayer;
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
                axMapControlMain.ActiveView.Refresh();
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

        private void 可燃性分析ToolStripMenuItem_Click_1(object sender, EventArgs e)
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
        

        /* 曹之怿 6.10 */
        #region 路径规划
        private void 逃生路线规划ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CoorPoint = null;
            CoorPoint = new string[50];
            routeForm = new routeForm(mainForm);
            SendMsgEvent += routeForm.MainFormTxtChanged;
            routeForm.Show();
            //network1 f = new network1();
            //f.ShowDialog();
        }

        // <summary>
        // 获取距离鼠标点击最近的点
        // </summary>
        // <param name="featureClass"></param>
        // <param name="PointCollection"></param>
        public void CreateFeature(IFeatureClass featureClass, IPointCollection PointCollection)
        {
            //是否为点图层
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint)
            {
                return;
            }
            //创建点要素
            for (int i = 0; i < PointCollection.PointCount; i++)
            {
                IFeature feature = featureClass.CreateFeature();
                feature.Shape = PointCollection.get_Point(i);
                IRowSubtypes rowSubtypes = (IRowSubtypes)feature;
                feature.Store();
            }

        }

        /// <summary>
        /// 传递主窗体axMapControlMain控件
        public ESRI.ArcGIS.Controls.AxMapControl getMainAxMapControl()
        {
            return axMapControlMain;
        }

        /// <summary>
        /// 对3D控件pan功能鼠标状态的记录，完成3D与2D的联动平移，获取路径分析起点、终点位置坐标


        private void 打开网络数据集ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = Application.StartupPath; ;
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;

            string folderPath = folderBrowserDialog1.SelectedPath;

            //打开工作区间
            global.pFWorkspace = OpenWorkspace(folderPath) as IFeatureWorkspace;
            //打开网络数据集
            global.networkDataset = OpenNetworkDataset(global.pFWorkspace as IWorkspace, "同济新村道路_ND", "同济新村道路");
            //创建网络分析上下文，建立一种解决关系
            global.m_NAContext = CreateSolverContext(global.networkDataset);

            //打开停靠点数据集
            global.inputFClass = global.pFWorkspace.OpenFeatureClass("stops");

            //TEST_ND_JUNCTIONS图层
            IFeatureLayer vertex = new FeatureLayerClass();
            vertex.FeatureClass = global.pFWorkspace.OpenFeatureClass("同济新村道路_ND_Junctions");
            vertex.Name = vertex.FeatureClass.AliasName;
            axMapControlMain.AddLayer(vertex, 0);
            //street图层
            IFeatureLayer road;
            road = new FeatureLayerClass();
            road.FeatureClass = global.pFWorkspace.OpenFeatureClass("同济新村道路");
            road.Name = road.FeatureClass.AliasName;
            axMapControlMain.AddLayer(road, 0);

            //为networkdataset生成一个图层，并将该图层添加到axmapcontrol中
            ILayer pLayer;//网络图层
            INetworkLayer pNetworkLayer;
            pNetworkLayer = new NetworkLayerClass();
            pNetworkLayer.NetworkDataset = global.networkDataset;
            pLayer = pNetworkLayer as ILayer;
            pLayer.Name = "Network Dataset";
            axMapControlMain.AddLayer(pLayer, 0);
            //生成一个网络分析图层并添加到axmaptrol1中
            ILayer layer1;
            INALayer nalayer = global.m_NAContext.Solver.CreateLayer(global.m_NAContext);
            layer1 = nalayer as ILayer;
            layer1.Name = global.m_NAContext.Solver.DisplayName;
            axMapControlMain.AddLayer(layer1, 0);
            m_ipActiveView = axMapControlMain.ActiveView;
            global.p2DMap = m_ipActiveView.FocusMap;
            global.PGC = global.p2DMap as IGraphicsContainer;
        }

        /// <summary>
        /// 打开工作空间
        // <param name="strGDBName"></param>
        // <returns></returns>
        public IWorkspace OpenWorkspace(string strGDBName)
        {
            IWorkspaceFactory workspaceFactory;
            workspaceFactory = new FileGDBWorkspaceFactoryClass();
            return workspaceFactory.OpenFromFile(strGDBName, 0);
        }
        /// <summary>
        /// 打开网络数据集
        /// </summary>
        // <param name="networkDatasetWorkspace"></param>
        // <param name="networkDatasetName"></param>
        // <param name="featureDatasetName"></param>
        // <returns></returns>
        public INetworkDataset OpenNetworkDataset(IWorkspace networkDatasetWorkspace, System.String networkDatasetName, System.String featureDatasetName)
        {
            if (networkDatasetWorkspace == null || networkDatasetName == "" || featureDatasetName == null)
            {
                return null;
            }
            IDatasetContainer3 datasetContainer3 = null;

            switch (networkDatasetWorkspace.Type)
            {
                case ESRI.ArcGIS.Geodatabase.esriWorkspaceType.esriFileSystemWorkspace:

                    // Shapefile or SDC network dataset workspace
                    IWorkspaceExtensionManager workspaceExtensionManager = networkDatasetWorkspace as ESRI.ArcGIS.Geodatabase.IWorkspaceExtensionManager; // Dynamic Cast
                    ESRI.ArcGIS.esriSystem.UID networkID = new ESRI.ArcGIS.esriSystem.UIDClass();

                    networkID.Value = "esriGeoDatabase.NetworkDatasetWorkspaceExtension";
                    ESRI.ArcGIS.Geodatabase.IWorkspaceExtension workspaceExtension = workspaceExtensionManager.FindExtension(networkID);
                    datasetContainer3 = workspaceExtension as IDatasetContainer3; // Dynamic Cast
                    break;

                case ESRI.ArcGIS.Geodatabase.esriWorkspaceType.esriLocalDatabaseWorkspace:

                // Personal Geodatabase or File Geodatabase network dataset workspace

                case ESRI.ArcGIS.Geodatabase.esriWorkspaceType.esriRemoteDatabaseWorkspace:

                    // SDE Geodatabase network dataset workspace
                    ESRI.ArcGIS.Geodatabase.IFeatureWorkspace featureWorkspace = networkDatasetWorkspace as ESRI.ArcGIS.Geodatabase.IFeatureWorkspace; // Dynamic Cast
                    global.featureDataset = featureWorkspace.OpenFeatureDataset(featureDatasetName);
                    ESRI.ArcGIS.Geodatabase.IFeatureDatasetExtensionContainer featureDatasetExtensionContainer = global.featureDataset as ESRI.ArcGIS.Geodatabase.IFeatureDatasetExtensionContainer; // Dynamic Cast
                    ESRI.ArcGIS.Geodatabase.IFeatureDatasetExtension featureDatasetExtension = featureDatasetExtensionContainer.FindExtension(ESRI.ArcGIS.Geodatabase.esriDatasetType.esriDTNetworkDataset);
                    datasetContainer3 = featureDatasetExtension as ESRI.ArcGIS.Geodatabase.IDatasetContainer3; // Dynamic Cast
                    break;
            }

            if (datasetContainer3 == null)
                return null;

            ESRI.ArcGIS.Geodatabase.IDataset dataset = datasetContainer3.get_DatasetByName(ESRI.ArcGIS.Geodatabase.esriDatasetType.esriDTNetworkDataset, networkDatasetName);

            return dataset as ESRI.ArcGIS.Geodatabase.INetworkDataset; // Dynamic Cast
        }

        /// <summary>
        /// 创建网络分析上下文
        public INAContext CreateSolverContext(INetworkDataset networkDataset)
        {
            //获取创建网络分析上下文所需的IDENETWORKDATASET类型参数
            IDENetworkDataset deNDS = GetDENetworkDataset(networkDataset);
            INASolver naSolver;
            naSolver = new NARouteSolver();
            INAContextEdit contextEdit = naSolver.CreateContext(deNDS, naSolver.Name) as INAContextEdit;
            contextEdit.Bind(networkDataset, new GPMessagesClass());
            return contextEdit as INAContext;
        }

        /// <summary>
        /// 得到创建网络分析上下文所需的IDENETWORKDATASET类型参数
        public IDENetworkDataset GetDENetworkDataset(INetworkDataset networkDataset)
        {
            //将网络分析数据集QI添加到DATASETCOMPOENT
            IDatasetComponent dstComponent;
            dstComponent = networkDataset as IDatasetComponent;
            //获得数据元素
            return dstComponent.DataElement as IDENetworkDataset;

        }

        private void 消防救援规划ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CoorPoint = null;
            CoorPoint = new string[50];
            routeForm = new routeForm(mainForm);
            SendMsgEvent += routeForm.MainFormTxtChanged;
            routeForm.Show();
        }
        #endregion


        /* 李明哲 6.11 */
        # region 缓冲区建立
        private void 缓冲区建立ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Buffer BF = new Buffer();
            BF.mapControl = this.axMapControlMain;
            BF.ShowDialog();
        }
        #endregion

        /* 查昊天 6.11 */
        # region 测量，地图右键菜单
        private void axMapControlMain_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            global.MapMouseDown = 1;
            global.mapPoint.PutCoords(e.x, e.y);

            //屏幕坐标点转化为地图坐标点
            pPointPt = (axMapControlMain.Map as IActiveView).ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);

            if (global.networkAnalysis == true)
            {


                //this.Cursor = new System.Windows.Forms.Cursor("..\\..\\Resources\\locate.cur");
                IPointCollection points;//输入点集合
                IPoint point;
                points = new MultipointClass();
                point = axMapControlMain.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
                object o = Type.Missing;
                points.AddPoint(point, ref o, ref o);

                CreateFeature(global.inputFClass, points);//或者用鼠标点击最近点

                //把最近的点显示出来
                IElement element;
                ITextElement textelement = new TextElementClass();
                element = textelement as IElement;
                ITextSymbol textSymbol = new TextSymbol();

                textelement.Symbol = textSymbol;
                global.clickedcount++;
                textelement.Text = global.clickedcount.ToString();
                element.Geometry = m_ipActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
                global.PGC.AddElement(element, 0);
                m_ipActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                CoorPoint[global.clickedcount - 1] = e.x.ToString() + " , " + e.y.ToString();
                SendMsgEvent(this, new MyEventArgs() { Text = CoorPoint });
            }
            
            //左击显示坐标
            if (e.button == 1)
            {
                //MessageBox.Show(drawPoint+"      "+drawLine+"    "+drawPolygon);
                IActiveView pActiveView = axMapControlMain.ActiveView;
                IEnvelope pEnvelope = new EnvelopeClass();

                switch (pMouseOperator)
                {                    
                    #region 距离量测
                    case "MeasureLength":
                        //判断追踪对象是否为空，若为空则进行实例化并设置当前鼠标点的起始点
                        if (pNewLineFeedback == null)
                        {
                            //实例化追踪线对象 
                            pNewLineFeedback = new NewLineFeedbackClass();
                            pNewLineFeedback.Display = (axMapControlMain.Map as IActiveView).ScreenDisplay;
                            //设置起点，开始动态线绘制
                            pNewLineFeedback.Start(pPointPt);
                            dToltalLength = 0;
                        }
                        else //如果追踪对象不为空，则添加当前鼠标点
                        {
                            pNewLineFeedback.AddPoint(pPointPt);
                        }

                        if (dSegmentLength != 0)
                        {
                            dToltalLength = dToltalLength + dSegmentLength;
                        }
                        break;
                    #endregion

                    #region 面积量算
                    case "MeasureArea":
                        if (pNewPolygonFeedback == null)
                        {
                            //实例化追踪面对象
                            pNewPolygonFeedback = new NewPolygonFeedback();
                            pNewPolygonFeedback.Display = (axMapControlMain.Map as IActiveView).ScreenDisplay;
                            pAreaPointCol.RemovePoints(0, pAreaPointCol.PointCount);
                            //开始绘制多边形
                            pNewPolygonFeedback.Start(pPointPt);
                            pAreaPointCol.AddPoint(pPointPt, ref missing, ref missing);
                        }
                        else
                        {
                            pNewPolygonFeedback.AddPoint(pPointPt);
                            pAreaPointCol.AddPoint(pPointPt, ref missing, ref missing);
                        }
                        break;
                    #endregion

                    /*
                    #region draw
                    case "drawPoint":
                        IPoint pPoint = new PointClass();
                        pPoint.PutCoords(e.mapX, e.mapY);
                        IMarkerElement pMarkElement = new MarkerElementClass();
                        ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
                        pMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSX;
                        pMarkerSymbol.Color = GetRGB(200, 0, 0);
                        pMarkerSymbol.Angle = 30;
                        pMarkerSymbol.Size = 5;
                        pMarkerSymbol.Outline = true;
                        pMarkerSymbol.OutlineSize = 1;
                        pMarkElement.Symbol = pMarkerSymbol;

                        IElement pElementc = pMarkElement as IElement;
                        pElementc.Geometry = pPoint;
                        pGraphicsContainer = axMapControlMain.Map as IGraphicsContainer;
                        pGraphicsContainer.AddElement(pElementc, 0);
                        pActiveView.Refresh();

                        break;
                    case "drawLine":
                        IGeometry polyline;
                        polyline = axMapControlMain.TrackLine();
                        ILineElement pLineElement;
                        pLineElement = new LineElementClass();
                        IElement pElement;
                        pElement = pLineElement as IElement;
                        pElement.Geometry = polyline;
                        pGraphicsContainer = axMapControlMain.Map as IGraphicsContainer;
                        //pGraphicsContainer = pMap as IGraphicsContainer;
                        pGraphicsContainer.AddElement(pElement, 0);
                        axMapControlMain.ActiveView.Refresh();
                        // pActiveView.Refresh();
                        break;
                    case "drawPolygon":
                        IGeometry Polygon;
                        Polygon = axMapControlMain.TrackPolygon();
                        IPolygonElement PolygonElement;
                        PolygonElement = new PolygonElementClass();
                        IElement pElement1;
                        pElement1 = PolygonElement as IElement;
                        pElement1.Geometry = Polygon;
                        pGraphicsContainer = axMapControlMain.Map as IGraphicsContainer;
                        pGraphicsContainer.AddElement((IElement)PolygonElement, 0);
                        pActiveView.Refresh();
                        break;
                    #endregion

                    #region drawPolygons
                    case "drawPolygons":

                        DoQueryIndex = getFeatureTypeByName(CURRENT_LAYER_NAME);
                        if (DoQueryIndex == 1)
                        {
                            IFeatureLayer pFeatLayer = axMapControlMain.get_Layer(getIndexByName(CURRENT_LAYER_NAME))
                                  as IFeatureLayer;
                            IPoint point = new PointClass();
                            point.PutCoords(e.mapX, e.mapY);

                            ISpatialFilter spatialFilter = new SpatialFilterClass();
                            spatialFilter.Geometry = point;
                            spatialFilter.SpatialRel = ESRI.ArcGIS.Geodatabase.esriSpatialRelEnum.esriSpatialRelIntersects;
                            IFeatureCursor featureCursor = pFeatLayer.Search(spatialFilter, false);

                            IFeature pFeature1 = featureCursor.NextFeature();

                            while (pFeature1 != null)
                            {
                                axMapControlMain.FlashShape(pFeature1.Shape);
                                pFeature1 = featureCursor.NextFeature();
                            }


                        }
                        else if (DoQueryIndex == 2)
                        {
                        }

                        else if (DoQueryIndex == 3)
                        {

                            IPoint point = new PointClass();
                            point.PutCoords(e.mapX, e.mapY);
                            pointcollection.AddPoints(1, ref point);
                            if (pointcollection.PointCount > 2)
                            {
                                DrawPolygon(pointcollection, axMapControlMain);
                            }
                        }
                        break;
                    #endregion

                    case "spaticalQuery":

                        break;

                    #region selectPolygon
                    case "selectPolygon":
                        //实例化一个点
                        IPoint pPoint1 = new PointClass();
                        //以该点作拓扑算子
                        ITopologicalOperator pTopologicalOperator = pPoint1 as ITopologicalOperator;
                        //将点击的位置坐标赋予pPoint
                        pPoint1.PutCoords(e.mapX, e.mapY);
                        //以缓冲半径为0进行缓冲  得到一个点
                        IGeometry pGeometry = pTopologicalOperator.Buffer(0);
                        //以该点进行要素选择（只能选中面状要素，点和线无法选中）
                        axMapControlMain.Map.SelectByShape(pGeometry, null, false);
                        //刷新视图
                        axMapControlMain.Refresh(esriViewDrawPhase.esriViewGeoSelection, null, null);

                        // 获取选择集
                        ISelection pSelection = axMapControlMain.Map.FeatureSelection;
                        // 打开属性标签
                        IEnumFeatureSetup pEnumFeatureSetup = pSelection as IEnumFeatureSetup;
                        pEnumFeatureSetup.AllFields = true;
                        // 获取要素
                        IEnumFeature pEnumFeature = pSelection as IEnumFeature;
                        IFeature pFeature = pEnumFeature.Next();


                        while (pFeature != null)
                        {
                            double area = 0;
                            double mu = 0;
                            if (pFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                            {
                                //计算面积
                                IArea pArea = pFeature.Shape as IArea;
                                area = area + pArea.Area;//得到的面积单位是平方米
                                mu = area * 0.0015;//转换为亩
                            }
                            break;
                        }
                        break;
                    #endregion


                    #region 选择要素
                    case "SelFeature":
                        //MessageBox.Show("gggggggggggg");
                        IEnvelope pEnv = axMapControlMain.TrackRectangle();
                        IGeometry pGeo = pEnv as IGeometry;
                        //矩形框若为空，即为点选时，对点范围进行扩展
                        if (pEnv.IsEmpty == true)
                        {
                            tagRECT r;
                            r.left = e.x - 5;
                            r.top = e.y - 5;
                            r.right = e.x + 5;
                            r.bottom = e.y + 5;
                            pActiveView.ScreenDisplay.DisplayTransformation.TransformRect(pEnv, ref r, 4);
                            pEnv.SpatialReference = pActiveView.FocusMap.SpatialReference;
                        }
                        pGeo = pEnv as IGeometry;
                        axMapControlMain.Map.SelectByShape(pGeo, null, false);
                        axMapControlMain.Refresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                        break;
                    #endregion

                    #region addText
                    case "addText":
                        IGraphicsContainer pGraphicsContainer2 = axMapControlMain.Map as IGraphicsContainer;
                        ITextElement pTextEle = new TextElementClass();
                        InputText inputText = new InputText();
                        pTextEle.Text = InputText.inputText;
                        ITextSymbol pTextSymbol = new TextSymbolClass();
                        pTextSymbol.Size = 30;
                        pTextSymbol.Color = GetRGB(0, 0, 0);
                        pTextEle.Symbol = pTextSymbol;
                        IPoint pPoint2 = new PointClass();
                        pPoint2.PutCoords(e.mapX, e.mapY);
                        IElement pElement2 = pTextEle as IElement;
                        pElement2.Geometry = pPoint2;
                        pGraphicsContainer2.AddElement(pElement2, 0);
                        axMapControlMain.Refresh(esriViewDrawPhase.esriViewGraphics, null, null);
                        pMouseOperator = "";
                        break;
                    #endregion
                    */
                    default:
                        //改变地图控件显示范围为当前拖曳的区域
                        axMapControlMain.Extent = axMapControlMain.TrackRectangle();
                        break;

                }
            }
            else if (e.button == 2)
            {

                contextMenuStrip2.Show(System.Windows.Forms.Control.MousePosition.X,
                    System.Windows.Forms.Control.MousePosition.Y);
                xx = e.mapX.ToString();
                yy = e.mapY.ToString();
            }
        }

        // 测量结果窗口关闭响应事件
        private void frmMeasureResult_frmClosed()
        {
            //清空线对象
            if (pNewLineFeedback != null)
            {
                pNewLineFeedback.Stop();
                pNewLineFeedback = null;
            }
            //清空面对象 
            if (pNewPolygonFeedback != null)
            {
                pNewPolygonFeedback.Stop();
                pNewPolygonFeedback = null;
                pAreaPointCol.RemovePoints(0, pAreaPointCol.PointCount);//清空点集中所有的点

            }
            //清空量算的线、面对象
            axMapControlMain.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewForeground, null, null);
            //结束量算功能
            pMouseOperator = string.Empty;
            axMapControlMain.MousePointer = esriControlsMousePointer.esriPointerDefault;

        }

        private void 距离测量ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axMapControlMain.CurrentTool = null;
            pMouseOperator = "MeasureLength";

            axMapControlMain.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
            if (frmMeasureResult == null || frmMeasureResult.IsDisposed)
            {
                frmMeasureResult = new FormMeasureResult();
                frmMeasureResult.frmClosed += new FormMeasureResult.FormClosedEventHandler(frmMeasureResult_frmClosed);
                frmMeasureResult.Text = "距离测量：";
                frmMeasureResult.Show();
            }
            else
            {
                frmMeasureResult.Activate();
            }
        }
        private void 面积测量ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axMapControlMain.CurrentTool = null;
            pMouseOperator = "MeasureArea";
            axMapControlMain.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
            if (frmMeasureResult == null || frmMeasureResult.IsDisposed)
            {
                frmMeasureResult = new FormMeasureResult();

                frmMeasureResult.frmClosed += new FormMeasureResult.FormClosedEventHandler(frmMeasureResult_frmClosed);
                frmMeasureResult.lblMeasureResult.Text = "";
                frmMeasureResult.Text = "面积量测";
                frmMeasureResult.Show();
            }
            else
            {
                frmMeasureResult.Activate();
            }
        }

        // 获取地图单位
        private string GetMapUnit(esriUnits _esriMapUnit)
        {
            string sMapUnits = string.Empty;
            switch (_esriMapUnit)
            {
                case esriUnits.esriCentimeters:
                    sMapUnits = "厘米";
                    break;
                case esriUnits.esriDecimalDegrees:
                    sMapUnits = "十进制";
                    break;
                case esriUnits.esriDecimeters:
                    sMapUnits = "分米";
                    break;
                case esriUnits.esriFeet:
                    sMapUnits = "尺";
                    break;
                case esriUnits.esriInches:
                    sMapUnits = "英寸";
                    break;
                case esriUnits.esriKilometers:
                    sMapUnits = "千米";
                    break;
                case esriUnits.esriMeters:
                    sMapUnits = "米";
                    break;
                case esriUnits.esriMiles:
                    sMapUnits = "英里";
                    break;
                case esriUnits.esriMillimeters:
                    sMapUnits = "毫米";
                    break;
                case esriUnits.esriNauticalMiles:
                    sMapUnits = "海里";
                    break;
                case esriUnits.esriPoints:
                    sMapUnits = "点";
                    break;
                case esriUnits.esriUnitsLast:
                    sMapUnits = "UnitsLast";
                    break;
                case esriUnits.esriUnknownUnits:
                    sMapUnits = "未知单位";
                    break;
                case esriUnits.esriYards:
                    sMapUnits = "码";
                    break;
                default:
                    break;
            }
            return sMapUnits;
        }

        // 获取坐标信息
        private void 获取坐标信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("坐标X为：" + xx + "      坐标Y为：" + yy, "提示，如果原数据未定义坐标系，那么这个提示框显示的坐标有误");
        }

        // 点击完成退出当前操作
        private void 完成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pMouseOperator != "SelFeature")
            {
                pMouseOperator = null;
            }
            //结束线段测量功能
            if (pNewLineFeedback != null)
            {
                pNewLineFeedback.Stop();
                pMouseOperator = null;

            }
            //结束面积测量
            if (pNewPolygonFeedback != null)
            {
                pNewPolygonFeedback.Stop();
                pMouseOperator = null;
            }
        }

        // 
        private void axMapControlMain_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            sMapUnits = GetMapUnit(axMapControlMain.Map.MapUnits);
            pMovePt = (axMapControlMain.Map as IActiveView).ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);

            if (pMouseOperator == "MeasureLength")
            {
                if (pNewLineFeedback != null)
                {
                    pNewLineFeedback.MoveTo(pMovePt);
                }


                double deltax = 0;//两点之间x差值
                double deltay = 0;//两点之间y差值

                if ((pPointPt != null) && (pNewLineFeedback != null))
                {
                    deltax = pMovePt.X - pPointPt.X;
                    deltay = pMovePt.Y - pPointPt.Y;
                    dSegmentLength = Math.Round(Math.Sqrt((deltax * deltax) + (deltay * deltay)), 3);
                    dToltalLength = dToltalLength + dSegmentLength;
                    if (frmMeasureResult != null)
                    {
                        frmMeasureResult.lblMeasureResult.Text = String.Format(
                            "当前线段长度：{0:.###}{1}\r\n总长度为：    {2:.###}{1}",
                            dSegmentLength, sMapUnits, dToltalLength);
                        dToltalLength = dToltalLength - dSegmentLength; //鼠标移动到新点重新开始计算
                    }
                    frmMeasureResult.frmClosed += new FormMeasureResult.FormClosedEventHandler(frmMeasureResult_frmClosed);
                }
            }

            // 面积量算
            if (pMouseOperator == "MeasureArea")
            {
                if (pNewPolygonFeedback != null)
                {
                    pNewPolygonFeedback.MoveTo(pMovePt);
                }

                IPointCollection pPointCol = new Polygon();
                IPolygon pPolygon = new PolygonClass();
                IGeometry pGeo = null;

                ITopologicalOperator pTopo = null;
                for (int i = 0; i <= pAreaPointCol.PointCount - 1; i++)
                {
                    pPointCol.AddPoint(pAreaPointCol.get_Point(i), ref missing, ref missing);
                }
                pPointCol.AddPoint(pMovePt, ref missing, ref missing);

                if (pPointCol.PointCount < 3) return;
                pPolygon = pPointCol as IPolygon;

                if ((pPolygon != null))
                {
                    pPolygon.Close();
                    pGeo = pPolygon as IGeometry;
                    pTopo = pGeo as ITopologicalOperator;
                    //使几何图形的拓扑正确
                    pTopo.Simplify();
                    pGeo.Project(axMapControlMain.Map.SpatialReference);
                    IArea pArea = pGeo as IArea;

                    frmMeasureResult.lblMeasureResult.Text = String.Format(
                        "总面积为：{0:.####}平方{1}\r\n总长度为：{2:.####}{1}",
                        pArea.Area, sMapUnits, pPolygon.Length);
                    pPolygon = null;
                }


            }
        }

        private void 影响范围ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 全屏显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axMapControlMain.Extent = axMapControlMain.FullExtent;

        }
        #endregion
    }
}
