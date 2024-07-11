using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WiimoteLib;
using WpfScreenHelper;

namespace WpfApp7.ViewModels
{
    public class MainWinViewModel
    {
        private WiimoteCollection mWC;
        public WiimoteCollection MWC => mWC;

        private List<Wiimote> wiimoteList = new List<Wiimote>();
        public List<Wiimote> WiimoteList => wiimoteList;

        private enum CalibrationStep : ushort
        {
            None,
            CenterScreen,
            TopLeft,
            Done,
        }


        private CalibrationStep currentCalibStep = CalibrationStep.None;

        private int canvasWidth;
        public int CanvasWidth => canvasWidth;
        private int canvasHeight;
        public int CanvasHeight => canvasHeight;

        private int gunImageTop;
        public int GunImageTop => gunImageTop;
        public event EventHandler GunImageTopChanged;

        private int gunImageLeft;
        public int GunImageLeft => gunImageLeft;
        public event EventHandler GunImageLeftChanged;

        private int gunCenterTop;
        public int GunCenterTop => gunCenterTop;
        public event EventHandler GunCenterTopChanged;

        private int gunCenterLeft;
        public int GunCenterLeft => gunCenterLeft;
        public event EventHandler GunCenterLeftChanged;

        private bool displayTopLeftGunImg;
        public bool DisplayTopLeftGunImg
        {
            get => displayTopLeftGunImg;
            private set
            {
                if (displayTopLeftGunImg == value) return;
                displayTopLeftGunImg = value;
                DisplayTopLeftGunImgChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayTopLeftGunImgChanged;

        private bool displayCenterGunImg;
        public bool DisplayCenterGunImg => displayCenterGunImg;

        public event EventHandler DisplayCenterGunImgChanged;

        private WiimoteStateData stateData = new WiimoteStateData();
        public WiimoteStateData StateData => stateData;
        public event EventHandler StateDataChanged;

        private int lightGunPointX;
        public int LightGunPointX => lightGunPointX;
        public event EventHandler LightGunPointXChanged;

        private int lightGunPointY;
        public int LightGunPointY => lightGunPointY;
        public event EventHandler LightGunPointYChanged;

        private bool lightGunPointVisible;
        public bool LightGunPointVisible => lightGunPointVisible;
        public event EventHandler LightGunPointVisibleChanged;

        private PointF topLeftCalibPoint = new PointF();
        private PointF centerCalibPoint = new PointF();
        private PointF origTopLeftCalibPoint = new PointF();

        private bool previousBState;
        private bool currentBState;

        private string midpointString = string.Empty;
        public string MidPointString
        {
            get => midpointString;
            private set
            {
                if (midpointString == value) return;
                midpointString = value;
                MidPointStringChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler MidPointStringChanged;

        private string calibPointString = string.Empty;
        public string CalibPointString
        {
            get => calibPointString;
            private set
            {
                if (calibPointString == value) return;
                calibPointString = value;
                CalibPointStringChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CalibPointStringChanged;

        private string currentStepHelpText = string.Empty;
        public string CurrentStepHelpText
        {
            get => currentStepHelpText;
            private set
            {
                if (currentStepHelpText == value) return;
                currentStepHelpText = value;
                CurrentStepHelpTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CurrentStepHelpTextChanged;

        private string warningHelpText = string.Empty;
        public string WarningHelpText
        {
            get => warningHelpText;
            private set
            {
                if (warningHelpText == value) return;
                warningHelpText = value;
                WarningHelpTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler WarningHelpTextChanged;

        private double offsetTopLeftX;
        private double offsetTopLeftY;

        private double topLeftXCoorAdj;
        public double TopLeftXCoorAdj
        {
            get => topLeftXCoorAdj;
            set
            {
                //double temp = Convert.ToDouble(value);
                //if (topLeftXCoorAdj == temp) return;
                if (topLeftXCoorAdj == value) return;
                topLeftXCoorAdj = value;
                TopLeftXCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TopLeftXCoorAdjChanged;

        private double topLeftYCoorAdj;
        public double TopLeftYCoorAdj
        {
            get => topLeftYCoorAdj;
            set
            {
                //double temp = Convert.ToDouble(value);
                //if (topLeftYCoorAdj == temp) return;
                //topLeftYCoorAdj = temp;
                if (topLeftYCoorAdj == value) return;
                topLeftYCoorAdj = value;
                TopLeftYCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TopLeftYCoorAdjChanged;

        private double centerXCoorAdj;
        public double CenterXCoorAdj
        {
            get => centerXCoorAdj;
            set
            {
                if (centerXCoorAdj == value) return;
                centerXCoorAdj = value;
                CenterXCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CenterXCoorAdjChanged;

        private double centerYCoorAdj;
        public double CenterYCoorAdj
        {
            get => centerYCoorAdj;
            set
            {
                if (centerYCoorAdj == value) return;
                centerYCoorAdj = value;
                CenterYCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CenterYCoorAdjChanged;

        private bool displayDoneVis;
        public bool DisplayDoneVis
        {
            get => displayDoneVis;
            set
            {
                if (displayDoneVis == value) return;
                displayDoneVis = value;
                DisplayDoneVisChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayDoneVisChanged;

        private WiiGunMappingConfig gunMappingConfig;

        public event EventHandler WiimoteStatePreprocess;

        private const int GUN_IMG_WIDTH = 72;
        private const int GUN_IMG_HEIGHT = 72;
        private const int IRSENSORS_LEN = 4;

        public int GunImgWidth => GUN_IMG_WIDTH;
        public int GunImgHeight => GUN_IMG_HEIGHT;

        public MainWinViewModel()
        {
            mWC = new WiimoteCollection();
            SetCanvasDimensions((int)WpfScreenHelper.SystemInformation.VirtualScreen.Width,
                (int)WpfScreenHelper.SystemInformation.VirtualScreen.Height);

            gunMappingConfig = new WiiGunMappingConfig();
            DetectPreviousMapping();

            TopLeftXCoorAdjChanged += MainWinViewModel_TopLeftXCoorAdjChanged;
            TopLeftYCoorAdjChanged += MainWinViewModel_TopLeftYCoorAdjChanged;
            CenterXCoorAdjChanged += MainWinViewModel_CenterXCoorAdjChanged;
            CenterYCoorAdjChanged += MainWinViewModel_CenterYCoorAdjChanged;
        }

        private void DetectPreviousMapping()
        {
            string testPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WiimoteGunTester", "mappings.json");

            if (!Directory.Exists(Path.GetDirectoryName(testPath)))
            {
                string dirPath = Path.GetDirectoryName(testPath);
                Directory.CreateDirectory(dirPath);
            }

            gunMappingConfig = null;
            if (File.Exists(testPath))
            {
                string json = string.Empty;
                using (StreamReader sr = new StreamReader(testPath))
                {
                    json = sr.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(json))
                {
                    bool fileRead = false;

                    try
                    {
                        WiiGunMappingConfig config = JsonSerializer.Deserialize<WiiGunMappingConfig>(json);
                        gunMappingConfig = config;
                        fileRead = true;
                    }
                    catch (JsonException)
                    {
                        // File parsing failed. Make blank instance of config
                        gunMappingConfig = new WiiGunMappingConfig();
                    }

                    if (fileRead)
                    {
                        currentCalibStep = CalibrationStep.Done;
                        EstablishDoneFromMapping();
                        SetupDone();
                    }
                }
            }
            else
            {
                gunMappingConfig = new WiiGunMappingConfig();
            }
        }

        public void SaveMappingConfig()
        {
            if (currentCalibStep != CalibrationStep.Done || gunMappingConfig == null)
            {
                return;
            }

            string testPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WiimoteGunTester", "mappings.json");

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };

            string json = JsonSerializer.Serialize(gunMappingConfig, options);
            try
            {
                using (StreamWriter sw = new StreamWriter(testPath, false))
                {
                    sw.Write(json);
                }
            }
            catch (IOException) { }
        }

        private void MainWinViewModel_CenterYCoorAdjChanged(object sender, EventArgs e)
        {
            if (currentCalibStep == CalibrationStep.Done)
            {
                centerCalibPoint.Y = (float)centerYCoorAdj;
                gunMappingConfig.RemoteMapping.MappingPoints.CenterY = centerYCoorAdj;
                GenerateCalibPointOutput();
            }
        }

        private void MainWinViewModel_CenterXCoorAdjChanged(object sender, EventArgs e)
        {
            if (currentCalibStep == CalibrationStep.Done)
            {
                centerCalibPoint.X = (float)centerXCoorAdj;
                gunMappingConfig.RemoteMapping.MappingPoints.CenterX = centerXCoorAdj;
                GenerateCalibPointOutput();
            }
        }

        private void MainWinViewModel_TopLeftYCoorAdjChanged(object sender, EventArgs e)
        {
            if (currentCalibStep == CalibrationStep.Done)
            {
                topLeftCalibPoint.Y = (float)topLeftYCoorAdj;
                gunMappingConfig.RemoteMapping.MappingPoints.TopLeftY = topLeftYCoorAdj;
                GenerateCalibPointOutput();
            }
        }

        private void MainWinViewModel_TopLeftXCoorAdjChanged(object sender, EventArgs e)
        {
            if (currentCalibStep == CalibrationStep.Done)
            {
                topLeftCalibPoint.X = (float)topLeftXCoorAdj;
                gunMappingConfig.RemoteMapping.MappingPoints.TopLeftX = topLeftXCoorAdj;
                GenerateCalibPointOutput();
            }
        }

        public void FindWiimotes()
        {
            mWC.FindAllWiimotes();

            int index = 1;
            foreach (Wiimote wm in mWC)
            {
                if (wm.WiimoteState.ExtensionType != ExtensionType.BalanceBoard)
                    wm.SetReportType(InputReport.IRExtensionAccel, IRSensitivity.Maximum, true);

                //if (index == 1)
                {
                    wm.WiimoteChanged += Wm_WiimoteChanged;
                }

                wiimoteList.Add(wm);
                index++;
            }
        }

        public void SetCanvasDimensions(int width, int height)
        {
            canvasWidth = width;
            canvasHeight = height;

            SetupGunImgCoords();
        }

        private void SetupGunImgCoords()
        {
            gunImageTop = (int)(2 - (GUN_IMG_HEIGHT / 2.0));
            gunImageLeft = (int)(2 - (GUN_IMG_WIDTH / 2.0));

            gunCenterTop = (int)((canvasHeight - GUN_IMG_HEIGHT) / 2.0);
            gunCenterLeft = (int)((canvasWidth - GUN_IMG_WIDTH) / 2.0);

            offsetTopLeftX = (GUN_IMG_WIDTH / 2.0) / canvasWidth;
            offsetTopLeftY = (GUN_IMG_HEIGHT / 2.0) / canvasHeight;
        }

        private void Wm_WiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            WiimoteState ws = e.WiimoteState;

            WiimoteStatePreprocess?.Invoke(this, EventArgs.Empty);

            stateData.MidPointX = 1.0 - ws.IRState.Midpoint.X;
            stateData.MidPointY = ws.IRState.Midpoint.Y;

            MidPointString = $"{stateData.MidPointX} {stateData.MidPointY}";

            //Trace.WriteLine($"{stateData.MidPointX} {stateData.MidPointY}");

            //ws.IRState.Midpoint.X;
            //ws.IRState.Midpoint.Y;

            currentBState = ws.ButtonState.B;

            if (currentCalibStep != CalibrationStep.None &&
                currentCalibStep != CalibrationStep.Done)
            {
                if (previousBState && !currentBState)
                {
                    bool found = false;
                    for (int i = 0; i < IRSENSORS_LEN && !found; i++)
                    {
                        if (ws.IRState.IRSensors[i].Found)
                        {
                            for (int j = i + 1; j < IRSENSORS_LEN && !found; j++)
                            {
                                if (ws.IRState.IRSensors[j].Found)
                                {
                                    found = true;
                                }
                            }
                        }
                    }

                    if (found)
                    {
                        NextCalibrationStep();
                    }
                    else
                    {
                        WarningHelpText = "Failed to find a midpoint. Please try again";
                    }
                }
            }
            else if (currentCalibStep == CalibrationStep.Done)
            {
                double tempX = ((1.0 - ws.IRState.Midpoint.X) - topLeftCalibPoint.X) / ((centerCalibPoint.X - topLeftCalibPoint.X) * 2.0);
                double tempY = (ws.IRState.Midpoint.Y - topLeftCalibPoint.Y) / ((centerCalibPoint.Y - topLeftCalibPoint.Y) * 2.0);
                lightGunPointX = (int)(canvasWidth * Math.Clamp(tempX, 0.0, 1.0));
                lightGunPointY = (int)(canvasHeight * Math.Clamp(tempY, 0.0, 1.0));
                //lightGunPointX = (int)ws.IRState.Midpoint.X;
                //lightGunPointY = (int)ws.IRState.Midpoint.Y;

                //LightGunPointXChanged?.Invoke(this, EventArgs.Empty);
                //LightGunPointYChanged?.Invoke(this, EventArgs.Empty);
                
            }

            StateDataChanged?.Invoke(this, EventArgs.Empty);
            previousBState = currentBState;
        }

        public void UpdateLightGunPoint()
        {
            LightGunPointXChanged?.Invoke(this, EventArgs.Empty);
            LightGunPointYChanged?.Invoke(this, EventArgs.Empty);
        }

        public void CheckStartProcess()
        {
            if (currentCalibStep != CalibrationStep.Done)
            {
                StartCalibration();
            }
        }

        public void StartCalibration()
        {
            NextCalibrationStep();
        }

        public void Reset()
        {
            currentCalibStep = CalibrationStep.None;
            SetupReset();
        }

        public void NextCalibrationStep()
        {
            switch(currentCalibStep)
            {
                case CalibrationStep.None:
                    currentCalibStep = CalibrationStep.CenterScreen;
                    SetupCenterScreen();
                    break;
                case CalibrationStep.CenterScreen:
                    centerCalibPoint.X = (float)stateData.MidPointX;
                    centerCalibPoint.Y = (float)stateData.MidPointY;
                    centerXCoorAdj = centerCalibPoint.X;
                    centerYCoorAdj = centerCalibPoint.Y;

                    currentCalibStep = CalibrationStep.TopLeft;
                    SetupTopLeftScreen();
                    break;
                case CalibrationStep.TopLeft:
                    //double distanceX = (centerCalibPoint.X - stateData.MidPointX) * 2.0;
                    //double distanceY = (centerCalibPoint.Y - stateData.MidPointY) * 2.0;

                    //double diffX = distanceX * offsetTopLeftX;
                    //double diffY = distanceY * offsetTopLeftY;

                    //origTopLeftCalibPoint = new PointF();
                    //origTopLeftCalibPoint.X = (float)stateData.MidPointX;
                    //origTopLeftCalibPoint.Y = (float)stateData.MidPointY;
                    //Trace.WriteLine($"{origTopLeftCalibPoint.X} | {origTopLeftCalibPoint.Y}");

                    topLeftCalibPoint.X = (float)stateData.MidPointX;
                    topLeftCalibPoint.Y = (float)stateData.MidPointY;
                    topLeftXCoorAdj = topLeftCalibPoint.X;
                    topLeftYCoorAdj = topLeftCalibPoint.Y;
                    //topLeftCalibPoint.X = (float)Math.Clamp(stateData.MidPointX - diffX,
                    //    0.0, 1.0);
                    //topLeftCalibPoint.Y = (float)Math.Clamp(stateData.MidPointY - diffY,
                    //    0.0, 1.0);

                    currentCalibStep = CalibrationStep.Done;
                    SetupDone();
                    break;
                case CalibrationStep.Done:
                    currentCalibStep = CalibrationStep.None;
                    SetupReset();
                    break;
                default: break;
            }
        }

        private void SetupReset()
        {
            displayTopLeftGunImg = false;
            displayCenterGunImg = false;
            lightGunPointVisible = false;
            topLeftCalibPoint = new PointF();
            centerCalibPoint = new PointF();
            CalibPointString = string.Empty;
            CurrentStepHelpText = string.Empty;
            WarningHelpText = string.Empty;
            DisplayDoneVis = false;

            DisplayTopLeftGunImgChanged?.Invoke(this, EventArgs.Empty);
            DisplayCenterGunImgChanged?.Invoke(this, EventArgs.Empty);
            LightGunPointVisibleChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetupCenterScreen()
        {
            displayTopLeftGunImg = false;
            displayCenterGunImg = true;
            topLeftCalibPoint = new PointF();
            centerCalibPoint = new PointF();
            CurrentStepHelpText = "Aim for center point and press B";
            WarningHelpText = string.Empty;
            DisplayDoneVis = false;

            DisplayTopLeftGunImgChanged?.Invoke(this, EventArgs.Empty);
            DisplayCenterGunImgChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetupTopLeftScreen()
        {
            displayTopLeftGunImg = true;
            displayCenterGunImg = false;
            CurrentStepHelpText = "Aim for top left point and press B";
            WarningHelpText = string.Empty;

            DisplayTopLeftGunImgChanged?.Invoke(this, EventArgs.Empty);
            DisplayCenterGunImgChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EstablishDoneFromMapping()
        {
            topLeftXCoorAdj = gunMappingConfig.RemoteMapping.MappingPoints.TopLeftX;
            topLeftCalibPoint.X = (float)gunMappingConfig.RemoteMapping.MappingPoints.TopLeftX;
            //builder.AppendLine($"{topLeftCalibPoint.X} {topLeftCalibPoint.Y}");
            //builder.AppendLine($"{centerCalibPoint.X} {centerCalibPoint.Y}");

            topLeftYCoorAdj = gunMappingConfig.RemoteMapping.MappingPoints.TopLeftY;
            topLeftCalibPoint.Y = (float)gunMappingConfig.RemoteMapping.MappingPoints.TopLeftY;

            centerXCoorAdj = gunMappingConfig.RemoteMapping.MappingPoints.CenterX;
            centerCalibPoint.X = (float)gunMappingConfig.RemoteMapping.MappingPoints.CenterX;


            centerYCoorAdj = gunMappingConfig.RemoteMapping.MappingPoints.CenterY;
            centerCalibPoint.Y = (float)gunMappingConfig.RemoteMapping.MappingPoints.CenterY;
        }

        private void SetupDone()
        {
            displayTopLeftGunImg = true;
            displayCenterGunImg = true;
            lightGunPointVisible = true;
            CurrentStepHelpText = string.Empty;
            WarningHelpText = string.Empty;
            DisplayDoneVis = true;

            DisplayTopLeftGunImgChanged?.Invoke(this, EventArgs.Empty);
            DisplayCenterGunImgChanged?.Invoke(this, EventArgs.Empty);
            LightGunPointVisibleChanged?.Invoke(this, EventArgs.Empty);
            TopLeftXCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            TopLeftYCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            CenterXCoorAdjChanged?.Invoke(this, EventArgs.Empty);
            CenterYCoorAdjChanged?.Invoke(this, EventArgs.Empty);

            GenerateCalibPointOutput();
            SaveCalibPointConfigInfo();
        }

        private void SaveCalibPointConfigInfo()
        {
            //gunMappingConfig.RemoteMaping.Find((t) => t.Serial)
            if (gunMappingConfig != null)
            {
                gunMappingConfig.RemoteMapping.MappingPoints = new MappingPoints()
                {
                    TopLeftX = topLeftXCoorAdj,
                    TopLeftY = topLeftYCoorAdj,
                    CenterX = centerXCoorAdj,
                    CenterY = centerYCoorAdj,
                };
            }
        }

        private void RefreshCalibPointConfigInfo()
        {
            if (gunMappingConfig != null)
            {
                MappingPoints mapPoints = gunMappingConfig.RemoteMapping.MappingPoints;

                mapPoints.TopLeftX = topLeftXCoorAdj;
                mapPoints.TopLeftY = topLeftYCoorAdj;
                mapPoints.CenterX = centerXCoorAdj;
                mapPoints.CenterY = centerYCoorAdj;
            }
        }

        private void GenerateCalibPointOutput()
        {
            StringBuilder builder = new StringBuilder();
            //builder.AppendLine($"OG TL {origTopLeftCalibPoint.X} | {origTopLeftCalibPoint.Y}");
            builder.AppendLine($"test_topLeftGunX: {string.Create(CultureInfo.InvariantCulture, $"{topLeftCalibPoint.X}")}");
            builder.AppendLine($"test_topLeftGunY: {string.Create(CultureInfo.InvariantCulture, $"{topLeftCalibPoint.Y}")},");
            builder.AppendLine($"test_centerGunX: {string.Create(CultureInfo.InvariantCulture, $"{centerCalibPoint.X}")},");
            builder.AppendLine($"test_centerGunY: {string.Create(CultureInfo.InvariantCulture, $"{centerCalibPoint.Y}")},");
            CalibPointString = builder.ToString();
            //CalibPointStringChanged?.Invoke(this, EventArgs.Empty);

            // Debug cmd output
            //Trace.WriteLine($"OG TL {origTopLeftCalibPoint.X} | {origTopLeftCalibPoint.Y}");
            Trace.WriteLine($"test_topLeftGun {string.Create(CultureInfo.InvariantCulture, $"{topLeftCalibPoint.X}")} {string.Create(CultureInfo.InvariantCulture, $"{topLeftCalibPoint.Y}")}");
            Trace.WriteLine($"test_centerGunX {string.Create(CultureInfo.InvariantCulture, $"{centerCalibPoint.X}")} {string.Create(CultureInfo.InvariantCulture, $"{centerCalibPoint.Y}")}");
            Trace.WriteLine("");
        }

        public void TearDown()
        {
            SaveMappingConfig();
            StateDataChanged = null;

            foreach (Wiimote wm in mWC)
            {
                wm.Disconnect();
            }

            mWC.Clear();
            wiimoteList.Clear();
        }
    }

    public class WiimoteStateData
    {
        private double midPointX;
        public double MidPointX
        {
            get => midPointX;
            set => midPointX = value;
        }

        private double midPointY;
        public double MidPointY
        {
            get => midPointY;
            set => midPointY = value;
        }

        private double lightGunX;
        public double LightGunX
        {
            get => lightGunX;
            set => lightGunX = value;
        }

        private double lightGunY;
        public double LightGunY
        {
            get => lightGunY;
            set => lightGunY = value;
        }
    }

    public class WiiGunMappingConfig
    {
        private WiimoteMapping remoteMapping = new WiimoteMapping();
        public WiimoteMapping RemoteMapping
        {
            get => remoteMapping;
            set => remoteMapping = value;
        }
    }

    public class WiimoteMapping
    {
        //private string serial;
        //public string Serial
        //{
        //    get => serial;
        //    set => serial = value;
        //}

        private MappingPoints mappingPoints = new MappingPoints();
        public MappingPoints MappingPoints
        {
            get => mappingPoints;
            set => mappingPoints = value;
        }
    }

    public class MappingPoints
    {
        private double topLeftX;
        public double TopLeftX
        {
            get => topLeftX;
            set => topLeftX = value;
        }

        private double topLeftY;
        public double TopLeftY
        {
            get => topLeftY;
            set => topLeftY = value;
        }

        private double centerX;
        public double CenterX
        {
            get => centerX;
            set => centerX = value;
        }

        private double centerY;
        public double CenterY
        {
            get => centerY;
            set => centerY = value;
        }
    }
}
