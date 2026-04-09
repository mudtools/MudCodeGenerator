namespace ComObjectWrapTest;


internal class DisposableList : List<IDisposable>, IDisposable
{
    private bool _disposed = false;
    private readonly object _lockObject = new();


    /// <summary>
    /// 添加一个可释放对象到列表
    /// </summary>
    public new void Add(IDisposable item)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DisposableList));
        }

        lock (_lockObject)
        {
            base.Add(item);
        }
    }

    /// <summary>
    /// 添加多个可释放对象到列表
    /// </summary>
    public new void AddRange(IEnumerable<IDisposable> items)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DisposableList));

        if (items == null)
            throw new ArgumentNullException(nameof(items));

        lock (_lockObject)
        {
            base.AddRange(items);
        }
    }

    /// <summary>
    /// 尝试移除并释放指定的对象
    /// </summary>
    public bool RemoveAndDispose(IDisposable item)
    {
        if (_disposed)
            return false;

        lock (_lockObject)
        {
            var removed = base.Remove(item);
            if (removed)
            {
                SafeDispose(item);
            }
            return removed;
        }
    }

    /// <summary>
    /// 释放所有对象并清空列表
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            List<IDisposable> itemsToDispose;
            lock (_lockObject)
            {
                itemsToDispose = this.ToList();
                base.Clear();
            }

            List<Exception> exceptions = new List<Exception>();

            // 在锁外释放对象，避免死锁和性能问题
            foreach (var item in itemsToDispose)
            {
                try
                {
                    item?.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// 安全释放单个对象（不抛出异常）
    /// </summary>
    private void SafeDispose(IDisposable item)
    {
        try
        {
            item?.Dispose();
        }
        catch (Exception ex)
        {

        }
    }

    /// <summary>
    /// 获取是否已释放
    /// </summary>
    public bool IsDisposed => _disposed;

    ~DisposableList()
    {
        Dispose(false);
    }
}