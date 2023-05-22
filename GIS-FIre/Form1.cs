using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace GIS_FIre
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            axTOCControl1.SetBuddyControl(axMapControlMain);
        }

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
    }
}
