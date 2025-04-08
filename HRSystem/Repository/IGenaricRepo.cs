using System.Linq.Expressions;

namespace HRSystem.Repository
{
    public interface IGenaricRepo <T> where T : class
    {
        Task<List<T>> GetAll();
        Task<T> GetById<T2>(T2 id);
        Task ADD(T obj);
        void Delete(int id);
        void Update(T obj);
        IEnumerable<T> Filter(Expression<Func<T, bool>> expression);
        T Find(Expression<Func<T, bool>> expression);
        Task<bool> AnyAsync(Expression<Func<T, bool>> expression);
    }
}
