using HibernatingRhinos.Profiler.Appender.NHibernate;

namespace NHibernate5.SqlAzure.Tests
{
    public class NHibernateProfilerBootstrapper
    {
        public static void PreStart()
		{
			// Initialize the profiler
			NHibernateProfiler.Initialize();
			
			// You can also use the profiler in an offline manner.
			// This will generate a file with a snapshot of all the EntityFramework activity in the application,
			// which you can use for later analysis by loading the file into the profiler.
			// var filename = @"c:\profiler-log";
			// NHibernateProfiler.InitializeOfflineProfiling(filename);

			// You can use the following for production profiling.
			// NHibernateProfiler.InitializeForProduction(11234, "A strong password like: ze38r/b2ulve2HLQB8NK5AYig");
		}
    }
}