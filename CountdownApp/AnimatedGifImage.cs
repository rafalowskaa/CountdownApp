using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
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

            foreach (var candidateUri in EnumerateCandidateUris(GifSource))
            {
                resourceInfo = TryGetResourceStream(candidateUri);

                if (resourceInfo?.Stream != null)
                {
                    break;
                }
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

        private static IEnumerable<Uri> EnumerateCandidateUris(Uri gifSource)
        {
            if (gifSource.IsAbsoluteUri)
            {
                yield return gifSource;

                if (string.Equals(gifSource.Scheme, "pack", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var uri in EnumeratePackCandidates(gifSource))
                    {
                        yield return uri;
                    }
                }
            }
            else
            {
                foreach (var uri in EnumerateRelativeCandidates(gifSource.OriginalString))
                {
                    yield return uri;
                }
            }
        }

        private static IEnumerable<Uri> EnumeratePackCandidates(Uri packUri)
        {
            var path = packUri.GetComponents(UriComponents.Path, UriFormat.Unescaped).TrimStart('/');

            foreach (var uri in EnumerateRelativeCandidates(path))
            {
                yield return uri;
            }
        }

        private static IEnumerable<Uri> EnumerateRelativeCandidates(string rawPath)
        {
            var relativePath = rawPath.TrimStart('/');
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

            yield return new Uri(relativePath, UriKind.Relative);
            yield return new Uri($"/{assemblyName};component/{relativePath}", UriKind.Relative);
            yield return new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
            yield return new Uri($"pack://application:,,,/{assemblyName};component/{relativePath}", UriKind.Absolute);
        }

        private static StreamResourceInfo? TryGetResourceStream(Uri uri)
        {
            try
            {
                return Application.GetResourceStream(uri);
            }
            catch (IOException)
            {
                return null;
            }
        }

    }
}