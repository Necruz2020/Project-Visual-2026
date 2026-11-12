using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParameterManagement
{
    /// <summary>
    /// Lógica de interacción para CreateAndFillParameter.xaml
    /// </summary>
    public partial class CreateAndFillParameter : Window
    {
        private ExternalEvent m_ExEvent;

        public CreateAndFillParameter(ExternalEvent exEvent)
        {
            InitializeComponent();
            m_ExEvent = exEvent;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //verificar si existe  el archivo
            if (!File.Exists(ViewTools.pathFile))
            {
                TaskDialog.Show("CASA", "No existe el archivo excel");
                return;
            }

            m_ExEvent.Raise();
        }

        private void btn_FilePath_Click(object sender, RoutedEventArgs e)
        {
            string filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }
            }

            if (filePath == string.Empty)
            {
                return;
            }

            txtFilePath.Text = filePath;

            // Guardo el path
            ViewTools.pathFile = filePath;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            System.Drawing.Image img = Properties.Resources.ToolTip;
            ImageSource imgSrc = ImageUti.GetImageSource(img);
            this.Icon = imgSrc;
            this.imgLogo.Source = imgSrc;
        }
    }
}
