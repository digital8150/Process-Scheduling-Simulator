using System;
using System.Windows;
using System.Windows.Media.Animation; // 네임스페이스 추가

namespace Process_Scheduling_Simulator.Classes
{
    class AnimationController
    {
        /// <summary>
        /// 지정된 UI 요소의 DependencyProperty에 DoubleAnimation을 시작합니다.
        /// </summary>
        /// <param name="target">애니메이션을 적용할 UI 요소</param>
        /// <param name="property">애니메이션을 적용할 DependencyProperty</param>
        /// <param name="from">애니메이션 시작 값</param>
        /// <param name="to">애니메이션 종료 값</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        /// <param name="easingMode">Easing 함수 모드</param>
        /// <param name="easingFunction">사용자 지정 Easing 함수 (null이면 기본 Linear 사용)</param>
        /// <param name="removeOnComplete">애니메이션 완료 시 제거할지 여부 (기본값 true)</param>
        public static void BeginAnimation(
            UIElement target,
            DependencyProperty property,
            double from = 0,
            double to = 1,
            double duration = 0.3,
            EasingMode easingMode = EasingMode.EaseInOut,
            IEasingFunction easingFunction = null,
            bool removeOnComplete = false) // 완료 시 제거 옵션 추가
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(duration)
            };

            if (easingFunction != null)
            {
                // 주의: 전달된 easingFunction 인스턴스가 공유될 경우 EasingMode 변경이
                // 다른 곳에 영향을 줄 수 있습니다. 필요시 복제(Clone)하거나 새 인스턴스를 만드세요.
                if (easingFunction is EasingFunctionBase easingFunc)
                {
                    easingFunc.EasingMode = easingMode;
                }
                animation.EasingFunction = easingFunction;
            }

            // --- 애니메이션 완료 시 제거 로직 추가 ---
            if (removeOnComplete)
            {
                // Completed 이벤트 핸들러 정의 (람다식 사용)
                EventHandler animationCompletedHandler = null;
                animationCompletedHandler = (sender, e) =>
                {
                    target.BeginAnimation(property, null); // 애니메이션 제거
                };

                // 애니메이션에 Completed 이벤트 핸들러 연결
                animation.Completed += animationCompletedHandler;
            }
            // --- 완료 시 제거 로직 끝 ---


            // 애니메이션 시작
            target.BeginAnimation(property, animation);
        }
    }
}