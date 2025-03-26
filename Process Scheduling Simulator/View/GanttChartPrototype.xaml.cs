using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Brushes 등을 사용하기 위해 추가

namespace Process_Scheduling_Simulator.View
{
    /// <summary>
    /// GanttChartPrototype.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GanttChartPrototype : Window
    {
        // --- 상수 정의 ---
        private const double ProcessorRowHeight = 30.0; // 각 프로세서 행의 높이
        private const double TimeUnitWidth = 40.0;      // 시간 단위당 너비 (픽셀)
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

        public GanttChartPrototype()
        {
            InitializeComponent();
        }

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
                Text = processorName,
                Height = ProcessorRowHeight,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 0, 0, 0), // 약간의 왼쪽 여백
                Tag = _processorLabels.Count // 인덱스를 Tag에 저장 (나중에 식별용)
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
                ToolTip = $"{processName}\nTime: {startTime} - {endTime}\nProcessor: {_processorLabels[processorIndex].Text}"
            };

            // 바 안에 프로세스 이름 표시 (옵션)
            var textBlock = new TextBlock
            {
                Text = processName,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis // 긴 이름 자르기
            };
            border.Child = textBlock;


            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top + GanttBarMarginY); // 세로 중앙 정렬을 위해 마진만큼 오프셋

            MainCanvas.Children.Add(border);
            _ganttBars.Add(border);

            // 최대 시간 업데이트 및 타임바 갱신
            if (endTime > _maxTime)
            {
                _maxTime = endTime;
                UpdateTimebar(_maxTime); // 타임바 확장
            }

            // 메인 캔버스 크기 업데이트
            UpdateMainCanvasSize();
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
    }
}