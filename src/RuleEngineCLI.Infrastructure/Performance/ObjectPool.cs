using System.Collections.Concurrent;

namespace RuleEngineCLI.Infrastructure.Performance;

/// <summary>
/// Pool de objetos reutilizables para reducir GC pressure (Phase 4).
/// Implementación thread-safe usando ConcurrentBag.
/// 
/// Beneficios:
/// - Reduce allocations en heap
/// - Minimiza Gen0/Gen1 collections
/// - Mejora throughput en escenarios de alta carga
/// </summary>
/// <typeparam name="T">Tipo de objeto a poolear</typeparam>
public sealed class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _objects = new();
    private readonly Func<T> _objectFactory;
    private readonly Action<T>? _resetAction;
    private readonly int _maxSize;
    private int _currentSize;

    /// <summary>
    /// Crea un nuevo ObjectPool.
    /// </summary>
    /// <param name="objectFactory">Función para crear nuevas instancias</param>
    /// <param name="resetAction">Acción opcional para resetear objetos antes de devolverlos al pool</param>
    /// <param name="maxSize">Tamaño máximo del pool (default: 100)</param>
    public ObjectPool(Func<T> objectFactory, Action<T>? resetAction = null, int maxSize = 100)
    {
        _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
        _resetAction = resetAction;
        _maxSize = maxSize;
        _currentSize = 0;
    }

    /// <summary>
    /// Obtiene un objeto del pool o crea uno nuevo si el pool está vacío.
    /// </summary>
    public T Rent()
    {
        if (_objects.TryTake(out var obj))
        {
            Interlocked.Decrement(ref _currentSize);
            return obj;
        }

        return _objectFactory();
    }

    /// <summary>
    /// Devuelve un objeto al pool para reutilización futura.
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null)
            return;

        // Resetear el objeto si se proporcionó una acción de reset
        _resetAction?.Invoke(obj);

        // Solo agregar al pool si no excedemos el tamaño máximo
        if (_currentSize < _maxSize)
        {
            _objects.Add(obj);
            Interlocked.Increment(ref _currentSize);
        }
        // Si excedemos el límite, dejamos que el GC se encargue del objeto
    }

    /// <summary>
    /// Obtiene el tamaño actual del pool.
    /// </summary>
    public int Count => _currentSize;

    /// <summary>
    /// Limpia el pool, descartando todos los objetos.
    /// </summary>
    public void Clear()
    {
        while (_objects.TryTake(out _))
        {
            Interlocked.Decrement(ref _currentSize);
        }
    }
}

/// <summary>
/// Wrapper RAII para usar objetos del pool con disposición automática.
/// Asegura que el objeto se devuelva al pool incluso si hay excepciones.
/// 
/// Uso:
/// using var pooledObj = pool.RentScoped();
/// // usar pooledObj.Value
/// // automáticamente se devuelve al pool al salir del scope
/// </summary>
public readonly struct PooledObject<T> : IDisposable where T : class
{
    private readonly ObjectPool<T> _pool;
    public T Value { get; }

    internal PooledObject(ObjectPool<T> pool, T value)
    {
        _pool = pool;
        Value = value;
    }

    public void Dispose()
    {
        _pool.Return(Value);
    }
}

/// <summary>
/// Extensiones para facilitar el uso del pool con RAII pattern.
/// </summary>
public static class ObjectPoolExtensions
{
    /// <summary>
    /// Renta un objeto del pool con disposición automática (RAII pattern).
    /// </summary>
    public static PooledObject<T> RentScoped<T>(this ObjectPool<T> pool) where T : class
    {
        var obj = pool.Rent();
        return new PooledObject<T>(pool, obj);
    }
}
