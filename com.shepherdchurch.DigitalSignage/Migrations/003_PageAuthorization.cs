using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock;
using Rock.Model;
using Rock.Plugin;
using Rock.Security;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 3, "1.6.2" )]
    class PageAuthorization : ExtendedMigration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSecurityAuthForPage( SystemGuid.Page.DIGITAL_SIGN,
                0, Authorization.VIEW, true, string.Empty, SpecialRole.AllUsers.ConvertToInt(), Guid.NewGuid().ToString() );
        }

        public override void Down()
        {
        }
    }
}
