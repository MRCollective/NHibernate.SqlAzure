using System.Collections.Generic;

namespace NHibernate.SqlAzure.Tests.Entities
{
    public class User
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<UserProperty> Properties { get; set; }
    }
}
