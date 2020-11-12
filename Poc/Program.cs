using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Poc
{
    class Program
    {
        private static SingletonTransactionLockManager _SingletonTransactionLockManager = new SingletonTransactionLockManager();

        static void Main(string[] args)
        {
            Task[] tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                var next = new Random().Next();
                Console.WriteLine(next);
                tasks[i] = Task.Factory.StartNew(() => GoGo("984654", i));
            }
            Task.WaitAll(tasks);

            Console.ReadLine();
        }

        public static void GoGo(string refno, int i)
        {
            using (var transactionLock = new TransactionLockFactory(_SingletonTransactionLockManager).Create(refno))
            {
                if (transactionLock.Acquire())
                {
                    Console.WriteLine($"has acquire {Thread.CurrentThread.ManagedThreadId} {i}");
                    Console.WriteLine($"Id:{i} Start Deposit {refno}");
                    Console.WriteLine($"Current Thread = {Thread.CurrentThread.ManagedThreadId}");
                    Console.WriteLine($"Current Time Tick = {DateTime.Now.Ticks}");
                }
            }
        }
    }
}



internal class CustomData
{
    public int Id { get; set; }
    public long CreatedOn { get; set; }
    public int ThreadNum { get; set; }
}



public class TransactionLockFactory
{
    private readonly SingletonTransactionLockManager _manager;

    public TransactionLockFactory(SingletonTransactionLockManager manager)
    {
        _manager = manager;
    }

    public SingletonTransactionLockManager.ITransactionLock Create(string refNo)
    {
        return new SingletonTransactionLockManager.TransactionLock(_manager, refNo);
    }
}


public class SingletonTransactionLockManager
{
    public interface ITransactionLock : IDisposable
    {
        bool Acquire();
        void Release();
    }

    public class TransactionLock : ITransactionLock
    {
        private readonly SingletonTransactionLockManager _manager;
        private readonly string _refno;
        private bool _hasAcquired;

        public TransactionLock(SingletonTransactionLockManager manager, string refno)
        {
            _manager = manager;
            _refno = refno;
        }
        public bool Acquire()
        {
            _hasAcquired = _manager.Acquire(_refno);
            return _hasAcquired;
        }

        public void Dispose()
        {
            Release();
        }

        public void Release()
        {
            if (_hasAcquired)
            {
                Console.WriteLine($"release {Thread.CurrentThread.ManagedThreadId}");
                if (!_manager.Release(_refno))
                {
                    throw new Exception($"Very serious problem {_refno}");
                }
            }
        }
    }

    private static readonly Lazy<ConcurrentDictionary<string, Guid>> Dictionary =
    new Lazy<ConcurrentDictionary<string, Guid>>(
    () => new ConcurrentDictionary<string, Guid>());

    public static ConcurrentDictionary<string, Guid> LockDictionary => Dictionary.Value;

    private bool Acquire(string refNo)
    {
        return LockDictionary.TryAdd(refNo, Guid.Empty);
    }

    private bool Release(string refNo)
    {
        return LockDictionary.TryRemove(refNo, out _);
    }
}

