using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFPolupanov.Models
{
    class Series :INotifyPropertyChanged
    {
        public ulong series_id { get; set; }
        public string? title { get; set; }
        public string? series_info { get; set; }
        public DateTime? release_date { get; set; }
        public Series(ulong series_id, string? title, string? series_info, DateTime? release_date)
        {
            this.title = title;
            this.series_id = series_id;
            this.series_info = series_info;
            this.release_date = release_date;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
