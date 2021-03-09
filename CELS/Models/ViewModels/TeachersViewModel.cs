using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class TeachersViewModel
    {
        private IQueryable<AppUser> teachers;

        public PagingInfo PagingInfo { get; set; }

        /*public IEnumerable<AppUser> TeachersListTop =>
            teachers.Skip((PagingInfo.CurrentPage - 1) * PagingInfo.ItemsPerPage)
            .Take((PagingInfo.ItemsPerPage) / 2);
        public IEnumerable<AppUser> TeachersListBottom =>
            teachers.Skip((PagingInfo.CurrentPage - 1) * PagingInfo.ItemsPerPage + (PagingInfo.ItemsPerPage) / 2)
            .Take((PagingInfo.ItemsPerPage) / 2);*/

        public IEnumerable<AppUser> TeachersList =>
            teachers.Skip((PagingInfo.CurrentPage - 1) * PagingInfo.ItemsPerPage)
            .Take(PagingInfo.ItemsPerPage);

        // sort by: rate, years of experience, times booked, name (default is rating)
        // default order: descending
        public string SortBy { get; set; }

        public bool Order { get; set; }

        public string SearchKey { get; set; } // used to filter the list

        public TeachersViewModel(IQueryable<AppUser> teachers)
        {
            this.teachers = teachers;
        }
    }
}
