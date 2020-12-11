using Plow.Logging;
using System;
using System.Linq;

namespace MyPad
{
    public static class ILoggerFacadeExtension
    {
        public static void Log(this ILoggerFacade self, string message, Category category)
            => self.Log(message, category, Priority.None);

        public static void Log(this ILoggerFacade self, string message, Category category, Exception exception)
            => self.Log($"{message}{Environment.NewLine}{string.Join(Environment.NewLine, exception.ToString().Split(Environment.NewLine).Select(s => $"\t{s}"))}", category);
    }
}
