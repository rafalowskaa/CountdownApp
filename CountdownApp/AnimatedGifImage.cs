using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace CountdownApp
{
    public class AnimatedGifImage : Image
    {
        public static readonly DependencyProperty GifSourceProperty = DependencyProperty.Register(
            nameof(GifSource),
            typeof(Uri),
            typeof(AnimatedGifImage),
            new PropertyMetadata(null, OnGifSourceChanged));

        private ObjectAnimationUsingKeyFrames? _animation;

        public Uri? GifSource
        {
            get => (Uri?)GetValue(GifSourceProperty);
            set => SetValue(GifSourceProperty, value);
        }

        private static void OnGifSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedGifImage gifImage)
            {
                gifImage.StartAnimation();
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            StartAnimation();
        }

        private void StartAnimation()
        {
            BeginAnimation(SourceProperty, null);
            _animation = null;

            if (GifSource == null)
            {
                Source = null;
                return;
            }

            StreamResourceInfo? resourceInfo = null;

            if (GifSource.IsAbsoluteUri)
            {
                resourceInfo = Application.GetResourceStream(GifSource);
            }
            else
            {
                var absoluteUri = new Uri($"pack://application:,,,/{GifSource.OriginalString.TrimStart('/')}");
                resourceInfo = Application.GetResourceStream(absoluteUri);
            }

            if (resourceInfo?.Stream == null)
            {
                return;
            }

            using Stream stream = resourceInfo.Stream;
            var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count == 0)
            {
                return;
            }

            Source = decoder.Frames[0];

            if (decoder.Frames.Count == 1)
            {
                return;
            }

            _animation = new ObjectAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            TimeSpan currentTime = TimeSpan.Zero;

            foreach (var frame in decoder.Frames)
            {
                _animation.KeyFrames.Add(new DiscreteObjectKeyFrame(frame, KeyTime.FromTimeSpan(currentTime)));
                currentTime += GetFrameDelay(frame);
            }

            if (currentTime == TimeSpan.Zero)
            {
                currentTime = TimeSpan.FromMilliseconds(100 * decoder.Frames.Count);
            }

            _animation.Duration = new Duration(currentTime);
            BeginAnimation(SourceProperty, _animation);
        }

        private static TimeSpan GetFrameDelay(BitmapFrame frame)
        {
            const string DelayQuery = "/grctlext/Delay";

            if (frame.Metadata is BitmapMetadata metadata && metadata.ContainsQuery(DelayQuery))
            {
                object? query = metadata.GetQuery(DelayQuery);
                if (query is ushort delay && delay > 0)
                {
                    return TimeSpan.FromMilliseconds(delay * 10);
                }
            }

            return TimeSpan.FromMilliseconds(100);
        }
    }
}