using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagsEditor
{
    public class DiffListItem
    {
        public string DiffName { get; set; }
        public string FilePath { get; set; }
        public bool IsEdited { get; set; }
        public Metadata OriginalMetadata { get; set; }
        public Metadata EditedMetadata { get; set; }

        public override string ToString() => DiffName;
    }
}
