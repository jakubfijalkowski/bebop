namespace Core.Meta.Attributes
{
    public class CommandAttribute : BaseAttribute
    {
        public CommandAttribute()
        {
            Value = string.Empty;
        }

        public override bool TryValidate(out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}