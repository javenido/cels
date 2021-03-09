using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models
{
    [DynamoDBTable("Comments")]
    public class Comment
    {
        [DynamoDBHashKey]
        public string CommentId { get; set; }

        [Required]
        public string TeacherId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime DateSubmitted { get; set; }

        public DateTime DateEdited { get; set; }

        [Required]
        public string Course { get; set; }

        /* THE FOLLOWING FIELDS/METHODS ARE USED IN VIEWS */
        private AppUser teacher, author;
        public AppUser GetTeacher() => teacher;
        public void SetTeacher(AppUser _teacher) => teacher = _teacher;
        public AppUser GetAuthor() => author;
        public void SetAuthor(AppUser student) => author = student;

        public static async Task<IEnumerable<Comment>> GetAllComments(AmazonDynamoDBClient client)
        {
            List<Comment> comments = new List<Comment>();
            Table commentsTable = Table.LoadTable(client, "Comments", DynamoDBEntryConversion.V2);
            Search search = commentsTable.Scan(new ScanFilter());
            List<Document> documentList = new List<Document>();
            do
            {
                documentList = await search.GetNextSetAsync();
                foreach (var document in documentList)
                {
                    comments.Add(DocumentToComment(document));
                }
            } while (!search.IsDone);
            return comments;
        }

        private static Comment DocumentToComment(Document doc)
        {
            JObject comment = JObject.Parse(doc.ToJson());
            return new Comment
            {
                CommentId = comment["CommentId"].ToString(),
                TeacherId = comment["TeacherId"].ToString(),
                StudentId = comment["StudentId"].ToString(),
                Content = comment["Content"].ToString(),
                DateSubmitted = DateTime.Parse(comment["DateSubmitted"].ToString()),
                DateEdited = DateTime.Parse(comment["DateEdited"].ToString()),
                Course = comment["Course"].ToString()
            };
        }
    }
}
