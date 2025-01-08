using Luval.AuthMate.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Core.Resolver
{
    public class ServiceUserResolver
    {
        public DateTime ConvertToUserDateTime(DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        public AppUser GetUser()
        {
            return new AppUser()
            {
                Id = 1,
                AccountId = 1,
                Email = GetUserEmail(),
                DisplayName = GetUserName(),
                Timezone = GetUserTimezone().Id,
                ProviderKey = Guid.NewGuid().ToString(),
                ProfilePictureUrl = "ServiceUser",
                ProviderType = "ServiceUser",
                Version = 1
            };
        }

        public string GetUserEmail()
        {
            return "oscar.marin.saenz@gmail.com";
        }

        public string GetUserName()
        {
            return "Oscar Marin";
        }

        public TimeZoneInfo GetUserTimezone()
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
    }
}
