using System.Collections.Generic;
namespace samane.Models
{
    public class ExportQuery
    {
        public ReviewQuery Query { get; set; } = new ReviewQuery();
        public int PageIndex { get; set; }
        public List<string> SelectedIds { get; set; }
        public string Cursor { get; set; }
    }
}
