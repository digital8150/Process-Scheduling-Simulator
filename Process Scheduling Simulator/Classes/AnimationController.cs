using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows;

namespace Process_Scheduling_Simulator.Classes
{
    class AnimationController
    {
        public static void BeginAnimation(
            UIElement target,
            DependencyProperty property,
            int from = 0,
            int to = 1,
            double duration = 0.3,
            EasingMode easingMode = EasingMode.EaseInOut,
            IEasingFunction easingFunction = null)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(duration)
            };

            // EasingFunction이 null이 아닐 경우 설정
            if (easingFunction != null)
            {
                if (easingFunction is EasingFunctionBase easingFunc)
                {
                    easingFunc.EasingMode = easingMode;
                }
                animation.EasingFunction = easingFunction;
            }

            target.BeginAnimation(property, animation);
        }

    }
}
