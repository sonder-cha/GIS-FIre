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

/* 虞世宁 更改于5/27 添加了图层右键菜单（上下移、删除、缩放至图层、属性表）
 
 
 */

namespace GIS_FIre
{
    public partial class Form1 : Form
    {
        public static Form1 mainForm;
        public routeForm routeForm;

        private ILayer pGlobeLayer = null;
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            axTOCControl1.SetBuddyControl(axMapControlMain);
        }

        private IActiveView m_ipActiveView;
        public event EventHandler SendMsgEvent;//定义事件
        public string[] CoorPoint = null;//存储路径分析坐标位置


        private void 打开OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ESRI.ArcGIS.SystemUI.ICommand pCommand;
            pCommand = new ESRI.ArcGIS.Controls.ControlsOpenDocCommand();
            pCommand.OnCreate(axMapControlMain.Object);
            pCommand.OnClick();
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
            ESRI.ArcGIS.SystemUI.ICommand pCommand;
            pCommand = new ESRI.ArcGIS.Controls.ControlsEditingSaveCommand();
            pCommand.OnCreate(axMapControlMain.Object);
            pCommand.OnClick();
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
        //曹之怿
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
        ///曹之怿
        public ESRI.ArcGIS.Controls.AxMapControl getMainAxMapControl()
        {
            return axMapControlMain;
        }

        /// <summary>
        /// 对3D控件pan功能鼠标状态的记录，完成3D与2D的联动平移，获取路径分析起点、终点位置坐标
        // 曹之怿
        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            global.MapMouseDown = 1;
            global.mapPoint.PutCoords(e.x, e.y);

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
        }

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
        // 曹之怿
        /// </summary>
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
        /// 曹之怿
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
        /// 曹之怿
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
        /// 曹之怿

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

        private void 缓冲区生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Buffer BF = new Buffer();
            BF.mapControl = this.axMapControlMain;
            BF.ShowDialog();
        }
    }
}
