using System;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace MyPad.Views.Helpers
{
    public class IconSourceExtension : MarkupExtension
    {
        private string _source;
        public string Source
        {
            get => this._source;
            set => this._source = $"pack://application:,,,/{this.GetType().Assembly.GetName().Name};component/{(value.StartsWith("/") ? value[1..] : value)}";
        }

        public IconSourceExtension()
        {
        }

        public IconSourceExtension(string source)
        {
            this.Source = source;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return BitmapDecoder.Create(new Uri(this.Source), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand)
                .Frames.OrderBy(f => f.Width).FirstOrDefault();
        }
    }
}
