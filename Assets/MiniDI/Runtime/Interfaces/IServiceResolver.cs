
namespace MiniDI
{
    public interface IServiceResolver
    {
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T value) where T : class;
    }
}



