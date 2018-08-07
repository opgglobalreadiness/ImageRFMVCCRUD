using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ImageRFMVCCRUD.Models
{
    public class TargetMarketCheckbox
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
    public class TartetMarketCheckboxes
    {
        public List<TargetMarketCheckbox> TargetMarket { get; set; }

    }

}