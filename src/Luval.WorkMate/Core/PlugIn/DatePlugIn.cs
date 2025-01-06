using Luval.AuthMate.Core.Interfaces;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.PlugIn
{
    /// <summary>
    /// This class contains the functions to interact with the date and time in the system
    /// </summary>
    public class DatePlugIn
    {

        private readonly IUserResolver _userResolver;

        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="userResolver"></param>
        public DatePlugIn(IUserResolver userResolver)
        {
            _userResolver = userResolver;
        }

        [KernelFunction("get_date_time_now")]
        [Description("Get's the current date and time for now, in the timezone of the user")]
        public Task<string> GetDate()
        {
            var tz = _userResolver.GetUserTimezone();
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return Task.FromResult(now.ToString("yyyy-MM-dd hh:mm:ss"));
        }
    }
}
