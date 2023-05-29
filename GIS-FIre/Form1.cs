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
using ESRI.ArcGIS.Geodatabase;

/* 虞世宁 更改于5/27 添加了图层右键菜单（上下移、删除、缩放至图层、属性表）
 
 
 */

namespace GIS_FIre
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 选中图层的标识
        /// </summary>

        private ILayer pGlobeLayer = null;
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            axTOCControl1.SetBuddyControl(axMapControlMain);
        }

        #region 功能栏-文件
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
        #endregion

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

    }


}
