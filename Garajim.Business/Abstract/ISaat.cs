namespace Garajim.Business.Abstract
{
    public interface ISaat
    {
        DateTime SimdiUtc { get; }

        DateTime YerelSimdi { get; }

        DateTime Bugun { get; }
    }
}
