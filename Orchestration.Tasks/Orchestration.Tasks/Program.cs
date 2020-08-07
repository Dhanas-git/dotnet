#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;

namespace Orchestration.Tasks
{
    public class Program
    {

		public static void Main(string[] args)
		{           

			var config = new ConfigurationBuilder()
				.AddCommandLine(args)
				.Build();

			var host = new WebHostBuilder()
				.UseKestrel(o => o.Limits.KeepAliveTimeout = Timeout.InfiniteTimeSpan)
				.UseConfiguration(config)
				.UseStartup<Startup>()
				.UseContentRoot(AppContext.BaseDirectory)
				.Build();

			host.Run();

		}
       

    }
}
