using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CloudCapTileConverter
{
    public partial class Form1 : Form
    {
        public string xmlPath = "";
        public string imgFolder = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text += "Reading index.xml file. Please Wait... " + Environment.NewLine;

            OpenFileDialog renderExcelOFD = new OpenFileDialog();

            renderExcelOFD.InitialDirectory = "C:\\data\\CloudCapCache";
            renderExcelOFD.Filter = "xml files (*.xml)|*xml|ossim session files (*.session)|*.session|All files (*.*)|*.*";
            renderExcelOFD.FilterIndex = 1;
            renderExcelOFD.RestoreDirectory = true;
            if (renderExcelOFD.ShowDialog() == DialogResult.OK)
            {
                string ccXmlPath = renderExcelOFD.FileName;
                xmlPath = ccXmlPath;
                imgFolder = Path.GetDirectoryName(renderExcelOFD.FileName);
               
                richTextBox1.Text += "Sucessfully Read xml File at " + Environment.NewLine + ccXmlPath + Environment.NewLine;

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Text += "Processing index xml File at " + Environment.NewLine + xmlPath + Environment.NewLine;
            string regionId = textBox2.Text;

            XElement xIndex = XElement.Load(xmlPath);
            IEnumerable<FileRequestInfo> cacheReqs = from item in xIndex.Descendants("ImageDescriptor")
                                          where item.Element("FilePath").ToString().Contains(regionId)
                                          select new FileRequestInfo { fileHandle= item.Element("FilePath").Value, cdataPayload= item.Element("Extents").Value.Trim() };

            List<FileRequestInfo> cacheRequests =  cacheReqs.ToList<FileRequestInfo>();
            richTextBox1.Text += "Index file processed. We will now process " + cacheRequests.Count.ToString() + " entries." + Environment.NewLine+ Environment.NewLine;

            //set thread pool and concurrent request limit
            ThreadPool.SetMinThreads(10, 4);
            System.Net.ServicePointManager.DefaultConnectionLimit = 10;

            foreach (var req in cacheRequests) {
                runArcgisRequest(req);
            }
        }

        private void runArcgisRequest(FileRequestInfo tempFileInfo) {
            string agUrl = textBox1.Text;
            //for some reason the data is stored as cdata, so clean the payload & parse xml
            string cleanXmlStr = Regex.Replace(tempFileInfo.cdataPayload, @"\t|\n|\r", "");

            XElement extentXml = XElement.Parse(cleanXmlStr);
            ExtentInfo cacheReqsExt = (from item in extentXml.DescendantsAndSelf()
                            select new ExtentInfo { minLat = (string)item.Element("minLat"),
                                maxLat = (string)item.Element("maxLat"),
                                minLon = (string)item.Element("minLon"),
                                maxLon = (string)item.Element("maxLon")
                            }).First();
          
            string agsPayload = agUrl + "/export?bbox="+ cacheReqsExt.minLon + "%2C"+ cacheReqsExt.minLat + "%2C"+ cacheReqsExt.maxLon + "%2C"+ cacheReqsExt.maxLat + "&bboxSR=4326&layers=&layerDefs=&size=1024%2C1024&imageSR=4326&format=jpg&transparent=false&dpi=&time=&layerTimeOptions=&dynamicLayers=&gdbVersion=&mapScale=&rotation=&f=image";
            //richTextBox1.Text += agsPayload + Environment.NewLine + Environment.NewLine;
            GetImgAsync(agsPayload, tempFileInfo.fileHandle);

        }


        private async Task GetImgAsync(string uri , string fileInfo)
        {
            try
            {
                using (System.Net.WebClient webClient = new System.Net.WebClient())
                {
                    string downloadToDirectory = imgFolder + "\\" + fileInfo;
                    //set proxy or crdentials here
                    webClient.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                    await webClient.DownloadFileTaskAsync(new Uri(uri), @downloadToDirectory);                   
                }
            }
            catch (Exception e)
            {
                richTextBox1.Text += "Failed to download File: " + fileInfo;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }


        class ExtentInfo
        {
            public string minLat { get; set; }
            public string maxLat { get; set; }
            public string minLon { get; set; }
            public string maxLon { get; set; }
            
        }
        class FileRequestInfo {
            public string fileHandle { get; set; }
            public string cdataPayload { get; set; }
        }
    }

}
