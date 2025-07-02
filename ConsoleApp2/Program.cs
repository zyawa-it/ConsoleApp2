using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class Server
{
    // Сам ресурс, который мы защищаем.
    private static int count = 0;

    // Объект блокировки.
    private static readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

    public static int GetCount()
    {
        // Входим в режим чтения. Несколько потоков могут войти сюда одновременно.
        cacheLock.EnterReadLock();
        try
        {
            // Имитируем небольшую задержку, чтобы лучше видеть параллельную работу
            Thread.Sleep(50); 
            return count;
        }
        finally
        {
            // Гарантированно освобождаем блокировку чтения.
            cacheLock.ExitReadLock();
        }
    }
    
    public static void AddToCount(int value)
    {
        // Входим в режим записи. Только один поток может получить эту блокировку.
        cacheLock.EnterWriteLock();
        try
        {
            // Имитируем "тяжелую" операцию записи
            Console.WriteLine($"--- Писатель входит в критическую секцию, чтобы добавить {value} ---");
            Thread.Sleep(250);
            count += value;
            Console.WriteLine($"--- Писатель успешно добавил {value}. Текущее значение: {count} ---");
        }
        finally
        {
            // Гарантированно освобождаем блокировку записи.
            cacheLock.ExitWriteLock();
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Запускаем симуляцию работы клиентов...");
        Console.WriteLine($"Начальное значение счетчика: {Server.GetCount()}\n");

        // Создаем список для всех задач
        var tasks = new List<Task>();

        // Запускаем 10 "читателей"
        for (int i = 0; i < 10; i++)
        {
            int readerId = i;
            tasks.Add(Task.Run(() =>
            {
                int value = Server.GetCount();
                Console.WriteLine($"Читатель #{readerId} прочитал значение: {value}");
            }));
        }

        // Запускаем 2 "писателей"
        tasks.Add(Task.Run(() => Server.AddToCount(10)));
        tasks.Add(Task.Run(() => Server.AddToCount(5)));

        // Запускаем еще 5 "читателей", которые придут уже после писателей
        for (int i = 10; i < 15; i++)
        {
            int readerId = i;
            tasks.Add(Task.Run(() =>
            {
                // Эти читатели будут ждать, пока писатели закончат свою работу
                int value = Server.GetCount();
                Console.WriteLine($"Читатель #{readerId} прочитал значение: {value}");
            }));
        }

        // Ждем, пока все задачи завершатся
        await Task.WhenAll(tasks);

        Console.WriteLine($"\nСимуляция завершена.");
        Console.WriteLine($"Финальное значение счетчика: {Server.GetCount()}");
    }
}
