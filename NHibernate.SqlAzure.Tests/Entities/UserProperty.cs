namespace NHibernate.SqlAzure.Tests.Entities
{
    public class UserProperty
    {
        public virtual User User { get; set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }

        #region Equals
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            var a = obj as UserProperty;
            if (a == null)
                return false;
            if (User.Id == a.User.Id && Name == a.Name)
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            return string.Format("{0}|{1}", User.Id, Name).GetHashCode();
        }
        #endregion
    }
}
