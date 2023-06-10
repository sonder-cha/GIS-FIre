using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.NetworkAnalysis;
using ESRI.ArcGIS.NetworkAnalyst;
using ESRI.ArcGIS.Display;
using System.IO;

namespace GIS_FIre
{
    public partial class network1 : Form
    {
        private INAContext m_NAContext;//网络分析上下文
        private INetworkDataset networkDataset;//网络数据集
        private IFeatureWorkspace pFWorkspace;
        private IFeatureClass inputFClass;//打开stops数据集
        private IFeatureDataset featureDataset;
        private bool networkanalasia = false;//判断是否点击新路线按钮，进入添加起点阶段
        private int clickedcount = 0;//mapcontrol加点显示点数
        private IActiveView m_ipActiveView;
        private IGraphicsContainer PGC;
        private IMap m_ipMap;
        public network1()
        {
            InitializeComponent();
        }


        private void initialize()
        {
            axMapControl1.ActiveView.Clear();
            axMapControl1.ActiveView.Refresh();
            //获取当前应用程序的目录名称
            string path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            int t;
            for (t = 0; t < path.Length; t++)
            {
                if (path.Substring(t, 14) == "NetworkAnalasis")
                {
                    break;
                }
            }
            //根据目录名称获取数据存取路径
            string name = path.Substring(0, t - 1) + "\\TestData\\Test.gdb";
            //打开工作空间
            pFWorkspace = OpenWorkspace(name) as IFeatureWorkspace;
            //打开网络数据集
            networkDataset = OpenNetworkDataset(pFWorkspace as IWorkspace, "street_ND", "test");
            //创建网络分析上下文，建立一种解决关系
            m_NAContext = CreateSolverContext(networkDataset);
            //打开数据集
            inputFClass = pFWorkspace.OpenFeatureClass("stops");
            //TEST_ND_JUNCTIONS图层
            IFeatureLayer vertex = new FeatureLayerClass();
            vertex.FeatureClass = pFWorkspace.OpenFeatureClass("Test_ND_Junctions");
            vertex.Name = vertex.FeatureClass.AliasName;
            axMapControl1.AddLayer(vertex, 0);
            //street图层
            IFeatureLayer road3;
            road3 = new FeatureLayerClass();
            road3.FeatureClass = pFWorkspace.OpenFeatureClass("street");
            road3.Name = road3.FeatureClass.AliasName;
            axMapControl1.AddLayer(road3, 0);
            //为networkdataset生成一个图层，并将该图层添加到axmapcontrol中
            ILayer pLayer;//网络图层
            INetworkLayer pNetworkLayer;
            pNetworkLayer = new NetworkLayerClass();
            pNetworkLayer.NetworkDataset = networkDataset;
            pLayer = pNetworkLayer as ILayer;
            pLayer.Name = "Network Dataset";
            axMapControl1.AddLayer(pLayer, 0);
            //生成一个网络分析图层并添加到axmaptrol1中
            ILayer layer1;
            INALayer nalayer = m_NAContext.Solver.CreateLayer(m_NAContext);
            layer1 = nalayer as ILayer;
            layer1.Name = m_NAContext.Solver.DisplayName;
            axMapControl1.AddLayer(layer1, 0);
            m_ipActiveView = axMapControl1.ActiveView;
            m_ipMap = m_ipActiveView.FocusMap;
            PGC = m_ipMap as IGraphicsContainer;
        }
        //打开工作空间
        public IWorkspace OpenWorkspace(string strGDBName)
        {
            IWorkspaceFactory workspaceFactory;
            workspaceFactory = new FileGDBWorkspaceFactoryClass();
            return workspaceFactory.OpenFromFile(strGDBName, 0);
        }
        //打开网络数据集
        public INetworkDataset OpenNetworkDataset(IWorkspace networkDatasetWorkspace, System.String networkDatasetName, System.String featureDatasetName)
        {
            if (networkDatasetWorkspace == null || networkDatasetName == "" || featureDatasetName == null)
            {
                return null;
            }
            IDatasetContainer3 datasetContainer3 = null;
            IFeatureWorkspace featureWorkspace = networkDatasetWorkspace as IFeatureWorkspace;
            featureDataset = featureWorkspace.OpenFeatureDataset(featureDatasetName);
            IFeatureDatasetExtensionContainer featureDatasetExtensionContainer = featureDataset as IFeatureDatasetExtensionContainer;
            IFeatureDatasetExtension featureDatasetExtension = featureDatasetExtensionContainer.FindExtension(esriDatasetType.esriDTNetworkDataset);
            datasetContainer3 = featureDatasetExtensionContainer as IDatasetContainer3;
            if (datasetContainer3 == null)

                return null;
            IDataset dataset = datasetContainer3.get_DatasetByName(esriDatasetType.esriDTNetworkDataset, networkDatasetName);
            return dataset as INetworkDataset;

        }
        //创建网络分析上下文
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
        //得到创建网络分析上下文所需的IDENETWORKDATASET类型参数
        public IDENetworkDataset GetDENetworkDataset(INetworkDataset networkDataset)
        {
            //将网络分析数据集QI添加到DATASETCOMPOENT
            IDatasetComponent dstComponent;
            dstComponent = networkDataset as IDatasetComponent;
            //获得数据元素
            return dstComponent.DataElement as IDENetworkDataset;

        }


        private void axMapControl1_OnMouseDown(object sender, ESRI.ArcGIS.Controls.IMapControlEvents2_OnMouseDownEvent e)
        {
            if (networkanalasia == true)
            {
                IPointCollection m_ipPoints;//输入点集合
                IPoint ipNew;
                m_ipPoints = new MultipointClass();
                ipNew = axMapControl1.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
                object o = Type.Missing;
                m_ipPoints.AddPoint(ipNew, ref o, ref o);
                CreateFeature(inputFClass, m_ipPoints);//或者用鼠标点击最近点
                //把最近的点显示出来
                IElement element;
                ITextElement textelement = new TextElementClass();
                element = textelement as IElement;
                ITextSymbol textSymbol = new TextSymbol();

                textelement.Symbol = textSymbol;
                clickedcount++;
                textelement.Text = clickedcount.ToString();
                element.Geometry = m_ipActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
                PGC.AddElement(element, 0);
                m_ipActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            }
        }
        //获取距离鼠标点击最近的点
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
        private void axMapControl1_OnDoubleClick(object sender, ESRI.ArcGIS.Controls.IMapControlEvents2_OnDoubleClickEvent e)
        {


        }

        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            networkanalasia = true;
            axMapControl1.CurrentTool = null;
            ITable pTable = inputFClass as ITable;
            pTable.DeleteSearchedRows(null);
            //提取路径前，删除上一次路径route网络上下文
            IFeatureClass routesFC;
            routesFC = m_NAContext.NAClasses.get_ItemByName("Routes") as IFeatureClass;
            ITable pTable1 = routesFC as ITable;
            pTable1.DeleteSearchedRows(null);
            //提取路径前，删除上一次路径Stops网络上下文
            INAClass stopsNAClass = m_NAContext.NAClasses.get_ItemByName("Stops") as INAClass;
            ITable ptable2 = stopsNAClass as ITable;
            ptable2.DeleteSearchedRows(null);
            //提取路径前，删除上一次barries网络上下文
            INAClass barriesNAClass = m_NAContext.NAClasses.get_ItemByName("Barriers") as INAClass;
            ITable pTable3 = barriesNAClass as ITable;
            pTable3.DeleteSearchedRows(null);
            //提取路径前，删除上次路径polyline
            IFeatureClass getroute = pFWorkspace.OpenFeatureClass("get_route");
            ITable ptable_polyline = getroute as ITable;
            ptable_polyline.DeleteSearchedRows(null);
            PGC.DeleteAllElements();
            clickedcount = 0;
            axMapControl1.Refresh();
        }

        private void toolStripSplitButton2_ButtonClick(object sender, EventArgs e)
        {
            IGPMessages gpMessages = new GPMessagesClass();
            loadNANetworkLocations("Stops", inputFClass, 80);
            INASolver naSlover = m_NAContext.Solver;
            naSlover.Solve(m_NAContext, gpMessages, null);
            //解决完后，删除图层内容
            ITable pTable_inputFClass = inputFClass as ITable;
            pTable_inputFClass.DeleteSearchedRows(null);
            axMapControl1.Refresh();

        }
        public void loadNANetworkLocations(string strNAClassName, IFeatureClass inputFC, double snapTolerance)
        {
            INAClass naClass;
            INamedSet classes;
            classes = m_NAContext.NAClasses;
            naClass = classes.get_ItemByName(strNAClassName) as INAClass;
            //删除naClasses中添加的项
            naClass.DeleteAllRows();
            //加载网络分析对象，设置容差值
            INAClassLoader classLoader = new NAClassLoader();
            classLoader.Locator = m_NAContext.Locator;
            if (snapTolerance > 0) classLoader.Locator.SnapTolerance = snapTolerance;
            classLoader.NAClass = naClass;
            //创建INAclassFieldMap,用于字段映射
            INAClassFieldMap fieldMap;
            fieldMap = new NAClassFieldMap();
            //加载网络分析类
            int rowsln = 0;
            int rowsLocated = 0;
            IFeatureCursor featureCursor = inputFC.Search(null, true);
            classLoader.Load((ICursor)featureCursor, null, ref rowsln, ref rowsLocated);
            ((INAContextEdit)m_NAContext).ContextChanged();
        }

        private void network1_Load(object sender, EventArgs e)
        {

        }
    }
}
