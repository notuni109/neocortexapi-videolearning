using Microsoft.Extensions.Logging;
using System.IO;

namespace AppLogger
{
	public class ApplicationLogger
	{
		private static ILoggerFactory _Factory = null;

		private static ILoggerFactory ConfigureLogger()
		{
			return LoggerFactory.Create(logBuilder =>
			{
				logBuilder.SetMinimumLevel(LogLevel.Debug);
				logBuilder.AddDebug();
                logBuilder.AddConsole();

				var path = Directory.GetCurrentDirectory();
				logBuilder.AddFile(Path.Combine(path, "logs", "Log.txt"));
			});
        }


		private static ILoggerFactory AppLoggerFactory
		{
			get
			{
				if (_Factory == null)
				{
					_Factory = ConfigureLogger();
				}
				return _Factory;
			}
			set { _Factory = value; }
		}
		public static ILogger CreateLogger(string categoryName = "VideoLearning") => AppLoggerFactory.CreateLogger(categoryName);
	}
}
