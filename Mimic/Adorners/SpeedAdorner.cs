using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using zArm.Api;
using zArm.Api.Specialized;

namespace Mimic.Adorners
{
    class SpeedAdorner : Adorner
    {
        VisualCollection _visualChildren;
        Polyline _element;
        List<Thumb> _thumbs = new List<Thumb>();
        Point _size;
        double _easeingRange;
        double _widthBox = 10;
        double _widthLine = 20;
        int _speed = 100;
        int _easeIn = 0;
        int _easeOut = 0;
        bool _enableEasing;

        public event EventHandler<int> EaseInChanged;
        public event EventHandler<int> EaseOutChanged;
        public event EventHandler<int> SpeedChanged;
        public event EventHandler<bool> IsDragging;

        public SpeedAdorner(UIElement adornedElement, bool enableEasing = true, string thumbTemplateKey = "speedthumb")
            : base(adornedElement)
        {
            //set fields
            _enableEasing = enableEasing;
            _visualChildren = new VisualCollection(this);
            _element = adornedElement as Polyline;
            if (_element == null || _element.Points.Count != 5)
                return;

            //create thumbs
            if (_enableEasing)
                CreateThumb(1, thumbTemplateKey, Cursors.SizeWE, "Ease In");
            else
                _thumbs.Add(null);
            CreateThumb(2, thumbTemplateKey, Cursors.SizeNS, "Speed");
            if (_enableEasing)
                CreateThumb(3, thumbTemplateKey, Cursors.SizeWE, "Ease Out");
            else
                _thumbs.Add(null);

            //get max range
            _size = _element.Points[4];
            _easeingRange = (_size.X - _widthLine - _widthBox) / 2;

        }

        public int EaseIn
        {
            get { return _easeIn; }
            set
            {
                _easeIn = value.Clamp(0, 100);
                if (_enableEasing)
                {
                    SetPoint(_thumbs[0], ((double)_easeIn).Map(0, 100, 0, _easeingRange));
                    EaseInChanged?.Invoke(this, _easeIn);
                }
            }
        }

        public int Speed
        {
            get { return _speed; }
            set
            {
                _speed = value.Clamp(1, 100);
                SetPoint(_thumbs[1], ((double)_speed).Map(1, 100, _size.Y, 0));
                SpeedChanged?.Invoke(this, _speed);
            }
        }

        public int EaseOut
        {
            get { return _easeOut; }
            set
            {
                _easeOut = value.Clamp(0, 100);
                if (_enableEasing)
                {
                    SetPoint(_thumbs[2], ((double)_easeOut).Map(0, 100, _size.X, _size.X - _easeingRange));
                    EaseOutChanged?.Invoke(this, _easeOut);
                }
            }
        }

        void CreateThumb(int pointIndex, string templateKey, Cursor cursor, string toolTip)
        {
            var thumb = new Thumb();
            thumb.Tag = pointIndex;
            thumb.Cursor = cursor;
            thumb.ToolTip = toolTip;
            thumb.DragDelta += thumb_DragDelta;
            thumb.DragStarted += Thumb_DragStarted;
            thumb.DragCompleted += Thumb_DragCompleted;
            try
            {
                var template = _element.FindResource(templateKey) as ControlTemplate;
                if (template != null)
                    thumb.Template = template;
            }
            catch { }
            _visualChildren.Add(thumb);
            _thumbs.Add(thumb);
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsDragging?.Invoke(this, false);
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            IsDragging?.Invoke(this, true);
        }

        void SetPoint(Thumb thumb, double value)
        {
            var pointIndex = (int)thumb.Tag;
            if (pointIndex == 2)
            {
                for (int i = pointIndex - 1; i <= pointIndex + 1; i++)
                {
                    var point = _element.Points[i];
                    point.Y = value;
                    _element.Points[i] = point;
                }

            }
            else
            {
                var point = _element.Points[pointIndex];
                point.X = value;
                _element.Points[pointIndex] = point;
            }

            InvalidateArrange();

        }

        void thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var thumbIndex = (int)thumb.Tag;
            var point = _element.Points[thumbIndex];
            var newValue = (thumbIndex == 2) ? e.VerticalChange + point.Y : e.HorizontalChange + point.X;
            if (thumbIndex == 1)
                EaseIn = (int)Math.Round(newValue.Map(0, _easeingRange, 0, 100));
            else if (thumbIndex == 2)
                Speed = (int)Math.Round(newValue.Map(_size.Y, 0, 1, 100));
            else if (thumbIndex == 3)
                EaseOut = (int)Math.Round(newValue.Map(_size.X, _size.X - _easeingRange, 0, 100));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //position each thumb over the point
            foreach (var thumb in _thumbs.Where(i=>i!=null))
            {
                var pointIndex = (int)thumb.Tag;
                var point = _element.Points[pointIndex];
                if (pointIndex == 2)
                    thumb.Arrange(new Rect(point.X - _widthLine/2, point.Y, _widthLine, _widthBox));
                else if (pointIndex == 1)
                    thumb.Arrange(new Rect(point.X, point.Y, _widthBox, _widthBox));
                else if (pointIndex == 3)
                    thumb.Arrange(new Rect(point.X - _widthBox, point.Y, _widthBox, _widthBox));
            }
            return finalSize;
        }

        protected override Visual GetVisualChild(int index) { return _visualChildren[index]; }

        protected override int VisualChildrenCount { get { return _visualChildren.Count; } }
    }
}
