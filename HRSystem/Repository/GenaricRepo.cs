using HRSystem.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HRSystem.Repository
{
    public class GenaricRepo<T> : IGenaricRepo<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;
        public GenaricRepo(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<List<T>> GetAll()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetById<T2>(T2 id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task ADD(T obj)
        {
            await _dbSet.AddAsync(obj);
        }

        public async Task AddRange(IEnumerable<T> obj)
        {
            await _dbSet.AddRangeAsync(obj);
        }

        public void Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }

        public void Update(T obj)
        {
            _dbSet.Update(obj);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.AnyAsync(expression);
        }

        public IEnumerable<T> Filter(Expression<Func<T, bool>> expression)
        {
            return _dbSet.Where(expression);
        }

        public T Find(Expression<Func<T, bool>> expression)
        {
            return _dbSet.FirstOrDefault(expression);
        }
    }
}
