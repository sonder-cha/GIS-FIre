using System;
using System.IO;
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
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesFile;


/*  
    缓冲区建立窗体 
    实现人：李明哲 
    功能：根据选择的图层和设定的缓冲半径以及融合类型，实现该图层的缓冲区计算 
*/
namespace GIS_FIre
{
    public partial class Buffer : Form
    {

        public IMap pMap { get; set; }
        public AxMapControl mapControl { get; set; }
        public Buffer()
        {
            InitializeComponent();
        }

        private void Buffer_Load(object sender, EventArgs e)
        {
            pMap = mapControl.Map;
            //向图层comboBox1中预置噪音来源
            if (pMap.LayerCount > 0)
            {
                for (int i = 0; i < pMap.LayerCount; i++)
                {
                    ILayer pLayer = pMap.get_Layer(i);
                    if (pLayer != null)
                    {
                        if (pLayer is IFeatureLayer)
                        {
                            comboBox_InputDataset.Items.Add(pLayer.Name);
                        }
                    }
                }
                comboBox_InputDataset.SelectedIndex = 0;

                DistanceTextBox.Text = "500";
                this.comboBox2.Items.Add("NONE");//选择项1
                this.comboBox2.Items.Add("ALL");
                comboBox2.SelectedIndex = 0;
                textEdit_Output.Text = System.Environment.CurrentDirectory + "buffer.shp";
            }
            else return;
        }

        //取消
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //选择保存路径
        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog flg = new SaveFileDialog();
            flg.Title = "保存路径";
            flg.Filter = "ShpFile(*shp)|*.shp";
            flg.ShowDialog();

            textEdit_Output.Text = flg.FileName;
        }

        #region 获取图层
        private ILayer GetLayerByName(IMap pMap, string layerName)
        {
            ILayer pLayer = null;
            ILayer tempLayer = null;
            try
            {
                for (int i = 0; i < pMap.LayerCount; i++)
                {
                    tempLayer = pMap.Layer[i];
                    if (tempLayer.Name.ToUpper() == layerName.ToUpper())      //判断名字大写是否一致
                    {
                        pLayer = tempLayer;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            return pLayer;
        }

        #endregion


        //确定按钮执行
        private void btnOK_Click(object sender, EventArgs e)
        {
            ILayer inputDataset = GetLayerByName(pMap, comboBox_InputDataset.Text.Trim());
            IFeatureLayer inputLayer = inputDataset as IFeatureLayer;
            //缓冲区分析-GP工具调用
            Geoprocessor gp = new Geoprocessor();
            object sev = null;
            try
            {
                gp.OverwriteOutput = true;
                ESRI.ArcGIS.AnalysisTools.Buffer pBuffer = new ESRI.ArcGIS.AnalysisTools.Buffer();
                pBuffer.in_features = inputLayer;
                //设置生成结果存储路径
                pBuffer.out_feature_class = textEdit_Output.Text;
                //设置缓冲区距离
                string buffer_distance = DistanceTextBox.Text + " Meters";
                pBuffer.buffer_distance_or_field = buffer_distance;
                pBuffer.dissolve_option = comboBox2.Text;
                //执行缓冲区分析
                gp.Execute(pBuffer, null);
                //将生成结果添加到地图中
                string pPath = System.IO.Path.GetDirectoryName(textEdit_Output.Text); //获取文件路径
                string pName = System.IO.Path.GetFileName(textEdit_Output.Text); //获取文件名
                this.mapControl.AddShapeFile(pPath, pName);
                this.mapControl.MoveLayerTo(1, 0);

                Console.WriteLine(gp.GetMessages(ref sev));
            }
            catch (Exception ex)
            {
                Console.WriteLine(gp.GetMessages(ref sev));
            }
        }
    }
}
