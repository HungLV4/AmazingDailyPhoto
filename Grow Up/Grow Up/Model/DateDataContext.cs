using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grow_Up.Model
{
    public class DateDataContext : System.Data.Linq.DataContext
    {
        public DateDataContext(string connectionString) : base(connectionString) { }
        public Table<DateData> Dates;
        public Table<Entry> Entries;
    }
}
