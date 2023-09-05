using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModalLayer.Modal
{
    public class UserComments
    {
        public string TITLE { set; get; }
        public Guid? UserUid { set; get; }
        public string COMMENTS { set; get; }
        public Guid? COMMENTSUID { set; get; }
        public string EMAILID { set; get; }
        public string Company { set; get; }
        public string USERNAME { set; get; }
    }
}
