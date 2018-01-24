Imports HibernatingRhinos.Profiler.Appender.NHibernate

' If you're using .NET Core please remove this line and call NHibernateProfiler.Initialize() on the very beginning of your application.
<assembly: WebActivatorEx.PreApplicationStartMethod(GetType(Global.NHibernate5.SqlAzure.Tests.App_Start.NHibernateProfilerBootstrapper), "PreStart")>
Namespace App_Start
	Public Class NHibernateProfilerBootstrapper
		Public Shared Sub PreStart()
			' Initialize the profiler
			NHibernateProfiler.Initialize()

			' You can also use the profiler in an offline manner.
			' This will generate a file with a snapshot of all the NHibernate activity in the application,
			' which you can use for later analysis by loading the file into the profiler.
			' Dim FileName as String = @"c:\profiler-log";
			' NHibernateProfiler.InitializeOfflineProfiling(FileName)

			' You can use the following for production profiling.
			' NHibernateProfiler.InitializeForProduction(11234, "A strong password like: ze38r/b2ulve2HLQB8NK5AYig");
		End Sub
	End Class
End Namespace

