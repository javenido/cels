using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class CommentsViewModel
    {
        private IQueryable<Comment> comments;

        public PagingInfo PagingInfo { get; set; }

        public IEnumerable<Comment> CommentsList =>
            comments.Skip((PagingInfo.CurrentPage - 1) * PagingInfo.ItemsPerPage)
            .Take(PagingInfo.ItemsPerPage);

        public CommentsViewModel(IQueryable<Comment> comments)
        {
            this.comments = comments;
        }

        public string TeacherId { get; set; }
        public string TeacherName { get; set; }
    }
}
