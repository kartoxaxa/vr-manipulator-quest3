using System.Collections.Generic;

public static class MessageCatalog
{
    public static readonly Dictionary<string, MessageType> Types =
        new Dictionary<string, MessageType>()
        {
            // --- СИСТЕМНЫЕ СООБЩЕНИЯ ---
            { "Процесс обработки запущен", MessageType.Normal },
            { "Процесс обработки завершён", MessageType.Normal },

            // --- ОСТАНОВКИ ---
            { "Процесс остановлен", MessageType.Warning },              // нормальный STOP
            { "Аварийная остановка процесса", MessageType.Error },      // аварийный STOP
            { "Плазма отключена аварийно", MessageType.Error },         // аварийное отключение плазмы

            // --- ОШИБКИ ---
            { "Ошибка: камера не готова к запуску", MessageType.Error },
            { "Ошибка: выберите режим и установите деталь", MessageType.Warning },
            { "Ошибка: перегрев!", MessageType.Error },

            // --- ПРЕДУПРЕЖДЕНИЯ ---
            { "Внимание: давление приближается к пределу", MessageType.Warning },

            // --- МАТЕРИАЛЫ ---
            { "Выбрана деталь: Медь (Ar + H₂)", MessageType.Normal },
            { "Выбрана деталь: Алюминий (Ar)", MessageType.Normal },
            { "Выбрана деталь: Сталь (Ar + N₂)", MessageType.Normal },
            { "Выбрана деталь: Латунь (Ar)", MessageType.Normal },

            // Эти два — потенциально опасные смеси
            { "Выбрана деталь: Нержавейка (N₂ + H₂)", MessageType.Warning },
            { "Выбрана деталь: Титан (Ar)", MessageType.Warning },
        };
}
