﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.Native;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class RegionSelector : Window
    {
        public Monitor Monitor { get; set; }

        private Action<Monitor, Rect> _selected;
        private Action<Monitor> _changed;
        private Action<Monitor> _gotHover;
        private Action _aborted;
        private double _scale = 1;


        public RegionSelector()
        {
            InitializeComponent();
        }


        public void Select(Monitor monitor, SelectControl.ModeType mode, Rect previousRegion, Action<Monitor, Rect> selected, Action<Monitor> changed, Action<Monitor> gotHover, Action aborted)
        {
            //Resize to fit given window.
            Left = monitor.Bounds.Left;
            Top = monitor.Bounds.Top;
            Width = monitor.Bounds.Width;
            Height = monitor.Bounds.Height;

            Monitor = monitor;

            _scale = monitor.Dpi / 96d;
            _selected = selected;
            _changed = changed;
            _gotHover = gotHover;
            _aborted = aborted;

            SelectControl.Scale = monitor.Scale;
            SelectControl.ParentLeft = Left;
            SelectControl.ParentTop = Top;
            SelectControl.BackImage = CaptureBackground();
            SelectControl.Mode = mode;

            if (mode == SelectControl.ModeType.Region)
            {
                //Since each region selector is attached to a single screen, the selection must be translated.
                SelectControl.Selected = previousRegion.Translate(monitor.Bounds.Left * -1, monitor.Bounds.Top * -1);
                SelectControl.Windows.Clear();
            }
            else if (mode == SelectControl.ModeType.Window)
            {
                //Get only the windows that are located inside the given screen.
                var win = Util.Native.EnumerateWindowsByMonitor(monitor);

                //Since each region selector is attached to a single screen, the list of positions must be translated.
                SelectControl.Windows = win.AdjustPosition(monitor.Bounds.Left, monitor.Bounds.Top);
            }
            else if (mode == SelectControl.ModeType.Fullscreen)
            {
                //Each selector is the whole screen.
                SelectControl.Windows = new List<DetectedRegion>
                {
                    new DetectedRegion(monitor.Handle, new Rect(new Size(monitor.Bounds.Width, monitor.Bounds.Height)), monitor.Name)
                };
            }
            
            //Call the selector to select the region.
            SelectControl.IsPickingRegion = true;
            Show();
        }

        public void ClearSelection()
        {
            SelectControl.Retry();
        }

        public void ClearHoverEffects()
        {
            SelectControl.HideZoom();
        }

        public void CancelSelection()
        {
            Close();
        }


        private double GetScreenDpi()
        {
            try
            {
                var source = Dispatcher?.Invoke(() => PresentationSource.FromVisual(this));

                if (source?.CompositionTarget != null)
                    return Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);
                else
                    return 1;
            }
            catch (Exception)
            {
                return 1;
            }
            finally
            {
                GC.Collect(1);
            }
        }

        private BitmapSource CaptureBackground()
        {
            //A 7 pixel offset is added to allow the crop by the magnifying glass.
            //var left = Math.Round((Left - 8d) * _scale, MidpointRounding.AwayFromZero);
            //var top = Math.Round((Top - 8d) * _scale, MidpointRounding.AwayFromZero);

            //return Util.Native.CaptureBitmapSource((int)Math.Round((Width + 16) * _scale), (int)Math.Round((Height + 16) * _scale),
            //    (int)left, (int)top);

            return Util.Native.CaptureBitmapSource((int)Math.Round((Width + 14 + 1) * _scale), (int)Math.Round((Height + 14 + 1) * _scale),
                (int)Math.Round((Left - 7) * _scale), (int)Math.Round((Top - 7) * _scale));
        }


        private void SelectControl_MouseHovering(object sender, RoutedEventArgs e)
        {
            _gotHover.Invoke(Monitor);
        }

        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs e)
        {
            SelectControl.IsPickingRegion = false;
            _selected.Invoke(Monitor, SelectControl.Selected.Translate(Monitor.Bounds.Left, Monitor.Bounds.Top)); //NonExpandedSelection
            Close();
        }

        private void SelectControl_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _changed.Invoke(Monitor);
        }

        private void SelectControl_SelectionCanceled(object sender, RoutedEventArgs e)
        {
            SelectControl.IsPickingRegion = false;
            _aborted.Invoke();
            Close();
        }
    }
}