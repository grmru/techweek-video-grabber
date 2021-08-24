using System.Collections.Generic;

namespace techweek_download
{
    public class DataRecord
    {
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public string YTUrl { get { return $"https://www.youtube.com/watch?v={this.YTCode}"; } }

        public string YTCode { get; set; } = string.Empty;
    }
}