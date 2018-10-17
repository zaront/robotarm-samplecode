using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interactivity;

namespace Mimic.Behaviors
{
    class AdornerBehavior : Behavior<UIElement>
    {
        Adorner _adorner;

        public static readonly DependencyProperty AdornerProperty = DependencyProperty.Register("Adorner", typeof(Type), typeof(AdornerBehavior), new PropertyMetadata(OnAdornerChanged));
        public static readonly DependencyProperty AdornerFactoryProperty = DependencyProperty.Register("AdornerFactory", typeof(IAdornerFactory), typeof(AdornerBehavior), new PropertyMetadata(null));

        static void OnAdornerChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var me = source as AdornerBehavior;
            me.Adorn(e.NewValue as Type);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            var window = Window.GetWindow(AssociatedObject);
            if (window == null)
                return;
            if (!window.IsLoaded)
                window.Loaded += Window_Loaded;
            else if (_adorner == null)
                Adorn(Adorner);
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_adorner == null)
                Adorn(Adorner);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Adorn(null);
        }

        void Adorn(Type type)
        {
            //validate
            if (AssociatedObject == null)
                return;

            var adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            if (adornerLayer == null)
                return;

            //remove old adorner
            if (_adorner != null)
            {
                adornerLayer.Remove(_adorner);
                _adorner = null;
            }

            //add new adorner
            var factory = AdornerFactory;
            if (type != null || factory != null)
            {
                if (factory != null)
                    _adorner = factory.CreateAdorner(AssociatedObject, type, AdornerParameter);
                else
                    _adorner = Activator.CreateInstance(type, AssociatedObject) as Adorner;
                if (_adorner != null)
                    adornerLayer.Add(_adorner);
            }
        }

        public Type Adorner
        {
            get { return GetValue(AdornerProperty) as Type; }
            set { SetValue(AdornerProperty, value); }
        }

        public IAdornerFactory AdornerFactory
        {
            get { return GetValue(AdornerFactoryProperty) as IAdornerFactory; }
            set { SetValue(AdornerFactoryProperty, value); }
        }

        public object AdornerParameter { get; set; }
    }




    interface IAdornerFactory
    {
        Adorner CreateAdorner(UIElement element, Type adorner, object adornerParameter);
    }
}
