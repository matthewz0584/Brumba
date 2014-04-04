using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Dss.Diagnostics;

namespace Brumba.WaiterStupid
{
/*	public class FileLogHandler : IDiagnosticLogHandler, IDisposable
	{
		public delegate void FileLogHandlerFailure(object sender, FileLogHandlerFailureArgs e);

		Thread loggingThread = null;
		ConcurrentQueue<LogDetails> logQueue = new ConcurrentQueue<LogDetails>();
	
		public FileLogHandler(string logFile)
		{
			StartThread();
		}
	
		public event FileLogHandlerFailure FileLogHandlerFailureEvent;
	
		public void SetOrigin(LogDetails logDetails)
		{
			if (logDetails == null)
			{
				throw new ArgumentNullException();
			}
			Diagnostics.DefaultLogHandler.SetOrigin(logDetails);
		}
	
		public void Log(LogDetails logDetails)
		{
			if (logDetails == null)
			{
				throw new ArgumentNullException();
			}
			logQueue.Enqueue(logDetails);
		}
	
		protected virtual void Dispose(bool disposing)
		{
			StopAndFlush();
			Diagnostics.DefaultLogHandler.DetachHandler(this);
		}
	
		static string LogDetailsAsString(LogDetails logDetails)
		{
			var builder = new StringBuilder();
			DataContractSerializer serializer = new DataContractSerializer(typeof(LogDetails));
			using (XmlWriter writer = XmlWriter.Create(builder))
			{
				serializer.WriteObject(writer, logDetails);
				writer.Flush();
			}
			return builder != null ? builder.ToString() : null;
		}
	
		static List<string> FromLogDetails(LogDetails logDetails)
		{ // Converting from LogDetails to the list of strings
			return new List<string>();
		}
	
		void StartThread()
		{
			loggingThread = new Thread(new ThreadStart(this.WriteToFileThread));
			loggingThread.Start();
		}
	
		void WriteToFileThread()
		{
			while (this.isRunning)
			{
				LogDetails logDetails = null;
				if (!logQueue.TryPeek(out logDetails))
				{
					Thread.Sleep(0); // yielding because queue is empty
					continue;
				}
				Write(logDetails);
				if (this.LastError != null)
				{
					OnWriteFail(new FileLogHandlerFailureArgs(msg));
				}
				logQueue.TryDequeue(out logDetails);
			}
		}
	
		void Write(LogDetails logDetails)
		{
			lock (streamWriterLock)
			{
				LastError = null;
				var logList = FromLogDetails(logDetails);
				try
				{
					foreach (var line in logList)
						streamWriter.WriteLine(line);
				}
				catch (Exception ex)
				{
					LastError = ex.Message;
				}
			}
		}
	
		void StopAndFlush()
		{
			StopThread();
			LogDetails logDetails = null;
			while (logQueue.TryDequeue(out logDetails))
			{
				try
				{
					Write(logDetails);
				}
				catch
				{}
			}
		}
	
		void OnWriteFail(FileLogHandlerFailureArgs args)
		{
			FileLogHandlerFailureEvent(this, args);
		}
	
		void StopThread()
		{
			isRunning = false;
			loggingThread.Join();
		}
	}*/
}