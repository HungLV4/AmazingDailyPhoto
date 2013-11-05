using Grow_Up.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;

namespace Grow_Up.TemplateSelector
{
    public class TemplateSelector : DataTemplateSelector
    {
        public DataTemplate HasCaption { get; set; }
        public DataTemplate NoCaption { get; set; }
        
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Entry entry = item as Entry;
            if (entry != null)
            {
                try
                {
                    if (!String.Empty.Equals(entry.Location) || !entry.Note.Equals(String.Empty))
                        return HasCaption;
                    else
                        return NoCaption;
                }
                catch (Exception) { }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
