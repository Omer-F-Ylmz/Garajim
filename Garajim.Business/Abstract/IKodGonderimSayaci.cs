namespace Garajim.Business.Abstract
{
    public interface IKodGonderimSayaci
    {
        bool IzinVer(string email);
        void Say(string email);
    }
}
