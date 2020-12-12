namespace Core.Meta.Attributes
{
    public class QueryAttribute : BaseAttribute
    {
        public QueryAttribute(string value)
        {
            Value = value;
        }

        public override bool TryValidate(out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}