using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 2, "1.6.2" )]
    class AddPageAndBlock : ExtendedMigration
    {
        public override void Up()
        {
            //
            // Update the defined value attributes to show on grid.
            //
            Sql( "UPDATE [Attribute] SET [IsGridColumn] = 1 WHERE [Guid] IN ('BF344704-6228-4467-AE4C-42B5EDF6954F', 'B92A5ECF-E819-43C3-8949-EC9BD0F59AAC')" );

            RockMigrationHelper.AddBlockType( "Digital Sign Rotator",
                "Displays a full-screen interface for displaying images and videos in a rotator.",
                "~/Plugins/com_shepherdchurch/DigitalSignage/Digital Sign Rotator.ascx",
                "Shepherd Church > Digital Signage", SystemGuid.BlockType.DIGITAL_SIGN_ROTATOR );

            RockMigrationHelper.AddPage( "5B6DBC42-8B03-4D15-8D92-AAFA28FD8616", "2E169330-D7D7-4ECA-B417-72C64BE150F0",
                "Digital Sign", "Default page for displaying digital sign material on a kiosk device",
                SystemGuid.Page.DIGITAL_SIGN );

            Sql( "UPDATE [Page] SET [DisplayInNavWhen] = 2 WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.Page.DIGITAL_SIGN } } );

            RockMigrationHelper.AddBlock( SystemGuid.Page.DIGITAL_SIGN, string.Empty,
                SystemGuid.BlockType.DIGITAL_SIGN_ROTATOR, "Digital Sign Rotator",
                "Main", string.Empty, string.Empty, 0, SystemGuid.Block.DIGITAL_SIGN_DIGITAL_SIGN_ROTATOR );

            RockMigrationHelper.AddPageRoute( SystemGuid.Page.DIGITAL_SIGN, "digitalsign" );
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.DIGITAL_SIGN, "digitalsign/{deviceId}" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.DIGITAL_SIGN_DIGITAL_SIGN_ROTATOR );
            RockMigrationHelper.DeletePage( SystemGuid.Page.DIGITAL_SIGN );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.DIGITAL_SIGN_ROTATOR );
        }
    }
}
