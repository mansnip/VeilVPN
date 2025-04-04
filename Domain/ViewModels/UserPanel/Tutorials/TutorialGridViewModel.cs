using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel.Tutorials
{
    public class TutorialGridViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string CoverImagePath { get; set; }
        public string? Category { get; set; }
    }
}
