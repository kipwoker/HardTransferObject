namespace HardTransferObject
{
    public class IdealConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            return @in;
        }
    }
}