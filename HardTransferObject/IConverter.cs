namespace HardTransferObject
{
    public interface IConverter<in TIn, out TOut>
    {
        TOut Convert(TIn @in);
    }
}