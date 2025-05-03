using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Process_Scheduling_Simulator.Classes;
using Process_Scheduling_Simulator.Classes.Scheduler;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator
{

    public class Process : INotifyPropertyChanged
    {
        // --- 기존 속성들 ---
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private int _arrivalTime;
        public int ArrivalTime
        {
            get { return _arrivalTime; }
            set { _arrivalTime = value; OnPropertyChanged("ArrivalTime"); }
        }

        private int _burstTime;
        public int BurstTime // 초기 총 작업량
        {
            get { return _burstTime; }
            set { _burstTime = value; OnPropertyChanged("BurstTime"); }
        }

        private double _remainingBurstTime; // 남은 작업량 (double로 변경하여 P코어 처리 반영)
        public double RemainingBurstTime
        {
            get { return _remainingBurstTime; }
            set { _remainingBurstTime = value; OnPropertyChanged("RemainingBurstTime"); }
        }

        private int waitingTime;
        public int WaitingTime
        {
            get { return waitingTime; }
            set { waitingTime = value; OnPropertyChanged("WaitingTime"); }
        }

        private int _turnaroundTime;
        public int TurnaroundTime
        {
            get { return _turnaroundTime; }
            set { _turnaroundTime = value; OnPropertyChanged("TurnaroundTime"); }
        }

        private double _normalizedTTime; // 정규화된 TT는 소수점이 나올 수 있음
        public double NormalizedTTime
        {
            get { return _normalizedTTime; }
            set { _normalizedTTime = value; OnPropertyChanged("NormalizedTTime"); }
        }

        public int CPUTicks { get; internal set; } // CPU에서 소모된 Tick 수 (예: 1ms = 1 Tick으로 가정)


        private Brush _processColor;
        [JsonIgnore]
        public Brush ProcessColor
        {
            get { return _processColor ?? Brushes.Gray; } // 기본값으로 회색 반환
            set
            {
                _processColor = value;
                OnPropertyChanged("ProcessColor");
            }
        }

        public object RemainingTime { get; internal set; }
        public int CompletionTime { get; internal set; }

        // --- INotifyPropertyChanged 구현 ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- 생성자 (예시) ---
        public Process(string name, int arrivalTime, int burstTime, Brush color)
        {
            Name = name; // 이름 설정
            ArrivalTime = arrivalTime;
            BurstTime = burstTime;
            RemainingBurstTime = burstTime; // 초기에는 남은 시간이 총 Burst Time과 같음
            WaitingTime = 0;
            TurnaroundTime = 0;
            NormalizedTTime = 0;
            ProcessColor = color; // 기본 색상 설정
        }

        // Add a ResetState method if needed
        public void ResetState()
        {
            // Reset metrics calculated during simulation
            WaitingTime = 0;
            TurnaroundTime = 0;
            NormalizedTTime = 0.0;
            // Crucially, reset the remaining burst time to the original burst time
            RemainingBurstTime = this.BurstTime;
            // Reset any other state variables (like start time, completion time if tracked)
        }

        // 기본 생성자 (필요시)
        public Process() { }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<Process> ProcessList { get; set; }
        public static bool usePerformanceBoost = false; // 성능 향상 사용 여부
        public MainWindow()
        {
            InitializeComponent();

            BorderMain.Opacity = 0;
            Grid_Gantt.Opacity = 0;
            Grid_SettingsStats.Opacity = 0;
            
            Grid_Gantt.Visibility = Visibility.Collapsed;
            Grid_SettingsStats.Visibility = Visibility.Collapsed;
            ProcessList = new ObservableCollection<Process>()
            {
                new("P01", 0, 3, Brushes.Gray), // 이름 부여
                new("P02", 1, 5, Brushes.Gray),
                new("P03", 2, 2, Brushes.Gray),
                new("P04", 2, 7, Brushes.Gray),
                new("P05", 3, 3, Brushes.Gray),
                new("P06", 4, 2, Brushes.Gray),
                new("P07", 5, 5, Brushes.Gray),
                new("P08", 6, 5, Brushes.Gray),
                new("P09", 7, 4, Brushes.Gray),
                new("P10",7, 6, Brushes.Gray),
                new("P11", 8, 3, Brushes.Gray),
                new("P12", 9, 4, Brushes.Gray),
                new("P13", 9, 3, Brushes.Gray),
                new("P14", 10, 2, Brushes.Gray)
            };

            DataContext = this;
        }

        private void ViewChanger_hideAll()
        {
            Grid_ProcessorSettings.Visibility = Visibility.Collapsed;
            Grid_SchedulingSettings.Visibility = Visibility.Collapsed;
            Grid_ProcessSettings.Visibility = Visibility.Collapsed;
        }

        private void ViewChanger_ProcessorSettingsClicked(object sender, RoutedEventArgs e)
        {
            ViewChanger_hideAll();
            Grid_ProcessorSettings.Visibility = Visibility.Visible;
        }

        private void ViewChanger_SchedulingSettingsClicked(object sender, RoutedEventArgs e)
        {
            ViewChanger_hideAll();
            Grid_SchedulingSettings.Visibility = Visibility.Visible;
        }   

        private void ViewChanger_ProcessSettingsClicked(object sender, RoutedEventArgs e)
        {
            ViewChanger_hideAll();
            Grid_ProcessSettings.Visibility = Visibility.Visible;
        }

        private async void LoadedEventHandler(object sender, RoutedEventArgs e)
        {

            await Task.Delay(200);
            AnimationController.BeginAnimation(BorderMain, OpacityProperty, duration: 0.3, easingFunction: null);
            AnimationController.BeginAnimation(BorderMain, HeightProperty, 0, 900, 0.5, easingFunction: new CubicEase(), easingMode: EasingMode.EaseOut);
            AnimationController.BeginAnimation(BorderMain, WidthProperty, 0, 1650, 0.5, easingFunction: new CubicEase(), easingMode: EasingMode.EaseOut);
            await Task.Delay(750);
            BorderMain.BeginAnimation(HeightProperty, null);
            BorderMain.BeginAnimation(WidthProperty, null);
            Grid_Gantt.Visibility= Visibility.Visible;

            Grid_SettingsStats.Visibility = Visibility.Visible;
            ViewChanger_hideAll();
            ViewChanger_SchedulingSettingsClicked(sender, e);

            AnimationController.BeginAnimation(Grid_Gantt, OpacityProperty, from: 0, to: 1, duration: 0.5, easingFunction: new SineEase());
            await Task.Delay(1);
            //DrawGanttBar(0, 0, 0, "P1", Brushes.Gray); // 초기화
            ClearChart();
            await Task.Delay(249);
            AnimationController.BeginAnimation(Grid_SettingsStats, OpacityProperty, from: 0, to: 1, duration: 0.5, easingFunction: new SineEase());



        }

        private async void AppCloseClickedEventHandler(object sender, RoutedEventArgs e)
        {
            AnimationController.BeginAnimation(Grid_Gantt, OpacityProperty, from: 1, to: 0, duration: 0.5, easingFunction: new CubicEase());
            await Task.Delay(250);
            AnimationController.BeginAnimation(Grid_SettingsStats, OpacityProperty, from: 1, to: 0, duration: 0.5, easingFunction: new CubicEase());
            await Task.Delay(500);
            Grid_Gantt.Visibility = Visibility.Collapsed;
            Grid_SettingsStats.Visibility = Visibility.Collapsed;
            AnimationController.BeginAnimation(BorderMain, OpacityProperty, from: 1, to: 0, duration: 0.5, easingFunction: new QuadraticEase());
            AnimationController.BeginAnimation(BorderMain, WidthProperty, BorderMain.ActualWidth, 0, 0.5, easingFunction: new QuadraticEase());
            AnimationController.BeginAnimation(BorderMain, HeightProperty, BorderMain.ActualHeight, 0, 0.5, easingFunction: new QuadraticEase());
            await Task.Delay(500);
            this.Close();
        }

        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private bool schedulerStarted = false; // 스케줄러 시작 여부 플래그
        private async void SchedulerStartClickedHandler(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false; // 버튼 비활성화
            AssignColorsToProcessList();
            try // 오류 발생 가능성이 있으므로 try-catch 블록 사용
            {
                if (int.Parse(VisDelayTextBox.Text) < 50) VisDelayTextBox.Text = "50";

                // --- 1. 이전 결과 초기화 ---
                // Gantt 차트 초기화 (MainWindow에 구현된 ClearChart 메서드 사용)
                this.ClearChart(); // 'this'는 MainWindow 인스턴스
                                   // 결과 테이블/텍스트 초기화
                LabelPcorePower.Text = "0.0";
                LabelEcorePower.Text = "0.0";
                LabelAvgResponseTime.Text = "0.0";
                LabelTotalElapsedTime.Text = "0.0";
                LabelAvgNTTime.Text = "0.0";


                // --- 2. 입력 값 가져오기 ---
                string selectedAlgorithm = ((ComboBoxItem)AlgorithmComboBox.SelectedItem)?.Content.ToString();
                if (string.IsNullOrEmpty(selectedAlgorithm))
                {
                    HandyControl.Controls.Growl.Warning("스케줄링 알고리즘을 선택해주세요.");
                    (sender as Button).IsEnabled = true;
                    return;
                }

                // 현재 UI에 있는 프로세스 목록 가져오기 (ObservableCollection<Process> ProcessList 가정)
                List<Process> processesToSchedule = ProcessList.ToList(); // 복사본 사용
                if (!processesToSchedule.Any())
                {
                    HandyControl.Controls.Growl.Warning("프로세스를 추가해주세요.");
                    (sender as Button).IsEnabled = true;
                    return;
                }


                // 프로세서 개수 가져오기 (UI 컨트롤 이름은 예시)
                int numPcores = 0;
                int numEcores = 0;
                if (!int.TryParse(PcoreCountTextBox.Text, out numPcores) || numPcores < 0 ||
                    !int.TryParse(EcoreCountTextBox.Text, out numEcores) || numEcores < 0)
                {
                    HandyControl.Controls.Growl.Error("잘못된 코어 설정입니다. 코어 설정을 확인해주세요.");
                    (sender as Button).IsEnabled = true;
                    return;
                }
                int totalProcessors = numPcores + numEcores;
                if (totalProcessors <= 0)
                {
                    HandyControl.Controls.Growl.Error("코어가 최소 한개는 필요합니다");
                    (sender as Button).IsEnabled = true;
                    return;
                }


                // --- 3. Init.mainApplication 확인 ---
                if (Init.mainApplication == null)
                {
                    Init.mainApplication = this; // 현재 MainWindow 인스턴스 할당
                    Console.WriteLine("Init.mainApplication initialized.");
                }

                // --- 4. 프로세서 인스턴스 생성 ---
                List<Processor> processors = new List<Processor>();
                // P-core 생성
                for (int i = 0; i < numPcores; i++)
                {
                    string processorName = $"P-Core {i}";
                    // Gantt 차트 UI에 먼저 추가하고 인덱스 받기
                    int ganttIndex = this.AddProcessor(processorName); // MainWindow의 AddProcessor 호출
                    processors.Add(new Processor(processorName, CoreType.P, ganttIndex));
                    Console.WriteLine($"Created Processor: {processorName} at Gantt Index {ganttIndex}");
                }
                // E-core 생성
                for (int i = 0; i < numEcores; i++)
                {
                    string processorName = $"E-Core {i}";
                    int ganttIndex = this.AddProcessor(processorName);
                    processors.Add(new Processor(processorName, CoreType.E, ganttIndex));
                    Console.WriteLine($"Created Processor: {processorName} at Gantt Index {ganttIndex}");
                }

                // --- 5. 스케줄러 인스턴스 생성 ---
                Scheduler scheduler = null;
                switch (selectedAlgorithm)
                {
                    case "HRRN":
                        scheduler = new HRRNScheduler(processesToSchedule, processors);
                        break;
                    case "FCFS":
                        scheduler = new FCFSScheduler(processesToSchedule, processors);
                        break;
                    case "SPN":
                        scheduler = new SPNScheduler(processesToSchedule, processors);
                        break;
                    case "SRTN":
                        scheduler = new SRTNScheduler(processesToSchedule, processors);
                        break;
                    case "RR":
                        int quantum = 0;
                        if (!int.TryParse(TimeQuantumTextBox.Text, out quantum) || quantum <= 0)
                        {
                            HandyControl.Controls.Growl.Error("잘못된 Time Quantum 설정입니다.");
                            (sender as Button).IsEnabled = true;
                            return;
                        }
                        scheduler = new RRScheduler(processesToSchedule, processors, quantum);
                        break;
                    case "GTMI":
                        int customThreshold = 0;
                        if (!int.TryParse(CustomThresholdTextBox.Text, out customThreshold) || customThreshold < 0)
                        {
                            HandyControl.Controls.Growl.Error("잘못된 최소 임계값 설정입니다.");
                            (sender as Button).IsEnabled = true;
                            return;
                        }
                        scheduler = new OriginalScheduler(processesToSchedule, processors, customThreshold, NormalQueueSchedulerComboBox.SelectedIndex, (bool)ToggleOriginalPCorePrefer.IsChecked);
                        break;
                    default:
                        HandyControl.Controls.Growl.Error($" '{selectedAlgorithm}' 알고리즘은 인식되지 않습니다.");
                        (sender as Button).IsEnabled = true;
                        return;
                }
                Console.WriteLine($"Scheduler created for algorithm: {selectedAlgorithm}");


                // --- 6. 시뮬레이션 실행 ---
                schedulerStarted = true;
                Console.WriteLine("Starting simulation...");
                
                await scheduler.Schedule();
                Console.WriteLine("Simulation finished.");

                // --- 7. 결과 표시 ---
                Console.WriteLine("Displaying results...");
                // 완료된 프로세스 목록을 결과 그리드에 바인딩
                ResultsDataGrid.ItemsSource = scheduler.CompletedProcesses;

                // 요약 정보 업데이트
                LabelPcorePower.Text = $"{scheduler.TotalPCorePower:F1}";
                LabelEcorePower.Text = $"{scheduler.TotalECorePower:F1}";
                LabelTotalPower.Text = $"{scheduler.OverallTotalPower:F1}";
                LabelTotalElapsedTime.Text = $"{scheduler.CurrentTime}";

                // 평균값 계산 및 표시
                if (scheduler.CompletedProcesses.Any())
                {
                    LabelAvgResponseTime.Text = $"{scheduler.CompletedProcesses.Average(p => p.TurnaroundTime):F2}";
                    LabelAvgNTTime.Text = $"{scheduler.CompletedProcesses.Average(p => p.NormalizedTTime):F2}";
                }
                else
                {
                    Console.WriteLine("No processes were completed during the simulation.");
                }
                HandyControl.Controls.Growl.Success($"{selectedAlgorithm} 시뮬레이션이 성공적으로 종료되었습니다."); // 사용자 알림

                Button btn = sender as Button;

            }
            catch(FormatException ex)
            {
                HandyControl.Controls.Growl.Warning("입력 필드에 올바른 숫자를 입력해주세요.");
            }
            catch (Exception ex)
            {
                // 오류 처리
                HandyControl.Controls.Growl.Error($"시뮬레이션 중 오류가 발생했습니다.: {ex.Message}\n\n{ex.StackTrace}");
                Console.WriteLine($"!!! Simulation Error: {ex}");
            }finally
                {
                (sender as Button).IsEnabled = true;
            }
        }

        private List<Brush> _processColors = new List<Brush>
        {
            Brushes.DodgerBlue, Brushes.LimeGreen, Brushes.OrangeRed, Brushes.MediumOrchid, Brushes.Gold,
            Brushes.Tomato, Brushes.Turquoise, Brushes.HotPink, Brushes.YellowGreen, Brushes.SlateBlue,
            Brushes.DarkSeaGreen, Brushes.IndianRed, Brushes.CadetBlue, Brushes.Plum, Brushes.BurlyWood
            // 최대 15개 프로세스 지원 가정 [Source 6]
        };
        private void AssignColorsToProcessList()
        {
            _nextColorIndex = 0; // 색인 초기화
            foreach (var process in ProcessList)
            {
                process.ProcessColor = GetNextProcessColor();
            }
        }

        // 다음 프로세스 색상을 순환하며 반환하는 헬퍼 메서드
        private Brush GetNextProcessColor()
        {
            Brush color = _processColors[_nextColorIndex % _processColors.Count];
            _nextColorIndex++;
            return color;
        }

        private int _nextColorIndex = 0;

        private void ClearProcessClicked(object sender, RoutedEventArgs e)
        {
            ProcessList.Clear();
            ResultsDataGrid.ItemsSource = this.ProcessList; // 결과 그리드 초기화
        }

        private void AddRandomProcessClicked(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            int arrivalTime = rand.Next(0, 30);
            int burstTime = rand.Next(1, 10);
            ProcessList.Add(new Process($"P{ProcessList.Count + 1:00}", arrivalTime, burstTime, GetNextProcessColor()));
            ResultsDataGrid.ItemsSource = this.ProcessList; // 결과 그리드 초기화
        }

        private void AddProcessClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessList.Add(new Process($"P{ProcessList.Count + 1:00}", int.Parse(TextBox_ArrivalTime.Text), int.Parse(TextBox_BurstTime.Text), GetNextProcessColor()));
                ResultsDataGrid.ItemsSource = this.ProcessList; // 결과 그리드 초기화
            }
            catch (FormatException ex) {
                HandyControl.Controls.Growl.Warning($"숫자를 입력해주세요.");
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error($"프로세스 추가 중 오류 발생 : {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        private void ImportProcessClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // 파일 열기 대화 상자 표시
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // 선택한 파일에서 JSON 문자열 읽기
                    string json = File.ReadAllText(openFileDialog.FileName);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // 대소문자 구분 없이 속성 이름 매칭
                        ReferenceHandler = ReferenceHandler.Preserve, // 순환 참조 처리
                    };

                    // JSON 문자열을 ObservableCollection<Process>로 역직렬화
                    var importedProcesses = JsonSerializer.Deserialize<ObservableCollection<Process>>(json, options);

                    if (importedProcesses != null)
                    {
                        // 기존 ProcessList를 새로 불러온 데이터로 교체
                        ProcessList.Clear();
                        foreach (var process in importedProcesses)
                        {
                            ProcessList.Add(process);
                        }

                        // 결과 그리드 업데이트
                        ResultsDataGrid.ItemsSource = ProcessList;

                        HandyControl.Controls.Growl.Success("프로세스 목록이 성공적으로 불러와졌습니다.");
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Warning("JSON 파일에서 데이터를 읽을 수 없습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error($"불러오기 중 오류 발생: {ex.Message}");
            }
        }

        private void ExportProcessClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // JSON 직렬화 옵션 설정
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // 보기 좋게 들여쓰기
                    ReferenceHandler = ReferenceHandler.Preserve, // 순환 참조 처리
                };

                // ProcessList를 JSON 문자열로 변환
                string json = JsonSerializer.Serialize(ProcessList, options);

                // 파일 저장 대화 상자 표시
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = "ProcessList.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 선택한 경로에 JSON 파일 저장
                    File.WriteAllText(saveFileDialog.FileName, json);
                    HandyControl.Controls.Growl.Success("프로세스 목록이 성공적으로 내보내졌습니다.");
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error($"내보내기 중 오류 발생: {ex.Message}");
            }
        }

        /* Gantt Chart Implementation */
        // --- 상수 정의 ---
        private const double ProcessorRowHeight = 50.0; // 각 프로세서 행의 높이
        private const double TimeUnitWidth = 75.0;      // 시간 단위당 너비 (픽셀)
        private const double TimeLabelOffsetY = 10.0;   // 타임 라벨의 세로 위치 오프셋
        private const double GanttBarMarginY = 2.0;     // 간트 바의 상하 마진

        // --- 멤버 변수 ---
        private double _maxTime = 0; // 현재까지 그려진 최대 시간

        // UI 요소 추적을 위한 리스트
        private readonly List<Border> _ganttBars = new List<Border>();
        private readonly List<TextBlock> _processorLabels = new List<TextBlock>();
        private readonly List<TextBlock> _timeLabels = new List<TextBlock>();

        // ScrollViewer 동기화를 위한 플래그 (무한 루프 방지)
        private bool _isSyncingScroll = false;

        // --- (1) 프로세서 관리 메서드 ---

        /// <summary>
        /// 프로세서 목록에 새 프로세서 이름을 추가합니다.
        /// </summary>
        /// <param name="processorName">추가할 프로세서의 이름</param>
        /// <returns>추가된 프로세서의 인덱스</returns>
        public int AddProcessor(string processorName)
        {
            var textBlock = new TextBlock
            {
                Text = $"\n{processorName}",
                Height = ProcessorRowHeight,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 15, 0), // 약간의 왼쪽 여백
                Tag = _processorLabels.Count, // 인덱스를 Tag에 저장 (나중에 식별용)
            };

            ProcessorStackPanel.Children.Add(textBlock);
            _processorLabels.Add(textBlock);

            // 메인 캔버스 높이 업데이트 (프로세서 추가 시)
            UpdateMainCanvasSize();

            return _processorLabels.Count - 1; // 새로 추가된 프로세서의 인덱스 반환
        }

        /// <summary>
        /// 지정된 이름의 프로세서를 목록에서 제거합니다.
        /// </summary>
        /// <param name="processorName">제거할 프로세서의 이름</param>
        /// <returns>제거 성공 여부</returns>
        public bool RemoveProcessor(string processorName)
        {
            TextBlock labelToRemove = _processorLabels.FirstOrDefault(lbl => lbl.Text == processorName);
            if (labelToRemove != null)
            {
                ProcessorStackPanel.Children.Remove(labelToRemove);
                _processorLabels.Remove(labelToRemove);

                // 제거 후 남은 라벨들의 Tag (인덱스) 업데이트 (필요 시)
                // 여기서는 간단히 두지만, 실제 사용 시 인덱스 관리가 중요할 수 있음.
                // 예를 들어, 간트 바를 그릴 때 사용한 인덱스와 일치시켜야 함.

                // 메인 캔버스 높이 업데이트 (프로세서 제거 시)
                UpdateMainCanvasSize();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 특정 인덱스의 프로세서를 제거합니다.
        /// </summary>
        /// <param name="index">제거할 프로세서의 인덱스</param>
        /// <returns>제거 성공 여부</returns>
        public bool RemoveProcessorAt(int index)
        {
            if (index >= 0 && index < _processorLabels.Count)
            {
                TextBlock labelToRemove = _processorLabels[index];
                ProcessorStackPanel.Children.Remove(labelToRemove);
                _processorLabels.RemoveAt(index);

                // 메인 캔버스 높이 업데이트
                UpdateMainCanvasSize();
                return true;
            }
            return false;
        }


        // --- (2) 간트 바 그리기 메서드 ---

        /// <summary>
        /// 간트 차트에 프로세스 실행 바를 그립니다.
        /// </summary>
        /// <param name="startTime">프로세스 시작 시간</param>
        /// <param name="endTime">프로세스 종료 시간</param>
        /// <param name="processorIndex">프로세스가 실행된 프로세서의 인덱스</param>
        /// <param name="processName">표시할 프로세스 이름 (툴팁 등)</param>
        /// <param name="barColor">바의 배경색</param>
        public void DrawGanttBar(double startTime, double endTime, int processorIndex, string processName, Brush barColor)
        {
            ScrollViewerMain.ScrollToHorizontalOffset(ScrollViewerMain.ExtentWidth - ScrollViewerMain.ViewportWidth);
            if (processorIndex < 0 || processorIndex >= _processorLabels.Count)
            {
                // 유효하지 않은 프로세서 인덱스 처리 (예: 오류 로깅 또는 무시)
                Console.WriteLine($"Warning: Invalid processor index {processorIndex} for process {processName}");
                return;
            }

            if (startTime >= endTime)
            {
                // 시작 시간이 종료 시간보다 크거나 같은 경우 처리
                Console.WriteLine($"Warning: Invalid time range ({startTime} >= {endTime}) for process {processName}");
                return;
            }

            double left = startTime * TimeUnitWidth;
            double top = processorIndex * ProcessorRowHeight;
            double width = (endTime - startTime) * TimeUnitWidth;
            double height = ProcessorRowHeight - (GanttBarMarginY * 2); // 상하 마진 적용

            var border = new Border
            {
                Width = width,
                Height = height,
                Background = barColor ?? Brushes.SkyBlue, // 기본 색상 지정
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                ToolTip = $"{processName}\nTime: {startTime} - {endTime}\nProcessor: {_processorLabels[processorIndex].Text}",
                Opacity = 1//usePerformanceBoost?1:0

            }; 

            // 바 안에 프로세스 이름 표시 (옵션)
            var textBlock = new TextBlock
            {
                Text = processName,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis, // 긴 이름 자르기
                Effect = new DropShadowEffect { ShadowDepth = 0, Opacity=1, BlurRadius=3}
            };
            border.Child = textBlock;


            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top + GanttBarMarginY); // 세로 중앙 정렬을 위해 마진만큼 오프셋

            MainCanvas.Children.Add(border);
            _ganttBars.Add(border);
            if(!usePerformanceBoost) AnimationController.BeginAnimation(border, Border.OpacityProperty, from: 0, to: 1, duration: 0.5, easingFunction: new CubicEase(), removeOnComplete:true);

            // 최대 시간 업데이트 및 타임바 갱신
            if (endTime > _maxTime)
            {
                _maxTime = endTime;
                UpdateTimebar(_maxTime); // 타임바 확장
            }

            // 메인 캔버스 크기 업데이트
            UpdateMainCanvasSize();
             // 스크롤 위치 조정
        }

        // --- (3) 차트 청소 메서드 ---

        /// <summary>
        /// 간트 차트의 모든 요소(프로세서, 간트 바, 타임 라벨)를 제거합니다.
        /// </summary>
        public void ClearChart()
        {
            // 간트 바 제거
            foreach (var bar in _ganttBars)
            {
                bar.BeginAnimation(Border.OpacityProperty, null);
                MainCanvas.Children.Remove(bar);
            }
            _ganttBars.Clear();

            // 프로세서 라벨 제거
            foreach (var label in _processorLabels)
            {
                ProcessorStackPanel.Children.Remove(label);
            }
            _processorLabels.Clear();

            // 타임 라벨 제거
            foreach (var label in _timeLabels)
            {
                TimebarCanvas.Children.Remove(label);
            }
            _timeLabels.Clear();

            // 상태 초기화
            _maxTime = 0;
            UpdateMainCanvasSize(); // 캔버스 크기 초기화
            UpdateTimebar(0);       // 타임바 초기화
        }

        // --- (4) 타임바 동적 확장 메서드 ---

        /// <summary>
        /// 지정된 최대 시간까지 타임바 라벨을 업데이트하고 Canvas 너비를 조정합니다.
        /// </summary>
        /// <param name="maxTime">표시할 최대 시간</param>
        public void UpdateTimebar(double maxTime)
        {
            // 기존 타임 라벨 제거
            foreach (var label in _timeLabels)
            {
                TimebarCanvas.Children.Remove(label);
            }
            _timeLabels.Clear();

            // 필요한 너비 계산
            double requiredWidth = maxTime * TimeUnitWidth;
            TimebarCanvas.Width = requiredWidth > ScrollViewerTimebar.ActualWidth ? requiredWidth : ScrollViewerTimebar.ActualWidth; // 최소 뷰포트 너비 확보

            // 타임 라벨 및 눈금 생성 (정수 단위로)
            for (int t = 0; t <= Math.Ceiling(maxTime); ++t)
            {
                double xPos = t * TimeUnitWidth;

                // 시간 숫자 라벨
                var timeLabel = new TextBlock
                {
                    Text = t.ToString(),
                    FontSize = 10
                };
                Canvas.SetLeft(timeLabel, xPos - (timeLabel.ActualWidth / 2)); // 중앙 정렬 시도 (ActualWidth는 초기엔 0일 수 있음, 개선 필요)
                Canvas.SetTop(timeLabel, TimeLabelOffsetY);
                TimebarCanvas.Children.Add(timeLabel);
                _timeLabels.Add(timeLabel); // 추적 리스트에 추가

                // 눈금선 (옵션)
                var tickLine = new System.Windows.Shapes.Line
                {
                    X1 = xPos,
                    Y1 = TimeLabelOffsetY + 15, // 라벨 아래부터
                    X2 = xPos,
                    Y2 = TimebarCanvas.Height, // 캔버스 바닥까지
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 0.5
                };
                TimebarCanvas.Children.Add(tickLine);
                // 눈금선은 _timeLabels 리스트에 추가하지 않음 (별도 관리 또는 필요 시 추가)
            }
            // 시간 라벨 위치 재조정 (ActualWidth를 사용하기 위해 Dispatcher 사용)
            Dispatcher.BeginInvoke(new Action(() => {
                foreach (var lbl in _timeLabels)
                {
                    double currentLeft = Canvas.GetLeft(lbl);
                    if (!double.IsNaN(currentLeft) && lbl.ActualWidth > 0)
                    {
                        Canvas.SetLeft(lbl, currentLeft - (lbl.ActualWidth / 2));
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


        // --- 도우미 메서드 ---

        /// <summary>
        /// 프로세서 수와 최대 시간에 따라 메인 캔버스의 크기를 업데이트합니다.
        /// </summary>
        private void UpdateMainCanvasSize()
        {
            double requiredHeight = _processorLabels.Count * ProcessorRowHeight;
            double requiredWidth = _maxTime * TimeUnitWidth;

            // ScrollViewer가 제대로 작동하려면 Canvas 크기가 내용물 크기 이상이어야 함
            MainCanvas.Width = Math.Max(requiredWidth, ScrollViewerMain.ViewportWidth);
            MainCanvas.Height = Math.Max(requiredHeight, ScrollViewerMain.ViewportHeight);

            // ProcessorStackPanel의 높이도 동기화 (선택적이지만 레이아웃 일관성 도움)
            // ProcessorStackPanel.Height = MainCanvas.Height; // 이렇게 하면 StackPanel 자체가 늘어나 버림.
            // StackPanel은 자식 요소 크기에 따라 자동 조절되므로 명시적 설정 불필요.
        }


        // --- 스크롤 동기화 이벤트 핸들러 ---

        private void MainScrollChainged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll) return; // 동기화 중이면 무시

            _isSyncingScroll = true; // 동기화 시작 플래그
            // HorizontalOffset 변경 시 Timebar 스크롤 동기화
            if (Math.Abs(ScrollViewerTimebar.HorizontalOffset - e.HorizontalOffset) > 0.1)
            {
                ScrollViewerTimebar.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            // VerticalOffset 변경 시 Processor 스크롤 동기화
            if (Math.Abs(ScrollViewerProcessor.VerticalOffset - e.VerticalOffset) > 0.1)
            {
                ScrollViewerProcessor.ScrollToVerticalOffset(e.VerticalOffset);
            }
            _isSyncingScroll = false; // 동기화 완료 플래그
        }

        private void ProcessorScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll) return;

            _isSyncingScroll = true;
            // VerticalOffset 변경 시 Main 스크롤 동기화
            if (Math.Abs(ScrollViewerMain.VerticalOffset - e.VerticalOffset) > 0.1)
            {
                ScrollViewerMain.ScrollToVerticalOffset(e.VerticalOffset);
            }
            _isSyncingScroll = false;
        }

        private void TimebarScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll) return;

            _isSyncingScroll = true;
            // HorizontalOffset 변경 시 Main 스크롤 동기화
            if (Math.Abs(ScrollViewerMain.HorizontalOffset - e.HorizontalOffset) > 0.1)
            {
                ScrollViewerMain.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            _isSyncingScroll = false;
        }

        private void AppMaxmizeClickedEventHandler(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void AppMinimizeClickedEventHandler(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void AlgorithmSelectionChangedHandler(object sender, SelectionChangedEventArgs e)
        {


            if ((sender as ComboBox).SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedAlgorithm = selectedItem.Content.ToString();
                if(TimeQuantumTextBox is null)
                {
                    return;
                }
                if (selectedAlgorithm == "RR")
                {
                    Grid_RR_TimeQuantum.Visibility = Visibility.Visible;
                }
                else
                {
                    Grid_RR_TimeQuantum.Visibility = Visibility.Collapsed;
                }
                if (selectedAlgorithm == "GTMI")
                {
                    Grid_Original_CustomThreshold.Visibility = Visibility.Visible;
                    Grid_Original_NormalQueueScheduler.Visibility = Visibility.Visible;
                    Grid_Original_PCorePrefer.Visibility = Visibility.Visible;
                    Grid_Original_Description.Visibility = Visibility.Visible;
                }
                else
                {
                    Grid_Original_CustomThreshold.Visibility = Visibility.Collapsed;
                    Grid_Original_NormalQueueScheduler.Visibility = Visibility.Collapsed;
                    Grid_Original_PCorePrefer.Visibility = Visibility.Collapsed;
                    Grid_Original_Description.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void open_credits(object sender, MouseButtonEventArgs e)
        {
            View.License license = new View.License();
            license.Show();
        }

        private void TogglePerformanceBoostClicked(object sender, RoutedEventArgs e)
        {
            if(sender as ToggleButton != null)
            {
                usePerformanceBoost = (bool)(sender as ToggleButton).IsChecked;
            }

        }
    }
}